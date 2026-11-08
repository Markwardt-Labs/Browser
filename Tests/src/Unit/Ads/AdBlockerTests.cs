namespace Tests.Unit.Ads;

/// <summary>
/// Tests for <see cref="AdBlocker"/> over the real parser and rule matching.
/// </summary>
public sealed class AdBlockerTests
{
    private readonly Mock<IFilterLists> lists = new();
    private readonly Mock<IRequestStats> stats = new();
    private readonly AdBlocker blocker;

    /// <summary>
    /// Creates the blocker over a mock list source.
    /// </summary>
    public AdBlockerTests()
    {
        lists.Setup(l => l.Read(It.IsAny<CancellationToken>())).ReturnsAsync(["||ads.example.com^\n||tracker.net^$third-party\n@@||ads.example.com/allowed/\n/banners/*/ad_\n|https://exact.example/ad.js|\n||media.example^$media\n||pop.example^$popup\n||site-only.example^$domain=a.com|~sub.a.com\n||wild.example/a*b^\n||redirected.example^$script,redirect=noopjs\nexample.com##.promo\nshop.*##.entity-ad\n##.generic-ad\n###sidebar-ad\nexample.com#@#.generic-ad\n"]);
        blocker = new AdBlocker(new FilterParser(), lists.Object, new YouTubeAdScript(), stats.Object);
    }

    /// <summary>
    /// Until the lists are loaded nothing is blocked.
    /// </summary>
    [Fact]
    public void ShouldBlock_BeforeLoad_IsFalse()
        => Assert.False(blocker.ShouldBlock("https://ads.example.com/a.js", "https://example.org/", AdResourceType.Script));

    /// <summary>
    /// Address matching: domain anchors cover subdomains but not look-alikes, separators, exceptions, wildcards and anchors work.
    /// </summary>
    /// <param name="url">The address requested.</param>
    /// <param name="blocked">Whether it should be blocked.</param>
    /// <returns>A task that completes when the check is done.</returns>
    [Theory]
    [InlineData("https://ads.example.com/a.js", true)]
    [InlineData("https://sub.ads.example.com/a.js", true)]
    [InlineData("https://ads.example.com", true)]
    [InlineData("https://notads.example.com/a.js", false)]
    [InlineData("https://ads.example.com.evil.org/a.js", false)]
    [InlineData("https://example.com/ads.example.com/a.js", false)]
    [InlineData("https://ads.example.com/allowed/a.js", false)]
    [InlineData("https://cdn.example.org/banners/300/ad_1.gif", true)]
    [InlineData("https://cdn.example.org/banners/ad_1.gif", false)]
    [InlineData("https://exact.example/ad.js", true)]
    [InlineData("https://exact.example/ad.js?x=1", false)]
    [InlineData("https://wild.example/axxb/c", true)]
    [InlineData("https://wild.example/axxbc/c", false)]
    [InlineData("https://www.example.org/", false)]
    public async Task ShouldBlock_MatchesAddresses(string url, bool blocked)
    {
        await blocker.Load();

        Assert.Equal(blocked, blocker.ShouldBlock(url, "https://page.org/", AdResourceType.Script));
    }

    /// <summary>
    /// Options limit a rule to third party requests, to some request types and to some sites.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task ShouldBlock_HonorsOptions()
    {
        await blocker.Load();

        Assert.True(blocker.ShouldBlock("https://tracker.net/p.gif", "https://page.org/", AdResourceType.Image));
        Assert.False(blocker.ShouldBlock("https://tracker.net/p.gif", "https://www.tracker.net/", AdResourceType.Image));
        Assert.True(blocker.ShouldBlock("https://media.example/v.mp4", "https://page.org/", AdResourceType.Media));
        Assert.False(blocker.ShouldBlock("https://media.example/v.mp4", "https://page.org/", AdResourceType.Script));
        Assert.True(blocker.ShouldBlock("https://site-only.example/x", "https://www.a.com/", AdResourceType.Script));
        Assert.False(blocker.ShouldBlock("https://site-only.example/x", "https://other.com/", AdResourceType.Script));
        Assert.False(blocker.ShouldBlock("https://site-only.example/x", "https://sub.a.com/", AdResourceType.Script));
    }

    /// <summary>
    /// Addresses that are not on the web are never blocked, and neither is a page's own navigation.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task ShouldBlock_NonWebOrDocument_IsFalse()
    {
        await blocker.Load();

        Assert.False(blocker.ShouldBlock("data:text/html,ads.example.com", "https://page.org/", AdResourceType.Script));
        Assert.False(blocker.ShouldBlock("https://ads.example.com/", "https://page.org/", AdResourceType.Document));
    }

