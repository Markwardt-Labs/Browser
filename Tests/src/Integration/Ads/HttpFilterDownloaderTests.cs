namespace Tests.Integration.Ads;

/// <summary>
/// Tests for <see cref="HttpFilterDownloader"/> against a local web server.
/// </summary>
public sealed class HttpFilterDownloaderTests : IDisposable
{
    /// <summary>
    /// Starts a server on a free local port.
    /// </summary>
    public HttpFilterDownloaderTests()
    {
        int port;

        using (TcpListener probe = new(IPAddress.Loopback, 0))
        {
            probe.Start();
            port = ((IPEndPoint)probe.LocalEndpoint).Port;
        }

        address = $"http://127.0.0.1:{port}/list.txt";
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        _ = Serve();
    }

    private readonly HttpListener listener = new();
    private readonly string address;
    private readonly HttpFilterDownloader downloader = new();
    private string? userAgent;
    private int status = 200;

    /// <summary>
    /// The text of the list is returned, and the request says who is asking.
    /// </summary>
    [Fact]
    public async Task Download_ReturnsTheText()
    {
        string text = await downloader.Download(address);

        Assert.Equal("||ads.example^", text);
        Assert.Equal("Markwardt.Browser", userAgent);
    }

    /// <summary>
    /// A failing server is an error for the caller to handle, not an empty list.
    /// </summary>
    [Fact]
    public async Task Download_ServerError_Throws()
    {
        status = 500;

        await Assert.ThrowsAsync<HttpRequestException>(() => downloader.Download(address));
    }

    /// <inheritdoc />
    public void Dispose()
        => listener.Close();

    private async Task Serve()
    {
        try
        {
            while (listener.IsListening)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                userAgent = context.Request.UserAgent;
                context.Response.StatusCode = status;
                byte[] body = Encoding.UTF8.GetBytes("||ads.example^");
                await context.Response.OutputStream.WriteAsync(body);
                context.Response.Close();
            }
        }
        catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException or InvalidOperationException)
        {
        }
    }
}
