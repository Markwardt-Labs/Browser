namespace Markwardt.Browser.Profile;

/// <summary>
/// Reports back when CEF has finished writing the cookie store to disk.
/// </summary>
/// <param name="completed">Called, on a CEF thread, once the flush is done.</param>
internal sealed class FlushCallback(Action completed) : CefCompletionCallback
{
    /// <inheritdoc />
    protected override void OnComplete()
        => completed();
}
