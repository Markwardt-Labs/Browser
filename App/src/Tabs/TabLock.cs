namespace Markwardt.Browser.Tabs;

/// <summary>
/// Whether a tab is protected from the automatic cleanup of tabs that are out of view.
/// </summary>
internal enum TabLock
{
    /// <summary>
    /// Nothing protects the tab; it is frozen, suspended and closed as it sits unused.
    /// </summary>
    Unlocked,

    /// <summary>
    /// The page is doing something that would be lost (see <see cref="AutoLockReason"/>); the protection ends by itself when that stops.
    /// </summary>
    AutoLocked,

    /// <summary>
    /// The user locked the tab, which holds until they unlock it.
    /// </summary>
    Locked,
}
