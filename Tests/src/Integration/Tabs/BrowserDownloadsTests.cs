namespace Tests.Integration.Tabs;

/// <summary>
/// Tests for how <see cref="BrowserDownloads"/> picks file names, against a real folder.
/// </summary>
public sealed class BrowserDownloadsTests : IDisposable
{
    /// <summary>
    /// Creates the handler over a fresh folder.
    /// </summary>
    public BrowserDownloadsTests()
    {
        Directory.CreateDirectory(folder);
        downloads = new BrowserDownloads(folder, (_, _) => { });
    }

    private readonly string folder = Path.Combine(Path.GetTempPath(), $"downloads-{Guid.NewGuid():N}");
    private readonly BrowserDownloads downloads;

    /// <summary>
    /// A new name is used as it is.
    /// </summary>
    [Fact]
    public void Choose_NewName_IsKept()
        => Assert.Equal(Path.Combine(folder, "file.zip"), downloads.Choose("file.zip"));

    /// <summary>
    /// A name that exists gets a number before its extension, and the next one the next number.
    /// </summary>
    [Fact]
    public void Choose_ExistingName_GetsANumber()
    {
        File.WriteAllText(Path.Combine(folder, "file.zip"), "x");
        Assert.Equal(Path.Combine(folder, "file (1).zip"), downloads.Choose("file.zip"));

        File.WriteAllText(Path.Combine(folder, "file (1).zip"), "x");
        Assert.Equal(Path.Combine(folder, "file (2).zip"), downloads.Choose("file.zip"));
    }

    /// <summary>
    /// A name that tries to leave the folder, or is empty, cannot.
    /// </summary>
    /// <param name="suggested">The suggested name.</param>
    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("/tmp/evil.sh")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("..")]
    public void Choose_UnsafeName_StaysInFolder(string? suggested)
        => Assert.Equal(folder, Path.GetDirectoryName(downloads.Choose(suggested)));

    /// <inheritdoc />
    public void Dispose()
        => Directory.Delete(folder, true);
}
