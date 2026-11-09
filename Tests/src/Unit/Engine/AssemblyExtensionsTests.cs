namespace Tests.Unit.Engine;

/// <summary>
/// Tests for <see cref="AssemblyExtensions"/>.
/// </summary>
public sealed class AssemblyExtensionsTests
{
    /// <summary>
    /// An embedded script is read as text.
    /// </summary>
    [Fact]
    public void ReadText_EmbeddedResource_ReturnsContent()
        => Assert.False(string.IsNullOrWhiteSpace(typeof(YouTubeRule).Assembly.ReadText("Markwardt.Browser.Sites.YouTube.js")));

    /// <summary>
    /// A resource the assembly does not have is reported by name.
    /// </summary>
    [Fact]
    public void ReadText_MissingResource_Throws()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => typeof(YouTubeRule).Assembly.ReadText("Nothing.js"));

        Assert.Contains("Nothing.js", exception.Message);
    }
}
