namespace Markwardt.Browser.Ads;

/// <summary>
/// Decides what is an ad: requests to block, popups to refuse and elements to hide.
/// </summary>
internal interface IAdBlocker
{
    /// <summary>
    /// Decides whether a request should be cancelled.
    /// </summary>
    /// <param name="url">The address requested.</param>
    /// <param name="pageUrl">The address of the page it belongs to; empty when unknown.</param>
    /// <param name="type">What kind of request it is.</param>
    /// <returns>True if it is an ad or tracker request.</returns>
    bool ShouldBlock(string url, string pageUrl, AdResourceType type);

    /// <summary>
    /// Decides whether a window a page tried to open is an ad.
    /// </summary>
    /// <param name="url">The address the new window would load.</param>
    /// <param name="openerUrl">The address of the page that opened it.</param>
    /// <returns>True if it is an ad.</returns>
    bool IsAdPopup(string url, string openerUrl);

    /// <summary>
    /// Builds the script that hides the ads in a frame (and, on YouTube, stops its ads from being loaded).
    /// </summary>
    /// <param name="frameUrl">The address the frame is loading.</param>
    /// <param name="isMain">Whether it is the page's main frame, which also gets the rules that apply to every site.</param>
    /// <returns>The script, or null when there is nothing to do for the frame.</returns>
    string? FrameScript(string frameUrl, bool isMain);

    /// <summary>
    /// Reads the lists that are on disk and starts blocking with them.
    /// </summary>
    /// <param name="cancellation">Cancels the load.</param>
    /// <returns>A task that completes when blocking is in place.</returns>
    Task Load(CancellationToken cancellation = default);

    /// <summary>
    /// Downloads newer lists, if any are due, and switches to them.
    /// </summary>
    /// <param name="cancellation">Cancels the update.</param>
    /// <returns>A task that completes when the update is done.</returns>
    Task Refresh(CancellationToken cancellation = default);
}

/// <inheritdoc />
/// <param name="parser">Reads the lists.</param>
/// <param name="lists">Where the lists come from.</param>
internal sealed class AdBlocker(IFilterParser parser, IFilterLists lists) : IAdBlocker
{
    private readonly string youTubeScript = ReadScript();
    private FilterSet? current;
    private Tuple<FilterSet, string>? genericCache;

    /// <inheritdoc />
    public bool ShouldBlock(string url, string pageUrl, AdResourceType type)
    {
        FilterSet? set = Volatile.Read(ref current);

        if (set is null)
        {
            return false;
        }

        AdRequest request = new(url, pageUrl, type);
        return request.IsFilterable && set.Blocks.Matches(request) && !set.Exceptions.Matches(request);
    }

    /// <inheritdoc />
    public bool IsAdPopup(string url, string openerUrl)
        => ShouldBlock(url, openerUrl, AdResourceType.Popup);

    /// <inheritdoc />
    public string? FrameScript(string frameUrl, bool isMain)
    {
        FilterSet? set = Volatile.Read(ref current);
        AdRequest frame = new(frameUrl, frameUrl, AdResourceType.Document);

        if (set is null || !frame.IsFilterable)
        {
            return null;
        }

        string css = Stylesheet(set, frame.Host, isMain);
        bool youTube = frame.Host == "youtube.com" || frame.Host.EndsWith(".youtube.com", StringComparison.Ordinal) || frame.Host.EndsWith("youtube-nocookie.com", StringComparison.Ordinal);

        if (css.Length == 0 && !youTube)
        {
            return null;
        }

        string hide = css.Length == 0 ? string.Empty : $"if (!window.__mwAds) {{ window.__mwAds = true; const css = {JsonSerializer.Serialize(css)}; const add = () => {{ const style = document.createElement('style'); style.textContent = css; (document.head || document.documentElement).appendChild(style); }}; if (document.documentElement) {{ add(); }} else {{ new MutationObserver((_, observer) => {{ if (document.documentElement) {{ observer.disconnect(); add(); }} }}).observe(document, {{ childList: true }}); }} }}";
        return $"(() => {{ {hide} }})();{(youTube ? youTubeScript : string.Empty)}";
    }

    /// <inheritdoc />
    public async Task Load(CancellationToken cancellation = default)
    {
        IReadOnlyList<string> texts = await lists.Read(cancellation);
        Volatile.Write(ref current, await Task.Run(() => parser.Parse(texts.SelectMany(text => text.Split('\n'))), cancellation));
    }

    /// <inheritdoc />
    public async Task Refresh(CancellationToken cancellation = default)
    {
        if (await lists.Refresh(cancellation))
        {
            await Load(cancellation);
        }
    }

    private static string ReadScript()
    {
        using Stream stream = typeof(AdBlocker).Assembly.GetManifestResourceStream("Markwardt.Browser.Ads.YouTubeAds.js")!;
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    private string Stylesheet(FilterSet set, string host, bool isMain)
    {
        List<string> suffixes = [];

        for (string name = host; name.Length > 0; name = name.Contains('.') ? name[(name.IndexOf('.') + 1)..] : string.Empty)
        {
            suffixes.Add(name);
        }

        HashSet<string> allowed = [.. suffixes.Where(set.HostExceptions.ContainsKey).SelectMany(suffix => set.HostExceptions[suffix])];
        StringBuilder css = new();

        foreach (CosmeticRule rule in suffixes.Where(set.HostRules.ContainsKey).SelectMany(suffix => set.HostRules[suffix]))
        {
            if (!allowed.Contains(rule.Selector) && !rule.ExcludedDomains.Any(domain => host == domain || host.EndsWith($".{domain}", StringComparison.Ordinal)))
            {
                css.Append(rule.Selector).Append("{display:none!important}");
            }
        }

        if (isMain)
        {
            css.Append(GenericCss(set, allowed));
        }

        return css.ToString();
    }

    private string GenericCss(FilterSet set, HashSet<string> allowed)
    {
        Tuple<FilterSet, string>? cached = Volatile.Read(ref genericCache);

        if (allowed.Count == 0 && cached is not null && ReferenceEquals(cached.Item1, set))
        {
            return cached.Item2;
        }

        StringBuilder css = new();

        foreach (string[] chunk in set.GenericSimple.Where(selector => !allowed.Contains(selector)).Chunk(1000))
        {
            css.Append(string.Join(',', chunk)).Append("{display:none!important}");
        }

        foreach (string selector in set.GenericComplex.Where(selector => !allowed.Contains(selector)))
        {
            css.Append(selector).Append("{display:none!important}");
        }

        if (allowed.Count == 0)
        {
            Volatile.Write(ref genericCache, Tuple.Create(set, css.ToString()));
        }

        return css.ToString();
    }
}
