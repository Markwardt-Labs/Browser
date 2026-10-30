namespace Markwardt.Browser.Shortcuts;

/// <summary>
/// The keyboard shortcuts of the browser window.
/// </summary>
internal interface IShortcutMap
{
    /// <summary>
    /// Finds the command for an Avalonia key press.
    /// </summary>
    /// <param name="key">The key pressed.</param>
    /// <param name="modifiers">The modifiers held.</param>
    /// <returns>The command, or null when the combination is not a shortcut.</returns>
    ShortcutCommand? Find(Key key, KeyModifiers modifiers);

    /// <summary>
    /// Finds the command for a key event reported by CEF, which identifies keys by Windows virtual key code.
    /// </summary>
    /// <param name="keyEvent">The key event.</param>
    /// <returns>The command, or null when the event is not a shortcut, or is a typed character.</returns>
    ShortcutCommand? Find(CefKeyEvent keyEvent);
}

/// <inheritdoc />
internal sealed class ShortcutMap : IShortcutMap
{
    private readonly List<Shortcut> shortcuts =
    [
        new() { Key = Key.T, VirtualKey = 0x54, Modifiers = KeyModifiers.Control, Command = ShortcutCommand.NewTab },
        new() { Key = Key.F2, VirtualKey = 0x71, Modifiers = KeyModifiers.None, Command = ShortcutCommand.NewTab },
        new() { Key = Key.W, VirtualKey = 0x57, Modifiers = KeyModifiers.Control, Command = ShortcutCommand.CloseTab },
        new() { Key = Key.L, VirtualKey = 0x4C, Modifiers = KeyModifiers.Control, Command = ShortcutCommand.FocusAddress },
        new() { Key = Key.F1, VirtualKey = 0x70, Modifiers = KeyModifiers.None, Command = ShortcutCommand.ToggleOverview },
        new() { Key = Key.R, VirtualKey = 0x52, Modifiers = KeyModifiers.Control, Command = ShortcutCommand.Reload },
        new() { Key = Key.F5, VirtualKey = 0x74, Modifiers = KeyModifiers.None, Command = ShortcutCommand.Reload },
        new() { Key = Key.F, VirtualKey = 0x46, Modifiers = KeyModifiers.Control, Command = ShortcutCommand.Find },
        new() { Key = Key.F4, VirtualKey = 0x73, Modifiers = KeyModifiers.None, Command = ShortcutCommand.ToggleVideoMode },
        new() { Key = Key.Left, VirtualKey = 0x25, Modifiers = KeyModifiers.Alt, Command = ShortcutCommand.Back },
        new() { Key = Key.Right, VirtualKey = 0x27, Modifiers = KeyModifiers.Alt, Command = ShortcutCommand.Forward },
    ];

    /// <inheritdoc />
    public ShortcutCommand? Find(Key key, KeyModifiers modifiers)
        => shortcuts.FirstOrDefault(shortcut => shortcut.Key == key && shortcut.Modifiers == modifiers)?.Command;

    /// <inheritdoc />
    public ShortcutCommand? Find(CefKeyEvent keyEvent)
    {
        // A Char event carries the typed character in the key code field, so "p" (112) would otherwise match F1 (0x70).
        if (keyEvent.EventType == CefKeyEventType.Char)
        {
            return null;
        }

        KeyModifiers modifiers = KeyModifiers.None;

        if (keyEvent.Modifiers.HasFlag(CefEventFlags.ControlDown))
        {
            modifiers |= KeyModifiers.Control;
        }

        if (keyEvent.Modifiers.HasFlag(CefEventFlags.AltDown))
        {
            modifiers |= KeyModifiers.Alt;
        }

        if (keyEvent.Modifiers.HasFlag(CefEventFlags.ShiftDown))
        {
            modifiers |= KeyModifiers.Shift;
        }

        return shortcuts.FirstOrDefault(shortcut => shortcut.VirtualKey == keyEvent.WindowsKeyCode && shortcut.Modifiers == modifiers)?.Command;
    }
}
