namespace Markwardt.Browser.Shortcuts;

/// <summary>
/// The actions a keyboard shortcut can trigger.
/// </summary>
internal enum ShortcutCommand
{
    /// <summary>
    /// Open a blank tab.
    /// </summary>
    NewTab,

    /// <summary>
    /// Close the active tab.
    /// </summary>
    CloseTab,

    /// <summary>
    /// Show the overview with the address box focused.
    /// </summary>
    FocusAddress,

    /// <summary>
    /// Show or hide the overview of tabs.
    /// </summary>
    ToggleOverview,

    /// <summary>
    /// Reload the active tab.
    /// </summary>
    Reload,

    /// <summary>
    /// Go back in the active tab.
    /// </summary>
    Back,

    /// <summary>
    /// Go forward in the active tab.
    /// </summary>
    Forward,

    /// <summary>
    /// Make the page's main video fill the page view, or put it back.
    /// </summary>
    ToggleVideoMode,

    /// <summary>
    /// Search the active page for text.
    /// </summary>
    Find,
}
