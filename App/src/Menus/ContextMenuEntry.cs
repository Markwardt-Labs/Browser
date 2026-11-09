namespace Markwardt.Browser.Menus;

/// <summary>
/// A snapshot of one entry of a CEF context menu, safe to use after CEF's own menu model is gone.
/// </summary>
internal sealed record ContextMenuEntry
{
    /// <summary>
    /// Copies the visible entries of a CEF menu model, dropping separators that would sit first, last or twice in a row.
    /// </summary>
    /// <param name="model">The model to read.</param>
    /// <returns>The entries in display order.</returns>
    public static IReadOnlyList<ContextMenuEntry> Read(CefMenuModel model)
    {
        List<ContextMenuEntry> entries = [];

        for (nuint index = 0; index < model.Count; index++)
        {
            if (!model.IsVisibleAt(index))
            {
                continue;
            }

            CefMenuItemType type = model.GetItemTypeAt(index);
            ContextMenuEntryKind kind = type switch
            {
                CefMenuItemType.Separator => ContextMenuEntryKind.Separator,
                CefMenuItemType.SubMenu => ContextMenuEntryKind.Submenu,
                CefMenuItemType.Check or CefMenuItemType.Radio => ContextMenuEntryKind.Check,
                _ => ContextMenuEntryKind.Command,
            };

            entries.Add(new ContextMenuEntry
            {
                Kind = kind,
                Id = model.GetCommandIdAt(index),
                Label = (model.GetLabelAt(index) ?? string.Empty).Replace("&", string.Empty, StringComparison.Ordinal),
                IsEnabled = model.IsEnabledAt(index),
                IsChecked = model.IsCheckedAt(index),
                Children = kind == ContextMenuEntryKind.Submenu && model.GetSubMenuAt(index) is { } submenu ? Read(submenu) : [],
            });
        }

        return Tidy(entries);
    }

    private static List<ContextMenuEntry> Tidy(List<ContextMenuEntry> entries)
    {
        List<ContextMenuEntry> tidy = [];

        foreach (ContextMenuEntry entry in entries)
        {
            bool isSeparator = entry.Kind == ContextMenuEntryKind.Separator;

            if (!isSeparator || (tidy.Count > 0 && tidy[^1].Kind != ContextMenuEntryKind.Separator))
            {
                tidy.Add(entry);
            }
        }

        if (tidy.Count > 0 && tidy[^1].Kind == ContextMenuEntryKind.Separator)
        {
            tidy.RemoveAt(tidy.Count - 1);
        }

        return tidy;
    }

    /// <summary>
    /// Gets what kind of entry this is.
    /// </summary>
    public required ContextMenuEntryKind Kind { get; init; }

    /// <summary>
    /// Gets the command id to hand back to CEF when the entry is chosen.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Gets the text shown for the entry.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the entry can be chosen.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a <see cref="ContextMenuEntryKind.Check"/> entry is ticked.
    /// </summary>
    public bool IsChecked { get; init; }

    /// <summary>
    /// Gets the entries of the nested menu of a <see cref="ContextMenuEntryKind.Submenu"/> entry.
    /// </summary>
    public IReadOnlyList<ContextMenuEntry> Children { get; init; } = [];
}
