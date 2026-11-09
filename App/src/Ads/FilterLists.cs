namespace Markwardt.Browser.Ads;

/// <summary>
/// The filter lists the blocker works from: a small built-in one and the downloaded ones, which are kept on disk so that they work without a connection.
/// </summary>
internal interface IFilterLists
{
    /// <summary>
    /// Reads the built-in list and every downloaded list that is on disk.
    /// </summary>
    /// <param name="cancellation">Cancels the read.</param>
    /// <returns>The text of each list, the built-in one first.</returns>
    Task<IReadOnlyList<string>> Read(CancellationToken cancellation = default);

    /// <summary>
    /// Downloads the lists that are missing or have not been updated for a few days. A list that cannot be downloaded is left as it was.
    /// </summary>
    /// <param name="cancellation">Cancels the update.</param>
    /// <returns>True if at least one list changed on disk.</returns>
    Task<bool> Refresh(CancellationToken cancellation = default);
}

/// <inheritdoc />
/// <param name="folder">The folder the downloaded lists are kept in.</param>
/// <param name="sources">The lists to download.</param>
/// <param name="downloader">Fetches the lists.</param>
/// <param name="assembly">The assembly the built-in list is embedded in.</param>
internal sealed class FilterLists(string folder, IReadOnlyList<FilterListSource> sources, IFilterDownloader downloader, Assembly assembly) : IFilterLists
{
    private readonly TimeSpan maxAge = TimeSpan.FromDays(3);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> Read(CancellationToken cancellation = default)
    {
        List<string> lists = [];

        using (Stream? stream = assembly.GetManifestResourceStream("Markwardt.Browser.Ads.BuiltIn.txt"))
        {
            if (stream is not null)
            {
                using StreamReader reader = new(stream);
                lists.Add(await reader.ReadToEndAsync(cancellation));
            }
        }

        foreach (FilterListSource source in sources)
        {
            string path = PathOf(source);

            if (File.Exists(path))
            {
                lists.Add(await File.ReadAllTextAsync(path, cancellation));
            }
        }

        return lists;
    }

    /// <inheritdoc />
    public async Task<bool> Refresh(CancellationToken cancellation = default)
    {
        bool changed = false;

        foreach (FilterListSource source in sources)
        {
            string path = PathOf(source);

            if (File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < maxAge)
            {
                continue;
            }

            try
            {
                string text = await downloader.Download(source.Url, cancellation);

                if (!text.TrimStart().StartsWith('[') && !text.TrimStart().StartsWith('!'))
                {
                    continue;
                }

                Directory.CreateDirectory(folder);
                string temporary = $"{path}.{Guid.NewGuid():N}.tmp";
                await File.WriteAllTextAsync(temporary, text, cancellation);
                File.Move(temporary, path, true);
                changed = true;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException && !cancellation.IsCancellationRequested)
            {
            }
        }

        return changed;
    }

    private string PathOf(FilterListSource source)
        => Path.Combine(folder, $"{source.Name}.txt");
}
