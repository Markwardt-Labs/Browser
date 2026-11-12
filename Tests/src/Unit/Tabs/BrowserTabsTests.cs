namespace Tests.Unit.Tabs;

/// <summary>
/// Tests for <see cref="BrowserTabs"/>.
/// </summary>
public sealed class BrowserTabsTests
{
    /// <summary>
    /// Creates the collection under test over a factory that hands out mock tabs.
    /// </summary>
    public BrowserTabsTests()
    {
        factory.Setup(f => f.Create(It.IsAny<string>())).Returns<string>(address =>
        {
            Mock<IBrowserTab> tab = new();
            tab.SetupAllProperties();
            tab.Setup(t => t.Address).Returns(address);
            created.Add(tab);
            return tab.Object;
        });

        tabs = new BrowserTabs(factory.Object);
    }

    private readonly Mock<IBrowserTab.IFactory> factory = new();
    private readonly List<Mock<IBrowserTab>> created = [];
    private readonly BrowserTabs tabs;
    private readonly TabCleanupPolicy policy = new() { FreezeAfter = TimeSpan.FromMinutes(5), SuspendAfter = TimeSpan.FromMinutes(15), CloseAfter = TimeSpan.FromHours(24) };

    /// <summary>
    /// Opening a tab adds it and makes it the active one.
    /// </summary>
    [Fact]
    public void Open_FirstTab_BecomesActive()
    {
        IBrowserTab tab = tabs.Open("https://example.com");

        Assert.Same(tab, tabs.Active);
        Assert.Single(tabs.Items);
        Assert.True(tab.IsActive);
    }

    /// <summary>
    /// Opening a second tab deactivates the first.
    /// </summary>
    [Fact]
    public void Open_SecondTab_DeactivatesFirst()
    {
        IBrowserTab first = tabs.Open("https://one.example");
        IBrowserTab second = tabs.Open("https://two.example");

        Assert.False(first.IsActive);
        Assert.True(second.IsActive);
        Assert.Same(second, tabs.Active);
    }

    /// <summary>
    /// Selecting a tab switches which one is active.
    /// </summary>
    [Fact]
    public void Select_OtherTab_SwitchesActive()
    {
        IBrowserTab first = tabs.Open("https://one.example");
        IBrowserTab second = tabs.Open("https://two.example");

        tabs.Select(first);

        Assert.Same(first, tabs.Active);
        Assert.True(first.IsActive);
        Assert.False(second.IsActive);
    }

    /// <summary>
    /// Closing the active tab shows the tab that took its place and releases the closed one.
    /// </summary>
    [Fact]
    public void Close_ActiveTab_SelectsNeighbourAndClosesBrowser()
    {
        IBrowserTab first = tabs.Open("https://one.example");
        IBrowserTab second = tabs.Open("https://two.example");
        IBrowserTab third = tabs.Open("https://three.example");
        tabs.Select(second);

        tabs.Close(second);

        Assert.Equal([first, third], tabs.Items);
        Assert.Same(third, tabs.Active);
        created[1].Verify(t => t.Close(), Times.Once);
    }

    /// <summary>
    /// Closing the last tab in the strip falls back to the one before it.
    /// </summary>
    [Fact]
    public void Close_LastPositionedTab_SelectsPrevious()
    {
        IBrowserTab first = tabs.Open("https://one.example");
        IBrowserTab second = tabs.Open("https://two.example");

        tabs.Close(second);

        Assert.Same(first, tabs.Active);
    }

    /// <summary>
    /// Closing a tab that is not active leaves the active tab alone.
    /// </summary>
    [Fact]
    public void Close_InactiveTab_KeepsActive()
    {
        IBrowserTab first = tabs.Open("https://one.example");
        IBrowserTab second = tabs.Open("https://two.example");

        tabs.Close(first);

        Assert.Same(second, tabs.Active);
        Assert.Single(tabs.Items);
    }

    /// <summary>
    /// Closing the only tab leaves a blank one open.
    /// </summary>
    [Fact]
    public void Close_OnlyTab_OpensBlankTab()
    {
        IBrowserTab only = tabs.Open("https://one.example");

        tabs.Close(only);

        Assert.Single(tabs.Items);
        Assert.NotSame(only, tabs.Active);
        factory.Verify(f => f.Create("about:blank"), Times.Once);
        Assert.True(tabs.Active!.IsActive);
    }

