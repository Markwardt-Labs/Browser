namespace Markwardt.Browser.Engine;

/// <summary>
/// Starts Chromium for the whole process.
/// </summary>
internal interface ICefStarter
{
    /// <summary>
    /// Initializes CEF in off-screen mode. Must run before the first browser control is created, which otherwise loads CEF itself.
    /// </summary>
    /// <param name="settings">The CEF settings; windowless rendering is switched on.</param>
    /// <param name="switches">The Chromium command line switches to add, as name and value (empty for none).</param>
    void Start(CefSettings settings, IReadOnlyList<KeyValuePair<string, string>> switches);
}

/// <inheritdoc />
internal sealed class CefStarter : ICefStarter
{
    /// <inheritdoc />
    public void Start(CefSettings settings, IReadOnlyList<KeyValuePair<string, string>> switches)
    {
        CefRuntime.Load();
        settings.UncaughtExceptionStackSize = 100;
        settings.WindowlessRenderingEnabled = true;
        settings.MultiThreadedMessageLoop = true;
        settings.NoSandbox = true;

        // CefGlue's control chooses off-screen mode from this internal flag, which only its own loader sets.
        typeof(CefRuntimeLoader).GetProperty("IsOSREnabled", BindingFlags.Static | BindingFlags.NonPublic)!.GetSetMethod(true)!.Invoke(null, [true]);

        CefRuntime.Initialize(new CefMainArgs([Path.GetFileName(Environment.ProcessPath) ?? "Markwardt.Browser"]), settings, new BrowserApp(switches), IntPtr.Zero);
    }
}
