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
