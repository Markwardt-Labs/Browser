namespace Markwardt.Browser;

/// <summary>
/// The browser window. It shows the active page under a top bar (address box and window controls) that either stays visible or only appears with the overview of the tabs, which F1 raises.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IBrowserTabs tabs;
    private readonly IAddressResolver addressResolver;
    private readonly IShortcutMap shortcutMap;
    private readonly IBookmarkList bookmarks;
    private readonly IUserSettings settings;
    private readonly LaunchOptions launch;
    private readonly Action newWindow;
    private bool showingAddress;
    private bool matchCase;
    private IBrowserTab? findTab;

    internal MainWindow(IBrowserTabs tabs, IAddressResolver addressResolver, IShortcutMap shortcutMap, IBookmarkList bookmarks, IUserSettings settings, LaunchOptions launch, bool startInOverview, Action newWindow)
    {
        this.newWindow = newWindow;
        this.settings = settings;
        this.launch = launch;
        this.tabs = tabs;
        this.addressResolver = addressResolver;
        this.shortcutMap = shortcutMap;
        this.bookmarks = bookmarks;
        DataContext = tabs;
        InitializeComponent();

        AddressBox.PropertyChanged += OnAddressBoxPropertyChanged;
        AddressBox.LostFocus += OnAddressLostFocus;
        Overview.PropertyChanged += OnOverviewPropertyChanged;
        settings.PropertyChanged += OnSettingsChanged;
        FindBox.PropertyChanged += OnFindBoxPropertyChanged;
        tabs.PropertyChanged += OnTabsPropertyChanged;
        BookmarkItems.ItemsSource = bookmarks.Items;
        bookmarks.Items.CollectionChanged += OnBookmarksChanged;
        UpdateBookmarks();
        tabs.Items.CollectionChanged += OnTabsChanged;
        tabs.ShortcutPressed += Run;
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
        Opened += (_, _) =>
        {
            if (startInOverview && !launch.Isolate)
            {
                ShowOverview();
            }
            else
            {
                tabs.Active?.Focus();
            }
        };

        if (launch.Isolate)
        {
            Navigation.IsVisible = false;
            NewWindowButton.IsVisible = false;
            PinButton.IsVisible = false;
        }

        MinimizeButton.IsVisible = !launch.CloseOnly;
        MaximizeButton.IsVisible = !launch.CloseOnly;
        UpdateTopBar();
        RefreshAddress();

        foreach (IBrowserTab tab in tabs.Items)
        {
            ViewHost.Children.Add(tab.View);
            tab.FindResultChanged += OnFindResult;
            tab.PropertyChanged += OnTabPropertyChanged;
        }
    }

    /// <summary>
    /// Gets the host name of the page being viewed, or an empty string for a blank page.
    /// </summary>
    internal string ActiveHost
        => Uri.TryCreate(tabs.Active?.Address, UriKind.Absolute, out Uri? uri) ? uri.Host : string.Empty;

    /// <summary>
    /// Releases the tabs of a window that is closing while others stay open, so their pages stop running.
    /// </summary>
    internal void CloseTabs()
        => tabs.CloseAll();

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        bookmarks.Items.CollectionChanged -= OnBookmarksChanged;
        settings.PropertyChanged -= OnSettingsChanged;
        base.OnClosed(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty && Edges is not null)
        {
            Edges.IsVisible = WindowState == WindowState.Normal;
        }
    }

    private void OnBookmarksChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => UpdateBookmarks();

    private void OnTabsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        foreach (IBrowserTab tab in e.OldItems?.Cast<IBrowserTab>() ?? [])
        {
            ViewHost.Children.Remove(tab.View);
            tab.FindResultChanged -= OnFindResult;
            tab.PropertyChanged -= OnTabPropertyChanged;
        }

        foreach (IBrowserTab tab in e.NewItems?.Cast<IBrowserTab>() ?? [])
        {
            ViewHost.Children.Add(tab.View);
            tab.FindResultChanged += OnFindResult;
            tab.PropertyChanged += OnTabPropertyChanged;
        }
    }

    private void OnTabsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IBrowserTabs.Active))
        {
            return;
        }

        if (FindBar.IsVisible && !ReferenceEquals(findTab, tabs.Active))
        {
            CloseFind(false);
        }

        RefreshAddress();
    }

    private void OnTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IBrowserTab.Address) && ReferenceEquals(sender, tabs.Active))
        {
            RefreshAddress();
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        => UpdateTopBar();

    private void OnOverviewPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty)
        {
            UpdateTopBar();
        }
    }

    private bool IsTopBarPinned => launch.Isolate || settings.TopBarVisible;

    private void UpdateTopBar()
    {
        bool pinned = IsTopBarPinned;
        TopBar.IsVisible = pinned || Overview.IsVisible;
        Page.Margin = new Thickness(0, pinned ? TopBar.Height : 0, 0, 0);
        PinButton.Classes.Set("on", settings.TopBarVisible);
        ToolTip.SetTip(PinButton, settings.TopBarVisible ? "Show the top bar only in the overview" : "Keep the top bar visible");
    }

    private void RefreshAddress()
    {
        UpdateBookmarks();

        if (!AddressBox.IsKeyboardFocusWithin)
        {
            ShowAddress();
        }
    }

    private void OnFindBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty && FindBar.IsVisible)
        {
            StartFind(true, false);
        }
    }

    private void OnFindResult(int count, int active)
    {
        if (!FindBar.IsVisible || string.IsNullOrEmpty(FindBox.Text))
        {
            return;
        }

        FindCount.Text = count == 0 ? "No results" : $"{Math.Max(active, 1)}/{count}";
    }

    private void ShowFind()
    {
        if (Overview.IsVisible)
        {
            HideOverview();
        }

        findTab = tabs.Active;
        FindBar.IsVisible = true;
        tabs.Active?.ReleaseFocus();
        Activate();
        FindBox.Focus();
        FindBox.SelectAll();
        StartFind(true, false);
    }

    private void StartFind(bool forward, bool continuing)
    {
        string text = FindBox.Text ?? string.Empty;

        if (text.Length == 0)
        {
            findTab?.StopFinding();
            FindCount.Text = string.Empty;
            return;
        }

        findTab?.Find(text, forward, matchCase, continuing);
    }

    private void CloseFind(bool focusPage)
    {
        if (!FindBar.IsVisible)
        {
            return;
        }

        FindBar.IsVisible = false;
        findTab?.StopFinding();
        findTab = null;
        FindCount.Text = string.Empty;

        if (focusPage)
        {
            tabs.Active?.Focus();
        }
    }

    private void OnFindKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            StartFind(!e.KeyModifiers.HasFlag(KeyModifiers.Shift), true);
            e.Handled = true;
        }
    }

    private void OnMatchCaseClick(object? sender, RoutedEventArgs e)
    {
        matchCase = !matchCase;
        MatchCaseButton.Classes.Set("on", matchCase);
        StartFind(true, false);
        FindBox.Focus();
    }

    private void OnFindPreviousClick(object? sender, RoutedEventArgs e)
    {
        StartFind(false, true);
        FindBox.Focus();
    }

    private void OnFindNextClick(object? sender, RoutedEventArgs e)
    {
        StartFind(true, true);
        FindBox.Focus();
    }

    private void OnFindCloseClick(object? sender, RoutedEventArgs e)
        => CloseFind(true);

    private void UpdateBookmarks()
    {
        NoBookmarks.IsVisible = bookmarks.Items.Count == 0;
        string address = tabs.Active?.Address ?? string.Empty;
        StarButton.IsEnabled = address.Length > 0 && !address.StartsWith("about:", StringComparison.OrdinalIgnoreCase);
        StarButton.Classes.Set("on", StarButton.IsEnabled && bookmarks.Contains(address));
        ToolTip.SetTip(StarButton, StarButton.Classes.Contains("on") ? "Remove bookmark" : "Bookmark this page");
    }

    private void ShowAddress()
    {
        showingAddress = true;
        AddressBox.Text = addressResolver.Describe(tabs.Active?.Address ?? string.Empty);
        showingAddress = false;
        Suggestions.IsOpen = false;
    }

    private void ShowOverview()
    {
        if (launch.Isolate)
        {
            return;
        }

        CloseFind(false);
        ShowAddress();
        UpdateBookmarks();
        tabs.Active?.CaptureThumbnail();
        Overview.IsVisible = true;
        FocusAddress();
    }

    private void FocusAddress()
    {
        tabs.Active?.ReleaseFocus();

        if (WindowState != WindowState.Minimized)
        {
            Activate();
        }

        AddressBox.Focus();
        AddressBox.SelectAll();
    }

    private void HideOverview()
    {
        Suggestions.IsOpen = false;
        Overview.IsVisible = false;
        tabs.Active?.Focus();
    }

    private void CloseActiveTab()
    {
        if (tabs.Active is { } tab)
        {
            tabs.Close(tab);
        }

        if (!Overview.IsVisible)
        {
            tabs.Active?.Focus();
        }
    }

    private void Run(ShortcutCommand command)
    {
        if (launch.Isolate && command is ShortcutCommand.NewTab or ShortcutCommand.FocusAddress or ShortcutCommand.ToggleOverview)
        {
            return;
        }

        switch (command)
        {
            case ShortcutCommand.NewTab:
                tabs.Open("about:blank");
                ShowOverview();
                break;
            case ShortcutCommand.CloseTab:
                CloseActiveTab();
                break;
            case ShortcutCommand.FocusAddress:
                if (IsTopBarPinned && !Overview.IsVisible)
                {
                    ShowAddress();
                    FocusAddress();
                }
                else
                {
                    ShowOverview();
                }

                break;
            case ShortcutCommand.ToggleOverview:
                if (Overview.IsVisible)
                {
                    HideOverview();
                }
                else
                {
                    ShowOverview();
                }

                break;
            case ShortcutCommand.Reload:
                tabs.Active?.Reload();
                break;
            case ShortcutCommand.Back:
                tabs.Active?.GoBack();
                break;
            case ShortcutCommand.Forward:
                tabs.Active?.GoForward();
                break;
            case ShortcutCommand.Find:
                ShowFind();
                break;
            case ShortcutCommand.ToggleVideoMode:
                if (Overview.IsVisible)
                {
                    HideOverview();
                }

                tabs.Active?.ToggleVideoMode();
                break;
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Source is TextBox { IsVisible: true } box && box.Classes.Contains("rename"))
        {
            return;
        }

        if (FindBar.IsVisible && FindBox.IsKeyboardFocusWithin && e.Key == Key.Escape)
        {
            CloseFind(true);
            e.Handled = true;
        }
        else if (Suggestions.IsOpen && e.Key == Key.Escape)
        {
            Suggestions.IsOpen = false;
            e.Handled = true;
        }
        else if (!Overview.IsVisible && AddressBox.IsKeyboardFocusWithin && e.Key == Key.Escape)
        {
            ShowAddress();
            tabs.Active?.Focus();
            e.Handled = true;
        }
        else if (Overview.IsVisible && e.Key == Key.Escape)
        {
            HideOverview();
            e.Handled = true;
        }
        else if (shortcutMap.Find(e.Key, e.KeyModifiers) is { } command)
        {
            Run(command);
            e.Handled = true;
        }
    }

    private void OnAddressBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TextBox.TextProperty || showingAddress)
        {
            return;
        }

        string text = AddressBox.Text ?? string.Empty;
        IReadOnlyList<Bookmark> matches = text.TrimStart().StartsWith(':') ? [] : bookmarks.Search(text, 6);
        SuggestionList.ItemsSource = matches;
        SuggestionList.SelectedIndex = -1;
        SuggestionBox.Width = Math.Max(AddressBox.Bounds.Width, 200);
        Suggestions.IsOpen = matches.Count > 0;
    }

    private void OnAddressKeyDown(object? sender, KeyEventArgs e)
    {
        int count = (SuggestionList.ItemsSource as IReadOnlyList<Bookmark>)?.Count ?? 0;

        if (Suggestions.IsOpen && e.Key == Key.Down)
        {
            SuggestionList.SelectedIndex = Math.Min(SuggestionList.SelectedIndex + 1, count - 1);
            e.Handled = true;
        }
        else if (Suggestions.IsOpen && e.Key == Key.Up)
        {
            SuggestionList.SelectedIndex = Math.Max(SuggestionList.SelectedIndex - 1, -1);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (Suggestions.IsOpen && SuggestionList.SelectedItem is Bookmark bookmark)
            {
                OpenBookmark(bookmark);
            }
            else
            {
                SubmitAddress();
            }

            e.Handled = true;
        }
    }

    private void SubmitAddress()
    {
        if (addressResolver.Interpret(AddressBox.Text ?? string.Empty) is not { } action)
        {
            return;
        }

        if (action.InNewTab)
        {
            tabs.Open(action.Address);
        }
        else
        {
            tabs.Active?.Navigate(action.Address);
        }

        HideOverview();
    }

    private void OpenBookmark(Bookmark bookmark)
    {
        tabs.Active?.Navigate(bookmark.Address);
        HideOverview();
    }

    private void OnSuggestionPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if ((e.Source as Visual)?.GetSelfAndVisualAncestors().OfType<ListBoxItem>().FirstOrDefault()?.DataContext is Bookmark bookmark)
        {
            OpenBookmark(bookmark);
            e.Handled = true;
        }
    }

    private void OnAddressGotFocus(object? sender, GotFocusEventArgs e)
    {
        AddressBox.SelectAll();

        if (!Overview.IsVisible)
        {
            tabs.Active?.ReleaseFocus();
        }
    }

    private void OnAddressLostFocus(object? sender, RoutedEventArgs e)
    {
        if (!Overview.IsVisible)
        {
            ShowAddress();
        }
    }

    private void OnPinClick(object? sender, RoutedEventArgs e)
    {
        settings.TopBarVisible = !settings.TopBarVisible;
        e.Handled = true;
    }

    private void OnStarClick(object? sender, RoutedEventArgs e)
    {
        if (tabs.Active is { } tab)
        {
            bookmarks.Toggle(tab.Title, tab.Address);
        }

        e.Handled = true;
    }

    private void OnBookmarkPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not Bookmark bookmark || (e.Source as Visual)?.FindAncestorOfType<Button>(true) is not null || (e.Source as Visual)?.FindAncestorOfType<TextBox>(true) is not null)
        {
            return;
        }

        PointerPointProperties properties = e.GetCurrentPoint(this).Properties;

        if (properties.IsMiddleButtonPressed)
        {
            tabs.Open(bookmark.Address);
        }
        else if (properties.IsLeftButtonPressed)
        {
            tabs.Active?.Navigate(bookmark.Address);
        }
        else
        {
            return;
        }

        HideOverview();
        e.Handled = true;
    }

    private (TextBlock Title, TextBox Edit) BookmarkParts(Visual inside)
    {
        Border item = inside.GetSelfAndVisualAncestors().OfType<Border>().First(border => border.Classes.Contains("bookmark"));
        IEnumerable<Visual> parts = item.GetVisualDescendants();
        return (parts.OfType<TextBlock>().First(text => text.Classes.Contains("bookmarkTitle")), parts.OfType<TextBox>().First());
    }

    private void FinishRename(TextBox edit, bool commit)
    {
        if (!edit.IsVisible)
        {
            return;
        }

        (TextBlock title, _) = BookmarkParts(edit);
        string text = edit.Text ?? string.Empty;
        edit.IsVisible = false;
        title.IsVisible = true;

        if (commit && edit.DataContext is Bookmark bookmark)
        {
            bookmarks.Rename(bookmark, text);
        }
    }

    private void OnRenameBookmarkClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: Bookmark bookmark } button)
        {
            (TextBlock title, TextBox edit) = BookmarkParts(button);
            edit.Text = bookmark.Title;
            title.IsVisible = false;
            edit.IsVisible = true;
            edit.Focus();
            edit.SelectAll();
        }

        e.Handled = true;
    }

    private void OnRenameKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox edit && e.Key is Key.Enter or Key.Escape)
        {
            FinishRename(edit, e.Key == Key.Enter);
            e.Handled = true;
        }
    }

    private void OnRenameLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox edit)
        {
            FinishRename(edit, true);
        }
    }

    private void OnNewWindowClick(object? sender, RoutedEventArgs e)
    {
        newWindow();
        e.Handled = true;
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        Run(ShortcutCommand.Back);
        HideOverview();
        e.Handled = true;
    }

    private void OnForwardClick(object? sender, RoutedEventArgs e)
    {
        Run(ShortcutCommand.Forward);
        HideOverview();
        e.Handled = true;
    }

    private void OnReloadClick(object? sender, RoutedEventArgs e)
    {
        Run(ShortcutCommand.Reload);
        HideOverview();
        e.Handled = true;
    }

    private void OnNewTabClick(object? sender, RoutedEventArgs e)
    {
        Run(ShortcutCommand.NewTab);
        e.Handled = true;
    }

    private void OnRemoveBookmarkClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is Bookmark bookmark)
        {
            bookmarks.Remove(bookmark);
        }

        e.Handled = true;
    }

    private void OnCloseTabClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is IBrowserTab tab)
        {
            tabs.Close(tab);
        }

        e.Handled = true;
    }

    private void OnCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not IBrowserTab tab || (e.Source as Visual)?.FindAncestorOfType<Button>(true) is not null)
        {
            return;
        }

        PointerPointProperties properties = e.GetCurrentPoint(this).Properties;

        if (properties.IsMiddleButtonPressed)
        {
            tabs.Close(tab);
        }
        else if (properties.IsLeftButtonPressed)
        {
            tabs.Select(tab);
            HideOverview();
        }

        e.Handled = true;
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
        => ToggleMaximized();

    private void OnCloseClick(object? sender, RoutedEventArgs e)
        => Close();

    private void ToggleMaximized()
    {
        if (!launch.CloseOnly)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }

    private void OnTopBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || (e.Source as Visual)?.FindAncestorOfType<Button>(true) is not null || (e.Source as Visual)?.FindAncestorOfType<TextBox>(true) is not null)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximized();
        }
        else
        {
            BeginMoveDrag(e);
        }
    }

    private void OnEdgePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (WindowState == WindowState.Normal && (sender as Control)?.Tag is string name && Enum.TryParse(name, out WindowEdge edge))
        {
            BeginResizeDrag(edge, e);
        }
    }
}
