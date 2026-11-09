namespace Markwardt.Browser.Ads;

/// <summary>
/// What hides one element of a page, for the sites that rule is limited to.
/// </summary>
/// <param name="Selector">The CSS selector of the elements to hide.</param>
/// <param name="ExcludedDomains">The sites it does not apply to.</param>
internal sealed record CosmeticRule(string Selector, IReadOnlyList<string> ExcludedDomains);
