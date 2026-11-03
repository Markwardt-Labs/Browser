namespace Tests.Unit.Ads;

/// <summary>
/// Tests for <see cref="FilterParser"/>.
/// </summary>
public sealed class FilterParserTests
{
    private readonly FilterParser parser = new();

    /// <summary>
    /// Comments, headers and blank lines are ignored, and repeated rules count once.
    /// </summary>
    [Fact]
    public void Parse_IgnoresCommentsAndDuplicates()
    {
        FilterSet set = parser.Parse(["[Adblock Plus 2.0]", "! comment", "", "||ads.example.com^", "||ads.example.com^"]);

        Assert.Equal(1, set.Blocks.Count);
        Assert.Equal(0, set.Exceptions.Count);
    }

    /// <summary>
    /// Rules that need features this blocker does not have are skipped instead of being applied too broadly.
    /// </summary>
    /// <param name="line">The rule.</param>
    [Theory]
    [InlineData("/ads[0-9]+/")]
    [InlineData("||example.com^$redirect=noopjs")]
    [InlineData("||example.com^$removeparam=utm")]
    [InlineData("||example.com^$csp=script-src 'none'")]
    [InlineData("example.com##+js(aopr, x)")]
    [InlineData("example.com##.ad:has-text(Sponsored)")]
    [InlineData("example.com#?#.ad:-abp-has(.x)")]
    [InlineData("google.*##.ad")]
    public void Parse_UnsupportedRule_IsSkipped(string line)
    {
        FilterSet set = parser.Parse([line]);

        Assert.Equal(0, set.Blocks.Count);
        Assert.Empty(set.GenericSimple);
        Assert.Empty(set.GenericComplex);
        Assert.Empty(set.HostRules);
    }

    /// <summary>
    /// Exception rules are kept apart from blocking rules.
    /// </summary>
    [Fact]
    public void Parse_ExceptionRule_GoesToExceptions()
    {
        FilterSet set = parser.Parse(["||ads.example.com^", "@@||ads.example.com/ok^"]);

        Assert.Equal(1, set.Blocks.Count);
        Assert.Equal(1, set.Exceptions.Count);
    }

    /// <summary>
    /// Selectors for every site are sorted into plain ones, which can be joined, and the rest.
    /// </summary>
    [Fact]
    public void Parse_GenericSelectors_AreClassified()
    {
        FilterSet set = parser.Parse(["##.ad-banner", "###top-ad", "##div.ad", "##div[id^=\"ad-\"]"]);

        Assert.Equal([".ad-banner", "#top-ad", "div.ad"], set.GenericSimple);
        Assert.Equal(["div[id^=\"ad-\"]"], set.GenericComplex);
    }

    /// <summary>
    /// Selectors limited to sites are kept by site, with the sites they exclude, and exceptions likewise.
    /// </summary>
    [Fact]
    public void Parse_SiteSelectors_AreKeptBySite()
    {
        FilterSet set = parser.Parse(["a.com,b.com,~sub.a.com##.promo", "a.com#@#.keep"]);

        Assert.Equal(".promo", set.HostRules["a.com"].Single().Selector);
        Assert.Equal(["sub.a.com"], set.HostRules["b.com"].Single().ExcludedDomains);
        Assert.Contains(".keep", set.HostExceptions["a.com"]);
    }
}
