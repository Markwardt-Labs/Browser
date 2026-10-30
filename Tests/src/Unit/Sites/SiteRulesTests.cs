namespace Tests.Unit.Sites;

/// <summary>
/// Tests for <see cref="SiteRules"/>.
/// </summary>
public sealed class SiteRulesTests
{
    private readonly Mock<ISiteRule> first = new();
    private readonly Mock<ISiteRule> second = new();
    private readonly Mock<ISiteRule> third = new();
    private readonly SiteRules rules;

    /// <summary>
    /// Creates the set under test over three mock rules.
    /// </summary>
    public SiteRulesTests()
    {
        first.Setup(rule => rule.Script).Returns("first");
        second.Setup(rule => rule.Script).Returns("second");
        third.Setup(rule => rule.Script).Returns("third");
        rules = new SiteRules([first.Object, second.Object, third.Object]);
    }

    /// <summary>
    /// Every matching rule contributes its script, in rule order.
    /// </summary>
    [Fact]
    public void Scripts_ReturnsMatchingScriptsInOrder()
    {
        first.Setup(rule => rule.Matches("https://example.com")).Returns(true);
        third.Setup(rule => rule.Matches("https://example.com")).Returns(true);

        Assert.Equal(["first", "third"], rules.Scripts("https://example.com"));
    }

    /// <summary>
    /// An address no rule matches gets no scripts.
    /// </summary>
    [Fact]
    public void Scripts_NoMatch_ReturnsEmpty()
        => Assert.Empty(rules.Scripts("https://example.com"));
}
