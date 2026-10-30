namespace Markwardt.Browser.Navigation;

/// <summary>
/// Turns what the user types into the address bar, or is handed on the command line, into something the browser can load.
/// </summary>
internal interface IAddressResolver
{
    /// <summary>
    /// Resolves an address, written with or without its scheme, or the path of a file or folder that exists.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns>An absolute address; the blank page when <paramref name="address"/> is empty.</returns>
    string Resolve(string address);

    /// <summary>
    /// Decides what submitting the address bar does: text starting with a colon is an address for the active tab, anything else is a web search in a new tab.
    /// </summary>
    /// <param name="text">The text in the address bar.</param>
    /// <returns>The action, or null when there is nothing to do.</returns>
    AddressBarAction? Interpret(string text);

    /// <summary>
    /// Gives the text the address bar shows for a page: empty for the blank page, otherwise the address behind a colon so that submitting it unchanged reloads the page.
    /// </summary>
    /// <param name="address">The address of the page.</param>
    /// <returns>The text to show.</returns>
    string Describe(string address);
}

/// <inheritdoc />
internal sealed class AddressResolver : IAddressResolver
{
    private readonly string searchUrl = "https://www.google.com/search?q=";
    private readonly string blank = "about:blank";
    private readonly string[] schemes = ["about:", "data:", "file:", "blob:", "view-source:"];

    /// <inheritdoc />
    public string Resolve(string address)
    {
        string text = address.Trim();

        if (text.Length == 0)
        {
            return blank;
        }

        if (text.Contains("://", StringComparison.Ordinal) || schemes.Any(scheme => text.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)))
        {
            return text;
        }

        if (Path.IsPathRooted(text) && (File.Exists(text) || Directory.Exists(text)))
        {
            return new Uri(Path.GetFullPath(text)).AbsoluteUri;
        }

        return $"https://{text}";
    }

    /// <inheritdoc />
    public AddressBarAction? Interpret(string text)
    {
        string trimmed = text.Trim();

        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed[0] != ':')
        {
            return new AddressBarAction { Address = searchUrl + Uri.EscapeDataString(trimmed), InNewTab = true };
        }

        string address = trimmed[1..].Trim();
        return address.Length == 0 ? null : new AddressBarAction { Address = Resolve(address), InNewTab = false };
    }

    /// <inheritdoc />
    public string Describe(string address)
        => address.Length == 0 || string.Equals(address, blank, StringComparison.OrdinalIgnoreCase) ? string.Empty : $":{address}";
}
