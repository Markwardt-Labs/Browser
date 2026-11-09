namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// Writes a log of how the browser is performing, for working out where lag comes from.
/// </summary>
internal interface IStatsRecorder
{
    /// <summary>
    /// Opens a new log file, writes a description of the environment to it, and takes a sample every two seconds until cancelled.
    /// </summary>
    /// <param name="header">Lines that describe the machine and the window.</param>
    /// <param name="cancellation">Stops recording.</param>
    /// <returns>A task that completes when recording stops.</returns>
    Task Start(IReadOnlyList<string> header, CancellationToken cancellation = default);

    /// <summary>
    /// Writes the requests file, replacing the previous one.
    /// </summary>
    /// <param name="cancellation">Cancels the write.</param>
    /// <returns>A task that completes when the file is written.</returns>
    Task WriteRequests(CancellationToken cancellation = default);

    /// <summary>
    /// Takes one sample and appends it to the log.
    /// </summary>
    /// <param name="cancellation">Cancels the write.</param>
    /// <returns>A task that completes when the line is written.</returns>
    Task Record(CancellationToken cancellation = default);
}

/// <inheritdoc />
/// <param name="frames">Counts the frames delivered.</param>
/// <param name="cpu">Reads CPU use.</param>
/// <param name="ui">Measures the UI thread.</param>
/// <param name="folder">The folder the logs go in.</param>
/// <param name="context">Gets what is going on around the window: the page, focus and where the pointer is. Called from a background thread.</param>
/// <param name="requests">Counts what the ad blocker decided.</param>
/// <param name="video">Collects what pages report about their playing video.</param>
/// <param name="maxBytes">The size at which the log is closed and a new one started, so that a browser left open for days does not fill the disk.</param>
internal sealed class StatsRecorder(IFrameStats frames, IProcessCpuSampler cpu, IUiProbe ui, string folder, Func<Task<StatsContext>> context, IRequestStats requests, IVideoStats video, long maxBytes = 20_000_000) : IStatsRecorder
{
    private readonly int keep = 10;
    private readonly long start = Stopwatch.GetTimestamp();
    private string path = string.Empty;
    private IReadOnlyList<string> header = [];
    private int rollovers;
    private TimeSpan lastTime;
    private int[] lastCollections = [0, 0, 0];
    private TimeSpan lastPause;
    private long lastAllocated;

    /// <inheritdoc />
    public async Task Start(IReadOnlyList<string> header, CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(folder);
        this.header = header;
        await Open(cancellation);
        lastTime = Stopwatch.GetElapsedTime(start);
        cpu.Take();
        lastCollections = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        lastPause = GC.GetTotalPauseDuration();
        lastAllocated = GC.GetTotalAllocatedBytes();

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(2));

