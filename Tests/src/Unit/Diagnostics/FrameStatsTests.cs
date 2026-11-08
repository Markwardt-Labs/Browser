namespace Tests.Unit.Diagnostics;

/// <summary>
/// Tests for <see cref="FrameStats"/>.
/// </summary>
public sealed class FrameStatsTests
{
    private readonly List<Action> posted = [];
    private TimeSpan now = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Nothing is counted when diagnostics are off.
    /// </summary>
    [Fact]
    public void Painted_Disabled_CountsNothing()
    {
        FrameStats stats = Create(false);

        stats.Painted(100, 100, 5);

        Assert.Equal(0, stats.Take(out _).Paints);
        Assert.Empty(posted);
    }

    /// <summary>
    /// Frames, their changed area and the latest size are counted, and taking resets the counts.
    /// </summary>
    [Fact]
    public void Take_ReturnsCountsAndResets()
    {
        FrameStats stats = Create(true);
        stats.Painted(1920, 1080, 100);
        now += TimeSpan.FromMilliseconds(16);
        stats.Painted(1920, 1080, 250);

        FrameStatsSnapshot first = stats.Take(out bool failed);
        FrameStatsSnapshot second = stats.Take(out _);

        Assert.False(failed);
        Assert.Equal(2, first.Paints);
        Assert.Equal(350, first.DirtyPixels);
        Assert.Equal((1920, 1080), (first.Width, first.Height));
        Assert.Equal(0, second.Paints);
        Assert.Equal(0, second.DirtyPixels);
    }

    /// <summary>
    /// A frame that comes more than 25 ms after the previous one counts as a gap, but a long pause (nothing was changing) does not.
    /// </summary>
    [Fact]
    public void Painted_CountsGapsOnlyWhileFramesFlow()
    {
        FrameStats stats = Create(true);
        stats.Painted(10, 10, 1);
        now += TimeSpan.FromMilliseconds(16);
        stats.Painted(10, 10, 1);
        now += TimeSpan.FromMilliseconds(60);
        stats.Painted(10, 10, 1);
        now += TimeSpan.FromSeconds(5);
        stats.Painted(10, 10, 1);

        FrameStatsSnapshot snapshot = stats.Take(out _);

        Assert.Equal(1, snapshot.Gaps);
        Assert.Equal(60, snapshot.MaxGapMilliseconds, 0.5);
    }

    /// <summary>
    /// How long the UI thread takes to pick up a frame is measured, with one measurement outstanding at a time.
    /// </summary>
    [Fact]
    public void Painted_MeasuresUiDelay()
    {
        FrameStats stats = Create(true);
        stats.Painted(10, 10, 1);
        stats.Painted(10, 10, 1);
        Assert.Single(posted);

        now += TimeSpan.FromMilliseconds(30);
        posted[0]();
        stats.Painted(10, 10, 1);
        now += TimeSpan.FromMilliseconds(10);
        posted[1]();

        FrameStatsSnapshot snapshot = stats.Take(out _);
        Assert.Equal(2, snapshot.Delays);
        Assert.Equal(20, snapshot.AverageDelayMilliseconds, 0.5);
        Assert.Equal(30, snapshot.MaxDelayMilliseconds, 0.5);
    }

    /// <summary>
    /// A failure to observe frames is reported.
    /// </summary>
    [Fact]
    public void HookFailed_IsReported()
    {
        FrameStats stats = Create(true);

        stats.HookFailed();
        stats.Take(out bool failed);

        Assert.True(failed);
    }

    private FrameStats Create(bool enabled)
        => new(enabled, posted.Add, () => now);
}
