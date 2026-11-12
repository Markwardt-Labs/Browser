namespace Markwardt.Browser.Tabs;

/// <summary>
/// How a request to go back or forward is to be carried out.
/// </summary>
internal enum HistoryMove
{
    /// <summary>
    /// There is nowhere to go.
    /// </summary>
    None,

    /// <summary>
    /// The browser's own history has the page.
    /// </summary>
    Native,

    /// <summary>
    /// The page is in the history kept from before the tab was suspended and has to be loaded in place.
    /// </summary>
    Restored,
}
