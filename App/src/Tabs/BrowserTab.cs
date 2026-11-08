namespace Markwardt.Browser.Tabs;

/// <summary>
/// One browser page: the CEF view plus the state the overview shows for it.
/// </summary>
internal interface IBrowserTab : INotifyPropertyChanged
{
    /// <summary>
    /// Raised with an address that the page or its context menu wants opened in a new tab, and whether that tab should stay in the background.
    /// </summary>
    event Action<string, bool>? NewTabRequested;

    /// <summary>
    /// Raised when a keyboard shortcut is pressed while the page has focus.
    /// </summary>
    event Action<ShortcutCommand>? ShortcutPressed;

    /// <summary>
    /// Raised with the number of matches of the current find and the position of the active one (0 for none).
    /// </summary>
    event Action<int, int>? FindResultChanged;

    /// <summary>
    /// Gets the title of the loaded page.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the address currently loaded.
    /// </summary>
    string Address { get; }

    /// <summary>
    /// Gets a value indicating whether there is a page to go back to.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets a value indicating whether there is a page to go forward to.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Gets a picture of the page as it last looked, or null before it has been shown.
    /// </summary>
    IImage? Thumbnail { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the tab being shown.
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// Gets the control that renders the page.
    /// </summary>
    Control View { get; }

    /// <summary>
    /// Loads an address.
    /// </summary>
    /// <param name="address">The URL to load.</param>
    void Navigate(string address);

    /// <summary>
    /// Goes back one page in history.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Goes forward one page in history.
    /// </summary>
    void GoForward();

    /// <summary>
    /// Reloads the page.
    /// </summary>
    void Reload();

    /// <summary>
    /// Searches the page and highlights the matches.
    /// </summary>
    /// <param name="text">The text to look for.</param>
    /// <param name="forward">Whether to look for the next match rather than the previous one.</param>
    /// <param name="matchCase">Whether letter case must match.</param>
    /// <param name="continuing">Whether this moves on from the active match of the same search, rather than starting a new one.</param>
    void Find(string text, bool forward, bool matchCase, bool continuing);

    /// <summary>
    /// Ends the search and removes the highlights.
    /// </summary>
    void StopFinding();

    /// <summary>
    /// Makes the page's main video fill the page view, or puts it back.
    /// </summary>
    void ToggleVideoMode();

    /// <summary>
    /// Updates <see cref="Thumbnail"/> from what the page is showing now.
    /// </summary>
    void CaptureThumbnail();

    /// <summary>
    /// Gives keyboard focus to the page.
    /// </summary>
    void Focus();

    /// <summary>
    /// Takes keyboard focus away from the page, so typing reaches the window's own controls.
    /// </summary>
    void ReleaseFocus();

    /// <summary>
    /// Releases the browser behind the tab.
    /// </summary>
    void Close();

    /// <summary>
    /// Creates tabs.
    /// </summary>
    interface IFactory
    {
        /// <summary>
        /// Creates a tab that starts on an address.
        /// </summary>
        /// <param name="address">The URL to load.</param>
        /// <returns>The new tab.</returns>
        IBrowserTab Create(string address);
    }

    /// <inheritdoc />
    /// <param name="shortcuts">The shortcuts the tabs catch while a page has focus.</param>
    /// <param name="siteRules">The per-site adjustments the tabs apply to the pages they load.</param>
    /// <param name="adBlocker">Decides what the tabs keep out of the pages they load.</param>
    /// <param name="frameStats">Counts the frames the pages deliver, when diagnostics are on.</param>
    sealed class Factory(IShortcutMap shortcuts, ISiteRules siteRules, IAdBlocker adBlocker, IFrameStats frameStats) : IFactory
    {
        /// <inheritdoc />
        public IBrowserTab Create(string address)
            => new BrowserTab(address, shortcuts, siteRules, adBlocker, frameStats);
    }
}

/// <inheritdoc cref="IBrowserTab" />
internal sealed class BrowserTab : ObservableObject, IBrowserTab
{
    private readonly PageBrowser browser;
    private readonly IAdBlocker adBlocker;
    private string title = string.Empty;
    private string address;
    private RenderTargetBitmap? thumbnail;
    private bool isActive;
    private bool canGoBack;
    private bool canGoForward;

