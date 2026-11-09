namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// What was going on around the browser window when a sample was taken.
/// </summary>
/// <param name="Page">The host name of the page being viewed, or an empty string.</param>
/// <param name="IsActive">Whether a browser window has keyboard focus.</param>
/// <param name="Pointer">Where the mouse pointer is relative to the window: over it, beside it on the same screen, on another screen, or unknown.</param>
/// <param name="Screen">The number of the screen the window is mostly on, or -1 when unknown.</param>
/// <param name="PointerIdleSeconds">How long the pointer has been where it is, in whole seconds; video players hide their controls and cursor after a few.</param>
internal sealed record StatsContext(string Page, bool IsActive, string Pointer, int Screen, int PointerIdleSeconds);
