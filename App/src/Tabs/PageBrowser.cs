namespace Markwardt.Browser.Tabs;

/// <summary>
/// The CEF browser control, adjusted for use as a full window page: it takes keyboard focus when clicked, shows the cursor CEF asks for, and exposes the one operation of the underlying CEF browser that the control does not.
/// </summary>
internal sealed class PageBrowser : AvaloniaCefBrowser
{
    private readonly BrowserTooltips tooltips;
    private readonly IFrameStats frameStats;
    private string hoveredLink = string.Empty;
    private bool swallowingClick;
    private Point tooltipOrigin;
    private Point lastPosition;

    /// <summary>
    /// Creates the control.
    /// </summary>
    /// <param name="frameStats">Counts the frames the page delivers, when diagnostics are on.</param>
    public PageBrowser(IFrameStats frameStats)
    {
        this.frameStats = frameStats;

        if (frameStats.IsEnabled)
        {
            AttachedToVisualTree += (_, _) => HookPaint();
        }

        tooltips = new(text => Dispatcher.UIThread.Post(() => ShowTooltip(text), DispatcherPriority.Input));
        Focusable = true;
        DisplayHandler = tooltips;
        FindHandler = new BrowserFind((count, active) => FindResult?.Invoke(count, active));
        StatusMessage += (_, message) => hoveredLink = message ?? string.Empty;
        AddHandler(PointerPressedEvent, OnPointerPressedOverPage, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleasedOverPage, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMovedOverPage, RoutingStrategies.Bubble, true);
        AddHandler(PointerExitedEvent, (_, _) => HideTooltip(), RoutingStrategies.Direct | RoutingStrategies.Bubble, true);
        AddHandler(PointerPressedEvent, (_, _) => HideTooltip(), RoutingStrategies.Bubble, true);
        AddHandler(PointerWheelChangedEvent, (_, _) => HideTooltip(), RoutingStrategies.Bubble, true);
        AddHandler(KeyDownEvent, (_, _) => HideTooltip(), RoutingStrategies.Bubble, true);
    }

    /// <summary>
    /// Raised, on a CEF thread, with the number of matches of the current find and the position of the active one.
    /// </summary>
    public event Action<int, int>? FindResult;

    /// <summary>
    /// Raised when the pointer asks to open the link under it elsewhere: the middle button opens it in a new tab, Alt plus the left button in a background tab.
    /// </summary>
    public event Action<string, LinkTarget>? LinkOpenRequested;

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsVisibleProperty && !IsVisible)
        {
            HideTooltip();
        }

        if (change.Property == CursorProperty)
        {
            foreach (InputElement element in this.GetVisualDescendants().OfType<InputElement>())
            {
                element.Cursor = Cursor;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        // Pressing captures the pointer, which makes Avalonia report an exit. CEF's control forwards that as the mouse leaving the page, so a page that reacts to it (YouTube's hover preview closes) loses the element under the click before the button is released.
        if (ReferenceEquals(e.Pointer.Captured, this))
        {
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsFocused)
        {
            Focus();
        }
    }

    /// <summary>
    /// Takes keyboard focus away from the page. Chromium otherwise keeps the X11 input focus, so keys typed after focus moves to another control never reach it.
    /// </summary>
    public void ReleaseFocus()
        => UnderlyingBrowser?.GetHost().SetFocus(false);

    /// <summary>
    /// Searches the page and highlights the matches; results arrive through <see cref="FindResult"/>.
    /// </summary>
    /// <param name="text">The text to look for.</param>
    /// <param name="forward">Whether to look for the next match rather than the previous one.</param>
    /// <param name="matchCase">Whether letter case must match.</param>
    /// <param name="continuing">Whether this moves on from the active match of the same search, rather than starting a new one.</param>
    public void Find(string text, bool forward, bool matchCase, bool continuing)
        => UnderlyingBrowser?.GetHost().Find(text, forward, matchCase, continuing);

    /// <summary>
    /// Ends the search and removes the highlights, leaving the active match selected.
    /// </summary>
    public void StopFinding()
        => UnderlyingBrowser?.GetHost().StopFinding(false);

    /// <summary>
    /// Hides the page's tooltips, including one that is showing, until <see cref="ReleaseTooltips"/> is called.
    /// </summary>
    public void HoldTooltips()
    {
        tooltips.Hold();
        HideTooltip();
    }

    /// <summary>
    /// Lets the page's tooltips show again.
    /// </summary>
    public void ReleaseTooltips()
        => tooltips.Release();

    private void HookPaint()
    {
        FieldInfo? field = typeof(BaseCefBrowser).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(candidate => candidate.FieldType.Name == "CommonBrowserAdapter");
        object? adapter = field?.GetValue(this);
        EventInfo? paintEvent = adapter?.GetType().GetEvent("Paint");

        if (adapter is null || paintEvent is null || paintEvent.EventHandlerType is null)
        {
            frameStats.HookFailed();
            return;
        }

        MethodInfo handler = typeof(PageBrowser).GetMethod(nameof(OnPaintDelivered), BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(paintEvent.EventHandlerType.GetGenericArguments()[0]);
        paintEvent.AddEventHandler(adapter, Delegate.CreateDelegate(paintEvent.EventHandlerType, this, handler));
    }

    private void OnPaintDelivered<T>(object? sender, T args)
    {
        Type type = typeof(T);
        int width = type.GetProperty("Width")?.GetValue(args) as int? ?? 0;
        int height = type.GetProperty("Height")?.GetValue(args) as int? ?? 0;
        CefRectangle[]? rects = type.GetProperty("DirtyRects")?.GetValue(args) as CefRectangle[];
        frameStats.Painted(width, height, rects?.Sum(rect => (long)rect.Width * rect.Height) ?? 0);
    }

    private void OnPointerPressedOverPage(object? sender, PointerPressedEventArgs e)
    {
        PointerPointProperties properties = e.GetCurrentPoint(this).Properties;
        LinkTarget? target = properties.IsMiddleButtonPressed ? LinkTarget.NewTab
            : properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Alt) ? LinkTarget.BackgroundTab
            : null;

        // Chromium would treat Alt plus a click as a download and a middle click as auto-scroll, so the click is kept from it.
        if (target is { } opening && hoveredLink.Length > 0)
        {
            swallowingClick = true;
            e.Handled = true;
            LinkOpenRequested?.Invoke(hoveredLink, opening);
        }
    }

    private void OnPointerReleasedOverPage(object? sender, PointerReleasedEventArgs e)
    {
        if (swallowingClick)
        {
            swallowingClick = false;
            e.Handled = true;
        }
    }

    private void OnPointerMovedOverPage(object? sender, PointerEventArgs e)
    {
        lastPosition = e.GetPosition(this);

        if (ToolTip.GetIsOpen(this) && Math.Abs(lastPosition.X - tooltipOrigin.X) + Math.Abs(lastPosition.Y - tooltipOrigin.Y) > 8)
        {
            HideTooltip();
        }
    }

    private void ShowTooltip(string text)
    {
        if (text.Length == 0 || !IsPointerOver || !IsEffectivelyVisible)
        {
            HideTooltip();
            return;
        }

        tooltipOrigin = lastPosition;
        ToolTip.SetTip(this, text);
        ToolTip.SetShowDelay(this, 0);
        ToolTip.SetIsOpen(this, true);
    }

    private void HideTooltip()
    {
        ToolTip.SetIsOpen(this, false);

        // The text must not stay on the control: Avalonia's own tooltip service would show it again whenever the pointer next enters.
        ToolTip.SetTip(this, null);
    }
}
