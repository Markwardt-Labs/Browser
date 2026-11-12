namespace Markwardt.Browser.Bookmarks;

/// <summary>
/// The user's bookmarks, saved whenever they change.
/// </summary>
internal interface IBookmarkList
{
    /// <summary>
    /// Gets the bookmarks in the order they were added.
    /// </summary>
    ObservableCollection<Bookmark> Items { get; }

    /// <summary>
    /// Determines whether a page is bookmarked.
    /// </summary>
    /// <param name="address">The address of the page.</param>
    /// <returns>True when a bookmark has that address.</returns>
    bool Contains(string address);

    /// <summary>
    /// Finds the bookmarks whose title contains some text, ignoring case, best matches first.
    /// </summary>
    /// <param name="text">The text to look for.</param>
    /// <param name="limit">The most bookmarks to return.</param>
    /// <returns>The matches; none when <paramref name="text"/> is blank. Titles that start with the text come before the rest.</returns>
    IReadOnlyList<Bookmark> Search(string text, int limit);

    /// <summary>
    /// Bookmarks a page, or removes its bookmark when it already has one.
    /// </summary>
    /// <param name="title">The title of the page, used when it is added.</param>
    /// <param name="address">The address of the page.</param>
    void Toggle(string title, string address);

    /// <summary>
    /// Removes a bookmark.
    /// </summary>
    /// <param name="bookmark">The bookmark to remove.</param>
    void Remove(Bookmark bookmark);

    /// <summary>
    /// Changes the title of a bookmark. A blank or unchanged title is ignored.
    /// </summary>
    /// <param name="bookmark">The bookmark to rename.</param>
    /// <param name="title">The new title.</param>
    void Rename(Bookmark bookmark, string title);

    /// <summary>
    /// Fills the list from what was saved last time, keeping any bookmark added in the meantime.
    /// </summary>
    /// <param name="cancellation">Cancels the load.</param>
    /// <returns>A task that completes when the list is filled.</returns>
    Task Load(CancellationToken cancellation = default);
}

/// <inheritdoc cref="IBookmarkList" />
/// <param name="storage">Reads and writes the saved bookmarks.</param>
internal sealed class BookmarkList(IBookmarkFile storage) : IBookmarkList
{
    private readonly SemaphoreSlim saving = new(1, 1);

    /// <inheritdoc />
    public ObservableCollection<Bookmark> Items { get; } = [];

    /// <inheritdoc />
    public bool Contains(string address)
        => Items.Any(bookmark => Matches(bookmark, address));

    /// <inheritdoc />
    public IReadOnlyList<Bookmark> Search(string text, int limit)
    {
        string query = text.Trim();

        if (query.Length == 0)
        {
            return [];
        }

        return [.. Items
            .Where(bookmark => bookmark.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(bookmark => bookmark.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .Take(limit)];
    }

    /// <inheritdoc />
    public void Toggle(string title, string address)
    {
        Bookmark? existing = Items.FirstOrDefault(bookmark => Matches(bookmark, address));

        if (existing is null)
        {
            Items.Add(new Bookmark { Title = string.IsNullOrWhiteSpace(title) ? address : title, Address = address });
        }
        else
        {
            Items.Remove(existing);
        }

        _ = Save();
    }

    /// <inheritdoc />
    public void Remove(Bookmark bookmark)
    {
        if (Items.Remove(bookmark))
        {
            _ = Save();
        }
    }

    /// <inheritdoc />
    public void Rename(Bookmark bookmark, string title)
    {
        string trimmed = title.Trim();
        int index = Items.IndexOf(bookmark);

        if (trimmed.Length == 0 || index < 0 || trimmed == bookmark.Title)
        {
            return;
        }

        Items[index] = bookmark with { Title = trimmed };
        _ = Save();
    }

    /// <inheritdoc />
    public async Task Load(CancellationToken cancellation = default)
    {
        IReadOnlyList<Bookmark> saved = await storage.Load(cancellation);

        for (int index = 0; index < saved.Count; index++)
        {
            if (!Contains(saved[index].Address))
            {
                Items.Insert(index, saved[index]);
            }
        }
    }

    private bool Matches(Bookmark bookmark, string address)
        => string.Equals(bookmark.Address, address, StringComparison.OrdinalIgnoreCase);

    private async Task Save()
    {
        await saving.WaitAsync();

        try
        {
            await storage.Save([.. Items]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
        finally
        {
            saving.Release();
        }
    }
}
