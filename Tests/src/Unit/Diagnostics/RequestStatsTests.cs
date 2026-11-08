namespace Tests.Unit.Diagnostics;

/// <summary>
/// Tests for <see cref="RequestStats"/>.
/// </summary>
public sealed class RequestStatsTests
{
    /// <summary>
    /// Nothing is counted when diagnostics are off.
    /// </summary>
    [Fact]
    public void Record_Disabled_CountsNothing()
    {
        RequestStats stats = new(false);

        stats.Record(true, "Script", "a.com", "b.net");

        Assert.Empty(stats.Snapshot());
    }

    /// <summary>
    /// The same request kind to the same host from the same page adds up, a different decision counts apart, and the most frequent come first.
    /// </summary>
    [Fact]
    public void Snapshot_GroupsAndOrders()
    {
        RequestStats stats = new(true);
        stats.Record(true, "Script", "a.com", "ads.net");
        stats.Record(false, "Image", "a.com", "cdn.net");
        stats.Record(false, "Image", "a.com", "cdn.net");
        stats.Record(false, "Image", "a.com", "cdn.net");
        stats.Record(false, "Script", "a.com", "ads.net");

        IReadOnlyList<RequestStatsEntry> snapshot = stats.Snapshot();

        Assert.Equal(3, snapshot.Count);
        Assert.Equal(new RequestStatsEntry("a.com", "cdn.net", "Image", false, 3), snapshot[0]);
        Assert.Contains(new RequestStatsEntry("a.com", "ads.net", "Script", true, 1), snapshot);
        Assert.Contains(new RequestStatsEntry("a.com", "ads.net", "Script", false, 1), snapshot);
    }
}
