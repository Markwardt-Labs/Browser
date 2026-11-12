namespace Tests.Unit.Tabs;

/// <summary>
/// Tests for <see cref="VirtualHistory"/>.
/// </summary>
public sealed class VirtualHistoryTests
{
    private readonly VirtualHistory history = new();
    private readonly HistoryEntry a = new("https://a.example", "A");
    private readonly HistoryEntry b = new("https://b.example", "B");
    private readonly HistoryEntry c = new("https://c.example", "C");
    private readonly HistoryEntry d = new("https://d.example", "D");

    /// <summary>
    /// Loading splits a history into the pages behind and ahead of the current one.
    /// </summary>
    [Fact]
    public void Load_SplitsAtTheCurrentPage()
    {
        history.Load([a, b, c, d], 2);

        Assert.Equal(2, history.BackCount);
        Assert.Equal(1, history.ForwardCount);
    }

    /// <summary>
    /// An index outside the history leaves nothing kept.
    /// </summary>
    [Fact]
    public void Load_BadIndex_KeepsNothing()
    {
        history.Load([a, b], 5);

        Assert.Equal(0, history.BackCount);
        Assert.Equal(0, history.ForwardCount);
    }

    /// <summary>
    /// Either history can supply a page to go back or forward to.
    /// </summary>
    [Fact]
    public void CanGo_ConsidersBothHistories()
    {
        Assert.False(history.CanGoBack(false));
        Assert.True(history.CanGoBack(true));
        Assert.False(history.CanGoForward(false));

        history.Load([a, b, c], 1);

        Assert.True(history.CanGoBack(false));
        Assert.True(history.CanGoForward(false));
    }

    /// <summary>
    /// Going back prefers the browser's own history, which has the nearer pages.
    /// </summary>
    [Fact]
    public void Back_NativeAvailable_UsesNative()
    {
        history.Load([a, b, c], 2);

        Assert.Equal(HistoryMove.Native, history.Back(true, c, out HistoryEntry? entry));
        Assert.Null(entry);
        Assert.Equal(2, history.BackCount);
    }

    /// <summary>
    /// Without a native page behind, going back takes the newest kept page and keeps the one left for going forward.
    /// </summary>
    [Fact]
    public void Back_ThenForward_WalksTheKeptPages()
    {
        history.Load([a, b, c], 2);

        Assert.Equal(HistoryMove.Restored, history.Back(false, c, out HistoryEntry? first));
        Assert.Equal(b, first);
        Assert.Equal(HistoryMove.Restored, history.Back(false, b, out HistoryEntry? second));
        Assert.Equal(a, second);
        Assert.Equal(HistoryMove.None, history.Back(false, a, out _));

        Assert.Equal(HistoryMove.Restored, history.Forward(false, a, out HistoryEntry? third));
        Assert.Equal(b, third);
        Assert.Equal(HistoryMove.Restored, history.Forward(false, b, out HistoryEntry? fourth));
        Assert.Equal(c, fourth);
        Assert.Equal(HistoryMove.None, history.Forward(false, c, out _));
    }

    /// <summary>
    /// Going forward prefers the kept pages, which are nearer than any the browser has ahead.
    /// </summary>
    [Fact]
    public void Forward_KeptPageAndNative_UsesKeptFirst()
    {
        history.Load([a, b, c], 0);

        Assert.Equal(HistoryMove.Restored, history.Forward(true, a, out HistoryEntry? entry));
        Assert.Equal(b, entry);
        Assert.Equal(HistoryMove.Restored, history.Forward(true, b, out _));
        Assert.Equal(HistoryMove.Native, history.Forward(true, c, out _));
    }

    /// <summary>
    /// Navigating somewhere new drops the pages ahead.
    /// </summary>
    [Fact]
    public void ClearForward_DropsOnlyPagesAhead()
    {
        history.Load([a, b, c], 1);
        history.ClearForward();

        Assert.Equal(1, history.BackCount);
        Assert.Equal(0, history.ForwardCount);
    }

    /// <summary>
    /// Composing puts the kept pages around the browser's own, in order, with the right page current.
    /// </summary>
    [Fact]
    public void Compose_PutsKeptPagesAroundNative()
    {
        history.Load([a, b, c], 1);

        IReadOnlyList<HistoryEntry> whole = history.Compose([b, d], 0, out int index);

        Assert.Equal([a, b, c, d], whole);
        Assert.Equal(1, index);
    }

    /// <summary>
    /// With nothing kept, composing returns the browser's history unchanged.
    /// </summary>
    [Fact]
    public void Compose_NothingKept_ReturnsNative()
    {
        IReadOnlyList<HistoryEntry> whole = history.Compose([a, b], 1, out int index);

        Assert.Equal([a, b], whole);
        Assert.Equal(1, index);
    }
}
