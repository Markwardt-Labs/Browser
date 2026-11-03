namespace Markwardt.Browser.Tabs;

/// <summary>
/// Cancels the requests a page makes that are ads or trackers. Navigations of the page itself are never touched, so a blocked site never leaves a blank error page behind.
/// </summary>
/// <param name="blocker">Decides which requests are ads.</param>
internal sealed class BrowserRequests(IAdBlocker blocker) : RequestHandler
{
    /// <inheritdoc />
    protected override CefResourceRequestHandler? GetResourceRequestHandler(CefBrowser browser, CefFrame frame, CefRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
    {
        try
        {
            CefResourceType type = request.ResourceType;

            if (isDownload || type is CefResourceType.MainFrame or CefResourceType.NavigationPreloadMainFrame)
            {
                return null;
            }

            string page = browser.GetMainFrame()?.Url ?? requestInitiator ?? string.Empty;
            return blocker.ShouldBlock(request.Url ?? string.Empty, page, Map(type)) ? new BlockedRequest() : null;
        }
        catch (Exception exception) when (exception is InvalidOperationException or NullReferenceException or ObjectDisposedException)
        {
            return null;
        }
    }

    private AdResourceType Map(CefResourceType type)
        => type switch
        {
            CefResourceType.SubFrame or CefResourceType.NavigationPreloadSubFrame => AdResourceType.SubDocument,
            CefResourceType.Stylesheet => AdResourceType.Stylesheet,
            CefResourceType.Script => AdResourceType.Script,
            CefResourceType.Image or CefResourceType.Favicon => AdResourceType.Image,
            CefResourceType.FontResource => AdResourceType.Font,
            CefResourceType.Media => AdResourceType.Media,
            CefResourceType.Object or CefResourceType.PluginResource => AdResourceType.Object,
            CefResourceType.Xhr => AdResourceType.XmlHttpRequest,
            CefResourceType.Ping or CefResourceType.CspReport => AdResourceType.Ping,
            _ => AdResourceType.Other,
        };

    private sealed class BlockedRequest : CefResourceRequestHandler
    {
        protected override CefCookieAccessFilter? GetCookieAccessFilter(CefBrowser browser, CefFrame frame, CefRequest request)
            => null;

        protected override CefReturnValue OnBeforeResourceLoad(CefBrowser browser, CefFrame frame, CefRequest request, CefCallback callback)
            => CefReturnValue.Cancel;
    }
}
