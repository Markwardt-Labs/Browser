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
    /// Gets a value indicating whether the page is frozen: it keeps its memory and state but runs nothing, and carries on when the tab is shown.
    /// </summary>
    bool IsFrozen { get; }

    /// <summary>
    /// Gets a value indicating whether the page has been released to free its resources. A suspended tab keeps its address, title, picture, history and where the page was scrolled to, and loads its address again when it is shown.
    /// </summary>
    bool IsSuspended { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the user locked the tab, which protects it from automatic cleanup until they unlock it.
    /// </summary>
    bool IsLocked { get; set; }

    /// <summary>
    /// Gets whether the tab is protected from automatic cleanup: by the user (<see cref="TabLock.Locked"/>), or for as long as the page is doing something that would be lost (<see cref="TabLock.AutoLocked"/>).
    /// </summary>
    TabLock Lock { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Lock"/> is <see cref="TabLock.AutoLocked"/>.
    /// </summary>
    bool IsAutoLocked { get; }

    /// <summary>
    /// Gets a sentence that says what the lock is and, when automatic, why.
    /// </summary>
    string LockDescription { get; }

    /// <summary>
    /// Gets how long the tab has been out of view, or zero while it is shown.
    /// </summary>
    TimeSpan IdleFor { get; }

    /// <summary>
    /// Gets the question the page is blocked on until the user answers it, or null.
    /// </summary>
    IPageDialog? Dialog { get; }

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
    /// Freezes the page so it stops using CPU. Does nothing for a shown tab, or one that is suspended or already frozen.
    /// </summary>
    void Freeze();

    /// <summary>
    /// Releases the page so it stops using CPU, GPU and memory. Does nothing for a shown tab or one that is already suspended.
    /// </summary>
    void Suspend();

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
    /// <param name="videoStats">Collects what pages report about their playing video, when diagnostics are on.</param>
    sealed class Factory(IShortcutMap shortcuts, ISiteRules siteRules, IAdBlocker adBlocker, IFrameStats frameStats, IVideoStats videoStats) : IFactory
    {
        /// <inheritdoc />
        public IBrowserTab Create(string address)
            => new BrowserTab(address, shortcuts, siteRules, adBlocker, frameStats, videoStats);
    }
}

/// <inheritdoc cref="IBrowserTab" />
internal sealed class BrowserTab : ObservableObject, IBrowserTab
{
    private static string Describe(AutoLockReason reasons)
    {
        List<string> parts = [];

        if (reasons.HasFlag(AutoLockReason.Typing))
        {
            parts.Add("you typed into a field");
        }

        if (reasons.HasFlag(AutoLockReason.Transfer))
        {
            parts.Add("a download or upload is running");
        }

        if (reasons.HasFlag(AutoLockReason.Sound))
        {
            parts.Add("it is playing sound");
        }

        return string.Join(" and ", parts);
    }

    /// <summary>
    /// Creates a tab and starts loading an address.
    /// </summary>
    /// <param name="address">The URL to load.</param>
    /// <param name="shortcuts">The shortcuts to catch while the page has focus.</param>
    /// <param name="siteRules">The per-site adjustments to apply to every frame that loads.</param>
    /// <param name="adBlocker">Decides what to keep out of the page.</param>
    /// <param name="frameStats">Counts the frames the page delivers, when diagnostics are on.</param>
    /// <param name="videoStats">Collects what the page reports about its playing video, when diagnostics are on.</param>
    public BrowserTab(string address, IShortcutMap shortcuts, ISiteRules siteRules, IAdBlocker adBlocker, IFrameStats frameStats, IVideoStats videoStats)
    {
        this.shortcuts = shortcuts;
        this.siteRules = siteRules;
        this.frameStats = frameStats;
        this.videoStats = videoStats;
        this.address = address;
        this.adBlocker = adBlocker;
        host.IsVisible = false;
        browser = CreateBrowser(address);
    }

    private readonly Panel host = new();
    private readonly IShortcutMap shortcuts;
    private readonly ISiteRules siteRules;
    private readonly IAdBlocker adBlocker;
    private readonly IFrameStats frameStats;
    private readonly IVideoStats videoStats;
    private readonly IPageRestore pageRestore = new PageRestore();
    private readonly IAutoLock autoLock = new AutoLock();
    private readonly IVirtualHistory history = new VirtualHistory();
    private readonly string downloadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    private PageBrowser? browser;
    private long hiddenSince = Stopwatch.GetTimestamp();
    private bool isFrozen;
    private bool isSuspended;
    private bool isLocked;
    private TabLock lastLock;
    private bool hasLoaded;
    private bool resuming;
    private string? pendingRestore;
    private PageReport? lastTop;
    private IReadOnlyList<HistoryEntry> nativeEntries = [];
    private int nativeIndex = -1;
    private int nativeTotal = -1;
    private bool nativeCanGoBack;
    private bool nativeCanGoForward;
    private IReadOnlyList<HistoryEntry>? savedEntries;
    private int savedIndex = -1;
    private string title = string.Empty;
    private string address;
    private IPageDialog? dialog;
    private RenderTargetBitmap? thumbnail;
    private bool isActive;
    private bool canGoBack;
    private bool canGoForward;

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
    public bool IsFrozen
    {
        get => isFrozen;
        private set => Set(ref isFrozen, value);
    }

    /// <inheritdoc />
    public bool IsSuspended
    {
        get => isSuspended;
        private set => Set(ref isSuspended, value);
    }

    /// <inheritdoc />
    public bool IsLocked
    {
        get => isLocked;
        set
        {
            if (isLocked == value)
            {
                return;
            }

            isLocked = value;
            Raise(nameof(IsLocked));

            if (value)
            {
                Unfreeze();
            }

            RefreshLock();
        }
    }

    /// <inheritdoc />
    public TabLock Lock
        => isLocked ? TabLock.Locked : autoLock.Reasons != AutoLockReason.None ? TabLock.AutoLocked : TabLock.Unlocked;

    /// <inheritdoc />
    public bool IsAutoLocked => Lock == TabLock.AutoLocked;

    /// <inheritdoc />
    public string LockDescription
        => Lock switch
        {
            TabLock.Locked => "Locked: this tab is never frozen, suspended or closed. Click to unlock it.",
            TabLock.AutoLocked => $"Locked for now because {Describe(autoLock.Reasons)}. Click to lock it until you unlock it.",
            _ => "Lock this tab so it is never frozen, suspended or closed.",
        };

    /// <inheritdoc />
    public TimeSpan IdleFor
        => isActive ? TimeSpan.Zero : Stopwatch.GetElapsedTime(hiddenSince);

    /// <inheritdoc />
    public IPageDialog? Dialog
    {
        get => dialog;
        private set => Set(ref dialog, value);
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
                hiddenSince = Stopwatch.GetTimestamp();
            }

            if (value)
            {
                Resume();
                Unfreeze();
            }

            Set(ref isActive, value);
            host.IsVisible = value;

            if (browser is not null)
            {
                browser.IsVisible = value;
            }
        }
    }

    /// <inheritdoc />
    public Control View => host;

    /// <inheritdoc />
    public void Navigate(string address)
    {
        if (browser is null)
        {
            savedEntries = [.. savedEntries?.Take(savedIndex + 1) ?? [], new HistoryEntry(address, string.Empty)];
            savedIndex = savedEntries.Count - 1;
            lastTop = null;
            Address = address;
            Resume();
            return;
        }

        Unfreeze();
        browser.Address = address;
    }

    /// <inheritdoc />
    public void GoBack()
    {
        if (browser is null)
        {
            return;
        }

        Unfreeze();

        switch (history.Back(nativeCanGoBack, new HistoryEntry(address, title), out HistoryEntry? entry))
        {
            case HistoryMove.Native:
                browser.GoBack();
                break;
            case HistoryMove.Restored when entry is not null:
                Restore(entry);
                break;
        }

        UpdateHistoryFlags();
    }

    /// <inheritdoc />
    public void GoForward()
    {
        if (browser is null)
        {
            return;
        }

        Unfreeze();

        switch (history.Forward(nativeCanGoForward, new HistoryEntry(address, title), out HistoryEntry? entry))
        {
            case HistoryMove.Native:
                browser.GoForward();
                break;
            case HistoryMove.Restored when entry is not null:
                Restore(entry);
                break;
        }

        UpdateHistoryFlags();
    }

    /// <inheritdoc />
    public void Reload()
    {
        if (browser is null)
        {
            Resume();
            return;
        }

        Unfreeze();
        browser.Reload(false);
    }

    /// <inheritdoc />
    public void Find(string text, bool forward, bool matchCase, bool continuing)
        => browser?.Find(text, forward, matchCase, continuing);

    /// <inheritdoc />
    public void StopFinding()
        => browser?.StopFinding();

    /// <inheritdoc />
    public void ToggleVideoMode()
        => browser?.ExecuteJavaScript("window.__markwardtVideoMode?.toggleAll();", "about:blank", 0);

    /// <inheritdoc />
    public void Freeze()
    {
        if (isActive || browser is null || isFrozen || isSuspended)
        {
            return;
        }

        browser.SetFrozen(true);
        IsFrozen = true;
    }

    /// <inheritdoc />
    public void Suspend()
    {
        if (isActive || browser is null)
        {
            return;
        }

        if (nativeEntries.Count > 0)
        {
            savedEntries = history.Compose(nativeEntries, nativeIndex, out int index);
            savedIndex = index;
        }

        PageBrowser released = browser;
        browser = null;
        host.Children.Remove(released);
        released.Dispose();
        nativeEntries = [];
        nativeIndex = -1;
        nativeTotal = -1;
        nativeCanGoBack = false;
        nativeCanGoForward = false;
        Dialog = null;
        CanGoBack = false;
        CanGoForward = false;
        IsFrozen = false;
        IsSuspended = true;
        autoLock.Reset();
        RefreshLock();
    }

    /// <inheritdoc />
    public void CaptureThumbnail()
    {
        if (browser is null || !hasLoaded)
        {
            return;
        }

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
        => browser?.Focus();

    /// <inheritdoc />
    public void ReleaseFocus()
        => browser?.ReleaseFocus();

    /// <inheritdoc />
    public void Close()
    {
        dialog = null;
        browser?.Dispose();
        browser = null;
        thumbnail?.Dispose();
    }

    private PageBrowser CreateBrowser(string startAddress)
    {
        PageBrowser created = new(frameStats)
        {
            IsVisible = false,
            Address = startAddress,
        };

        hasLoaded = false;
        created.Settings.BackgroundColor = new CefColor(255, 30, 30, 30);
        created.Settings.WindowlessFrameRate = 60;
        created.ContextMenuHandler = new BrowserContextMenu(created, Open);
        created.LinkOpenRequested += Open;
        created.JSDialogHandler = new BrowserDialogs(question => Dispatcher.UIThread.Post(() => Dialog = question), () => Dispatcher.UIThread.Post(() => Dialog = null));
        created.DownloadHandler = new BrowserDownloads(downloadFolder, (id, inProgress) => Dispatcher.UIThread.Post(() =>
        {
            if (autoLock.Download(id, inProgress))
            {
                RefreshLock();
            }
        }));
        created.FindResult += (count, active) => Dispatcher.UIThread.Post(() => FindResultChanged?.Invoke(count, active));
        created.LifeSpanHandler = new BrowserPopups(adBlocker, (url, background) => Dispatcher.UIThread.Post(() => Open(url, background ? LinkTarget.BackgroundTab : LinkTarget.Current)));
        created.LoadingStateChange += (_, state) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!ReferenceEquals(browser, created))
                {
                    return;
                }

                nativeCanGoBack = state.CanGoBack;
                nativeCanGoForward = state.CanGoForward;
                UpdateHistoryFlags();
            });

            if (!state.IsLoading)
            {
                created.ReadHistory((entries, index) => Dispatcher.UIThread.Post(() => TakeHistory(created, entries, index)));
            }
        };
        created.KeyboardHandler = new BrowserKeys(shortcuts, command => Dispatcher.UIThread.Post(() => ShortcutPressed?.Invoke(command)));
        created.RequestHandler = new BrowserRequests(adBlocker);
        created.ConsoleMessage += (_, console) =>
        {
            string message = console.Message ?? string.Empty;

            if (PageReport.TryParse(message, out PageReport report))
            {
                Dispatcher.UIThread.Post(() => TakeReport(created, report));
            }
            else if (videoStats.Script is not null)
            {
                videoStats.Note(console.Source ?? string.Empty, message);
            }
        };
        created.LoadStart += (_, started) =>
        {
            HideAds(started.Frame);

            if (started.Frame.IsMain)
            {
                Dispatcher.UIThread.Post(() => TakeNavigation(created));
            }
        };
        created.LoadEnd += (_, loaded) =>
        {
            string frameAddress = loaded.Frame.Url ?? string.Empty;
            HideAds(loaded.Frame);

            foreach (string script in siteRules.Scripts(frameAddress))
            {
                loaded.Frame.ExecuteJavaScript(script, frameAddress, 0);
            }

            if (loaded.Frame.IsMain)
            {
                Dispatcher.UIThread.Post(() => TakeLoaded(created));
            }
        };
        created.AddressChanged += (_, value) => Dispatcher.UIThread.Post(() =>
        {
            if (ReferenceEquals(browser, created))
            {
                Address = value ?? string.Empty;
            }
        });
        created.TitleChanged += (_, value) => Dispatcher.UIThread.Post(() =>
        {
            if (ReferenceEquals(browser, created))
            {
                Title = value is null or "about:blank" ? string.Empty : value;
            }
        });
        host.Children.Add(created);
        return created;
    }

    private void Resume()
    {
        if (browser is not null)
        {
            return;
        }

        hiddenSince = Stopwatch.GetTimestamp();
        string start = address;

        if (savedEntries is not null && savedIndex >= 0 && savedIndex < savedEntries.Count)
        {
            history.Load(savedEntries, savedIndex);
            start = savedEntries[savedIndex].Url;
        }

        pendingRestore = lastTop is null ? null : pageRestore.Script(lastTop);
        resuming = true;
        browser = CreateBrowser(start);
        IsSuspended = false;
        UpdateHistoryFlags();
    }

    private void Unfreeze()
    {
        if (!isFrozen)
        {
            return;
        }

        browser?.SetFrozen(false);
        IsFrozen = false;
    }

    private void RefreshLock()
    {
        TabLock now = Lock;

        if (now == TabLock.Unlocked && lastLock != TabLock.Unlocked && !isActive)
        {
            hiddenSince = Stopwatch.GetTimestamp();
        }

        lastLock = now;
        Raise(nameof(Lock));
        Raise(nameof(IsAutoLocked));
        Raise(nameof(LockDescription));
    }

    private void UpdateHistoryFlags()
    {
        CanGoBack = history.CanGoBack(nativeCanGoBack);
        CanGoForward = history.CanGoForward(nativeCanGoForward);
    }

    private void Restore(HistoryEntry entry)
    {
        lastTop = null;
        browser?.ExecuteJavaScript($"location.replace({JsonSerializer.Serialize(entry.Url)});", "about:blank", 0);
    }

    private void TakeHistory(PageBrowser source, IReadOnlyList<HistoryEntry> entries, int index)
    {
        if (!ReferenceEquals(browser, source))
        {
            return;
        }

        if (nativeTotal >= 0 && entries.Count > nativeTotal)
        {
            history.ClearForward();
        }

        nativeEntries = entries;
        nativeIndex = index;
        nativeTotal = entries.Count;
        UpdateHistoryFlags();
    }

    private void TakeReport(PageBrowser source, PageReport report)
    {
        if (!ReferenceEquals(browser, source))
        {
            return;
        }

        if (report.IsTop)
        {
            lastTop = report;
        }

        if (autoLock.Apply(report))
        {
            RefreshLock();
        }
    }

    private void TakeNavigation(PageBrowser source)
    {
        if (!ReferenceEquals(browser, source))
        {
            return;
        }

        if (!resuming)
        {
            lastTop = null;
        }

        if (autoLock.Reset())
        {
            RefreshLock();
        }
    }

    private void TakeLoaded(PageBrowser source)
    {
        if (!ReferenceEquals(browser, source))
        {
            return;
        }

        hasLoaded = true;
        resuming = false;

        if (pendingRestore is { } script)
        {
            pendingRestore = null;
            source.ExecuteJavaScript(script, "about:blank", 0);
        }
    }

    private void HideAds(CefFrame frame)
    {
        string address = frame.Url ?? string.Empty;

        if (adBlocker.FrameScript(address, frame.IsMain) is { } script)
        {
            frame.ExecuteJavaScript(script, address, 0);
        }

        if (videoStats.Script is { } statsScript && address.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            frame.ExecuteJavaScript(statsScript, address, 0);
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
