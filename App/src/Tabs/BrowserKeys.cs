namespace Markwardt.Browser.Tabs;

/// <summary>
/// Catches shortcut keys while a page has focus. CEF reports them here before Chromium's own accelerators run, which would otherwise open native Chrome windows.
/// </summary>
/// <param name="shortcuts">The shortcuts to catch.</param>
/// <param name="pressed">Called, on a CEF thread, when a shortcut is pressed.</param>
internal sealed class BrowserKeys(IShortcutMap shortcuts, Action<ShortcutCommand> pressed) : KeyboardHandler
{
    /// <inheritdoc />
    protected override bool OnPreKeyEvent(CefBrowser browser, CefKeyEvent keyEvent, IntPtr osEvent, out bool isKeyboardShortcut)
    {
        isKeyboardShortcut = false;

        if (shortcuts.Find(keyEvent) is not { } command)
        {
            return false;
        }

        if (keyEvent.EventType == CefKeyEventType.RawKeyDown && !keyEvent.Modifiers.HasFlag(CefEventFlags.IsRepeat))
        {
            pressed(command);
        }

        return true;
    }
}
