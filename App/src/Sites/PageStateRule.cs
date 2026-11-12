namespace Markwardt.Browser.Sites;

/// <summary>
/// Makes every web page report what the browser needs to know about it: whether the user has typed into it, whether it is uploading or playing sound, and where its scroll position and long video or audio are, so that the tab can be protected while it matters and restored after it is suspended.
/// </summary>
internal sealed class PageStateRule : ISiteRule
{
    private readonly List<string> schemes = [Uri.UriSchemeHttps, Uri.UriSchemeHttp, Uri.UriSchemeFile];

    /// <inheritdoc />
    public string Script { get; } = typeof(PageStateRule).Assembly.ReadText("Markwardt.Browser.Sites.PageState.js");

    /// <inheritdoc />
    public bool Matches(string address)
        => Uri.TryCreate(address, UriKind.Absolute, out Uri? uri) && schemes.Contains(uri.Scheme);
}
