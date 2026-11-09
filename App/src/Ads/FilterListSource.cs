namespace Markwardt.Browser.Ads;

/// <summary>
/// A filter list that is downloaded and kept up to date.
/// </summary>
internal sealed record FilterListSource
{
    /// <summary>
    /// Gets the name the list is kept under.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the address the list is downloaded from.
    /// </summary>
    public required string Url { get; init; }
}
