namespace Markwardt.Browser.Launch;

/// <summary>
/// What the command line asked for.
/// </summary>
internal sealed record LaunchOptions
{
    /// <summary>
    /// Gets the address given on its own, which opens as the first page with the overview raised over it; null for none.
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// Gets the address to go to straight away, without raising the overview; null for none.
    /// </summary>
    public string? Goto { get; init; }

    /// <summary>
    /// Gets a value indicating whether the overview is switched off and the top bar always shows, with only the window controls in it.
    /// </summary>
    public bool Isolate { get; init; }

    /// <summary>
    /// Gets the state the first window starts in; null for the default.
    /// </summary>
    public WindowState? Window { get; init; }

    /// <summary>
    /// Gets a value indicating whether the minimize and maximize buttons are hidden.
    /// </summary>
    public bool CloseOnly { get; init; }
}
