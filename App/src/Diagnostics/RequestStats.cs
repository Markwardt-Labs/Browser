namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// How often one kind of request to one host, made by one site, was blocked or let through.
/// </summary>
/// <param name="Page">The host of the page that made the request.</param>
/// <param name="Host">The host the request went to.</param>
/// <param name="Type">The kind of request.</param>
/// <param name="Blocked">Whether it was blocked.</param>
/// <param name="Count">How many times.</param>
internal sealed record RequestStatsEntry(string Page, string Host, string Type, bool Blocked, int Count);

/// <summary>
/// Counts what the ad blocker blocked and what it let through to other sites, so that what slips past it can be found.
/// </summary>
internal interface IRequestStats
{
    /// <summary>
    /// Gets a value indicating whether anything is being counted.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Counts a request.
    /// </summary>
    /// <param name="blocked">Whether it was blocked.</param>
    /// <param name="type">The kind of request.</param>
    /// <param name="pageHost">The host of the page that made it.</param>
    /// <param name="host">The host it went to.</param>
    void Record(bool blocked, string type, string pageHost, string host);

    /// <summary>
    /// Gets everything counted so far.
    /// </summary>
    /// <returns>The counts, the most frequent first.</returns>
    IReadOnlyList<RequestStatsEntry> Snapshot();
}

/// <inheritdoc />
/// <param name="isEnabled">Whether to count at all; when false every call does nothing.</param>
internal sealed class RequestStats(bool isEnabled) : IRequestStats
{
    private readonly ConcurrentDictionary<(string Page, string Host, string Type, bool Blocked), int> counts = new();
    private readonly int limit = 5000;

    /// <inheritdoc />
    public bool IsEnabled => isEnabled;

    /// <inheritdoc />
    public void Record(bool blocked, string type, string pageHost, string host)
    {
        if (!isEnabled || (counts.Count >= limit && !counts.ContainsKey((pageHost, host, type, blocked))))
        {
            return;
        }

        counts.AddOrUpdate((pageHost, host, type, blocked), 1, (_, count) => count + 1);
    }

    /// <inheritdoc />
    public IReadOnlyList<RequestStatsEntry> Snapshot()
        => [.. counts.Select(pair => new RequestStatsEntry(pair.Key.Page, pair.Key.Host, pair.Key.Type, pair.Key.Blocked, pair.Value)).OrderByDescending(entry => entry.Count)];
}
