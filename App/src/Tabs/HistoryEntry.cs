namespace Markwardt.Browser.Tabs;

/// <summary>
/// One page in a tab's back and forward history.
/// </summary>
/// <param name="Url">The address of the page.</param>
/// <param name="Title">The title the page had.</param>
internal sealed record HistoryEntry(string Url, string Title);
