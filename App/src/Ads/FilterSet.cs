namespace Markwardt.Browser.Ads;

/// <summary>
/// What hides one element of a page, for the sites that rule is limited to.
/// </summary>
/// <param name="Selector">The CSS selector of the elements to hide.</param>
/// <param name="ExcludedDomains">The sites it does not apply to.</param>
internal sealed record CosmeticRule(string Selector, IReadOnlyList<string> ExcludedDomains);

/// <summary>
/// Everything read from a set of filter lists, ready to be used.
/// </summary>
internal sealed record FilterSet
{
    /// <summary>
    /// Gets the rules that block requests.
    /// </summary>
    public required IRuleIndex Blocks { get; init; }

    /// <summary>
    /// Gets the rules that allow requests the blocking rules would stop.
    /// </summary>
    public required IRuleIndex Exceptions { get; init; }

    /// <summary>
    /// Gets the plain class, id and tag selectors that hide elements on every site, which can be joined into one rule.
    /// </summary>
    public IReadOnlyList<string> GenericSimple { get; init; } = [];

    /// <summary>
    /// Gets the other selectors that hide elements on every site, which each need a rule of their own.
    /// </summary>
    public IReadOnlyList<string> GenericComplex { get; init; } = [];

    /// <summary>
    /// Gets the selectors that hide elements, by the site (and its subdomains) they are limited to.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<CosmeticRule>> HostRules { get; init; } = new Dictionary<string, IReadOnlyList<CosmeticRule>>();

    /// <summary>
    /// Gets the selectors that are not hidden after all, by the site (and its subdomains) they are allowed on.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> HostExceptions { get; init; } = new Dictionary<string, IReadOnlySet<string>>();
}
