namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// Collects the reports that a script in each page sends about its playing video, which says whether Chromium produced the frames, separately from whether the window showed them.
/// </summary>
internal interface IVideoStats
{
    /// <summary>
    /// Gets the script to run in every frame, or null when diagnostics are off.
    /// </summary>
    string? Script { get; }

    /// <summary>
    /// Takes in a console message from a page, if it is one of the script's reports. Safe to call from any thread.
    /// </summary>
    /// <param name="source">The address of the script that wrote the message, which tells frames apart.</param>
    /// <param name="message">The message.</param>
    void Note(string source, string message);

    /// <summary>
    /// Gets the report of the video that is largest right now.
    /// </summary>
    /// <returns>The report, or null when no video reported in the last few seconds.</returns>
    VideoSnapshot? Latest();
}

/// <inheritdoc />
/// <param name="isEnabled">Whether to collect at all.</param>
/// <param name="clock">Reads a clock that only moves forward.</param>
internal sealed class VideoStats(bool isEnabled, Func<TimeSpan> clock) : IVideoStats
{
    private readonly string prefix = "MWSTAT ";
    private readonly TimeSpan lifetime = TimeSpan.FromSeconds(5);
    private readonly Lock gate = new();
    private readonly Dictionary<string, (VideoSnapshot Snapshot, TimeSpan At)> reports = [];
    private readonly string? script = isEnabled ? typeof(VideoStats).Assembly.ReadText("Markwardt.Browser.Diagnostics.VideoStats.js") : null;

    /// <inheritdoc />
    public string? Script => script;

    /// <inheritdoc />
    public void Note(string source, string message)
    {
        if (!isEnabled || !message.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(message[prefix.Length..]);
            JsonElement root = document.RootElement;
            VideoSnapshot snapshot = new(root.GetProperty("f").GetDouble(), root.GetProperty("d").GetInt32(), root.GetProperty("w").GetInt32(), root.GetProperty("h").GetInt32(), root.GetProperty("v").GetString() ?? string.Empty, root.GetProperty("x").GetBoolean());

            using (gate.EnterScope())
            {
                reports[source] = (snapshot, clock());
            }
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
        }
    }

    /// <inheritdoc />
    public VideoSnapshot? Latest()
    {
        TimeSpan now = clock();

        using (gate.EnterScope())
        {
            return reports.Values.Where(report => now - report.At <= lifetime).OrderByDescending(report => (long)report.Snapshot.Width * report.Snapshot.Height).Select(report => report.Snapshot).FirstOrDefault();
        }
    }
}