    /// <summary>
    /// Closing a tab that is not open does nothing.
    /// </summary>
    [Fact]
    public void Close_UnknownTab_DoesNothing()
    {
        tabs.Open("https://one.example");
        Mock<IBrowserTab> stranger = new();

        tabs.Close(stranger.Object);

        Assert.Single(tabs.Items);
        stranger.Verify(t => t.Close(), Times.Never);
    }

    /// <summary>
    /// A page asking for a new window opens a tab.
    /// </summary>
    [Fact]
    public void NewTabRequested_OpensTabForAddress()
    {
        tabs.Open("https://one.example");

        created[0].Raise(t => t.NewTabRequested += null, "https://popup.example", false);

        Assert.Equal(2, tabs.Items.Count);
        factory.Verify(f => f.Create("https://popup.example"), Times.Once);
        Assert.Same(tabs.Items[1], tabs.Active);
    }

    /// <summary>
    /// A background request opens the tab without leaving the current one.
    /// </summary>
    [Fact]
    public void NewTabRequested_Background_KeepsActive()
    {
        IBrowserTab first = tabs.Open("https://one.example");

        created[0].Raise(t => t.NewTabRequested += null, "https://popup.example", true);

        Assert.Equal(2, tabs.Items.Count);
        Assert.Same(first, tabs.Active);
        Assert.False(tabs.Items[1].IsActive);
    }

    /// <summary>
    /// A background tab opened into an empty window still becomes the one shown.
    /// </summary>
    [Fact]
    public void Open_BackgroundWithNoActiveTab_BecomesActive()
    {
        IBrowserTab tab = tabs.Open("https://one.example", false);

        Assert.Same(tab, tabs.Active);
    }

    /// <summary>
    /// Closing every tab empties the window without opening a blank tab.
    /// </summary>
    [Fact]
    public void CloseAll_ClosesEveryBrowserAndLeavesNoTabs()
    {
        tabs.Open("https://one.example");
        tabs.Open("https://two.example");

        tabs.CloseAll();

        Assert.Empty(tabs.Items);
        Assert.Null(tabs.Active);
        created[0].Verify(t => t.Close(), Times.Once);
        created[1].Verify(t => t.Close(), Times.Once);
        factory.Verify(f => f.Create("about:blank"), Times.Never);
    }

    /// <summary>
    /// In a window that cannot switch between tabs, a request for a new tab loads in the current one.
    /// </summary>
    [Fact]
    public void NewTabRequested_SingleTab_NavigatesCurrentTab()
    {
        BrowserTabs single = new(factory.Object, true);
        single.Open("https://one.example");

        created[0].Raise(t => t.NewTabRequested += null, "https://popup.example", false);

        Assert.Single(single.Items);
        created[0].Verify(t => t.Navigate("https://popup.example"), Times.Once);
    }

    /// <summary>
    /// A closed tab can no longer open tabs.
    /// </summary>
    [Fact]
    public void NewTabRequested_AfterClose_IsIgnored()
    {
        IBrowserTab first = tabs.Open("https://one.example");
        tabs.Open("https://two.example");
        tabs.Close(first);

        created[0].Raise(t => t.NewTabRequested += null, "https://late.example", false);

        factory.Verify(f => f.Create("https://late.example"), Times.Never);
    }

    /// <summary>
    /// Shortcuts pressed in any tab are forwarded.
    /// </summary>
    [Fact]
    public void ShortcutPressed_IsForwarded()
    {
        List<ShortcutCommand> received = [];
        tabs.ShortcutPressed += received.Add;
        tabs.Open("https://one.example");

        created[0].Raise(t => t.ShortcutPressed += null, ShortcutCommand.NewTab);

        Assert.Equal([ShortcutCommand.NewTab], received);
    }

    private Mock<IBrowserTab> OpenIdle(TimeSpan idle, TabLock lockState = TabLock.Unlocked)
    {
        tabs.Open($"https://idle{created.Count}.example");
        Mock<IBrowserTab> mock = created[^1];
        mock.Setup(t => t.IdleFor).Returns(idle);
        mock.Setup(t => t.Lock).Returns(lockState);
        tabs.Open($"https://shown{created.Count}.example");
        return mock;
    }

