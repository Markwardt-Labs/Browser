namespace Tests.Unit.Tabs;

/// <summary>
/// Tests for <see cref="AutoLock"/>.
/// </summary>
public sealed class AutoLockTests
{
    private readonly AutoLock autoLock = new();

    private PageReport Report(string id = "a", bool typed = false, bool uploading = false, bool audible = false)
        => new() { Id = id, Typed = typed, Uploading = uploading, Audible = audible };

    /// <summary>
    /// With nothing going on there is no reason to protect the tab.
    /// </summary>
    [Fact]
    public void Reasons_Initially_None()
        => Assert.Equal(AutoLockReason.None, autoLock.Reasons);

    /// <summary>
    /// Typed text, an upload and playing sound each give their own reason, and can add up.
    /// </summary>
    [Fact]
    public void Apply_BusyReport_GivesReasons()
    {
        Assert.True(autoLock.Apply(Report("a", typed: true)));
        Assert.True(autoLock.Apply(Report("b", uploading: true, audible: true)));

        Assert.Equal(AutoLockReason.Typing | AutoLockReason.Transfer | AutoLockReason.Sound, autoLock.Reasons);
    }

    /// <summary>
    /// A later report from the same frame replaces its earlier one, so the reason ends when the frame says so.
    /// </summary>
    [Fact]
    public void Apply_QuietReport_EndsTheReason()
    {
        autoLock.Apply(Report(typed: true));

        Assert.True(autoLock.Apply(Report()));
        Assert.Equal(AutoLockReason.None, autoLock.Reasons);
    }

    /// <summary>
    /// A report that changes nothing says so, so the tab does not announce a change.
    /// </summary>
    [Fact]
    public void Apply_SameState_ReportsNoChange()
    {
        autoLock.Apply(Report(typed: true));

        Assert.False(autoLock.Apply(Report(typed: true)));
        Assert.False(autoLock.Apply(Report("other")));
    }

    /// <summary>
    /// A running download protects the tab until it ends.
    /// </summary>
    [Fact]
    public void Download_RunningThenDone_HoldsThenReleases()
    {
        Assert.True(autoLock.Download(7, true));
        Assert.Equal(AutoLockReason.Transfer, autoLock.Reasons);

        Assert.True(autoLock.Download(7, false));
        Assert.Equal(AutoLockReason.None, autoLock.Reasons);
    }

    /// <summary>
    /// Two downloads need both to end.
    /// </summary>
    [Fact]
    public void Download_TwoRunning_NeedsBothToEnd()
    {
        autoLock.Download(1, true);
        autoLock.Download(2, true);
        autoLock.Download(1, false);

        Assert.Equal(AutoLockReason.Transfer, autoLock.Reasons);
    }

    /// <summary>
    /// Navigating forgets what the old document reported but not the downloads, which go on.
    /// </summary>
    [Fact]
    public void Reset_ForgetsFramesButKeepsDownloads()
    {
        autoLock.Apply(Report(typed: true, audible: true));
        autoLock.Download(1, true);

        Assert.True(autoLock.Reset());
        Assert.Equal(AutoLockReason.Transfer, autoLock.Reasons);
    }
}
