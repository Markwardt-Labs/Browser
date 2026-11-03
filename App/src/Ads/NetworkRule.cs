namespace Markwardt.Browser.Ads;

/// <summary>
/// One filter rule that blocks (or, for an exception, allows) requests whose address matches a pattern.
/// </summary>
internal sealed record NetworkRule
{
    /// <summary>
    /// Gets the pattern, in lower case. A star matches anything, a caret matches a separator (anything but a letter, digit, underscore, hyphen, dot or percent sign) or the end of the address.
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets a value indicating whether the pattern must start at the beginning of a host name or one of its parent names.
    /// </summary>
    public bool DomainAnchor { get; init; }

    /// <summary>
    /// Gets a value indicating whether the pattern must start at the beginning of the address.
    /// </summary>
    public bool StartAnchor { get; init; }

    /// <summary>
    /// Gets a value indicating whether the pattern must end at the end of the address.
    /// </summary>
    public bool EndAnchor { get; init; }

    /// <summary>
    /// Gets whether the rule is limited to requests to another site (true) or to the page's own site (false); null for both.
    /// </summary>
    public bool? ThirdParty { get; init; }

    /// <summary>
    /// Gets the request types the rule is limited to; none for every type.
    /// </summary>
    public AdResourceType Types { get; init; }

    /// <summary>
    /// Gets the request types the rule does not apply to.
    /// </summary>
    public AdResourceType ExcludedTypes { get; init; }

    /// <summary>
    /// Gets the sites the rule is limited to, as the page's host or a parent of it; empty for every site.
    /// </summary>
    public IReadOnlyList<string> Domains { get; init; } = [];

    /// <summary>
    /// Gets the sites the rule does not apply to.
    /// </summary>
    public IReadOnlyList<string> ExcludedDomains { get; init; } = [];

    /// <summary>
    /// Finds the run of letters and digits in the pattern that every address it matches must contain as a whole run, which lets a lookup skip most rules.
    /// </summary>
    /// <returns>The longest such run of at least three characters, or null when there is none.</returns>
    public string? FindToken()
    {
        string? best = null;
        int start = -1;

        for (int index = 0; index <= Pattern.Length; index++)
        {
            bool part = index < Pattern.Length && char.IsAsciiLetterOrDigit(Pattern[index]);

            if (part && start < 0)
            {
                start = index;
            }
            else if (!part && start >= 0)
            {
                bool leftBounded = start > 0 ? Pattern[start - 1] != '*' : DomainAnchor || StartAnchor;
                bool rightBounded = index < Pattern.Length ? Pattern[index] != '*' : EndAnchor;

                if (leftBounded && rightBounded && index - start >= 3 && (best is null || index - start > best.Length))
                {
                    best = Pattern[start..index];
                }

                start = -1;
            }
        }

        return best;
    }

    /// <summary>
    /// Checks a request against the rule.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>True if the rule applies to the request.</returns>
    public bool Matches(AdRequest request)
    {
        if (Types != AdResourceType.None ? (Types & request.Type) == 0 : request.Type == AdResourceType.Document)
        {
            return false;
        }

        if ((ExcludedTypes & request.Type) != 0 || (ThirdParty is { } thirdParty && thirdParty != request.IsThirdParty))
        {
            return false;
        }

        if ((Domains.Count > 0 && !Domains.Any(domain => IsWithin(request.PageHost, domain))) || ExcludedDomains.Any(domain => IsWithin(request.PageHost, domain)))
        {
            return false;
        }

        return MatchesPattern(request);
    }

    private bool IsWithin(string host, string domain)
        => host == domain || host.EndsWith($".{domain}", StringComparison.Ordinal);

    private bool MatchesPattern(AdRequest request)
    {
        string url = request.LowerUrl;

        if (DomainAnchor)
        {
            return request.HostStarts.Any(start => MatchAt(url, start));
        }

        if (StartAnchor)
        {
            return MatchAt(url, 0);
        }

        char first = Pattern.Length > 0 ? Pattern[0] : '*';

        for (int start = 0; start <= url.Length; start++)
        {
            if ((first == '*' || first == '^' || (start < url.Length && url[start] == first)) && MatchAt(url, start))
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchAt(string url, int urlIndex)
    {
        int patternIndex = 0;
        int star = -1;
        int mark = 0;

        while (true)
        {
            if (patternIndex == Pattern.Length)
            {
                if (!EndAnchor || urlIndex == url.Length)
                {
                    return true;
                }
            }
            else if (Pattern[patternIndex] == '*')
            {
                star = patternIndex++;
                mark = urlIndex;
                continue;
            }
            else if (Pattern[patternIndex] == '^')
            {
                if (urlIndex == url.Length)
                {
                    patternIndex++;
                    continue;
                }

                if (IsSeparator(url[urlIndex]))
                {
                    patternIndex++;
                    urlIndex++;
                    continue;
                }
            }
            else if (urlIndex < url.Length && Pattern[patternIndex] == url[urlIndex])
            {
                patternIndex++;
                urlIndex++;
                continue;
            }

            if (star < 0 || mark >= url.Length)
            {
                return false;
            }

            patternIndex = star + 1;
            urlIndex = ++mark;
        }
    }

    private bool IsSeparator(char character)
        => !char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-' or '.' or '%');
}
