namespace Tests.Unit.Diagnostics;

/// <summary>
/// Tests for <see cref="VideoStats"/>.
/// </summary>
public sealed class VideoStatsTests
{
    private TimeSpan now = TimeSpan.FromSeconds(10);

    /// <summary>
    /// With diagnostics off there is no script and nothing is collected.
    /// </summary>
    [Fact]
    public void Disabled_HasNoScriptAndCollectsNothing()
    {
        VideoStats stats = new(false, () => now);

        stats.Note("a", Message(30, 0, 1920, 1080));

        Assert.Null(stats.Script);
        Assert.Null(stats.Latest());
    }

    /// <summary>
    /// The script is available when diagnostics are on.
    /// </summary>
    [Fact]
    public void Enabled_HasScript()
        => Assert.Contains("MWSTAT", new VideoStats(true, () => now).Script);

    /// <summary>
    /// A report is read, and messages that are not reports are ignored.
    /// </summary>
    [Fact]
    public void Note_ReadsReportsAndIgnoresOtherMessages()
    {
        VideoStats stats = new(true, () => now);

        stats.Note("a", "hello from the page");
        stats.Note("a", "MWSTAT {not json");
        stats.Note("a", "MWSTAT {\"f\":1}");
        Assert.Null(stats.Latest());

        stats.Note("a", Message(23.5, 3, 1280, 720));

        Assert.Equal(new VideoSnapshot(23.5, 3, 1280, 720, "visible", false), stats.Latest());
    }

    /// <summary>
    /// With several frames reporting, the largest video wins, and a report goes away after a few seconds.
    /// </summary>
    [Fact]
    public void Latest_PicksLargestAndExpires()
    {
        VideoStats stats = new(true, () => now);
        stats.Note("small", Message(60, 0, 320, 180));
        stats.Note("big", Message(30, 1, 1920, 1080));

        Assert.Equal(1920, stats.Latest()!.Width);

        now += TimeSpan.FromSeconds(6);

        Assert.Null(stats.Latest());
    }

    private string Message(double fps, int dropped, int width, int height)
        => $"MWSTAT {{\"f\":{fps.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"d\":{dropped},\"w\":{width},\"h\":{height},\"v\":\"visible\",\"x\":false}}";
}
