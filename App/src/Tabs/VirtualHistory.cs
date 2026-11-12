namespace Markwardt.Browser.Tabs;

/// <summary>
/// The back and forward history a tab kept from before it was suspended. CEF cannot be given a history back, so the pages are kept here and loaded in place of the browser's current page when the user goes back or forward past what the new browser knows.
/// </summary>
internal interface IVirtualHistory
{
    /// <summary>
    /// Gets how many pages are kept behind the current one.
    /// </summary>
    int BackCount { get; }

    /// <summary>
    /// Gets how many pages are kept ahead of the current one.
    /// </summary>
    int ForwardCount { get; }

    /// <summary>
    /// Replaces what is kept with the history of a page that is about to be suspended.
    /// </summary>
    /// <param name="entries">Every page of the history, oldest first.</param>
    /// <param name="index">Which of them is the current one.</param>
    void Load(IReadOnlyList<HistoryEntry> entries, int index);

    /// <summary>
    /// Forgets the pages ahead, because the user went somewhere new.
    /// </summary>
    void ClearForward();

    /// <summary>
    /// Tells whether there is a page to go back to.
    /// </summary>
    /// <param name="nativeCanGoBack">Whether the browser's own history has one.</param>
    /// <returns>True if going back leads somewhere.</returns>
    bool CanGoBack(bool nativeCanGoBack);

    /// <summary>
    /// Tells whether there is a page to go forward to.
    /// </summary>
    /// <param name="nativeCanGoForward">Whether the browser's own history has one.</param>
    /// <returns>True if going forward leads somewhere.</returns>
    bool CanGoForward(bool nativeCanGoForward);

    /// <summary>
    /// Steps back, preferring the browser's own history, which holds the nearer pages.
    /// </summary>
    /// <param name="nativeCanGoBack">Whether the browser's own history has a page behind.</param>
    /// <param name="current">The page being left, which becomes the next one forward when the step is restored from here.</param>
    /// <param name="entry">The page to load, for <see cref="HistoryMove.Restored"/>.</param>
    /// <returns>How to carry the step out.</returns>
    HistoryMove Back(bool nativeCanGoBack, HistoryEntry current, out HistoryEntry? entry);

    /// <summary>
    /// Steps forward, preferring the pages kept here, which are nearer than any the browser has ahead.
    /// </summary>
    /// <param name="nativeCanGoForward">Whether the browser's own history has a page ahead.</param>
    /// <param name="current">The page being left, which becomes the next one back when the step is restored from here.</param>
    /// <param name="entry">The page to load, for <see cref="HistoryMove.Restored"/>.</param>
    /// <returns>How to carry the step out.</returns>
    HistoryMove Forward(bool nativeCanGoForward, HistoryEntry current, out HistoryEntry? entry);

    /// <summary>
    /// Joins what is kept here with the browser's own history into one history, for suspending again.
    /// </summary>
    /// <param name="native">The browser's own entries, oldest first.</param>
    /// <param name="nativeIndex">Which of them is current.</param>
    /// <param name="index">Receives which entry of the result is current.</param>
    /// <returns>The whole history, oldest first.</returns>
    IReadOnlyList<HistoryEntry> Compose(IReadOnlyList<HistoryEntry> native, int nativeIndex, out int index);
}

/// <inheritdoc />
internal sealed class VirtualHistory : IVirtualHistory
{
    private readonly List<HistoryEntry> back = [];
    private readonly List<HistoryEntry> forward = [];

    /// <inheritdoc />
    public int BackCount => back.Count;

    /// <inheritdoc />
    public int ForwardCount => forward.Count;

    /// <inheritdoc />
    public void Load(IReadOnlyList<HistoryEntry> entries, int index)
    {
        back.Clear();
        forward.Clear();

        if (index < 0 || index >= entries.Count)
        {
            return;
        }

        back.AddRange(entries.Take(index));
        forward.AddRange(entries.Skip(index + 1));
    }

    /// <inheritdoc />
    public void ClearForward()
        => forward.Clear();

    /// <inheritdoc />
    public bool CanGoBack(bool nativeCanGoBack)
        => nativeCanGoBack || back.Count > 0;

    /// <inheritdoc />
    public bool CanGoForward(bool nativeCanGoForward)
        => nativeCanGoForward || forward.Count > 0;

    /// <inheritdoc />
    public HistoryMove Back(bool nativeCanGoBack, HistoryEntry current, out HistoryEntry? entry)
    {
        entry = null;

        if (nativeCanGoBack)
        {
            return HistoryMove.Native;
        }

        if (back.Count == 0)
        {
            return HistoryMove.None;
        }

        entry = back[^1];
        back.RemoveAt(back.Count - 1);
        forward.Insert(0, current);
        return HistoryMove.Restored;
    }

    /// <inheritdoc />
    public HistoryMove Forward(bool nativeCanGoForward, HistoryEntry current, out HistoryEntry? entry)
    {
        entry = null;

        if (forward.Count > 0)
        {
            entry = forward[0];
            forward.RemoveAt(0);
            back.Add(current);
            return HistoryMove.Restored;
        }

        return nativeCanGoForward ? HistoryMove.Native : HistoryMove.None;
    }

    /// <inheritdoc />
    public IReadOnlyList<HistoryEntry> Compose(IReadOnlyList<HistoryEntry> native, int nativeIndex, out int index)
    {
        if (nativeIndex < 0 || nativeIndex >= native.Count)
        {
            index = back.Count;
            return [.. back, .. forward];
        }

        index = back.Count + nativeIndex;
        return [.. back, .. native.Take(nativeIndex + 1), .. forward, .. native.Skip(nativeIndex + 1)];
    }
}
