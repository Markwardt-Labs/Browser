namespace Tests.Unit.Sites;

/// <summary>
/// Tests for <see cref="YouTubeRule"/>.
/// </summary>
public sealed class YouTubeRuleTests
{
    private readonly YouTubeRule rule = new();

    /// <summary>
    /// YouTube pages and its embedded players match.
    /// </summary>
    /// <param name="address">The address of the frame.</param>
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=abc")]
    [InlineData("https://youtube.com/")]
    [InlineData("https://m.youtube.com/watch?v=abc")]
    [InlineData("https://www.youtube.com/embed/abc")]
    [InlineData("https://www.youtube-nocookie.com/embed/abc")]
    public void Matches_YouTubeAddress_ReturnsTrue(string address)
        => Assert.True(rule.Matches(address));

    /// <summary>
    /// Other sites, look-alike hosts and non-web addresses do not match.
    /// </summary>
    /// <param name="address">The address of the frame.</param>
    [Theory]
    [InlineData("https://example.com/youtube.com")]
    [InlineData("https://notyoutube.com/")]
    [InlineData("https://youtube.com.evil.example/")]
    [InlineData("file:///tmp/youtube.com/page.html")]
    [InlineData("about:blank")]
    [InlineData("")]
    public void Matches_OtherAddress_ReturnsFalse(string address)
        => Assert.False(rule.Matches(address));

    /// <summary>
    /// The embedded script is loaded and installs once per document.
    /// </summary>
    [Fact]
    public void Script_IsLoaded()
    {
        Assert.Contains("__markwardtYouTube", rule.Script);
        Assert.Contains("hd1080", rule.Script);
    }
}
