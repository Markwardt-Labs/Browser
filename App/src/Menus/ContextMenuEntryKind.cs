namespace Markwardt.Browser.Menus;

/// <summary>
/// The kinds of entry a page context menu can hold.
/// </summary>
internal enum ContextMenuEntryKind
{
    /// <summary>
    /// A plain command.
    /// </summary>
    Command,

    /// <summary>
    /// A command that can be ticked.
    /// </summary>
    Check,

    /// <summary>
    /// A line between groups of commands.
    /// </summary>
    Separator,

    /// <summary>
    /// A command that opens a nested menu.
    /// </summary>
    Submenu,
}