    /// <summary>
    /// A tab that has only been out of view briefly is left running.
    /// </summary>
    [Fact]
    public void Cleanup_RecentTab_IsLeftAlone()
    {
        Mock<IBrowserTab> idle = OpenIdle(TimeSpan.FromMinutes(4));

        tabs.Cleanup(policy);

        idle.Verify(t => t.Freeze(), Times.Never);
        idle.Verify(t => t.Suspend(), Times.Never);
    }

    /// <summary>
    /// After the first wait a tab is frozen, not yet suspended.
    /// </summary>
    [Fact]
    public void Cleanup_PastFreezeTime_Freezes()
    {
        Mock<IBrowserTab> idle = OpenIdle(TimeSpan.FromMinutes(6));

        tabs.Cleanup(policy);

        idle.Verify(t => t.Freeze(), Times.Once);
        idle.Verify(t => t.Suspend(), Times.Never);
    }

    /// <summary>
    /// A tab that is already frozen is not frozen again.
    /// </summary>
    [Fact]
    public void Cleanup_AlreadyFrozen_IsNotFrozenAgain()
    {
        Mock<IBrowserTab> idle = OpenIdle(TimeSpan.FromMinutes(6));
        idle.Setup(t => t.IsFrozen).Returns(true);

        tabs.Cleanup(policy);

        idle.Verify(t => t.Freeze(), Times.Never);
    }

    /// <summary>
    /// After the second wait a tab is suspended.
    /// </summary>
    [Fact]
    public void Cleanup_PastSuspendTime_Suspends()
    {
        Mock<IBrowserTab> idle = OpenIdle(TimeSpan.FromMinutes(16));

        tabs.Cleanup(policy);

        idle.Verify(t => t.Suspend(), Times.Once);
        idle.Verify(t => t.Freeze(), Times.Never);
    }

    /// <summary>
    /// A tab that is already suspended is not suspended or frozen again.
    /// </summary>
    [Fact]
    public void Cleanup_AlreadySuspended_IsSkipped()
    {
        Mock<IBrowserTab> idle = OpenIdle(TimeSpan.FromHours(3));
        idle.Setup(t => t.IsSuspended).Returns(true);

        tabs.Cleanup(policy);

        idle.Verify(t => t.Suspend(), Times.Never);
        idle.Verify(t => t.Freeze(), Times.Never);
    }

    /// <summary>
    /// After the last wait a tab is closed.
    /// </summary>
    [Fact]
    public void Cleanup_PastCloseTime_ClosesTheTab()
    {
        Mock<IBrowserTab> idle = OpenIdle(TimeSpan.FromHours(25));

        tabs.Cleanup(policy);

        Assert.DoesNotContain(idle.Object, tabs.Items);
        idle.Verify(t => t.Close(), Times.Once);
    }

    /// <summary>
    /// A tab locked by the user, or locked automatically, is never touched however long it sits.
    /// </summary>
    /// <param name="byUser">Whether the user locked it, rather than the page locking it automatically.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Cleanup_LockedTab_IsLeftAlone(bool byUser)
    {
        Mock<IBrowserTab> idle = OpenIdle(TimeSpan.FromHours(30), byUser ? TabLock.Locked : TabLock.AutoLocked);

        tabs.Cleanup(policy);

        Assert.Contains(idle.Object, tabs.Items);
        idle.Verify(t => t.Freeze(), Times.Never);
        idle.Verify(t => t.Suspend(), Times.Never);
        idle.Verify(t => t.Close(), Times.Never);
    }

    /// <summary>
    /// The shown tab is never cleaned up, however long its clock says.
    /// </summary>
    [Fact]
    public void Cleanup_ShownTab_IsLeftAlone()
    {
        IBrowserTab shown = tabs.Open("https://shown.example");
        created[0].Setup(t => t.IdleFor).Returns(TimeSpan.FromHours(30));

        tabs.Cleanup(policy);

        Assert.Same(shown, tabs.Active);
        created[0].Verify(t => t.Close(), Times.Never);
        created[0].Verify(t => t.Suspend(), Times.Never);
    }
}
