namespace Markwardt.Browser.Tabs;

/// <summary>
/// Hands the page's tooltip text to a callback instead of letting the CEF control show it, and holds it back around a context menu: the page gets no mouse events while the menu is open, so a tooltip it was about to show appears afterwards, wherever the pointer then is. Empty tooltips, which clear one, are never held back.
/// </summary>
/// <param name="show">Called on a CEF thread with the tooltip text, empty when the tooltip should go away.</param>
internal sealed class BrowserTooltips(Action<string> show) : DisplayHandler
{
    private long suppressedUntil;

    /// <summary>
    /// Hides tooltips until <see cref="Release"/> is called.
    /// </summary>
    public void Hold()
        => Interlocked.Exchange(ref suppressedUntil, DateTime.MaxValue.Ticks);

    /// <summary>
    /// Allows tooltips again after a short delay, long enough for one that was already pending to be dropped.
    /// </summary>
    public void Release()
        => Interlocked.Exchange(ref suppressedUntil, DateTime.UtcNow.AddMilliseconds(800).Ticks);

    /// <inheritdoc />
    protected override bool OnTooltip(CefBrowser browser, string text)
    {
        text ??= string.Empty;

        if (text.Length == 0 || DateTime.UtcNow.Ticks >= Interlocked.Read(ref suppressedUntil))
        {
            show(text);
        }

        return true;
    }
}
