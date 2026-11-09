namespace Markwardt.Browser.Sites;

/// <summary>
/// Makes video mode available on every page: a script that can make the page's main video fill the page view. Site rules call it to enter automatically on pages that exist to watch one video.
/// </summary>
internal sealed class VideoModeRule : ISiteRule
{
    private readonly List<string> schemes = [Uri.UriSchemeHttps, Uri.UriSchemeHttp, Uri.UriSchemeFile];

    /// <inheritdoc />
    public string Script { get; } = typeof(VideoModeRule).Assembly.ReadText("Markwardt.Browser.Sites.VideoMode.js");

    /// <inheritdoc />
    public bool Matches(string address)
        => Uri.TryCreate(address, UriKind.Absolute, out Uri? uri) && schemes.Contains(uri.Scheme);
}
