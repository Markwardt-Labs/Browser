namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// What was counted about the frames Chromium delivered to the window since the last time they were taken.
/// </summary>
/// <param name="Paints">The number of frames delivered.</param>
/// <param name="DirtyPixels">The total area of the regions that changed, in pixels.</param>
/// <param name="Width">The width of the latest frame, in pixels.</param>
/// <param name="Height">The height of the latest frame, in pixels.</param>
/// <param name="Gaps">How many times a frame came more than 25 ms after the one before it, while frames were flowing.</param>
/// <param name="MaxGapMilliseconds">The longest such gap.</param>
/// <param name="Delays">How many times the wait for the UI thread to pick up a frame was measured.</param>
/// <param name="AverageDelayMilliseconds">The average wait.</param>
/// <param name="MaxDelayMilliseconds">The longest wait.</param>
internal sealed record FrameStatsSnapshot(int Paints, long DirtyPixels, int Width, int Height, int Gaps, double MaxGapMilliseconds, int Delays, double AverageDelayMilliseconds, double MaxDelayMilliseconds);

/// <summary>
/// Counts the frames Chromium delivers and how long the UI thread takes to get to them, for diagnosing lag.
/// </summary>
internal interface IFrameStats
{
    /// <summary>
    /// Gets a value indicating whether anything is being counted.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Records that a frame was delivered. Safe to call from any thread.
    /// </summary>
    /// <param name="width">The width of the frame, in pixels.</param>
    /// <param name="height">The height of the frame, in pixels.</param>
    /// <param name="dirtyPixels">The area of the regions that changed, in pixels.</param>
    void Painted(int width, int height, long dirtyPixels);

    /// <summary>
    /// Records that frames could not be observed at all.
    /// </summary>
    void HookFailed();

    /// <summary>
    /// Gets the counts since the last call and starts counting again.
    /// </summary>
    /// <param name="hookFailed">Whether <see cref="HookFailed"/> was called.</param>
    /// <returns>The counts.</returns>
    FrameStatsSnapshot Take(out bool hookFailed);
}

/// <inheritdoc />
/// <param name="isEnabled">Whether to count at all; when false every call does nothing.</param>
/// <param name="postToUi">Runs an action on the UI thread, to measure how long it waits.</param>
/// <param name="clock">Reads a clock that only moves forward.</param>
internal sealed class FrameStats(bool isEnabled, Action<Action> postToUi, Func<TimeSpan> clock) : IFrameStats
{
    private readonly Lock gate = new();
    private int paints;
    private long dirtyPixels;
    private int width;
    private int height;
    private int gaps;
    private double maxGap;
    private int delays;
    private double delaySum;
    private double delayMax;
    private TimeSpan? lastPaint;
    private bool probePending;
    private bool hookFailed;

    /// <inheritdoc />
    public bool IsEnabled => isEnabled;

    /// <inheritdoc />
    public void Painted(int width, int height, long dirtyPixels)
    {
        if (!isEnabled)
        {
            return;
        }

        TimeSpan now = clock();
        bool probe = false;

        using (gate.EnterScope())
        {
            paints++;
            this.dirtyPixels += dirtyPixels;
            this.width = width;
            this.height = height;

            if (lastPaint is { } previous)
            {
                double gap = (now - previous).TotalMilliseconds;

                if (gap is > 25 and <= 250)
                {
                    gaps++;
                    maxGap = Math.Max(maxGap, gap);
                }
            }

            lastPaint = now;

            if (!probePending)
            {
                probePending = true;
                probe = true;
            }
        }

        if (probe)
        {
            postToUi(() =>
            {
                double delay = (clock() - now).TotalMilliseconds;

                using (gate.EnterScope())
                {
                    delays++;
                    delaySum += delay;
                    delayMax = Math.Max(delayMax, delay);
                    probePending = false;
                }
            });
        }
    }

    /// <inheritdoc />
    public void HookFailed()
    {
        using (gate.EnterScope())
        {
            hookFailed = true;
        }
    }

    /// <inheritdoc />
    public FrameStatsSnapshot Take(out bool hookFailed)
    {
        using (gate.EnterScope())
        {
            FrameStatsSnapshot snapshot = new(paints, dirtyPixels, width, height, gaps, maxGap, delays, delays == 0 ? 0 : delaySum / delays, delayMax);
            paints = 0;
            dirtyPixels = 0;
            gaps = 0;
            maxGap = 0;
            delays = 0;
            delaySum = 0;
            delayMax = 0;
            hookFailed = this.hookFailed;
            return snapshot;
        }
    }
}
