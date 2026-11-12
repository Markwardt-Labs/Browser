namespace Tests.Unit.Tabs;

/// <summary>
/// Tests for <see cref="PageReport"/> and <see cref="PageRestore"/>.
/// </summary>
public sealed class PageReportTests
{
    private readonly PageRestore restore = new();

    /// <summary>
    /// A full report is read, flags and positions included.
    /// </summary>
    [Fact]
    public void TryParse_FullReport_ReadsEverything()
    {
        bool ok = PageReport.TryParse("MWSTATE {\"id\":\"x1\",\"top\":true,\"typed\":true,\"uploading\":false,\"audible\":true,\"scroll\":[3,420],\"video\":{\"t\":61.5,\"d\":300.2,\"p\":true}}", out PageReport report);

        Assert.True(ok);
        Assert.Equal("x1", report.Id);
        Assert.True(report.IsTop);
        Assert.True(report.Typed);
        Assert.False(report.Uploading);
        Assert.True(report.Audible);
        Assert.Equal(3, report.ScrollX);
        Assert.Equal(420, report.ScrollY);
        Assert.Equal(new MediaPosition(61.5, 300.2, true), report.Media);
        Assert.True(report.IsBusy);
    }

    /// <summary>
    /// Anything that is not a well formed report is refused rather than guessed at.
    /// </summary>
    /// <param name="message">The console message.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("MWSTAT {\"f\":60}")]
    [InlineData("MWSTATE not json")]
    [InlineData("MWSTATE []")]
    [InlineData("MWSTATE {\"top\":true}")]
    [InlineData("MWSTATE {\"id\":\"x\",\"video\":{\"t\":1}}")]
    public void TryParse_NotAReport_ReturnsFalse(string? message)
        => Assert.False(PageReport.TryParse(message, out _));

    /// <summary>
    /// A quiet frame report is not busy.
    /// </summary>
    [Fact]
    public void IsBusy_QuietReport_IsFalse()
    {
        PageReport.TryParse("MWSTATE {\"id\":\"x\"}", out PageReport report);

        Assert.False(report.IsBusy);
    }

    /// <summary>
    /// A top frame report with a scroll position and a video becomes a script carrying both.
    /// </summary>
    [Fact]
    public void Script_ScrollAndVideo_CarriesBoth()
    {
        PageReport report = new() { Id = "x", IsTop = true, ScrollX = 0, ScrollY = 1200, Media = new MediaPosition(61.5, 300.25, false) };

        string? script = restore.Script(report);

        Assert.NotNull(script);
        Assert.Contains("scroll:[0,1200]", script);
        Assert.Contains("video:{t:61.5,d:300.25}", script);
        Assert.DoesNotContain("__STATE__", script);
    }

    /// <summary>
    /// A page that was at the top with no video needs nothing restored, and a subframe's report is never used.
    /// </summary>
    [Fact]
    public void Script_NothingToRestore_IsNull()
    {
        Assert.Null(restore.Script(new PageReport { Id = "x", IsTop = true }));
        Assert.Null(restore.Script(new PageReport { Id = "x", IsTop = false, ScrollY = 50 }));
    }
}
