namespace Markwardt.Browser.Tabs;

/// <summary>
/// Decides what a page's request to open a new window turns into. A script popup (window.open with features, as sign-in dialogs use) opens as a real native window, because the page behind it talks to its opener and breaks when the popup is loaded into the tab instead. Every other request (a link with a target, a plain window.open) is reported as a navigation so that tabs are only ever created on purpose; only the user's own Ctrl+click, which asks for a background tab, is reported as such.
/// </summary>
/// <param name="requested">Called with the address the page wants to open, and whether it should open in a new background tab rather than the current one.</param>
internal sealed class BrowserPopups(Action<string, bool> requested) : LifeSpanHandler
{
    /// <inheritdoc />
    protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, int popupId, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
    {
        if (targetDisposition == CefWindowOpenDisposition.NewPopup)
        {
            windowInfo.WindowlessRenderingEnabled = false;
            return false;
        }

        if (!string.IsNullOrEmpty(targetUrl))
        {
            requested(targetUrl, targetDisposition == CefWindowOpenDisposition.NewBackgroundTab);
        }

        return true;
    }
}
