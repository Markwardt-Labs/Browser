namespace Tests.Unit.Engine;

/// <summary>
/// Tests for <see cref="GraphicsEnvironment"/>.
/// </summary>
public sealed class GraphicsEnvironmentTests
{
    private readonly Dictionary<string, string> environment = [];

    /// <summary>
    /// On Linux the swap sync variables are set for both the NVIDIA and the Mesa drivers.
    /// </summary>
    [Fact]
    public void DisableSwapSync_Linux_SetsBothVariables()
    {
        IReadOnlyList<KeyValuePair<string, string>> applied = Create(true).DisableSwapSync();

        Assert.Equal("0", environment["__GL_SYNC_TO_VBLANK"]);
        Assert.Equal("0", environment["vblank_mode"]);
        Assert.Equal(2, applied.Count);
    }

    /// <summary>
    /// Elsewhere nothing is touched.
    /// </summary>
    [Fact]
    public void DisableSwapSync_NotLinux_DoesNothing()
    {
        IReadOnlyList<KeyValuePair<string, string>> applied = Create(false).DisableSwapSync();

        Assert.Empty(applied);
        Assert.Empty(environment);
    }

    /// <summary>
    /// A variable the user already set is left alone, and the other is still set.
    /// </summary>
    [Fact]
    public void DisableSwapSync_ExistingVariable_IsKept()
    {
        environment["__GL_SYNC_TO_VBLANK"] = "1";

        IReadOnlyList<KeyValuePair<string, string>> applied = Create(true).DisableSwapSync();

        Assert.Equal("1", environment["__GL_SYNC_TO_VBLANK"]);
        Assert.Equal("0", environment["vblank_mode"]);
        Assert.Equal([new KeyValuePair<string, string>("vblank_mode", "0")], applied);
    }

    /// <summary>
    /// Setting MARKWARDT_VSYNC keeps the drivers' default.
    /// </summary>
    [Fact]
    public void DisableSwapSync_OptOut_DoesNothing()
    {
        environment["MARKWARDT_VSYNC"] = "1";

        Assert.Empty(Create(true).DisableSwapSync());
        Assert.False(environment.ContainsKey("__GL_SYNC_TO_VBLANK"));
    }

    private GraphicsEnvironment Create(bool isLinux)
        => new(isLinux, name => environment.GetValueOrDefault(name), (name, value) => environment[name] = value);
}
