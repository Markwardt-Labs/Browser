namespace Tests.Integration.Engine;

/// <summary>
/// Tests for <see cref="NativeEnvironment"/> against the real process environment.
/// </summary>
public sealed class NativeEnvironmentTests
{
    private readonly NativeEnvironment environment = new();

    /// <summary>
    /// A variable that was never set reads as null.
    /// </summary>
    [Fact]
    public void Get_Unset_ReturnsNull()
        => Assert.Null(environment.Get($"MARKWARDT_TEST_{Guid.NewGuid():N}"));

    /// <summary>
    /// A variable that is set can be read back, and setting it again replaces the value.
    /// </summary>
    [Fact]
    public void Set_ThenGet_RoundTripsAndReplaces()
    {
        string name = $"MARKWARDT_TEST_{Guid.NewGuid():N}";

        environment.Set(name, "first");
        Assert.Equal("first", environment.Get(name));

        environment.Set(name, "second");
        Assert.Equal("second", environment.Get(name));
    }

    /// <summary>
    /// A value with characters outside ASCII survives.
    /// </summary>
    [Fact]
    public void Set_NonAscii_RoundTrips()
    {
        string name = $"MARKWARDT_TEST_{Guid.NewGuid():N}";

        environment.Set(name, "café ☃");

        Assert.Equal("café ☃", environment.Get(name));
    }
}
