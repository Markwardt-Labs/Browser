namespace Tests.Unit.Engine;

/// <summary>
/// Tests for <see cref="ChromiumSwitches"/>.
/// </summary>
public sealed class ChromiumSwitchesTests
{
    private readonly ChromiumSwitches switches = new();

    /// <summary>
    /// By default Chromium picks its own graphics backend, and pages are forced dark.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Build_NoBackend_LeavesChoiceToChromium(string? angle)
    {
        IReadOnlyList<KeyValuePair<string, string>> result = switches.Build(angle);

        Assert.Contains(new KeyValuePair<string, string>("force-dark-mode", string.Empty), result);
        Assert.Contains(new KeyValuePair<string, string>("enable-features", "WebContentsForceDark"), result);
        Assert.DoesNotContain(result, pair => pair.Key == "use-angle");
    }

    /// <summary>
    /// Asking for the Vulkan backend selects it and switches its feature on.
    /// </summary>
    [Fact]
    public void Build_Vulkan_SelectsItAndEnablesFeature()
    {
        IReadOnlyList<KeyValuePair<string, string>> result = switches.Build("vulkan");

        Assert.Contains(new KeyValuePair<string, string>("use-angle", "vulkan"), result);
        Assert.Contains(new KeyValuePair<string, string>("enable-features", "Vulkan,WebContentsForceDark"), result);
    }

    /// <summary>
    /// Any other backend is passed on as asked, without the Vulkan feature.
    /// </summary>
    [Fact]
    public void Build_OtherBackend_IsPassedOn()
    {
        IReadOnlyList<KeyValuePair<string, string>> result = switches.Build("gl");

        Assert.Contains(new KeyValuePair<string, string>("use-angle", "gl"), result);
        Assert.Contains(new KeyValuePair<string, string>("enable-features", "WebContentsForceDark"), result);
    }
}
