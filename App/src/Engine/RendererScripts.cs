namespace Markwardt.Browser.Engine;

/// <summary>
/// Runs scripts in a page's JavaScript context as soon as it exists.
/// </summary>
/// <param name="youTube">The script for YouTube's frames.</param>
internal sealed class RendererScripts(IYouTubeAdScript youTube) : CefRenderProcessHandler
{
    /// <inheritdoc />
    protected override void OnContextCreated(CefBrowser browser, CefFrame frame, CefV8Context context)
    {
        string address = frame.Url ?? string.Empty;

        if (youTube.For(address) is { } script)
        {
            context.TryEval(script, address, 0, out _, out _);
        }
    }
}
