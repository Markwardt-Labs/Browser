namespace Markwardt.Browser;

/// <summary>
/// The Avalonia application; opens the first tab and creates the main window once the framework is ready, and any further windows the user asks for. Windows share bookmarks and the profile, and keep their own tabs.
/// </summary>
/// <param name="instance">Receives the addresses that later launches hand over.</param>
/// <param name="profile">The folder that holds what outlives a run.</param>
internal sealed partial class App(ISingleInstance instance, IProfileFolder profile) : Application
{
    private readonly IAddressResolver addressResolver = new AddressResolver();
    private readonly IShortcutMap shortcutMap = new ShortcutMap();
    private readonly ISiteRules siteRules = new SiteRules([new VideoModeRule(), new YouTubeRule()]);
    private readonly IBookmarkList bookmarks = new BookmarkList(new BookmarkFile(Path.Combine(profile.Location, "bookmarks.json")));
    private readonly List<MainWindow> windows = [];
    private MainWindow? current;

    /// <inheritdoc />
    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _ = bookmarks.Load();
            MainWindow first = CreateWindow(addressResolver.Resolve(desktop.Args?.FirstOrDefault(argument => !argument.StartsWith('-')) ?? string.Empty));
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            desktop.MainWindow = first;
            instance.Listen(address => Dispatcher.UIThread.Post(() => (current ?? first).Receive(address)));
            KeepCookies(desktop, new CookieStore());
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MainWindow CreateWindow(string address)
    {
        IBrowserTabs tabs = new BrowserTabs(new IBrowserTab.Factory(shortcutMap, siteRules));
        tabs.Open(address);
        MainWindow window = new(tabs, addressResolver, shortcutMap, bookmarks, OpenNewWindow);
        windows.Add(window);
        window.Activated += (_, _) => current = window;
        window.Closed += (_, _) =>
        {
            windows.Remove(window);

            if (ReferenceEquals(current, window))
            {
                current = windows.FirstOrDefault();
            }

            if (windows.Count > 0)
            {
                window.CloseTabs();
            }
        };

        return window;
    }

    private void OpenNewWindow()
    {
        MainWindow window = CreateWindow("about:blank");
        window.Show();
    }

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
