namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// How the video playing in a page is doing, as the page itself sees it.
/// </summary>
/// <param name="PresentedFps">The video frames shown per second, not counting dropped ones.</param>
/// <param name="Dropped">The frames dropped since the previous report.</param>
/// <param name="Width">The width of the video, in pixels.</param>
/// <param name="Height">The height of the video, in pixels.</param>
/// <param name="Visibility">The page's visibility state, <c>visible</c> or <c>hidden</c>.</param>
/// <param name="HasFocus">Whether the page believes it has focus.</param>
internal sealed record VideoSnapshot(double PresentedFps, int Dropped, int Width, int Height, string Visibility, bool HasFocus);
