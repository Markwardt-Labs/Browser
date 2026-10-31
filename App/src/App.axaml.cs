namespace Markwardt.Browser;

/// <summary>
/// The Avalonia application; creates the main window once the framework is ready, and a further window for every later launch and every new window the user asks for. Windows share bookmarks, settings and the profile, and keep their own tabs and their own launch options.
/// </summary>
/// <param name="instance">Reports the command lines that later launches hand over.</param>
/// <param name="profile">The folder that holds what outlives a run.</param>
/// <param name="settings">The remembered choices.</param>
/// <param name="launchParser">Reads handed over command lines.</param>
/// <param name="launch">What this launch's command line asked for, for the first window.</param>
internal sealed partial class App(ISingleInstance instance, IProfileFolder profile, IUserSettings settings, ILaunchParser launchParser, LaunchOptions launch) : Application
{
    private readonly IAddressResolver addressResolver = new AddressResolver();
    private readonly IShortcutMap shortcutMap = new ShortcutMap();
    private readonly ISiteRules siteRules = new SiteRules([new VideoModeRule(), new YouTubeRule()]);
    private readonly IBookmarkList bookmarks = new BookmarkList(new BookmarkFile(Path.Combine(profile.Location, "bookmarks.json")));
    private readonly List<MainWindow> windows = [];

    /// <inheritdoc />
    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _ = bookmarks.Load();
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            desktop.MainWindow = CreateWindow(launch);
            instance.Listen(args => Dispatcher.UIThread.Post(() => OpenLaunched(launchParser.Parse(args))));
            KeepCookies(desktop, new CookieStore());
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MainWindow CreateWindow(LaunchOptions options)
    {
        IBrowserTabs tabs = new BrowserTabs(new IBrowserTab.Factory(shortcutMap, siteRules), options.Isolate);
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
        window.Closed += (_, _) =>
        {
            windows.Remove(window);

            if (windows.Count > 0)
            {
                window.CloseTabs();
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
