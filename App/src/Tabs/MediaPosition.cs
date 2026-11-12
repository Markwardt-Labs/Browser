namespace Markwardt.Browser.Tabs;

/// <summary>
/// Where a long video or audio in a page had got to.
/// </summary>
/// <param name="Time">The position, in seconds.</param>
/// <param name="Duration">The length, in seconds.</param>
/// <param name="IsPaused">Whether it was paused.</param>
internal sealed record MediaPosition(double Time, double Duration, bool IsPaused);
