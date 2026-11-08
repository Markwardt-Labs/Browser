namespace Markwardt.Browser.Engine;

/// <summary>
/// Runs this executable as one of CEF's helper processes (renderer, GPU, utility). The renderer one injects scripts into pages as their JavaScript context is created, before any script of the page runs, which a script sent from the browser process can never promise.
/// </summary>
internal interface IHelperProcess
{
    /// <summary>
    /// Runs the helper process until it is told to stop, or until the browser process that started it is gone.
    /// </summary>
    /// <param name="args">The command line arguments, which include <c>--type=</c>.</param>
    /// <returns>The exit code.</returns>
    int Run(IReadOnlyList<string> args);
}

/// <inheritdoc />
/// <param name="youTube">The script to run in YouTube's frames.</param>
internal sealed class HelperProcess(IYouTubeAdScript youTube) : IHelperProcess
{
    /// <inheritdoc />
    public int Run(IReadOnlyList<string> args)
    {
        if (args.FirstOrDefault(argument => argument.StartsWith("--parent-pid=", StringComparison.Ordinal)) is { } parent && int.TryParse(parent["--parent-pid=".Length..], out int id))
        {
            _ = Task.Run(() => Watch(id));
        }

        CefRuntime.Load();
        int code = CefRuntime.ExecuteProcess(new CefMainArgs(["BrowserProcess", .. args]), new RendererApp(new RendererScripts(youTube)), IntPtr.Zero);
        return code == -1 ? 0 : code;
    }

    private void Watch(int id)
    {
        try
        {
            using Process browser = Process.GetProcessById(id);
            browser.WaitForExit();
        }
        catch (ArgumentException)
        {
        }

        Environment.Exit(0);
    }
}
