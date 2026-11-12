namespace Markwardt.Browser.Tabs;

/// <summary>
/// Builds the script that puts a reloaded page back where the user left it.
/// </summary>
internal interface IPageRestore
{
    /// <summary>
    /// Builds the script for a page's last report.
    /// </summary>
    /// <param name="report">What the page last said about itself before it was suspended.</param>
    /// <returns>The script, or null when there is nothing worth restoring.</returns>
    string? Script(PageReport report);
}

/// <inheritdoc />
internal sealed class PageRestore : IPageRestore
{
    private readonly string template = typeof(PageRestore).Assembly.ReadText("Markwardt.Browser.Tabs.PageRestore.js");

    /// <inheritdoc />
    public string? Script(PageReport report)
    {
        if (!report.IsTop || (report.ScrollX == 0 && report.ScrollY == 0 && report.Media is null))
        {
            return null;
        }

        string scroll = $"[{Number(report.ScrollX)},{Number(report.ScrollY)}]";
        string video = report.Media is { } media ? $"{{t:{Number(media.Time)},d:{Number(media.Duration)}}}" : "null";
        return template.Replace("__STATE__", $"{{scroll:{scroll},video:{video}}}", StringComparison.Ordinal);
    }

    private string Number(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
