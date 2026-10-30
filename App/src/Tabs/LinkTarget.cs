namespace Markwardt.Browser.Tabs;

/// <summary>
/// Where a link opens.
/// </summary>
internal enum LinkTarget
{
    /// <summary>
    /// In the tab the link is on.
    /// </summary>
    Current,

    /// <summary>
    /// In a new tab that is shown right away.
    /// </summary>
    NewTab,

    /// <summary>
    /// In a new tab that loads without being shown.
    /// </summary>
    BackgroundTab,
}
