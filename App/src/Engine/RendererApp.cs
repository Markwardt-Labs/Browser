namespace Markwardt.Browser.Engine;

/// <summary>
/// The CEF application of the renderer process.
/// </summary>
/// <param name="handler">Receives the renderer process's events.</param>
internal sealed class RendererApp(CefRenderProcessHandler handler) : CefApp
{
    /// <inheritdoc />
    protected override CefRenderProcessHandler GetRenderProcessHandler()
        => handler;
}
