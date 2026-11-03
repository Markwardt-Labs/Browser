namespace Tests.Integration.Ads;

/// <summary>
/// Tests for <see cref="FilterLists"/> against a real folder.
/// </summary>
public sealed class FilterListsTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), $"filters-{Guid.NewGuid():N}");
    private readonly Mock<IFilterDownloader> downloader = new();
    private readonly FilterLists lists;

    /// <summary>
    /// Creates the lists over a mock downloader and one source.
    /// </summary>
    public FilterListsTests()
    {
        downloader.Setup(d => d.Download("https://lists.example/a.txt", It.IsAny<CancellationToken>())).ReturnsAsync("[Adblock Plus 2.0]\n||downloaded.example^\n");
        lists = new FilterLists(folder, [new FilterListSource { Name = "a", Url = "https://lists.example/a.txt" }], downloader.Object, typeof(AdBlocker).Assembly);
    }

    /// <summary>
    /// Deletes the test folder.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>
    /// With nothing downloaded yet only the built-in list is read.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Read_NothingDownloaded_ReturnsBuiltInOnly()
    {
        IReadOnlyList<string> texts = await lists.Read();

        Assert.Single(texts);
        Assert.Contains("doubleclick.net", texts[0]);
    }

    /// <summary>
    /// A missing list is downloaded and then read after the built-in one.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Refresh_MissingList_IsDownloaded()
    {
        Assert.True(await lists.Refresh());

        IReadOnlyList<string> texts = await lists.Read();
        Assert.Equal(2, texts.Count);
        Assert.Contains("downloaded.example", texts[1]);
        Assert.Single(Directory.GetFiles(folder));
    }

    /// <summary>
    /// A list that is recent is not downloaded again, and an old one is.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Refresh_OnlyWhenOld()
    {
        await lists.Refresh();

        Assert.False(await lists.Refresh());
        downloader.Verify(d => d.Download(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        File.SetLastWriteTimeUtc(Path.Combine(folder, "a.txt"), DateTime.UtcNow.AddDays(-4));

        Assert.True(await lists.Refresh());
        downloader.Verify(d => d.Download(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    /// <summary>
    /// A failed download or an answer that is not a filter list leaves the old list in place.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Refresh_BadDownload_KeepsOldList()
    {
        await lists.Refresh();
        File.SetLastWriteTimeUtc(Path.Combine(folder, "a.txt"), DateTime.UtcNow.AddDays(-4));

        downloader.Setup(d => d.Download(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException());
        Assert.False(await lists.Refresh());

        downloader.Setup(d => d.Download(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("<html>Error</html>");
        Assert.False(await lists.Refresh());

        Assert.Contains("downloaded.example", (await lists.Read())[1]);
    }
}
