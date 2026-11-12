namespace Markwardt.Browser.Tabs;

/// <summary>
/// How long a tab that is out of view and unlocked waits before each step of cleanup.
/// </summary>
internal sealed record TabCleanupPolicy
{
    /// <summary>
    /// Gets how long a tab is out of view before its page is frozen: it keeps its memory and state but stops running.
    /// </summary>
    public required TimeSpan FreezeAfter { get; init; }

    /// <summary>
    /// Gets how long a tab is out of view before its page is released altogether.
    /// </summary>
    public required TimeSpan SuspendAfter { get; init; }

    /// <summary>
    /// Gets how long a tab is out of view before it is closed.
    /// </summary>
    public required TimeSpan CloseAfter { get; init; }
}
