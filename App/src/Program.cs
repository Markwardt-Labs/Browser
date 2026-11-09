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
        INativeEnvironment environment = new NativeEnvironment();
        IReadOnlyList<KeyValuePair<string, string>> graphics = new GraphicsEnvironment(OperatingSystem.IsLinux(), environment.Get, environment.Set).DisableSwapSync();
        ICefRuntimeFolder runtime = new CefRuntimeFolder(AppContext.BaseDirectory, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Markwardt.Browser.Runtime"), new CefRuntimeArchive(typeof(Program).Assembly));

        IYouTubeAdScript youTube = new YouTubeAdScript();

        if (args.Any(argument => argument.StartsWith("--type=", StringComparison.Ordinal)))
        {
            runtime.Prepare();
            runtime.Activate();
            Environment.Exit(new HelperProcess(youTube).Run(args));
        }

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
        IRequestStats requestStats = new RequestStats(launch.Stats);
        IAdBlocker adBlocker = new AdBlocker(new FilterParser(), new FilterLists(Path.Combine(profile.Location, "filters"), [new FilterListSource { Name = "easylist", Url = "https://easylist.to/easylist/easylist.txt" }, new FilterListSource { Name = "ublock-filters", Url = "https://ublockorigin.github.io/uAssets/filters/filters.txt" }, new FilterListSource { Name = "ublock-unbreak", Url = "https://ublockorigin.github.io/uAssets/filters/unbreak.txt" }, new FilterListSource { Name = "peter-lowe", Url = "https://pgl.yoyo.org/adservers/serverlist.php?hostformat=adblockplus&showintro=0&mimetype=plaintext" }, new FilterListSource { Name = "ublock-badware", Url = "https://ublockorigin.github.io/uAssets/filters/badware.txt" }, new FilterListSource { Name = "adult-adservers", Url = "https://raw.githubusercontent.com/easylist/easylist/master/easylist_adult/adult_adservers.txt" }, new FilterListSource { Name = "adult-adservers-popup", Url = "https://raw.githubusercontent.com/easylist/easylist/master/easylist_adult/adult_adservers_popup.txt" }, new FilterListSource { Name = "adult-thirdparty", Url = "https://raw.githubusercontent.com/easylist/easylist/master/easylist_adult/adult_thirdparty.txt" }, new FilterListSource { Name = "adult-thirdparty-popup", Url = "https://raw.githubusercontent.com/easylist/easylist/master/easylist_adult/adult_thirdparty_popup.txt" }, new FilterListSource { Name = "adult-specific-block", Url = "https://raw.githubusercontent.com/easylist/easylist/master/easylist_adult/adult_specific_block.txt" }, new FilterListSource { Name = "adult-specific-hide", Url = "https://raw.githubusercontent.com/easylist/easylist/master/easylist_adult/adult_specific_hide.txt" }], new HttpFilterDownloader(), typeof(Program).Assembly), youTube, requestStats);
        adBlocker.Load().GetAwaiter().GetResult();
        ICefStarter cef = new CefStarter();

        AppDomain.CurrentDomain.ProcessExit += (_, _) => CefRuntime.Shutdown();

        return AppBuilder.Configure(() => new App(instance, profile, settings, launchParser, launch, adBlocker, requestStats, graphics))
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
                new ChromiumSwitches().Build(Environment.GetEnvironmentVariable("MARKWARDT_ANGLE"))))
            .StartWithClassicDesktopLifetime(args);
    }
}
