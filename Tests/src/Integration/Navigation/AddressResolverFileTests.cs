namespace Tests.Integration.Navigation;

/// <summary>
/// Tests for <see cref="AddressResolver"/> against the real file system.
/// </summary>
public sealed class AddressResolverFileTests
{
    private readonly AddressResolver resolver = new();

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
}