    /// <summary>
    /// A popup is an ad when a general rule or a popup rule matches it.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task IsAdPopup_UsesGeneralAndPopupRules()
    {
        await blocker.Load();

        Assert.True(blocker.IsAdPopup("https://ads.example.com/click", "https://page.org/"));
        Assert.True(blocker.IsAdPopup("https://pop.example/x", "https://page.org/"));
        Assert.False(blocker.IsAdPopup("https://accounts.example.org/login", "https://page.org/"));
        Assert.False(blocker.ShouldBlock("https://pop.example/x", "https://page.org/", AdResourceType.Script));
    }

    /// <summary>
    /// The script for a site hides its own selectors, and the selectors for every site only in the main frame, minus those the site is excepted from.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task FrameScript_HidesSelectors()
    {
        await blocker.Load();

        string main = blocker.FrameScript("https://www.example.com/", true)!;
        string frame = blocker.FrameScript("https://www.example.com/", false)!;
        string other = blocker.FrameScript("https://other.org/", true)!;

        Assert.Contains(".promo{display:none!important}", main);
        Assert.Contains("#sidebar-ad", main);
        Assert.DoesNotContain(".generic-ad", main);
        Assert.Contains(".promo", frame);
        Assert.DoesNotContain("#sidebar-ad", frame);
        Assert.Contains(".generic-ad", other);
        Assert.DoesNotContain(".promo", other);
    }

    /// <summary>
    /// A rule that redirects to a stand-in resource still blocks the request.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task ShouldBlock_RedirectRule_Blocks()
    {
        await blocker.Load();

        Assert.True(blocker.ShouldBlock("https://redirected.example/x.js", "https://page.org/", AdResourceType.Script));
    }

    /// <summary>
    /// A rule for a site under any top level domain hides its elements on every one of them, and on no other site.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task FrameScript_EntityRule_AppliesToAnyTopLevelDomain()
    {
        await blocker.Load();

        Assert.Contains(".entity-ad", blocker.FrameScript("https://www.shop.com/", false)!);
        Assert.Contains(".entity-ad", blocker.FrameScript("https://shop.co.uk/", false)!);
        Assert.DoesNotContain(".entity-ad", blocker.FrameScript("https://notshop.com/", false) ?? string.Empty);
    }

    /// <summary>
    /// Blocked requests and requests to other sites are counted when diagnostics are on, and first party requests are not.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task ShouldBlock_CountsForDiagnostics()
    {
        stats.Setup(s => s.IsEnabled).Returns(true);
        await blocker.Load();

        blocker.ShouldBlock("https://ads.example.com/a.js", "https://page.org/", AdResourceType.Script);
        blocker.ShouldBlock("https://cdn.other.net/a.js", "https://page.org/", AdResourceType.Script);
        blocker.ShouldBlock("https://page.org/own.js", "https://page.org/", AdResourceType.Script);

        stats.Verify(s => s.Record(true, "Script", "page.org", "ads.example.com"), Times.Once);
        stats.Verify(s => s.Record(false, "Script", "page.org", "cdn.other.net"), Times.Once);
        stats.Verify(s => s.Record(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), "page.org"), Times.Never);
    }

    /// <summary>
    /// Pages that are not on the web get no script, and YouTube always gets the one that stops its ads.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task FrameScript_NonWebAndYouTube()
    {
        await blocker.Load();

        Assert.Null(blocker.FrameScript("about:blank", true));
        Assert.Contains("ytInitialPlayerResponse", blocker.FrameScript("https://www.youtube.com/watch?v=x", true));
        Assert.Contains("ytInitialPlayerResponse", blocker.FrameScript("https://www.youtube-nocookie.com/embed/x", false));
        Assert.DoesNotContain("ytInitialPlayerResponse", blocker.FrameScript("https://www.example.com/", true));
    }

    /// <summary>
    /// A refresh that changed nothing keeps the current rules, and one that changed something loads again.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Refresh_ReloadsOnlyWhenListsChanged()
    {
        lists.Setup(l => l.Refresh(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await blocker.Refresh();
        lists.Verify(l => l.Read(It.IsAny<CancellationToken>()), Times.Never);

        lists.Setup(l => l.Refresh(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await blocker.Refresh();
        lists.Verify(l => l.Read(It.IsAny<CancellationToken>()), Times.Once);
    }
}
