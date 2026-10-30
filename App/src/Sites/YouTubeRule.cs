namespace Markwardt.Browser.Sites;

/// <summary>
/// Plays every YouTube video at a fixed 1080p, and puts watch pages in theater mode. It also covers embedded players, which are frames of their own.
/// </summary>
internal sealed class YouTubeRule : ISiteRule
{
    private readonly List<string> hosts = ["youtube.com", "youtube-nocookie.com"];

    /// <inheritdoc />
    public string Script { get; } = ReadScript();

    private static string ReadScript()
    {
        using Stream stream = typeof(YouTubeRule).Assembly.GetManifestResourceStream("Markwardt.Browser.Sites.YouTube.js")!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    /// <inheritdoc />
    public bool Matches(string address)
        => Uri.TryCreate(address, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && hosts.Any(host => uri.Host == host || uri.Host.EndsWith($".{host}", StringComparison.OrdinalIgnoreCase));
}
