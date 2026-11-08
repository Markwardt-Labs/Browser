namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// Reads how much CPU time the browser and its helper processes have used.
/// </summary>
internal interface IProcessCpuSampler
{
    /// <summary>
    /// Reads the CPU time used since the previous call. The first call returns everything used since the processes started, so it is only good for starting from.
    /// </summary>
    /// <returns>For the browser process ("browser") and each kind of helper process ("gpu-process", "renderer", "utility", ...), the CPU time used, in hundredths of a second. A helper that exited since the previous call counts for nothing, rather than taking its earlier time away.</returns>
    IReadOnlyDictionary<string, long> Take();

    /// <summary>
    /// Gets how many times the GPU process was replaced since the first reading: Chromium restarts it when it crashes or loses its context, which shows on screen as the page freezing and then repainting.
    /// </summary>
    int GpuRestarts { get; }
}

/// <inheritdoc />
/// <param name="procRoot">The folder of the process list, normally /proc.</param>
/// <param name="selfId">The process the helpers belong to.</param>
internal sealed class ProcessCpuSampler(string procRoot, int selfId) : IProcessCpuSampler
{
    private readonly Dictionary<int, long> previous = [];
    private readonly HashSet<int> gpuProcesses = [];

    /// <inheritdoc />
    public int GpuRestarts => Math.Max(0, gpuProcesses.Count - 1);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, long> Take()
    {
        if (!Directory.Exists(procRoot))
        {
            long total = (long)(Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds * 100);
            long used = total - previous.GetValueOrDefault(selfId);
            previous[selfId] = total;
            return new Dictionary<string, long> { ["browser"] = used };
        }

        Dictionary<int, (int Parent, long Ticks)> processes = [];

        foreach (string folder in Directory.EnumerateDirectories(procRoot))
        {
            if (int.TryParse(Path.GetFileName(folder), out int id) && Read(Path.Combine(folder, "stat")) is { } stat)
            {
                processes[id] = stat;
            }
        }

        Dictionary<string, long> totals = [];
        HashSet<int> family = [selfId];
        bool grew = true;

        while (grew)
        {
            grew = false;

            foreach ((int id, (int parent, _)) in processes)
            {
                if (family.Contains(parent) && family.Add(id))
                {
                    grew = true;
                }
            }
        }

        Dictionary<int, long> current = [];

        foreach (int id in family.Where(processes.ContainsKey))
        {
            string kind = id == selfId ? "browser" : KindOf(Path.Combine(procRoot, id.ToString(), "cmdline"));
            long ticks = processes[id].Ticks;
            current[id] = ticks;

            if (kind == "gpu-process")
            {
                gpuProcesses.Add(id);
            }

            totals[kind] = totals.GetValueOrDefault(kind) + Math.Max(0, ticks - previous.GetValueOrDefault(id));
        }

        previous.Clear();

        foreach ((int id, long ticks) in current)
        {
            previous[id] = ticks;
        }

        return totals;
    }

    private (int Parent, long Ticks)? Read(string path)
    {
        try
        {
            string text = File.ReadAllText(path);
            string[] fields = text[(text.LastIndexOf(')') + 2)..].Split(' ');
            return (int.Parse(fields[1], CultureInfo.InvariantCulture), long.Parse(fields[11], CultureInfo.InvariantCulture) + long.Parse(fields[12], CultureInfo.InvariantCulture));
        }
        catch (Exception exception) when (exception is IOException or FormatException or IndexOutOfRangeException or ArgumentOutOfRangeException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private string KindOf(string commandLinePath)
    {
        try
        {
            string? type = File.ReadAllText(commandLinePath).Split('\0').FirstOrDefault(argument => argument.StartsWith("--type=", StringComparison.Ordinal));
            return type is null ? "other" : type[7..];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return "other";
        }
    }
}
