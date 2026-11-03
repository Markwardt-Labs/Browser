namespace Markwardt.Browser.Ads;

/// <summary>
/// Fetches filter lists from the web.
/// </summary>
internal interface IFilterDownloader
{
    /// <summary>
    /// Downloads a list.
    /// </summary>
    /// <param name="url">The address of the list.</param>
    /// <param name="cancellation">Cancels the download.</param>
    /// <returns>The text of the list.</returns>
    Task<string> Download(string url, CancellationToken cancellation = default);
}

/// <inheritdoc />
internal sealed class HttpFilterDownloader : IFilterDownloader
{
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(90), DefaultRequestHeaders = { { "User-Agent", "Markwardt.Browser" } } };

    /// <inheritdoc />
    public async Task<string> Download(string url, CancellationToken cancellation = default)
        => await client.GetStringAsync(url, cancellation);
}
