namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// Describes how the window is being drawn, which decides whether a frame costs a texture upload or a trip through software.
/// </summary>
internal interface IRenderingInfo
{
    /// <summary>
    /// Describes the drawing of a window.
    /// </summary>
    /// <param name="window">The window, which must be open.</param>
    /// <returns>Lines of text.</returns>
    Task<IReadOnlyList<string>> Describe(Window window);
}

/// <inheritdoc />
internal sealed class RenderingInfo : IRenderingInfo
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> Describe(Window window)
    {
        List<string> lines = [];
        object? impl = typeof(TopLevel).GetProperty("PlatformImpl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(window);
        PropertyInfo? surfacesProperty = impl?.GetType().GetInterfaces().Select(contract => contract.GetProperty("Surfaces")).FirstOrDefault(property => property is not null);
        string surfaces = surfacesProperty?.GetValue(impl) is IEnumerable<object> found ? string.Join(", ", found.Select(surface => surface.GetType().Name)) : "unknown";
        Compositor? compositor = ElementComposition.GetElementVisual(window)?.Compositor;
        bool gpuInterop = compositor is not null && await compositor.TryGetCompositionGpuInterop() is not null;
        lines.Add($"window: {window.Bounds.Width:F0}x{window.Bounds.Height:F0} dip, scale {window.RenderScaling:F2}, state {window.WindowState}");
        lines.Add($"avalonia surfaces: {surfaces}  (an X11 framebuffer surface means software drawing, a GL or Vulkan surface means GPU drawing)");
        lines.Add($"avalonia gpu interop: {gpuInterop}");

        foreach ((Screen screen, int index) in window.Screens.All.Select((screen, index) => (screen, index)))
        {
            lines.Add($"screen {index}: {screen.Bounds.Width}x{screen.Bounds.Height} px, scale {screen.Scaling:F2}{(screen.IsPrimary ? ", primary" : string.Empty)}");
        }

        return lines;
    }
}