    /// <summary>
    /// Creates a tab and starts loading an address.
    /// </summary>
    /// <param name="address">The URL to load.</param>
    /// <param name="shortcuts">The shortcuts to catch while the page has focus.</param>
    /// <param name="siteRules">The per-site adjustments to apply to every frame that loads.</param>
    /// <param name="adBlocker">Decides what to keep out of the page.</param>
    /// <param name="frameStats">Counts the frames the page delivers, when diagnostics are on.</param>
    public BrowserTab(string address, IShortcutMap shortcuts, ISiteRules siteRules, IAdBlocker adBlocker, IFrameStats frameStats)
    {
        browser = new PageBrowser(frameStats);
        this.address = address;
        this.adBlocker = adBlocker;
        browser.IsVisible = false;
        browser.Settings.BackgroundColor = new CefColor(255, 30, 30, 30);
        browser.Settings.WindowlessFrameRate = 60;
        browser.ContextMenuHandler = new BrowserContextMenu(browser, Open);
        browser.LinkOpenRequested += Open;
        browser.FindResult += (count, active) => Dispatcher.UIThread.Post(() => FindResultChanged?.Invoke(count, active));
        browser.LifeSpanHandler = new BrowserPopups(adBlocker, (url, background) => Dispatcher.UIThread.Post(() => Open(url, background ? LinkTarget.BackgroundTab : LinkTarget.Current)));
        browser.LoadingStateChange += (_, state) => Dispatcher.UIThread.Post(() =>
        {
            CanGoBack = state.CanGoBack;
            CanGoForward = state.CanGoForward;
        });
        browser.KeyboardHandler = new BrowserKeys(shortcuts, command => Dispatcher.UIThread.Post(() => ShortcutPressed?.Invoke(command)));
        browser.Address = address;
        browser.RequestHandler = new BrowserRequests(adBlocker);
        browser.LoadStart += (_, started) => HideAds(started.Frame);
        browser.LoadEnd += (_, loaded) =>
        {
            string frameAddress = loaded.Frame.Url ?? string.Empty;
            HideAds(loaded.Frame);

            foreach (string script in siteRules.Scripts(frameAddress))
            {
                loaded.Frame.ExecuteJavaScript(script, frameAddress, 0);
            }
        };
        browser.AddressChanged += (_, value) => Dispatcher.UIThread.Post(() => Address = value ?? string.Empty);
        browser.TitleChanged += (_, value) => Dispatcher.UIThread.Post(() => Title = value is null or "about:blank" ? string.Empty : value);
    }

    /// <inheritdoc />
    public event Action<string, bool>? NewTabRequested;

    /// <inheritdoc />
    public event Action<ShortcutCommand>? ShortcutPressed;

    /// <inheritdoc />
    public event Action<int, int>? FindResultChanged;

    /// <inheritdoc />
    public string Title
    {
        get => title;
        private set => Set(ref title, value);
    }

    /// <inheritdoc />
    public string Address
    {
        get => address;
        private set => Set(ref address, value);
    }

    /// <inheritdoc />
    public bool CanGoBack
    {
        get => canGoBack;
        private set => Set(ref canGoBack, value);
    }

    /// <inheritdoc />
    public bool CanGoForward
    {
        get => canGoForward;
        private set => Set(ref canGoForward, value);
    }

    /// <inheritdoc />
    public IImage? Thumbnail => thumbnail;

    /// <inheritdoc />
    public bool IsActive
    {
        get => isActive;
        set
        {
            if (isActive && !value)
            {
                CaptureThumbnail();
            }

            Set(ref isActive, value);
            browser.IsVisible = value;
        }
    }

    /// <inheritdoc />
    public Control View => browser;

    /// <inheritdoc />
    public void Navigate(string address)
        => browser.Address = address;

    /// <inheritdoc />
    public void GoBack()
        => browser.GoBack();

    /// <inheritdoc />
    public void GoForward()
        => browser.GoForward();

    /// <inheritdoc />
    public void Reload()
        => browser.Reload(false);

    /// <inheritdoc />
    public void Find(string text, bool forward, bool matchCase, bool continuing)
        => browser.Find(text, forward, matchCase, continuing);

    /// <inheritdoc />
    public void StopFinding()
        => browser.StopFinding();

    /// <inheritdoc />
    public void ToggleVideoMode()
        => browser.ExecuteJavaScript("window.__markwardtVideoMode?.toggleAll();", "about:blank", 0);

    /// <inheritdoc />
    public void CaptureThumbnail()
    {
        PixelSize size = new((int)browser.Bounds.Width, (int)browser.Bounds.Height);

        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        RenderTargetBitmap previous = thumbnail!;
        RenderTargetBitmap bitmap = new(size);
        bitmap.Render(browser);
        thumbnail = bitmap;
        Raise(nameof(Thumbnail));
        previous?.Dispose();
    }

    /// <inheritdoc />
    public void Focus()
        => browser.Focus();

    /// <inheritdoc />
    public void ReleaseFocus()
        => browser.ReleaseFocus();

    /// <inheritdoc />
    public void Close()
    {
        browser.Dispose();
        thumbnail?.Dispose();
    }

    private void HideAds(CefFrame frame)
    {
        string address = frame.Url ?? string.Empty;

        if (adBlocker.FrameScript(address, frame.IsMain) is { } script)
        {
            frame.ExecuteJavaScript(script, address, 0);
        }
    }

    private void Open(string address, LinkTarget target)
    {
        if (target == LinkTarget.Current)
        {
            Navigate(address);
        }
        else
        {
            NewTabRequested?.Invoke(address, target == LinkTarget.BackgroundTab);
        }
    }
}
