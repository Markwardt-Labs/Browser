namespace Markwardt.Browser;

/// <summary>
/// Process entry point. The same executable also serves as the CEF helper process (renderer, GPU, etc.).
/// </summary>
internal static class Program
{
    /// <summary>
    /// Runs the CEF helper process if this invocation is one, otherwise starts the desktop application.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The process exit code.</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        CefSubProcess.Run(args, true);

        ISingleInstance instance = new SingleInstance($"Markwardt.Browser.{Environment.UserName}");

        if (!instance.Claim(args.FirstOrDefault(argument => !argument.StartsWith('-')) ?? string.Empty).GetAwaiter().GetResult())
        {
            return 0;
        }

        IProfileFolder profile = new ProfileFolder();
        ICefStarter cef = new CefStarter();

        AppDomain.CurrentDomain.ProcessExit += (_, _) => CefRuntime.Shutdown();

        return AppBuilder.Configure(() => new App(instance, profile))
            .UsePlatformDetect()
            .AfterPlatformServicesSetup(_ => cef.Start(
                new CefSettings
                {
                    RootCachePath = profile.Location,
                    CachePath = Path.Combine(profile.Location, "Profile"),
                    PersistSessionCookies = true,
                    BackgroundColor = new CefColor(255, 30, 30, 30),
                },
                [
                    new KeyValuePair<string, string>("force-dark-mode", string.Empty),
                    new KeyValuePair<string, string>("use-angle", "vulkan"),
                    new KeyValuePair<string, string>("enable-features", "Vulkan,WebContentsForceDark"),
                ]))
            .StartWithClassicDesktopLifetime(args);
    }
}
