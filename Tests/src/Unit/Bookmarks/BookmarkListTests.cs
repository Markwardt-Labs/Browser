namespace Tests.Unit.Bookmarks;

/// <summary>
/// Tests for <see cref="BookmarkList"/>.
/// </summary>
public sealed class BookmarkListTests
{
    /// <summary>
    /// Creates the list under test over a mock storage that records what it is asked to save.
    /// </summary>
    public BookmarkListTests()
    {
        storage.Setup(s => s.Load(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        storage.Setup(s => s.Save(It.IsAny<IReadOnlyList<Bookmark>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Bookmark>, CancellationToken>((bookmarks, _) => saves.Add(bookmarks))
            .Returns(Task.CompletedTask);
        list = new BookmarkList(storage.Object);
    }

    private readonly Mock<IBookmarkFile> storage = new();
    private readonly List<IReadOnlyList<Bookmark>> saves = [];
    private readonly BookmarkList list;

    /// <summary>
    /// Toggling a page that is not bookmarked adds it, and the list is saved.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Toggle_NewPage_AddsAndSaves()
    {
        list.Toggle("Example", "https://example.com");
        await Saved(1);

        Assert.Equal("Example", Assert.Single(list.Items).Title);
        Assert.True(list.Contains("https://example.com"));
        Assert.Equal("https://example.com", Assert.Single(saves[0]).Address);
    }

    /// <summary>
    /// Toggling a page that is already bookmarked removes it, and the list is saved.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Toggle_BookmarkedPage_RemovesAndSaves()
    {
        list.Toggle("Example", "https://example.com");
        list.Toggle("Example", "https://example.com");
        await Saved(2);

        Assert.Empty(list.Items);
        Assert.False(list.Contains("https://example.com"));
        Assert.Empty(saves[1]);
    }

    /// <summary>
    /// Addresses are compared ignoring case, so one page is never bookmarked twice.
    /// </summary>
    [Fact]
    public void Contains_DifferentCase_Matches()
    {
        list.Toggle("Example", "https://Example.com/Page");

        Assert.True(list.Contains("https://example.com/page"));
    }

    /// <summary>
    /// A page without a title is bookmarked under its address.
    /// </summary>
    [Fact]
    public void Toggle_BlankTitle_UsesAddress()
    {
        list.Toggle("  ", "https://example.com");

        Assert.Equal("https://example.com", list.Items[0].Title);
    }

    /// <summary>
    /// Removing a bookmark takes it out and saves.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Remove_Bookmark_RemovesAndSaves()
    {
        list.Toggle("One", "https://one.example");
        list.Toggle("Two", "https://two.example");
        await Saved(2);

        list.Remove(list.Items[0]);
        await Saved(3);

        Assert.Equal("Two", Assert.Single(list.Items).Title);
    }

    /// <summary>
    /// Removing something that is not in the list saves nothing.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Remove_UnknownBookmark_DoesNotSave()
    {
        list.Remove(new Bookmark { Title = "Stranger", Address = "https://stranger.example" });
        await Task.Delay(50);

        storage.Verify(s => s.Save(It.IsAny<IReadOnlyList<Bookmark>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Renaming changes the title in place, keeps the address and position, and saves.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Rename_Bookmark_ChangesTitleInPlaceAndSaves()
    {
        list.Toggle("One", "https://one.example");
        list.Toggle("Two", "https://two.example");
        await Saved(2);

        list.Rename(list.Items[0], "  First  ");
        await Saved(3);

        Assert.Equal(["First", "Two"], list.Items.Select(bookmark => bookmark.Title));
        Assert.Equal("https://one.example", list.Items[0].Address);
        Assert.Equal("First", saves[2][0].Title);
    }

    /// <summary>
    /// A blank, unchanged or unknown rename does nothing and saves nothing.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Rename_BlankUnchangedOrUnknown_IsIgnored()
    {
        list.Toggle("One", "https://one.example");
        await Saved(1);
        Bookmark one = list.Items[0];

        list.Rename(one, "   ");
        list.Rename(one, "One");
        list.Rename(new Bookmark { Title = "Stranger", Address = "https://stranger.example" }, "New");
        await Task.Delay(50);

        Assert.Equal("One", Assert.Single(list.Items).Title);
        Assert.Single(saves);
    }

    /// <summary>
    /// Search finds titles containing the text ignoring case, with titles that start with it first, and never matches on the address.
    /// </summary>
    [Fact]
    public void Search_Text_MatchesTitlesStartingWithItFirst()
    {
        list.Toggle("My YouTube", "https://one.example");
        list.Toggle("YouTube Music", "https://two.example");
        list.Toggle("Wikipedia", "https://youtube.example");

        Assert.Equal(["YouTube Music", "My YouTube"], list.Search("yOuT", 10).Select(bookmark => bookmark.Title));
    }

    /// <summary>
    /// Search returns nothing for blank text and no more than the limit otherwise.
    /// </summary>
    [Fact]
    public void Search_BlankOrManyMatches_ReturnsNothingOrLimit()
    {
        list.Toggle("Alpha", "https://a.example");
        list.Toggle("Alpine", "https://b.example");
        list.Toggle("Alps", "https://c.example");

        Assert.Empty(list.Search("   ", 5));
        Assert.Equal(2, list.Search("al", 2).Count);
    }

    /// <summary>
    /// Loading fills the list in saved order.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Load_SavedBookmarks_FillsInOrder()
    {
        storage.Setup(s => s.Load(It.IsAny<CancellationToken>())).ReturnsAsync([new Bookmark { Title = "One", Address = "https://one.example" }, new Bookmark { Title = "Two", Address = "https://two.example" }]);

        await list.Load();

        Assert.Equal(["One", "Two"], list.Items.Select(bookmark => bookmark.Title));
    }

    /// <summary>
    /// A bookmark added before loading finished is kept, and nothing is duplicated.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Load_AfterAdding_KeepsBothWithoutDuplicates()
    {
        list.Toggle("Two", "https://two.example");
        storage.Setup(s => s.Load(It.IsAny<CancellationToken>())).ReturnsAsync([new Bookmark { Title = "One", Address = "https://one.example" }, new Bookmark { Title = "Two", Address = "https://two.example" }]);

        await list.Load();

        Assert.Equal(["One", "Two"], list.Items.Select(bookmark => bookmark.Title));
    }

    /// <summary>
    /// A failing disk does not stop bookmarks from working in memory.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Toggle_WhenSavingFails_StillUpdatesList()
    {
        storage.Setup(s => s.Save(It.IsAny<IReadOnlyList<Bookmark>>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("disk full"));

        list.Toggle("Example", "https://example.com");
        await Task.Delay(50);

        Assert.True(list.Contains("https://example.com"));
    }

    private async Task Saved(int count)
    {
        for (int attempt = 0; attempt < 100 && saves.Count < count; attempt++)
        {
            await Task.Delay(20);
        }

        Assert.True(saves.Count >= count, $"Expected {count} saves but saw {saves.Count}.");
    }
}
