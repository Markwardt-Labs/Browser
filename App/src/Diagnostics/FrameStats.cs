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
/// <param name="Ticks">How many times the time until the window's next render tick was measured.</param>
/// <param name="AverageTickMilliseconds">The average time from a frame arriving until the window's render loop next ran.</param>
/// <param name="MaxTickMilliseconds">The longest such time.</param>
/// <param name="BigPaints">How many of the frames changed at least a quarter of the window, which is what a playing video does and a progress bar or a blinking caret does not.</param>
internal sealed record FrameStatsSnapshot(int Paints, long DirtyPixels, int Width, int Height, int Gaps, double MaxGapMilliseconds, int Delays, double AverageDelayMilliseconds, double MaxDelayMilliseconds, int Ticks, double AverageTickMilliseconds, double MaxTickMilliseconds, int BigPaints);

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
/// <param name="nextRenderTick">Runs an action on the UI thread when the window's render loop next runs, to measure how long that takes after a frame arrives; may do nothing when there is no window.</param>
/// <param name="clock">Reads a clock that only moves forward.</param>
internal sealed class FrameStats(bool isEnabled, Action<Action> postToUi, Action<Action> nextRenderTick, Func<TimeSpan> clock) : IFrameStats
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
    private int ticks;
    private int bigPaints;
    private double tickSum;
    private double tickMax;
    private bool tickPending;
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
            bigPaints += dirtyPixels * 4 >= (long)width * height ? 1 : 0;
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

        bool tick = false;

        using (gate.EnterScope())
        {
            if (!tickPending)
            {
                tickPending = true;
                tick = true;
            }
        }

        if (tick)
        {
            postToUi(() => nextRenderTick(() =>
            {
                double elapsed = (clock() - now).TotalMilliseconds;

                using (gate.EnterScope())
                {
                    ticks++;
                    tickSum += elapsed;
                    tickMax = Math.Max(tickMax, elapsed);
                    tickPending = false;
                }
            }));
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
            FrameStatsSnapshot snapshot = new(paints, dirtyPixels, width, height, gaps, maxGap, delays, delays == 0 ? 0 : delaySum / delays, delayMax, ticks, ticks == 0 ? 0 : tickSum / ticks, tickMax, bigPaints);
            paints = 0;
            dirtyPixels = 0;
            gaps = 0;
            maxGap = 0;
            delays = 0;
            delaySum = 0;
            delayMax = 0;
            ticks = 0;
            bigPaints = 0;
            tickSum = 0;
            tickMax = 0;
            hookFailed = this.hookFailed;
            return snapshot;
        }
    }
}
