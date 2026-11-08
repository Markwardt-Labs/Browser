namespace Markwardt.Browser.Ads;

/// <summary>
/// The script that keeps YouTube's ads out of its player.
/// </summary>
internal interface IYouTubeAdScript
{
    /// <summary>
    /// Finds the script for a frame.
    /// </summary>
    /// <param name="frameUrl">The address the frame is loading.</param>
    /// <returns>The script when the frame is YouTube's (or an embedded YouTube player), otherwise null.</returns>
    string? For(string frameUrl);
}

/// <inheritdoc />
internal sealed class YouTubeAdScript : IYouTubeAdScript
{
    private readonly string script = Read();

    /// <inheritdoc />
    public string? For(string frameUrl)
    {
        if (!Uri.TryCreate(frameUrl, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("https" or "http"))
        {
            return null;
        }

        string host = uri.Host.ToLowerInvariant();
        bool youTube = host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal) || host == "youtube-nocookie.com" || host.EndsWith(".youtube-nocookie.com", StringComparison.Ordinal);
        return youTube ? script : null;
    }

    private static string Read()
    {
        using Stream stream = typeof(YouTubeAdScript).Assembly.GetManifestResourceStream("Markwardt.Browser.Ads.YouTubeAds.js")!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
