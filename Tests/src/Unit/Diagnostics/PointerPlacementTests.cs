namespace Tests.Unit.Diagnostics;

/// <summary>
/// Tests for <see cref="PointerPlacement"/>.
/// </summary>
public sealed class PointerPlacementTests
{
    private readonly PointerPlacement placement = new();
    private readonly IReadOnlyList<PixelRect> screens = [new PixelRect(0, 0, 1920, 1080), new PixelRect(1920, 0, 1920, 1080)];
    private readonly PixelRect window = new(320, 140, 1280, 800);

    /// <summary>
    /// A pointer inside the window is over it, wherever the window is.
    /// </summary>
    [Fact]
    public void Classify_InsideWindow_IsOver()
    {
        Assert.Equal("over", placement.Classify(new PixelPoint(600, 500), window, screens, out int screen));
        Assert.Equal(0, screen);
    }

    /// <summary>
    /// A pointer outside the window but on its screen is beside it.
    /// </summary>
    [Fact]
    public void Classify_OutsideWindowSameScreen_IsBeside()
        => Assert.Equal("beside", placement.Classify(new PixelPoint(1800, 1000), window, screens, out _));

    /// <summary>
    /// A pointer on the other screen is reported as such, and the window's own screen is identified.
    /// </summary>
    [Fact]
    public void Classify_OtherScreen_IsOtherScreen()
    {
        PixelRect onSecond = new(2200, 140, 1280, 800);

        Assert.Equal("other-screen", placement.Classify(new PixelPoint(100, 100), onSecond, screens, out int screen));
        Assert.Equal(1, screen);
    }

    /// <summary>
    /// A pointer that could not be read is unknown.
    /// </summary>
    [Fact]
    public void Classify_NoPointer_IsUnknown()
        => Assert.Equal("unknown", placement.Classify(null, window, screens, out _));

    /// <summary>
    /// A window that is on none of the screens leaves the screen unknown, and a pointer outside it is not on its screen.
    /// </summary>
    [Fact]
    public void Classify_WindowOffAllScreens_ScreenUnknown()
    {
        Assert.Equal("other-screen", placement.Classify(new PixelPoint(10, 10), new PixelRect(9000, 9000, 100, 100), screens, out int screen));
        Assert.Equal(-1, screen);
    }
}
