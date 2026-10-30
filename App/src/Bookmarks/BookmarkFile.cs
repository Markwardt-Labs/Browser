namespace Markwardt.Browser.Bookmarks;

/// <summary>
/// Where the bookmarks are kept between runs.
/// </summary>
internal interface IBookmarkStorage
{
    /// <summary>
    /// Reads the saved bookmarks.
    /// </summary>
    /// <param name="cancellation">Cancels the read.</param>
    /// <returns>The bookmarks in saved order; none when nothing usable has been saved.</returns>
    Task<IReadOnlyList<Bookmark>> Load(CancellationToken cancellation = default);

    /// <summary>
    /// Replaces the saved bookmarks.
    /// </summary>
    /// <param name="bookmarks">The bookmarks to save, in order.</param>
    /// <param name="cancellation">Cancels the write.</param>
    /// <returns>A task that completes when the bookmarks are saved.</returns>
    Task Save(IReadOnlyList<Bookmark> bookmarks, CancellationToken cancellation = default);
}

/// <summary>
/// Keeps the bookmarks in a JSON file.
/// </summary>
/// <param name="path">The path of the file.</param>
internal sealed class BookmarkFile(string path) : IBookmarkStorage
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Bookmark>> Load(CancellationToken cancellation = default)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            await using FileStream stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<List<Bookmark>>(stream, cancellationToken: cancellation) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public async Task Save(IReadOnlyList<Bookmark> bookmarks, CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = $"{path}.tmp";

        await using (FileStream stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, bookmarks, new JsonSerializerOptions { WriteIndented = true }, cancellation);
        }

        File.Move(temporary, path, true);
    }
}
