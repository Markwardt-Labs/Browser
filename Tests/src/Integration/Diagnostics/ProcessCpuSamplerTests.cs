namespace Tests.Integration.Diagnostics;

/// <summary>
/// Tests for <see cref="ProcessCpuSampler"/> over a fake process list.
/// </summary>
public sealed class ProcessCpuSamplerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"proc-{Guid.NewGuid():N}");

    /// <summary>
    /// Deletes the fake process list.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    /// <summary>
    /// The browser's own time and each kind of helper's time are summed, nested helpers count, and unrelated processes do not.
    /// </summary>
    [Fact]
    public void Sample_SumsFamilyByKind()
    {
        Add(100, 1, 30, 20, "app");
        Add(101, 100, 10, 5, "app\0--type=gpu-process\0--x");
        Add(102, 100, 7, 3, "app\0--type=renderer");
        Add(103, 100, 4, 1, "app\0--type=renderer");
        Add(104, 101, 2, 2, "app\0--type=utility");
        Add(105, 1, 999, 999, "unrelated");

        IReadOnlyDictionary<string, long> sample = new ProcessCpuSampler(root, 100).Take();

        Assert.Equal(50, sample["browser"]);
        Assert.Equal(15, sample["gpu-process"]);
        Assert.Equal(15, sample["renderer"]);
        Assert.Equal(4, sample["utility"]);
        Assert.Equal(4, sample.Count);
    }

    /// <summary>
    /// A process whose name contains spaces and brackets is still read correctly.
    /// </summary>
    [Fact]
    public void Sample_NameWithParentheses_IsParsed()
    {
        Directory.CreateDirectory(Path.Combine(root, "100"));
        File.WriteAllText(Path.Combine(root, "100", "stat"), "100 (my (odd) app) S 1 0 0 0 0 0 0 0 0 0 11 22 0 0 0 0 0");
        File.WriteAllText(Path.Combine(root, "100", "cmdline"), "app");

        Assert.Equal(33, new ProcessCpuSampler(root, 100).Take()["browser"]);
    }

    /// <summary>
    /// Each reading is the time used since the previous one, and a helper that exits does not take time away.
    /// </summary>
    [Fact]
    public void Take_ReturnsTimeSincePreviousReading()
    {
        Add(100, 1, 30, 20, "app");
        Add(101, 100, 10, 5, "app\0--type=gpu-process");
        ProcessCpuSampler sampler = new(root, 100);
        sampler.Take();

        Add(100, 1, 40, 20, "app");
        Add(101, 100, 10, 15, "app\0--type=gpu-process");
        IReadOnlyDictionary<string, long> second = sampler.Take();

        Directory.Delete(Path.Combine(root, "101"), true);
        Add(102, 100, 3, 1, "app\0--type=renderer");
        IReadOnlyDictionary<string, long> third = sampler.Take();

        Assert.Equal(10, second["browser"]);
        Assert.Equal(10, second["gpu-process"]);
        Assert.False(third.ContainsKey("gpu-process"));
        Assert.Equal(4, third["renderer"]);
        Assert.Equal(0, third["browser"]);
    }

    /// <summary>
    /// A new GPU process appearing after the first one is counted as a restart.
    /// </summary>
    [Fact]
    public void GpuRestarts_CountsReplacedGpuProcesses()
    {
        Add(100, 1, 1, 1, "app");
        Add(101, 100, 1, 1, "app\0--type=gpu-process");
        ProcessCpuSampler sampler = new(root, 100);
        sampler.Take();
        Assert.Equal(0, sampler.GpuRestarts);

        Directory.Delete(Path.Combine(root, "101"), true);
        Add(102, 100, 1, 1, "app\0--type=gpu-process");
        sampler.Take();
        Directory.Delete(Path.Combine(root, "102"), true);
        Add(103, 100, 1, 1, "app\0--type=gpu-process");
        sampler.Take();
        sampler.Take();

        Assert.Equal(2, sampler.GpuRestarts);
    }

    private void Add(int id, int parent, int user, int system, string commandLine)
    {
        string folder = Path.Combine(root, id.ToString());
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "stat"), $"{id} (app) S {parent} 0 0 0 0 0 0 0 0 0 {user} {system} 0 0 0 0 0");
        File.WriteAllText(Path.Combine(folder, "cmdline"), commandLine);
    }
}
