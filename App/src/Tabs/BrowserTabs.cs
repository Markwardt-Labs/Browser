namespace Markwardt.Browser.Tabs;

/// <summary>
/// The open tabs and which one is shown.
/// </summary>
internal interface IBrowserTabs : INotifyPropertyChanged
{
    /// <summary>
    /// Raised when a keyboard shortcut is pressed while a page has focus.
    /// </summary>
    event Action<ShortcutCommand>? ShortcutPressed;

    /// <summary>
    /// Gets the open tabs in display order.
    /// </summary>
    ObservableCollection<IBrowserTab> Items { get; }

    /// <summary>
    /// Gets the tab being shown, or null before the first tab opens.
    /// </summary>
    IBrowserTab? Active { get; }

    /// <summary>
    /// Opens a tab.
    /// </summary>
    /// <param name="address">The URL to load.</param>
    /// <param name="activate">Whether to show the new tab; otherwise it loads in the background.</param>
    /// <returns>The new tab.</returns>
    IBrowserTab Open(string address, bool activate = true);

    /// <summary>
    /// Closes every tab without opening a blank one, for when the window they belong to is gone.
    /// </summary>
    void CloseAll();

    /// <summary>
    /// Cleans up the tabs that sit out of view: freezes them, then suspends them, then closes them, as each step of the policy comes due. Locked tabs (by the user or automatically) and the shown tab are left alone.
    /// </summary>
    /// <param name="policy">How long each step waits.</param>
    void Cleanup(TabCleanupPolicy policy);

    /// <summary>
    /// Shows a tab.
    /// </summary>
    /// <param name="tab">The tab to show.</param>
    void Select(IBrowserTab tab);

    /// <summary>
    /// Closes a tab. Closing the last tab leaves a blank one open, so the window is never empty.
    /// </summary>
    /// <param name="tab">The tab to close.</param>
    void Close(IBrowserTab tab);
}

/// <inheritdoc cref="IBrowserTabs" />
/// <param name="factory">Creates the tabs.</param>
/// <param name="singleTab">Whether requests for a new tab load in the current one instead, for windows with no way to switch between tabs.</param>
internal sealed class BrowserTabs(IBrowserTab.IFactory factory, bool singleTab = false) : ObservableObject, IBrowserTabs
{
    private IBrowserTab? active;

    /// <inheritdoc />
    public event Action<ShortcutCommand>? ShortcutPressed;

    /// <inheritdoc />
    public ObservableCollection<IBrowserTab> Items { get; } = [];

    /// <inheritdoc />
    public IBrowserTab? Active
    {
        get => active;
        private set => Set(ref active, value);
    }

    /// <inheritdoc />
    public IBrowserTab Open(string address, bool activate = true)
    {
        IBrowserTab tab = factory.Create(address);
        tab.NewTabRequested += OnNewTabRequested;
        tab.ShortcutPressed += OnShortcutPressed;
        Items.Add(tab);

        if (activate || active is null)
        {
            Select(tab);
        }

        return tab;
    }

    /// <inheritdoc />
    public void CloseAll()
    {
        foreach (IBrowserTab tab in Items.ToList())
        {
            tab.NewTabRequested -= OnNewTabRequested;
            tab.ShortcutPressed -= OnShortcutPressed;
            Items.Remove(tab);
            tab.Close();
        }

        Active = null;
    }

    /// <inheritdoc />
    public void Cleanup(TabCleanupPolicy policy)
    {
        foreach (IBrowserTab tab in Items.Where(tab => !ReferenceEquals(tab, active) && tab.Lock == TabLock.Unlocked).ToList())
        {
            TimeSpan idle = tab.IdleFor;

            if (idle >= policy.CloseAfter)
            {
                Close(tab);
            }
            else if (idle >= policy.SuspendAfter)
            {
                if (!tab.IsSuspended)
                {
                    tab.Suspend();
                }
            }
            else if (idle >= policy.FreezeAfter && !tab.IsFrozen && !tab.IsSuspended)
            {
                tab.Freeze();
            }
        }
    }

    /// <inheritdoc />
    public void Select(IBrowserTab tab)
    {
        if (active is not null)
        {
            active.IsActive = false;
        }

        tab.IsActive = true;
        Active = tab;
    }

    /// <inheritdoc />
    public void Close(IBrowserTab tab)
    {
        int index = Items.IndexOf(tab);

        if (index < 0)
        {
            return;
        }

        tab.NewTabRequested -= OnNewTabRequested;
        tab.ShortcutPressed -= OnShortcutPressed;
        Items.RemoveAt(index);
        tab.Close();

        if (Items.Count == 0)
        {
            Active = null;
            Open("about:blank");
        }
        else if (ReferenceEquals(active, tab))
        {
            active = null;
            Select(Items[Math.Min(index, Items.Count - 1)]);
        }
    }

    private void OnNewTabRequested(string address, bool background)
    {
        if (singleTab)
        {
            active?.Navigate(address);
        }
        else
        {
            Open(address, !background);
        }
    }

    private void OnShortcutPressed(ShortcutCommand command)
        => ShortcutPressed?.Invoke(command);
}
