namespace Tests.Unit.Ads;

/// <summary>
/// Tests for <see cref="AdRequest"/>.
/// </summary>
public sealed class AdRequestTests
{
    /// <summary>
    /// The host is taken out of the address, without port, user info, path or case.
    /// </summary>
    /// <param name="url">The address.</param>
    /// <param name="host">The expected host.</param>
    [Theory]
    [InlineData("https://Ads.Example.com/banner.gif?x=1", "ads.example.com")]
    [InlineData("http://example.com:8080/path", "example.com")]
    [InlineData("https://user:pass@example.com/", "example.com")]
    [InlineData("https://example.com", "example.com")]
    [InlineData("wss://sock.example.com/a", "sock.example.com")]
    public void Host_IsExtracted(string url, string host)
        => Assert.Equal(host, new AdRequest(url, string.Empty, AdResourceType.Script).Host);

    /// <summary>
    /// Only web addresses are filterable.
    /// </summary>
    /// <param name="url">The address.</param>
    [Theory]
    [InlineData("data:text/html,hi")]
    [InlineData("file:///tmp/a.html")]
    [InlineData("about:blank")]
    [InlineData("chrome://gpu")]
    [InlineData("")]
    public void IsFilterable_NonWebAddress_IsFalse(string url)
        => Assert.False(new AdRequest(url, string.Empty, AdResourceType.Script).IsFilterable);

    /// <summary>
    /// A request is third party when its site differs from the page's, treating subdomains and common two part suffixes as one site.
    /// </summary>
    /// <param name="url">The address requested.</param>
    /// <param name="page">The page address.</param>
    /// <param name="thirdParty">The expected answer.</param>
    [Theory]
    [InlineData("https://cdn.example.com/a.js", "https://www.example.com/", false)]
    [InlineData("https://tracker.net/a.js", "https://www.example.com/", true)]
    [InlineData("https://a.example.co.uk/a.js", "https://b.example.co.uk/", false)]
    [InlineData("https://other.co.uk/a.js", "https://example.co.uk/", true)]
    [InlineData("https://tracker.net/a.js", "", false)]
    public void IsThirdParty_ComparesSites(string url, string page, bool thirdParty)
        => Assert.Equal(thirdParty, new AdRequest(url, page, AdResourceType.Script).IsThirdParty);

    /// <summary>
    /// The tokens are the runs of three or more letters and digits.
    /// </summary>
    [Fact]
    public void Tokens_AreRunsOfAtLeastThreeCharacters()
        => Assert.True(new AdRequest("https://ads.example.com/banner/300x250.gif?a=1", string.Empty, AdResourceType.Image).Tokens.SetEquals(["https", "ads", "example", "com", "banner", "300x250", "gif"]));

    /// <summary>
    /// The host starts are the host itself and each parent name.
    /// </summary>
    [Fact]
    public void HostStarts_CoverHostAndParents()
        => Assert.Equal([8, 12, 20], new AdRequest("https://ads.example.com/x", string.Empty, AdResourceType.Script).HostStarts);
}
