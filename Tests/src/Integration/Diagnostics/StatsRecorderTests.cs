namespace Tests.Integration.Diagnostics;

/// <summary>
/// Tests for <see cref="StatsRecorder"/> against a real folder.
/// </summary>
public sealed class StatsRecorderTests : IDisposable
{
    /// <summary>
    /// Creates the recorder over mocks that report a busy video page.
    /// </summary>
    public StatsRecorderTests()
    {
        bool failed = false;
        frames.Setup(f => f.Take(out failed)).Returns(new FrameStatsSnapshot(120, 240_000_000, 1920, 1080, 2, 40, 10, 1.5, 9, 8, 3.25, 21, 30));
        cpu.Setup(c => c.Take()).Returns(new Dictionary<string, long> { ["browser"] = 0, ["gpu-process"] = 0 });
        ui.Setup(u => u.Take()).Returns(new UiProbeSnapshot(70, 1));
        cpu.Setup(c => c.GpuRestarts).Returns(2);
        requests.Setup(r => r.IsEnabled).Returns(true);
        requests.Setup(r => r.Snapshot()).Returns([new RequestStatsEntry("example.com", "ads.example.net", "Script", true, 7), new RequestStatsEntry("example.com", "cdn.example.net", "Image", false, 2)]);
        recorder = new StatsRecorder(frames.Object, cpu.Object, ui.Object, folder, () => Task.FromResult(new StatsContext("example.com", false, "other-screen", 1, 7)), requests.Object, video.Object);
    }

    private readonly string folder = Path.Combine(Path.GetTempPath(), $"stats-{Guid.NewGuid():N}");
    private readonly Mock<IFrameStats> frames = new();
    private readonly Mock<IProcessCpuSampler> cpu = new();
    private readonly Mock<IUiProbe> ui = new();
    private readonly Mock<IRequestStats> requests = new();
    private readonly Mock<IVideoStats> video = new();
    private readonly StatsRecorder recorder;

    /// <summary>
    /// Deletes the test folder.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>
    /// Starting writes the header as comments, and a sample is one line of readable fields.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Start_WritesHeaderThenSamples()
    {
        using CancellationTokenSource stop = new();
        Task running = recorder.Start(["machine one", "screen 1920x1080"], stop.Token);
        await Task.Delay(100);
        cpu.Setup(c => c.Take()).Returns(new Dictionary<string, long> { ["browser"] = 300, ["gpu-process"] = 120 });
        await recorder.Record();
        await stop.CancelAsync();
        await running;

        string[] lines = await File.ReadAllLinesAsync(Directory.GetFiles(folder).Single());
        Assert.Equal("# machine one", lines[0]);
        Assert.Equal("# screen 1920x1080", lines[1]);
        string sample = lines[2];
        Assert.Contains("paints=", sample);
        Assert.Contains("frame=1920x1080", sample);
        Assert.Contains("dirty=2.00Mpx/frame", sample);
        Assert.Contains("gaps=2 maxgap=40ms paintwait=1.5/9.0ms", sample);
        Assert.Contains("painttick=3.2/21.0ms uistall=70ms uistalls=1", sample);
        Assert.Contains("active=no pointer=other-screen idle=7s screen=1", sample);
        Assert.Matches(@"big=\d+\.\d/s", sample);
        Assert.DoesNotContain("video=", sample);
        Assert.Contains("gpu-restarts=2", sample);
        Assert.Matches(@"cpu\.browser=\d+%", sample);
        Assert.Matches(@"cpu\.gpu-process=\d+%", sample);
        Assert.EndsWith("page=example.com", sample);
    }

    /// <summary>
    /// What the ad blocker decided is written to a file of its own that sits beside the log.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task WriteRequests_WritesCountsBesideLog()
    {
        using CancellationTokenSource stop = new();
        Task running = recorder.Start([], stop.Token);
        await Task.Delay(100);
        await recorder.WriteRequests();
        await stop.CancelAsync();
        await running;

        string file = Directory.GetFiles(folder, "requests-*.log").Single();
        string[] lines = await File.ReadAllLinesAsync(file);
        Assert.Equal("7\tBLOCKED\tScript\texample.com\tads.example.net", lines[1]);
        Assert.Equal("2\tallowed\tImage\texample.com\tcdn.example.net", lines[2]);
    }

    /// <summary>
    /// What a playing video reports about itself is added to the sample.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Record_PlayingVideo_IsIncluded()
    {
        video.Setup(v => v.Latest()).Returns(new VideoSnapshot(23.9, 4, 1920, 1080, "visible", false));
        using CancellationTokenSource stop = new();
        Task running = recorder.Start([], stop.Token);
        await Task.Delay(100);
        await recorder.Record();
        await stop.CancelAsync();
        await running;

        string sample = (await File.ReadAllLinesAsync(Directory.GetFiles(folder, "stats-*.log").Single()))[0];
        Assert.Contains("video=23.9fps dropped=4 vres=1920x1080 pagevis=visible pagefocus=no", sample);
        Assert.EndsWith("page=example.com", sample);
    }

    /// <summary>
    /// A failure to observe frames is visible in the log.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Record_HookFailed_IsFlagged()
    {
        bool failed = true;
        frames.Setup(f => f.Take(out failed)).Returns(new FrameStatsSnapshot(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        using CancellationTokenSource stop = new();
        Task running = recorder.Start([], stop.Token);
        await Task.Delay(100);
        await recorder.Record();
        await stop.CancelAsync();
        await running;

        Assert.Contains("painthook=FAILED", await File.ReadAllTextAsync(Directory.GetFiles(folder).Single()));
    }

    /// <summary>
    /// A log that has reached its size limit is closed and a new one started with the same header.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Record_PastSizeLimit_StartsNewLogWithHeader()
    {
        StatsRecorder small = new(frames.Object, cpu.Object, ui.Object, folder, () => Task.FromResult(new StatsContext("example.com", true, "over", 0, 0)), requests.Object, video.Object, 600);
        using CancellationTokenSource stop = new();
        Task running = small.Start(["machine one"], stop.Token);
        await Task.Delay(100);

        for (int index = 0; index < 4; index++)
        {
            await small.Record();
        }

        await stop.CancelAsync();
        await running;

        string[] files = Directory.GetFiles(folder, "stats-*.log");
        Assert.True(files.Length >= 2);
        Assert.All(files, file => Assert.Equal("# machine one", File.ReadLines(file).First()));
    }

    /// <summary>
    /// Only the newest logs are kept.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Start_KeepsOnlyNewestLogs()
    {
        Directory.CreateDirectory(folder);

        for (int index = 0; index < 12; index++)
        {
            await File.WriteAllTextAsync(Path.Combine(folder, $"stats-2020010{index % 10}-0000{index:00}.log"), string.Empty);
            await File.WriteAllTextAsync(Path.Combine(folder, $"requests-2020010{index % 10}-0000{index:00}.log"), string.Empty);
        }

        using CancellationTokenSource stop = new();
        Task running = recorder.Start([], stop.Token);
        await Task.Delay(100);
        await stop.CancelAsync();
        await running;

        Assert.Equal(10, Directory.GetFiles(folder, "stats-*.log").Length);
        Assert.Equal(9, Directory.GetFiles(folder, "requests-*.log").Length);
    }
}
