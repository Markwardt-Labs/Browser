namespace Tests.Unit.Navigation;

/// <summary>
/// Tests for <see cref="AddressResolver"/>.
/// </summary>
public sealed class AddressResolverTests
{
    private readonly AddressResolver resolver = new();

    /// <summary>
    /// An empty address resolves to the blank page.
    /// </summary>
    /// <param name="address">The address.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_Blank_ReturnsAboutBlank(string address)
        => Assert.Equal("about:blank", resolver.Resolve(address));

    /// <summary>
    /// An address that already carries a scheme is kept.
    /// </summary>
    /// <param name="address">The address.</param>
    [Theory]
    [InlineData("https://example.com/path?q=1")]
    [InlineData("http://localhost:8080")]
    [InlineData("file:///tmp/page.html")]
    [InlineData("about:blank")]
    [InlineData("view-source:https://example.com")]
    public void Resolve_WithScheme_ReturnsAddress(string address)
        => Assert.Equal(address, resolver.Resolve(address));

    /// <summary>
    /// An address without a scheme gets https.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="expected">The expected result.</param>
    [Theory]
    [InlineData("example.com", "https://example.com")]
    [InlineData("  youtube.com/watch?v=1  ", "https://youtube.com/watch?v=1")]
    [InlineData("localhost:5000", "https://localhost:5000")]
    public void Resolve_WithoutScheme_PrependsHttps(string address, string expected)
        => Assert.Equal(expected, resolver.Resolve(address));

    /// <summary>
    /// The path of a file that exists, as a launcher passes it, becomes a file address, including characters that need escaping.
    /// </summary>
    [Fact]
    public void Resolve_ExistingFilePath_ReturnsFileAddress()
    {
        string path = Path.Combine(Path.GetTempPath(), $"page {Guid.NewGuid():N}.html");
        File.WriteAllText(path, "<html></html>");

        try
        {
            string resolved = resolver.Resolve(path);

            Assert.StartsWith("file:///", resolved);
            Assert.EndsWith(".html", resolved);
            Assert.Contains("%20", resolved);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A rooted path that does not exist is not guessed to be a file, so it still gets https like any other address.
    /// </summary>
    [Fact]
    public void Resolve_MissingRootedPath_FallsBackToHttps()
        => Assert.Equal("https:///no/such/file.html", resolver.Resolve("/no/such/file.html"));

    /// <summary>
    /// Text that starts with a colon is an address for the active tab.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="expected">The expected address.</param>
    [Theory]
    [InlineData(":youtube.com", "https://youtube.com")]
    [InlineData("  :  youtube.com  ", "https://youtube.com")]
    [InlineData(":https://example.com/a", "https://example.com/a")]
    [InlineData(":about:blank", "about:blank")]
    public void Interpret_LeadingColon_NavigatesActiveTab(string text, string expected)
    {
        AddressBarAction? action = resolver.Interpret(text);

        Assert.Equal(expected, action?.Address);
        Assert.False(action?.InNewTab);
    }

    /// <summary>
    /// Anything else is a web search, opened in a new tab, with the text escaped.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="expected">The expected address.</param>
    [Theory]
    [InlineData("hello world", "https://www.google.com/search?q=hello%20world")]
    [InlineData("youtube.com", "https://www.google.com/search?q=youtube.com")]
    [InlineData("a & b?", "https://www.google.com/search?q=a%20%26%20b%3F")]
    public void Interpret_PlainText_SearchesInNewTab(string text, string expected)
    {
        AddressBarAction? action = resolver.Interpret(text);

        Assert.Equal(expected, action?.Address);
        Assert.True(action?.InNewTab);
    }

    /// <summary>
    /// Empty text, or a colon with nothing after it, does nothing.
    /// </summary>
    /// <param name="text">The text.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(":")]
    [InlineData(" :  ")]
    public void Interpret_NothingToDo_ReturnsNull(string text)
        => Assert.Null(resolver.Interpret(text));

    /// <summary>
    /// The address bar is empty for the blank page and shows other addresses behind a colon, so submitting them unchanged loads them again.
    /// </summary>
    /// <param name="address">The address of the page.</param>
    /// <param name="expected">The text expected in the address bar.</param>
    [Theory]
    [InlineData("about:blank", "")]
    [InlineData("ABOUT:BLANK", "")]
    [InlineData("", "")]
    [InlineData("https://example.com/", ":https://example.com/")]
    public void Describe_Address_ReturnsAddressBarText(string address, string expected)
        => Assert.Equal(expected, resolver.Describe(address));

    /// <summary>
    /// What the address bar shows for a page loads that same page when submitted.
    /// </summary>
    [Fact]
    public void Describe_ThenInterpret_RoundTrips()
        => Assert.Equal("https://example.com/path", resolver.Interpret(resolver.Describe("https://example.com/path"))?.Address);
}
