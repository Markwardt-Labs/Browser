namespace Markwardt.Browser;

/// <summary>
/// The Avalonia application; creates the main window once the framework is ready, and a further window for every later launch and every new window the user asks for. Windows share bookmarks, settings and the profile, and keep their own tabs and their own launch options.
/// </summary>
/// <param name="instance">Reports the command lines that later launches hand over.</param>
/// <param name="profile">The folder that holds what outlives a run.</param>
/// <param name="settings">The remembered choices.</param>
/// <param name="launchParser">Reads handed over command lines.</param>
/// <param name="launch">What this launch's command line asked for, for the first window.</param>
/// <param name="adBlocker">Keeps ads out of every page.</param>
/// <param name="requestStats">Counts what the ad blocker decided, when diagnostics are on.</param>
/// <param name="graphics">The graphics environment variables that were set at startup, for the diagnostics header.</param>
internal sealed partial class App(ISingleInstance instance, IProfileFolder profile, IUserSettings settings, ILaunchParser launchParser, LaunchOptions launch, IAdBlocker adBlocker, IRequestStats requestStats, IReadOnlyList<KeyValuePair<string, string>> graphics) : Application
{
    private readonly IAddressResolver addressResolver = new AddressResolver();
    private readonly IShortcutMap shortcutMap = new ShortcutMap();
    private readonly ISiteRules siteRules = new SiteRules([new VideoModeRule(), new YouTubeRule(), new PageStateRule()]);
    private readonly IBookmarkList bookmarks = new BookmarkList(new BookmarkFile(Path.Combine(profile.Location, "bookmarks.json")));
    private readonly List<MainWindow> windows = [];
    private readonly IFrameStats frameStats = new FrameStats(launch.Stats, action => Dispatcher.UIThread.Post(action), action => (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.RequestAnimationFrame(_ => action()), () => Stopwatch.GetElapsedTime(0));
    private readonly IVideoStats videoStats = new VideoStats(launch.Stats, () => Stopwatch.GetElapsedTime(0));
    private readonly IPointerLocator pointerLocator = new X11PointerLocator();
    private readonly IPointerPlacement pointerPlacement = new PointerPlacement();
    private IShutdownGuard? shutdownGuard;
    private IClassicDesktopStyleApplicationLifetime? lifetime;
    private PixelPoint? lastPointer;
    private long lastPointerMove = Stopwatch.GetTimestamp();

    /// <inheritdoc />
    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _ = bookmarks.Load();
            _ = adBlocker.Refresh();
            lifetime = desktop;
            shutdownGuard = new ShutdownGuard(() => windows.Count > 0, () => RequestShutdown(desktop), ForceExit, (action, delay) => DispatcherTimer.RunOnce(action, delay), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(8));
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            MainWindow first = CreateWindow(launch);
            desktop.MainWindow = first;
            RecordStats(first);
            instance.Listen(args => Dispatcher.UIThread.Post(() => OpenLaunched(launchParser.Parse(args))));
            KeepCookies(desktop, new CookieStore());
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void RecordStats(MainWindow first)
    {
        if (!launch.Stats)
        {
            return;
        }

        IStatsRecorder recorder = new StatsRecorder(frameStats, new ProcessCpuSampler("/proc", Environment.ProcessId), new UiProbe(), Path.Combine(profile.Location, "stats"), DescribeSurroundings, requestStats, videoStats);

        first.Opened += async (_, _) =>
        {
            List<string> header =
            [
                $"started {DateTime.Now:O}",
                $"os: {RuntimeInformation.OSDescription}, {RuntimeInformation.ProcessArchitecture}, {Environment.ProcessorCount} logical cores",
                $"runtime: {RuntimeInformation.FrameworkDescription}, avalonia {typeof(Application).Assembly.GetName().Version}",
                $"session: type={Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")} desktop={Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")} display={Environment.GetEnvironmentVariable("DISPLAY")} wayland={Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")}",
                $"command line: {string.Join(' ', Environment.GetCommandLineArgs().Skip(1))}",
                $"graphics environment set at startup: {(graphics.Count == 0 ? "nothing" : string.Join(' ', graphics.Select(pair => $"{pair.Key}={pair.Value}")))} (swap sync off lets Avalonia's one render thread serve several windows without waiting on the screen refresh in turn)",
                $"graphics backend asked of chromium: {Environment.GetEnvironmentVariable("MARKWARDT_ANGLE") ?? "its own default"}",
                $"libcef: {Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Where(module => module.ModuleName.StartsWith("libcef", StringComparison.Ordinal)).Select(module => $"{module.FileName}, {new FileInfo(module.FileName).Length / 1_000_000} MB").FirstOrDefault() ?? "not found"} (538 MB is the build with H.264 and AAC, 277 MB the stock one)",
                .. await new RenderingInfo().Describe(first),
            ];

