namespace Markwardt.Browser.Navigation;

/// <summary>
/// What submitting the address bar should do.
/// </summary>
internal sealed record AddressBarAction
{
    /// <summary>
    /// Gets the absolute address to load.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Gets a value indicating whether the address opens in a new tab instead of the active one.
    /// </summary>
    public required bool InNewTab { get; init; }
}
