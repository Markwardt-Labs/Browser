namespace Markwardt.Browser.Engine;

/// <summary>
/// Tells every helper process which process started it, so a helper can exit when this one dies instead of lingering.
/// </summary>
internal sealed class ChildProcessHandler : CefBrowserProcessHandler
{
    private readonly string processId = Environment.ProcessId.ToString();

    /// <inheritdoc />
    protected override void OnBeforeChildProcessLaunch(CefCommandLine commandLine)
        => commandLine.AppendSwitch("--parent-pid", processId);
}
