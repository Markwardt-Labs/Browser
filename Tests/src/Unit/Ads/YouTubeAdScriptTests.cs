namespace Tests.Unit.Ads;

/// <summary>
/// Tests for <see cref="YouTubeAdScript"/>.
/// </summary>
public sealed class YouTubeAdScriptTests
{
    private readonly YouTubeAdScript script = new();

    /// <summary>
    /// YouTube's pages and embedded players get the script.
    /// </summary>
    /// <param name="address">The address of the frame.</param>
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=abc")]
    [InlineData("https://youtube.com/")]
    [InlineData("https://m.youtube.com/watch?v=abc")]
    [InlineData("https://www.youtube.com/embed/abc")]
    [InlineData("https://www.youtube-nocookie.com/embed/abc")]
    public void For_YouTube_ReturnsScript(string address)
        => Assert.Contains("ytInitialPlayerResponse", script.For(address));

    /// <summary>
    /// Other sites, look-alike hosts and non-web addresses do not.
    /// </summary>
    /// <param name="address">The address of the frame.</param>
    [Theory]
    [InlineData("https://example.com/youtube.com")]
    [InlineData("https://notyoutube.com/")]
    [InlineData("https://youtube.com.evil.example/")]
    [InlineData("file:///tmp/youtube.com/a.html")]
    [InlineData("about:blank")]
    [InlineData("")]
    public void For_Other_ReturnsNull(string address)
        => Assert.Null(script.For(address));
}
