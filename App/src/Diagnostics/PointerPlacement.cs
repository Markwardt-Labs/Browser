namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// Works out where the mouse pointer is relative to the browser window.
/// </summary>
internal interface IPointerPlacement
{
    /// <summary>
    /// Classifies the pointer position.
    /// </summary>
    /// <param name="pointer">The pointer position on the desktop, or null when it could not be read.</param>
    /// <param name="window">The area the window covers on the desktop.</param>
    /// <param name="screens">The area of each screen on the desktop.</param>
    /// <param name="screen">The number of the screen the window is mostly on, or -1.</param>
    /// <returns><c>over</c> when the pointer is inside the window, <c>beside</c> when it is outside it but on the window's screen, <c>other-screen</c> when it is on a different screen, otherwise <c>unknown</c>.</returns>
    string Classify(PixelPoint? pointer, PixelRect window, IReadOnlyList<PixelRect> screens, out int screen);
}

/// <inheritdoc />
internal sealed class PointerPlacement : IPointerPlacement
{
    /// <inheritdoc />
    public string Classify(PixelPoint? pointer, PixelRect window, IReadOnlyList<PixelRect> screens, out int screen)
    {
        PixelPoint center = window.Center;
        screen = -1;

        for (int index = 0; index < screens.Count; index++)
        {
            if (screens[index].Contains(center))
            {
                screen = index;
                break;
            }
        }

        if (pointer is not { } position)
        {
            return "unknown";
        }

        if (window.Contains(position))
        {
            return "over";
        }

        return screen >= 0 && screens[screen].Contains(position) ? "beside" : "other-screen";
    }
}
