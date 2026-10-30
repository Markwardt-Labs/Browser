namespace Markwardt.Browser.Tabs;

/// <summary>
/// Turns every page request to open a new window into a callback. Only the user's own Ctrl+click, which asks for a background tab, is reported as such; every other request (a link with a target, a script's window.open) is reported as a plain navigation so that tabs are only ever created on purpose.
/// </summary>
/// <param name="requested">Called with the address the page wants to open, and whether it should open in a new background tab rather than the current one.</param>
internal sealed class BrowserPopups(Action<string, bool> requested) : LifeSpanHandler
{
    /// <inheritdoc />
    protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, int popupId, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
    {
        if (!string.IsNullOrEmpty(targetUrl))
        {
            requested(targetUrl, targetDisposition == CefWindowOpenDisposition.NewBackgroundTab);
        }

        return true;
    }
}
