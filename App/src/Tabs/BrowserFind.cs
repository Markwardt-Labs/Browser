namespace Markwardt.Browser.Tabs;

/// <summary>
/// Passes on the results CEF reports for a find in page.
/// </summary>
/// <param name="reported">Called, on a CEF thread, with the number of matches and the position of the active one (1 based, 0 for none).</param>
internal sealed class BrowserFind(Action<int, int> reported) : FindHandler
{
    /// <inheritdoc />
    protected override void OnFindResult(CefBrowser browser, int identifier, int count, CefRectangle selectionRect, int activeMatchOrdinal, bool finalUpdate)
        => reported(count, activeMatchOrdinal);
}
