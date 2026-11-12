namespace Markwardt.Browser.Tabs;

/// <summary>
/// Reads the navigation entries of a CEF browser into a list.
/// </summary>
/// <param name="done">Called, on a CEF thread, with every entry and the index of the current one.</param>
internal sealed class BrowserHistory(Action<IReadOnlyList<HistoryEntry>, int> done) : CefNavigationEntryVisitor
{
    private readonly List<HistoryEntry> entries = [];
    private int current = -1;

    /// <inheritdoc />
    protected override bool Visit(CefNavigationEntry entry, bool current, int index, int total)
    {
        if (entry.IsValid)
        {
            if (current)
            {
                this.current = entries.Count;
            }

            entries.Add(new HistoryEntry(entry.Url ?? string.Empty, entry.Title ?? string.Empty));
        }

        if (index >= total - 1)
        {
            done(entries, this.current);
        }

        return true;
    }
}
