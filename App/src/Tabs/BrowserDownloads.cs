namespace Markwardt.Browser.Tabs;

/// <summary>
/// Saves a page's downloads into a folder without asking, and says when each one starts and ends. Without a handler CEF drops downloads, so none would arrive at all.
/// </summary>
/// <param name="folder">The folder the files go to.</param>
/// <param name="changed">Called, on a CEF thread, with a download's identifier and whether it is running.</param>
internal sealed class BrowserDownloads(string folder, Action<uint, bool> changed) : DownloadHandler
{
    /// <summary>
    /// Picks the path a download is saved to: the suggested name in the folder, with a number added before the extension if a file of that name exists.
    /// </summary>
    /// <param name="suggestedName">The name the server suggested.</param>
    /// <returns>A path of a file that does not exist yet.</returns>
    public string Choose(string? suggestedName)
    {
        string name = string.Concat(Path.GetFileName(suggestedName ?? string.Empty).Where(letter => !Path.GetInvalidFileNameChars().Contains(letter))).Trim();
        name = name.Length == 0 || name is "." or ".." ? "download" : name;
        string path = Path.Combine(folder, name);

        for (int number = 1; File.Exists(path); number++)
        {
            path = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(name)} ({number}){Path.GetExtension(name)}");
        }

        return path;
    }

    /// <inheritdoc />
    protected override bool OnBeforeDownload(CefBrowser browser, CefDownloadItem downloadItem, string suggestedName, CefBeforeDownloadCallback callback)
    {
        Directory.CreateDirectory(folder);
        callback.Continue(Choose(suggestedName), false);
        return true;
    }

    /// <inheritdoc />
    protected override void OnDownloadUpdated(CefBrowser browser, CefDownloadItem downloadItem, CefDownloadItemCallback callback)
        => changed(downloadItem.Id, downloadItem.IsInProgress);
}
