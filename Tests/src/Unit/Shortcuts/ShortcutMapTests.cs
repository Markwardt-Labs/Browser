namespace Tests.Unit.Shortcuts;

/// <summary>
/// Tests for <see cref="ShortcutMap"/>.
/// </summary>
public sealed class ShortcutMapTests
{
    private readonly ShortcutMap map = new();

    /// <summary>
    /// Avalonia key presses resolve to their commands.
    /// </summary>
    /// <param name="key">The key pressed.</param>
    /// <param name="modifiers">The modifiers held.</param>
    /// <param name="expected">The name of the expected command.</param>
    [Theory]
    [InlineData(Key.T, KeyModifiers.Control, "NewTab")]
    [InlineData(Key.W, KeyModifiers.Control, "CloseTab")]
    [InlineData(Key.L, KeyModifiers.Control, "FocusAddress")]
    [InlineData(Key.F1, KeyModifiers.None, "ToggleOverview")]
    [InlineData(Key.F2, KeyModifiers.None, "NewTab")]
    [InlineData(Key.R, KeyModifiers.Control, "Reload")]
    [InlineData(Key.F5, KeyModifiers.None, "Reload")]
    [InlineData(Key.F4, KeyModifiers.None, "ToggleVideoMode")]
    [InlineData(Key.F, KeyModifiers.Control, "Find")]
    [InlineData(Key.Left, KeyModifiers.Alt, "Back")]
    [InlineData(Key.Right, KeyModifiers.Alt, "Forward")]
    public void Find_Key_ReturnsCommand(Key key, KeyModifiers modifiers, string expected)
        => Assert.Equal(Enum.Parse<ShortcutCommand>(expected), map.Find(key, modifiers));

    /// <summary>
    /// CEF key events resolve to the same commands as the matching Avalonia keys.
    /// </summary>
    /// <param name="virtualKey">The Windows virtual key code.</param>
    /// <param name="flags">The modifier flags.</param>
    /// <param name="expected">The name of the expected command.</param>
    [Theory]
    [InlineData(0x54, CefEventFlags.ControlDown, "NewTab")]
    [InlineData(0x57, CefEventFlags.ControlDown, "CloseTab")]
    [InlineData(0x25, CefEventFlags.AltDown, "Back")]
    [InlineData(0x27, CefEventFlags.AltDown, "Forward")]
    [InlineData(0x70, CefEventFlags.None, "ToggleOverview")]
    [InlineData(0x71, CefEventFlags.None, "NewTab")]
    [InlineData(0x73, CefEventFlags.None, "ToggleVideoMode")]
    [InlineData(0x46, CefEventFlags.ControlDown, "Find")]
    [InlineData(0x74, CefEventFlags.None, "Reload")]
    public void Find_CefKeyEvent_ReturnsCommand(int virtualKey, CefEventFlags flags, string expected)
        => Assert.Equal(Enum.Parse<ShortcutCommand>(expected), map.Find(new CefKeyEvent { EventType = CefKeyEventType.RawKeyDown, WindowsKeyCode = virtualKey, Modifiers = flags }));

    /// <summary>
    /// A typed character is never a shortcut, even when its character code equals a shortcut's virtual key: "p" is 112, which is also F1.
    /// </summary>
    [Fact]
    public void Find_CharEventWithF1Code_ReturnsNull()
        => Assert.Null(map.Find(new CefKeyEvent { EventType = CefKeyEventType.Char, WindowsKeyCode = 'p', Modifiers = CefEventFlags.None }));

    /// <summary>
    /// Lock and keypad flags that CEF adds to ordinary key events do not stop a shortcut from matching.
    /// </summary>
    [Fact]
    public void Find_CefKeyEventWithUnrelatedFlags_StillMatches()
        => Assert.Equal(ShortcutCommand.ToggleOverview, map.Find(new CefKeyEvent { EventType = CefKeyEventType.RawKeyDown, WindowsKeyCode = 0x70, Modifiers = CefEventFlags.IsKeyPad | CefEventFlags.NumLockOn }));

    /// <summary>
    /// A shortcut only matches with exactly its modifiers, so typing and other chords are left alone.
    /// </summary>
    /// <param name="key">The key pressed.</param>
    /// <param name="modifiers">The modifiers held.</param>
    [Theory]
    [InlineData(Key.T, KeyModifiers.None)]
    [InlineData(Key.T, KeyModifiers.Control | KeyModifiers.Shift)]
    [InlineData(Key.Left, KeyModifiers.None)]
    [InlineData(Key.A, KeyModifiers.Control)]
    [InlineData(Key.F1, KeyModifiers.Shift)]
    public void Find_OtherCombination_ReturnsNull(Key key, KeyModifiers modifiers)
        => Assert.Null(map.Find(key, modifiers));

    /// <summary>
    /// An ordinary letter key from CEF is not a shortcut.
    /// </summary>
    [Fact]
    public void Find_CefLetterKey_ReturnsNull()
        => Assert.Null(map.Find(new CefKeyEvent { EventType = CefKeyEventType.RawKeyDown, WindowsKeyCode = 0x50, Modifiers = CefEventFlags.None }));
}
