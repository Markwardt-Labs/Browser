namespace Markwardt.Browser.Tabs;

/// <summary>
/// Takes over the questions a page blocks on (alert, confirm, prompt, and the "leave this page" check before navigating away, going back or reloading). CEF otherwise opens its own native window for them, which can land behind the browser, leaving a page that is waiting on a window nobody can see and so looks frozen.
/// </summary>
/// <param name="requested">Called, on a CEF thread, with a question the page waits on. It must be answered, or the page stays blocked.</param>
/// <param name="closed">Called, on a CEF thread, when the question no longer applies because the page navigated away or closed.</param>
internal sealed class BrowserDialogs(Action<IPageDialog> requested, Action closed) : JSDialogHandler
{
    /// <inheritdoc />
    protected override bool OnJSDialog(CefBrowser browser, string originUrl, CefJSDialogType dialogType, string messageText, string defaultPromptText, CefJSDialogCallback callback, out bool suppressMessage)
    {
        suppressMessage = false;

        PageDialogKind kind = dialogType switch
        {
            CefJSDialogType.Confirm => PageDialogKind.Confirm,
            CefJSDialogType.Prompt => PageDialogKind.Prompt,
            _ => PageDialogKind.Alert,
        };

        requested(new PageDialog(kind, Describe(originUrl), messageText ?? string.Empty, defaultPromptText ?? string.Empty, (accepted, text) => Complete(callback, accepted, text)));
        return true;
    }

    /// <inheritdoc />
    protected override bool OnBeforeUnloadDialog(CefBrowser browser, string messageText, bool isReload, CefJSDialogCallback callback)
    {
        string message = isReload ? "Reload this page? Changes you made may not be saved." : "Leave this page? Changes you made may not be saved.";
        requested(new PageDialog(PageDialogKind.LeavePage, string.Empty, message, string.Empty, (accepted, text) => Complete(callback, accepted, text)));
        return true;
    }

    /// <inheritdoc />
    protected override void OnResetDialogState(CefBrowser browser)
        => closed();

    /// <inheritdoc />
    protected override void OnDialogClosed(CefBrowser browser)
        => closed();

    private string Describe(string? originUrl)
        => Uri.TryCreate(originUrl, UriKind.Absolute, out Uri? uri) && uri.Host.Length > 0 ? uri.Host : originUrl ?? string.Empty;

    private void Complete(CefJSDialogCallback callback, bool accepted, string text)
    {
        try
        {
            callback.Continue(accepted, text);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ObjectDisposedException)
        {
        }
    }
}
