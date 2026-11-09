namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// Reads where the mouse pointer is on the desktop, wherever the window is and whether or not the pointer is over it.
/// </summary>
internal interface IPointerLocator
{
    /// <summary>
    /// Reads the pointer position.
    /// </summary>
    /// <returns>The position in desktop pixels, or null when it cannot be read on this system.</returns>
    PixelPoint? Locate();
}

/// <inheritdoc />
internal sealed class X11PointerLocator : IPointerLocator
{
    private nint display;
    private bool unavailable;

    /// <inheritdoc />
    public PixelPoint? Locate()
    {
        if (unavailable || !OperatingSystem.IsLinux())
        {
            return null;
        }

        try
        {
            display = display == 0 ? XOpenDisplay(0) : display;

            if (display == 0)
            {
                unavailable = true;
                return null;
            }

            return XQueryPointer(display, XDefaultRootWindow(display), out _, out _, out int x, out int y, out _, out _, out _) != 0 ? new PixelPoint(x, y) : null;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            unavailable = true;
            return null;
        }
    }

    [DllImport("libX11.so.6")]
    private static extern nint XOpenDisplay(nint name);

    [DllImport("libX11.so.6")]
    private static extern nint XDefaultRootWindow(nint display);

    [DllImport("libX11.so.6")]
    private static extern int XQueryPointer(nint display, nint window, out nint root, out nint child, out int rootX, out int rootY, out int windowX, out int windowY, out uint mask);
}