            _ = recorder.Start(header);
        };
    }

    private void RequestShutdown(IClassicDesktopStyleApplicationLifetime desktop)
    {
        NoteLifetime($"no window left after the grace period, shutting down: avalonia windows={desktop.Windows.Count}");
        desktop.TryShutdown();
    }

    private void ForceExit()
    {
        NoteLifetime("still running after the shutdown request, ending the process");

        // Environment.Exit runs CEF's shutdown, which can wait on browsers forever, so a second path ends the process outright if it does.
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(4));
            Process.GetCurrentProcess().Kill();
        });

        Environment.Exit(0);
    }

    private void NoteLifetime(string message)
    {
        if (!launch.Stats)
        {
            return;
        }

        try
        {
            string folder = Path.Combine(profile.Location, "stats");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "lifetime.log"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private async Task<StatsContext> DescribeSurroundings()
        => await Dispatcher.UIThread.InvokeAsync(() =>
        {
            MainWindow? active = windows.FirstOrDefault(window => window.IsActive);
            MainWindow? target = active ?? windows.FirstOrDefault();

            if (target is null)
            {
                return new StatsContext(string.Empty, false, "unknown", -1, 0);
            }

            PixelRect area = new(target.Position, new PixelSize((int)(target.Bounds.Width * target.RenderScaling), (int)(target.Bounds.Height * target.RenderScaling)));
            PixelPoint? position = pointerLocator.Locate();

            if (position != lastPointer)
            {
                lastPointer = position;
                lastPointerMove = Stopwatch.GetTimestamp();
            }

            string pointer = pointerPlacement.Classify(position, area, [.. target.Screens.All.Select(screen => screen.Bounds)], out int screen);
            return new StatsContext(target.ActiveHost, active is not null, pointer, screen, (int)Stopwatch.GetElapsedTime(lastPointerMove).TotalSeconds);
        });

    private MainWindow CreateWindow(LaunchOptions options)
    {
        IBrowserTabs tabs = new BrowserTabs(new IBrowserTab.Factory(shortcutMap, siteRules, adBlocker, frameStats, videoStats), options.Isolate);
        tabs.Open(addressResolver.Resolve(options.Goto ?? options.Address ?? string.Empty));
        MainWindow window = new(tabs, addressResolver, shortcutMap, bookmarks, settings, options, !options.Isolate && options.Goto is null, OpenNewWindow);

        if (options.Window is { } state)
        {
            window.WindowState = state;

            if (state == WindowState.Minimized)
            {
                window.Opened += (_, _) =>
                {
                    window.WindowState = WindowState.Normal;
                    window.WindowState = state;
                };
            }
        }

        windows.Add(window);
        NoteLifetime($"window opened: app windows={windows.Count} avalonia windows={lifetime?.Windows.Count}");
        window.Closed += (_, _) =>
        {
            windows.Remove(window);
            window.CloseTabs();
            NoteLifetime($"window closed: app windows={windows.Count} avalonia windows={lifetime?.Windows.Count}");

            if (windows.Count == 0)
            {
                shutdownGuard?.LastWindowClosed();
            }
        };

        return window;
    }

    private void OpenLaunched(LaunchOptions options)
    {
        MainWindow window = CreateWindow(options);
        window.Show();

        if (options.Window != WindowState.Minimized)
        {
            window.Activate();
        }
    }

    private void OpenNewWindow()
        => CreateWindow(new LaunchOptions()).Show();

    private void KeepCookies(IClassicDesktopStyleApplicationLifetime desktop, ICookieStore cookieStore)
    {
        DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(15) };
        timer.Tick += (_, _) => cookieStore.Flush(() => { });
        timer.Start();

        bool flushed = false;
        desktop.ShutdownRequested += (_, e) =>
        {
            if (flushed)
            {
                return;
            }

            flushed = true;
            e.Cancel = true;
            cookieStore.Flush(() => Dispatcher.UIThread.Post(() => desktop.Shutdown()));
            DispatcherTimer.RunOnce(() => desktop.Shutdown(), TimeSpan.FromSeconds(2));
        };
    }
}
