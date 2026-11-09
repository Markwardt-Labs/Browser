namespace Markwardt.Browser.Ads;

/// <summary>
/// One request, taken apart once so that every rule can be checked against it cheaply.
/// </summary>
internal sealed record AdRequest
{
    /// <summary>
    /// Takes a request apart.
    /// </summary>
    /// <param name="url">The address requested.</param>
    /// <param name="pageUrl">The address of the page the request belongs to; empty when unknown.</param>
    /// <param name="type">What kind of request it is.</param>
    public AdRequest(string url, string pageUrl, AdResourceType type)
    {
        Type = type;
        LowerUrl = url.ToLowerInvariant();
        (int start, int end) = HostRange(LowerUrl);
        IsFilterable = end > start;
        Host = IsFilterable ? LowerUrl[start..end] : string.Empty;
        (int pageStart, int pageEnd) = HostRange(pageUrl.ToLowerInvariant());
        PageHost = pageEnd > pageStart ? pageUrl.ToLowerInvariant()[pageStart..pageEnd] : string.Empty;
        IsThirdParty = PageHost.Length > 0 && Site(Host) != Site(PageHost);

        List<int> starts = [start];
        HashSet<string> tokens = [];
        int tokenStart = -1;

        for (int index = 0; index <= LowerUrl.Length; index++)
        {
            bool part = index < LowerUrl.Length && char.IsAsciiLetterOrDigit(LowerUrl[index]);

            if (IsFilterable && index >= start && index < end && LowerUrl[index] == '.')
            {
                starts.Add(index + 1);
            }

            if (part && tokenStart < 0)
            {
                tokenStart = index;
            }
            else if (!part && tokenStart >= 0)
            {
                if (index - tokenStart >= 3)
                {
                    tokens.Add(LowerUrl[tokenStart..index]);
                }

                tokenStart = -1;
            }
        }

        HostStarts = starts;
        Tokens = tokens;
    }

    private readonly string multiPartSecondLevel = " co com org net gov edu ac or ne go ";

    /// <summary>
    /// Gets what kind of request it is.
    /// </summary>
    public AdResourceType Type { get; }

    /// <summary>
    /// Gets the address in lower case.
    /// </summary>
    public string LowerUrl { get; }

    /// <summary>
    /// Gets a value indicating whether the address is one that filters apply to (a web address).
    /// </summary>
    public bool IsFilterable { get; }

    /// <summary>
    /// Gets the host of the address, in lower case; empty when it is not a web address.
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// Gets the host of the page, in lower case; empty when unknown.
    /// </summary>
    public string PageHost { get; }

    /// <summary>
    /// Gets a value indicating whether the request goes to a different site than the page.
    /// </summary>
    public bool IsThirdParty { get; }

    /// <summary>
    /// Gets the positions in <see cref="LowerUrl"/> where the host name or one of its parent names starts.
    /// </summary>
    public IReadOnlyList<int> HostStarts { get; }

    /// <summary>
    /// Gets the runs of at least three letters and digits in the address, which rules are looked up by.
    /// </summary>
    public IReadOnlySet<string> Tokens { get; }

    private (int Start, int End) HostRange(string lower)
    {
        int start = lower.StartsWith("https://", StringComparison.Ordinal) ? 8 : lower.StartsWith("http://", StringComparison.Ordinal) ? 7 : lower.StartsWith("wss://", StringComparison.Ordinal) ? 6 : lower.StartsWith("ws://", StringComparison.Ordinal) ? 5 : 0;

        if (start == 0)
        {
            return (0, 0);
        }

        int end = lower.IndexOfAny(['/', '?', '#'], start);
        end = end < 0 ? lower.Length : end;
        int at = lower.LastIndexOf('@', end - 1, end - start);
        start = at >= 0 ? at + 1 : start;
        int port = lower.IndexOf(':', start, end - start);
        return (start, port >= 0 ? port : end);
    }

    private string Site(string host)
    {
        string[] labels = host.Split('.');

        if (labels.Length <= 2 || labels.All(label => label.All(char.IsAsciiDigit)))
        {
            return host;
        }

        int count = labels[^1].Length == 2 && multiPartSecondLevel.Contains($" {labels[^2]} ", StringComparison.Ordinal) ? 3 : 2;
        return string.Join('.', labels[^count..]);
    }
}
