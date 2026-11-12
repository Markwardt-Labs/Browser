namespace Markwardt.Browser.Tabs;

/// <summary>
/// What a page is doing that makes it unsafe to freeze, suspend or close.
/// </summary>
[Flags]
internal enum AutoLockReason
{
    /// <summary>
    /// Nothing.
    /// </summary>
    None = 0,

    /// <summary>
    /// The user has typed text into a field of the page that is still there.
    /// </summary>
    Typing = 1,

    /// <summary>
    /// A download or an upload is in progress.
    /// </summary>
    Transfer = 2,

    /// <summary>
    /// The page is playing sound.
    /// </summary>
    Sound = 4,
}
