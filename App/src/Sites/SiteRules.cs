namespace Markwardt.Browser.Sites;

/// <summary>
/// The set of per-site rules.
/// </summary>
internal interface ISiteRules
{
    /// <summary>
    /// Collects the scripts that adjust a frame.
    /// </summary>
    /// <param name="address">The address the frame has loaded.</param>
    /// <returns>The script of every rule that matches, in rule order.</returns>
    IReadOnlyList<string> Scripts(string address);
}

/// <inheritdoc />
/// <param name="rules">The rules, whose scripts run in this order.</param>
internal sealed class SiteRules(IReadOnlyList<ISiteRule> rules) : ISiteRules
{
    /// <inheritdoc />
    public IReadOnlyList<string> Scripts(string address)
        => [.. rules.Where(rule => rule.Matches(address)).Select(rule => rule.Script)];
}
