namespace Markwardt.Browser.Shortcuts;

/// <summary>
/// A key combination bound to a command.
/// </summary>
internal sealed record Shortcut
{
    /// <summary>
    /// Gets the key that triggers the command.
    /// </summary>
    public required Key Key { get; init; }

    /// <summary>
    /// Gets the Windows virtual key code of <see cref="Key"/>, which is how CEF identifies keys.
    /// </summary>
    public required int VirtualKey { get; init; }

    /// <summary>
    /// Gets the modifiers that must be held, and no others.
    /// </summary>
    public required KeyModifiers Modifiers { get; init; }

    /// <summary>
    /// Gets the command the combination triggers.
    /// </summary>
    public required ShortcutCommand Command { get; init; }
}
