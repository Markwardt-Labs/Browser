namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// How late the UI thread has been when asked to run something, since the last time it was taken.
/// </summary>
/// <param name="MaxLateMilliseconds">The longest delay.</param>
/// <param name="Stalls">How many times the delay was over 50 ms.</param>
internal sealed record UiProbeSnapshot(double MaxLateMilliseconds, int Stalls);

/// <summary>
/// Measures whether the UI thread is keeping up, which is what input and animation lag come down to.
/// </summary>
internal interface IUiProbe
{
    /// <summary>
    /// Gets the measurements since the last call and starts again.
    /// </summary>
    /// <returns>The measurements.</returns>
    UiProbeSnapshot Take();
}

/// <inheritdoc />
internal sealed class UiProbe : IUiProbe
{
    private readonly Lock gate = new();
    private readonly TimeSpan interval = TimeSpan.FromMilliseconds(20);
    private readonly long start = Stopwatch.GetTimestamp();
    private TimeSpan last;
    private double maxLate;
    private int stalls;

    /// <summary>
    /// Starts measuring. Must be created on the UI thread.
    /// </summary>
    public UiProbe()
    {
        last = Stopwatch.GetElapsedTime(start);
        DispatcherTimer timer = new(interval, DispatcherPriority.Normal, (_, _) => Tick());
        timer.Start();
    }

    /// <inheritdoc />
    public UiProbeSnapshot Take()
    {
        using (gate.EnterScope())
        {
            UiProbeSnapshot snapshot = new(maxLate, stalls);
            maxLate = 0;
            stalls = 0;
            return snapshot;
        }
    }

    private void Tick()
    {
        TimeSpan now = Stopwatch.GetElapsedTime(start);
        double late = (now - last - interval).TotalMilliseconds;
        last = now;

        using (gate.EnterScope())
        {
            maxLate = Math.Max(maxLate, late);
            stalls += late > 50 ? 1 : 0;
        }
    }
}
