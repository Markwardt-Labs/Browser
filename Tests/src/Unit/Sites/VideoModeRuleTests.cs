namespace Tests.Unit.Sites;

/// <summary>
/// Tests for <see cref="VideoModeRule"/>.
/// </summary>
public sealed class VideoModeRuleTests
{
    private readonly VideoModeRule rule = new();

    /// <summary>
    /// Web and local pages get video mode.
    /// </summary>
    /// <param name="address">The address of the frame.</param>
    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("http://example.com/video")]
    [InlineData("file:///tmp/page.html")]
    public void Matches_WebOrFileAddress_ReturnsTrue(string address)
        => Assert.True(rule.Matches(address));

    /// <summary>
    /// Internal and malformed addresses do not.
    /// </summary>
    /// <param name="address">The address of the frame.</param>
    [Theory]
    [InlineData("about:blank")]
    [InlineData("chrome://gpu")]
    [InlineData("view-source:https://example.com")]
    [InlineData("")]
    public void Matches_OtherAddress_ReturnsFalse(string address)
        => Assert.False(rule.Matches(address));

    /// <summary>
    /// The embedded script is loaded and exposes the video mode API.
    /// </summary>
    [Fact]
    public void Script_IsLoaded()
        => Assert.Contains("__markwardtVideoMode", rule.Script);
}