        try
        {
            int ticks = 0;

            while (await timer.WaitForNextTickAsync(cancellation))
            {
                await Record(cancellation);

                if (++ticks % 5 == 0)
                {
                    await WriteRequests(cancellation);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <inheritdoc />
    public async Task WriteRequests(CancellationToken cancellation = default)
    {
        if (!requests.IsEnabled || path.Length == 0)
        {
            return;
        }

        IEnumerable<string> lines = requests.Snapshot().Select(entry => $"{entry.Count}\t{(entry.Blocked ? "BLOCKED" : "allowed")}\t{entry.Type}\t{entry.Page}\t{entry.Host}");

        try
        {
            await File.WriteAllLinesAsync(RequestsPath(path), ["# count, decision, kind, page host, request host (blocked requests, and requests to other sites that were allowed)", .. lines], cancellation);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <inheritdoc />
    public async Task Record(CancellationToken cancellation = default)
    {
        TimeSpan now = Stopwatch.GetElapsedTime(start);
        double seconds = Math.Max((now - lastTime).TotalSeconds, 0.001);
        lastTime = now;
        FrameStatsSnapshot frame = frames.Take(out bool hookFailed);
        UiProbeSnapshot stall = ui.Take();
        IReadOnlyDictionary<string, long> usedCpu = cpu.Take();
        int[] collections = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        TimeSpan pause = GC.GetTotalPauseDuration();
        long allocated = GC.GetTotalAllocatedBytes();
        StringBuilder line = new();
        line.Append(CultureInfo.InvariantCulture, $"{DateTime.Now:HH:mm:ss} dt={seconds:F1}s");
        line.Append(CultureInfo.InvariantCulture, $"{(hookFailed ? " painthook=FAILED" : string.Empty)} paints={frame.Paints / seconds:F1}/s");
        line.Append(CultureInfo.InvariantCulture, $" frame={frame.Width}x{frame.Height} dirty={(frame.Paints == 0 ? 0 : frame.DirtyPixels / (double)frame.Paints / 1e6):F2}Mpx/frame big={frame.BigPaints / seconds:F1}/s");
        line.Append(CultureInfo.InvariantCulture, $" gaps={frame.Gaps} maxgap={frame.MaxGapMilliseconds:F0}ms paintwait={frame.AverageDelayMilliseconds:F1}/{frame.MaxDelayMilliseconds:F1}ms");
        line.Append(CultureInfo.InvariantCulture, $" painttick={frame.AverageTickMilliseconds:F1}/{frame.MaxTickMilliseconds:F1}ms uistall={stall.MaxLateMilliseconds:F0}ms uistalls={stall.Stalls}");

        foreach ((string kind, long ticks) in usedCpu.OrderBy(pair => pair.Key))
        {
            line.Append(CultureInfo.InvariantCulture, $" cpu.{kind}={ticks / seconds:F0}%");
        }

        line.Append(CultureInfo.InvariantCulture, $" gpu-restarts={cpu.GpuRestarts} gc={collections[0] - lastCollections[0]}/{collections[1] - lastCollections[1]}/{collections[2] - lastCollections[2]} gcpause={(pause - lastPause).TotalMilliseconds:F0}ms alloc={(allocated - lastAllocated) / seconds / 1048576:F1}MB/s ws={Environment.WorkingSet / 1048576}MB");
        StatsContext around = await context();
        line.Append(CultureInfo.InvariantCulture, $" active={(around.IsActive ? "yes" : "no")} pointer={around.Pointer} idle={around.PointerIdleSeconds}s screen={around.Screen}");

        if (video.Latest() is { } playing)
        {
            line.Append(CultureInfo.InvariantCulture, $" video={playing.PresentedFps:F1}fps dropped={playing.Dropped} vres={playing.Width}x{playing.Height} pagevis={playing.Visibility} pagefocus={(playing.HasFocus ? "yes" : "no")}");
        }

        line.Append(CultureInfo.InvariantCulture, $" page={around.Page}");
        lastCollections = collections;
        lastPause = pause;
        lastAllocated = allocated;

        try
        {
            if (new FileInfo(path).Length >= maxBytes)
            {
                await Open(cancellation);
            }

            await File.AppendAllTextAsync(path, line.AppendLine().ToString(), cancellation);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private string RequestsPath(string statsPath)
        => Path.Combine(Path.GetDirectoryName(statsPath)!, Path.GetFileName(statsPath).Replace("stats-", "requests-", StringComparison.Ordinal));

    private async Task Open(CancellationToken cancellation = default)
    {
        string suffix = rollovers == 0 ? string.Empty : $"-{rollovers}";
        path = Path.Combine(folder, $"stats-{DateTime.Now:yyyyMMdd-HHmmss}{suffix}.log");
        rollovers++;

        foreach (string old in Directory.GetFiles(folder, "stats-*.log").Where(file => file != path).OrderByDescending(file => file).Skip(keep - 1))
        {
            File.Delete(old);
            File.Delete(RequestsPath(old));
        }

        await File.WriteAllLinesAsync(path, header.Select(line => $"# {line}"), cancellation);
    }
}
