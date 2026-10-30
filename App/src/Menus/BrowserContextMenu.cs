namespace Markwardt.Browser.Menus;

/// <summary>
/// Shows a page's context menu as an Avalonia menu, extended with Reload, the ways to open a link, and Inspect.
/// </summary>
/// <param name="host">The browser the menu belongs to.</param>
/// <param name="open">Called with an address and where to open it.</param>
internal sealed class BrowserContextMenu(PageBrowser host, Action<string, LinkTarget> open) : ContextMenuHandler
{
    private readonly int back = 100;
    private readonly int reload = 102;
    // OSR has no print handler, so Print would be a menu entry that does nothing.
    private readonly int print = 131;
    private readonly int viewSource = 132;
    private readonly int openLink = 26500;
    private readonly int inspect = 26501;
    private readonly int openLinkInTab = 26502;
    private readonly int openLinkInBackground = 26503;

    /// <inheritdoc />
    protected override void OnBeforeContextMenu(CefBrowser browser, CefFrame frame, CefContextMenuParams parameters, CefMenuModel model)
    {
        model.Remove(print);
        int backIndex = model.GetIndexOf(back);

        if (backIndex >= 0)
        {
            model.InsertItemAt((nuint)Math.Min(backIndex + 2, (int)model.Count), reload, "Reload");
        }

        if (!string.IsNullOrEmpty(parameters.LinkUrl))
        {
            model.AddSeparator();
            model.AddItem(openLink, "Open");
            model.AddItem(openLinkInTab, "Open in new tab");
            model.AddItem(openLinkInBackground, "Open in background tab");
        }

        model.AddSeparator();
        model.AddItem(inspect, "Inspect");
    }

    /// <inheritdoc />
    protected override bool RunContextMenu(CefBrowser browser, CefFrame frame, CefContextMenuParams parameters, CefMenuModel model, CefRunContextMenuCallback callback)
    {
        IReadOnlyList<ContextMenuEntry> entries = ContextMenuEntry.Read(model);
        Point position = new(parameters.X, parameters.Y);
        string pageUrl = parameters.PageUrl;
        string linkUrl = parameters.LinkUrl;

        if (entries.Count == 0)
        {
            callback.Cancel();
            return true;
        }

        Dispatcher.UIThread.Post(() => Show(entries, position, pageUrl, linkUrl, callback));
        return true;
    }

    private void Show(IReadOnlyList<ContextMenuEntry> entries, Point position, string pageUrl, string linkUrl, CefRunContextMenuCallback callback)
    {
        bool completed = false;
        ContextMenu menu = new()
        {
            Placement = PlacementMode.AnchorAndGravity,
            PlacementAnchor = PopupAnchor.TopLeft,
            PlacementGravity = PopupGravity.BottomRight,
            PlacementRect = new Rect(position, new Size(1, 1)),
        };

        foreach (Control item in Build(entries, id =>
        {
            completed = true;

            if (id == viewSource)
            {
                callback.Cancel();
                open($"view-source:{pageUrl}", LinkTarget.NewTab);
            }
            else if (id == openLink || id == openLinkInTab || id == openLinkInBackground)
            {
                callback.Cancel();
                open(linkUrl, id == openLink ? LinkTarget.Current : id == openLinkInTab ? LinkTarget.NewTab : LinkTarget.BackgroundTab);
            }
            else if (id == reload)
            {
                callback.Cancel();
                host.Reload(false);
            }
            else if (id == inspect)
            {
                callback.Cancel();
                host.ShowDeveloperTools();
            }
            else
            {
                callback.Continue(id, CefEventFlags.None);
            }
        }))
        {
            menu.Items.Add(item);
        }

        host.HoldTooltips();
        menu.Closed += (_, _) =>
        {
            host.ReleaseTooltips();

            if (!completed)
            {
                completed = true;
                callback.Cancel();
            }
        };

        menu.Open(host);
    }

    private List<Control> Build(IReadOnlyList<ContextMenuEntry> entries, Action<int> choose)
    {
        List<Control> items = [];

        foreach (ContextMenuEntry entry in entries)
        {
            if (entry.Kind == ContextMenuEntryKind.Separator)
            {
                items.Add(new Separator());
                continue;
            }

            MenuItem item = new() { Header = entry.Label, IsEnabled = entry.IsEnabled };

            if (entry.Kind == ContextMenuEntryKind.Check)
            {
                item.ToggleType = MenuItemToggleType.CheckBox;
                item.IsChecked = entry.IsChecked;
            }

            if (entry.Kind == ContextMenuEntryKind.Submenu)
            {
                foreach (Control child in Build(entry.Children, choose))
                {
                    item.Items.Add(child);
                }
            }
            else
            {
                item.Click += (_, _) => choose(entry.Id);
            }

            items.Add(item);
        }

        return items;
    }
}
