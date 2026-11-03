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
        ICefRuntimeFolder runtime = new CefRuntimeFolder(AppContext.BaseDirectory, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Markwardt.Browser.Runtime"), new CefRuntimeArchive(typeof(Program).Assembly));

        if (args.Any(argument => argument.StartsWith("--type=", StringComparison.Ordinal)))
        {
            runtime.Prepare();
            runtime.Activate();
        }

        CefSubProcess.Run(args, true);

        ILaunchParser launchParser = new LaunchParser();
        ISingleInstance instance = new SingleInstance($"Markwardt.Browser.{Environment.UserName}");

        if (!instance.Claim(args).GetAwaiter().GetResult())
        {
            return 0;
        }

        runtime.Prepare();
        runtime.Activate();
        LaunchOptions launch = launchParser.Parse(args);
        IProfileFolder profile = new ProfileFolder();
        IUserSettings settings = new UserSettings(new SettingsFile(Path.Combine(profile.Location, "settings.json")));
        settings.Load().GetAwaiter().GetResult();
        ICefStarter cef = new CefStarter();

        AppDomain.CurrentDomain.ProcessExit += (_, _) => CefRuntime.Shutdown();

        return AppBuilder.Configure(() => new App(instance, profile, settings, launchParser, launch))
            .UsePlatformDetect()
            .AfterPlatformServicesSetup(_ => cef.Start(
                new CefSettings
                {
                    ResourcesDirPath = runtime.Location,
                    LocalesDirPath = Path.Combine(runtime.Location, "locales"),
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
