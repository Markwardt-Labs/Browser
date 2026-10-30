namespace Markwardt.Browser.Bookmarks;

/// <summary>
/// A saved page.
/// </summary>
internal sealed record Bookmark
{
    /// <summary>
    /// Gets the title the page had when it was saved.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the address of the page.
    /// </summary>
    public required string Address { get; init; }
}
