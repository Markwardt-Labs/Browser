namespace Tests.Integration.Bookmarks;

/// <summary>
/// Tests for <see cref="BookmarkFile"/> against a real folder.
/// </summary>
public sealed class BookmarkFileTests : IDisposable
{
    /// <summary>
    /// Creates a file in a folder that does not exist yet.
    /// </summary>
    public BookmarkFileTests()
        => file = new BookmarkFile(Path.Combine(folder, "bookmarks.json"));

    private readonly string folder = Path.Combine(Path.GetTempPath(), $"bookmarks-{Guid.NewGuid():N}");
    private readonly BookmarkFile file;

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
    /// Nothing has been saved yet, so there are no bookmarks.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Load_NothingSaved_ReturnsNone()
        => Assert.Empty(await file.Load());

    /// <summary>
    /// Saved bookmarks come back in order, including after the folder had to be created.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Save_ThenLoad_RoundTripsInOrder()
    {
        List<Bookmark> bookmarks = [new() { Title = "One", Address = "https://one.example" }, new() { Title = "Two", Address = "https://two.example" }];

        await file.Save(bookmarks);

        Assert.Equal(bookmarks, await file.Load());
    }

    /// <summary>
    /// Saving again replaces what was there.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Save_Twice_ReplacesPreviousContent()
    {
        await file.Save([new Bookmark { Title = "One", Address = "https://one.example" }]);
        await file.Save([new Bookmark { Title = "Two", Address = "https://two.example" }]);

        Assert.Equal("Two", Assert.Single(await file.Load()).Title);
    }

    /// <summary>
    /// A file that is not valid JSON is treated as having no bookmarks instead of failing.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Load_CorruptFile_ReturnsNone()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "bookmarks.json"), "{ not json");

        Assert.Empty(await file.Load());
    }
}
