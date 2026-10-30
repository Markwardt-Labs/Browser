namespace Markwardt.Browser.Engine;

/// <summary>
/// The CEF application object. It replaces CefGlue's own, which in off-screen mode switches the GPU off for Chromium with no way to opt out.
/// </summary>
/// <param name="switches">The Chromium command line switches to add, as name and value (empty for none).</param>
internal sealed class BrowserApp(IReadOnlyList<KeyValuePair<string, string>> switches) : CefApp
{
    private readonly ChildProcessHandler processHandler = new();

    /// <inheritdoc />
    protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
    {
        if (!string.IsNullOrEmpty(processType))
        {
            return;
        }

        if (CefRuntime.Platform == CefRuntimePlatform.Linux)
        {
            commandLine.AppendSwitch("no-zygote");
        }

        commandLine.AppendSwitch("enable-begin-frame-scheduling");

        foreach (KeyValuePair<string, string> pair in switches)
        {
            commandLine.AppendSwitch(pair.Key, pair.Value);
        }
    }

    /// <inheritdoc />
    protected override CefBrowserProcessHandler GetBrowserProcessHandler()
        => processHandler;
}
