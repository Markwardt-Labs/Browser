namespace Markwardt.Browser.Profile;

/// <summary>
/// The browser's cookies, which survive restarts so that sign-ins are remembered.
/// </summary>
internal interface ICookieStore
{
    /// <summary>
    /// Writes the cookies to disk. Chromium otherwise only does so in delayed batches, and not at all when the process is closed the way this one is.
    /// </summary>
    /// <param name="completed">Called, on a CEF thread, once the cookies are on disk.</param>
    void Flush(Action completed);
}

/// <inheritdoc />
internal sealed class CookieStore : ICookieStore
{
    /// <inheritdoc />
    public void Flush(Action completed)
        => CefCookieManager.GetGlobal(null).FlushStore(new FlushCallback(completed));
}
