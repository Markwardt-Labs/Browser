namespace Markwardt.Browser.Ads;

/// <summary>
/// Reads filter lists in the EasyList / uBlock Origin syntax.
/// </summary>
internal interface IFilterParser
{
    /// <summary>
    /// Reads the lines of one or more filter lists. Rules that use features this blocker does not have (scriptlets, redirects, regular expressions, extended selectors, ...) are skipped rather than approximated.
    /// </summary>
    /// <param name="lines">The lines of the lists.</param>
    /// <returns>The rules that were understood.</returns>
    FilterSet Parse(IEnumerable<string> lines);
}

/// <inheritdoc />
internal sealed class FilterParser : IFilterParser
{
    private readonly Regex cosmetic = new(@"^(?<domains>[^#/|$ ]*)(?<kind>##|#@#|#\?#|#@\?#|#\$#|#@\$#|#%#|#@%#)(?<selector>.+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly Regex simpleSelector = new(@"^(?:[A-Za-z][A-Za-z0-9-]*|[A-Za-z0-9-]*(?:[.#][A-Za-z0-9_-]+)+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly List<string> unsupportedSelectors = [":-abp-", ":has-text(", ":contains(", ":xpath(", ":matches-", ":upward(", ":remove(", ":style(", ":watch-attr(", ":others(", ":nth-ancestor(", ":min-text-length(", ":-ext-", ":is-", "+js(", ":not(:", "\\"];
    private readonly Dictionary<string, AdResourceType> typeNames = new()
    {
        ["script"] = AdResourceType.Script,
        ["image"] = AdResourceType.Image,
        ["stylesheet"] = AdResourceType.Stylesheet,
        ["css"] = AdResourceType.Stylesheet,
        ["font"] = AdResourceType.Font,
        ["media"] = AdResourceType.Media,
        ["subdocument"] = AdResourceType.SubDocument,
        ["frame"] = AdResourceType.SubDocument,
        ["xmlhttprequest"] = AdResourceType.XmlHttpRequest,
        ["xhr"] = AdResourceType.XmlHttpRequest,
        ["ping"] = AdResourceType.Ping,
        ["object"] = AdResourceType.Object,
        ["other"] = AdResourceType.Other,
        ["popup"] = AdResourceType.Popup,
    };

    /// <inheritdoc />
    public FilterSet Parse(IEnumerable<string> lines)
    {
        HashSet<string> seen = [];
        List<NetworkRule> blocks = [];
        List<NetworkRule> exceptions = [];
        List<string> genericSimple = [];
        List<string> genericComplex = [];
        Dictionary<string, List<CosmeticRule>> hostRules = [];
        Dictionary<string, List<CosmeticRule>> entityRules = [];
        Dictionary<string, HashSet<string>> hostExceptions = [];

        foreach (string raw in lines)
        {
            string line = raw.Trim();

            if (line.Length < 2 || line[0] is '!' or '[' || !seen.Add(line))
            {
                continue;
            }

            Match match = line.Contains('#') ? cosmetic.Match(line) : Match.Empty;

            if (match.Success)
            {
                ReadCosmetic(match, genericSimple, genericComplex, hostRules, entityRules, hostExceptions);
            }
            else if (ReadNetwork(line) is { } rule)
            {
                (rule.Item2 ? exceptions : blocks).Add(rule.Item1);
            }
        }

        return new FilterSet
        {
            Blocks = new RuleIndex(blocks),
            Exceptions = new RuleIndex(exceptions),
            GenericSimple = genericSimple,
            GenericComplex = genericComplex,
            HostRules = hostRules.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<CosmeticRule>)pair.Value),
            EntityRules = entityRules.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<CosmeticRule>)pair.Value),
            HostExceptions = hostExceptions.ToDictionary(pair => pair.Key, pair => (IReadOnlySet<string>)pair.Value),
        };
    }

    private void ReadCosmetic(Match match, List<string> genericSimple, List<string> genericComplex, Dictionary<string, List<CosmeticRule>> hostRules, Dictionary<string, List<CosmeticRule>> entityRules, Dictionary<string, HashSet<string>> hostExceptions)
    {
        string kind = match.Groups["kind"].Value;
        string selector = match.Groups["selector"].Value.Trim();

        if ((kind != "##" && kind != "#@#") || selector.Length == 0 || unsupportedSelectors.Any(selector.Contains))
        {
            return;
        }

        string[] domains = match.Groups["domains"].Value.ToLowerInvariant().Split(',', StringSplitOptions.RemoveEmptyEntries);

        List<string> entities = [.. domains.Where(domain => domain.EndsWith(".*", StringComparison.Ordinal) && domain.IndexOf('*') == domain.Length - 1 && !domain.StartsWith('~')).Select(domain => domain[..^2])];
        domains = [.. domains.Where(domain => !domain.Contains('*'))];

        if (kind == "##" && entities.Count > 0 && selector.Length > 0)
        {
            foreach (string entity in entities)
            {
                (entityRules.TryGetValue(entity, out List<CosmeticRule>? entityList) ? entityList : entityRules[entity] = []).Add(new CosmeticRule(selector, []));
            }
        }

        if (domains.Length == 0 && entities.Count > 0)
        {
            return;
        }

        List<string> included = [.. domains.Where(domain => !domain.StartsWith('~'))];
        List<string> excluded = [.. domains.Where(domain => domain.StartsWith('~')).Select(domain => domain[1..])];

        if (kind == "#@#")
        {
            foreach (string domain in included)
            {
                (hostExceptions.TryGetValue(domain, out HashSet<string>? set) ? set : hostExceptions[domain] = []).Add(selector);
            }
        }
        else if (included.Count == 0)
        {
            (simpleSelector.IsMatch(selector) ? genericSimple : genericComplex).Add(selector);
        }
        else
        {
            foreach (string domain in included)
            {
                (hostRules.TryGetValue(domain, out List<CosmeticRule>? list) ? list : hostRules[domain] = []).Add(new CosmeticRule(selector, excluded));
            }
        }
    }

    private (NetworkRule, bool)? ReadNetwork(string line)
    {
        bool isException = line.StartsWith("@@", StringComparison.Ordinal);
        string body = isException ? line[2..] : line;
        string options = string.Empty;
        int dollar = body.LastIndexOf('$');

        if (dollar >= 0 && body[(dollar + 1)..].All(character => char.IsAsciiLetterOrDigit(character) || character is '~' or '-' or '_' or '=' or '|' or ',' or '.' or '*' or ':' or '/'))
        {
            options = body[(dollar + 1)..];
            body = body[..dollar];
        }

        bool domainAnchor = body.StartsWith("||", StringComparison.Ordinal);
        bool startAnchor = !domainAnchor && body.StartsWith('|');
        body = domainAnchor ? body[2..] : startAnchor ? body[1..] : body;
        bool endAnchor = body.EndsWith('|');
        body = endAnchor ? body[..^1] : body;

        if ((body.Length > 1 && body[0] == '/' && body[^1] == '/') || body.Any(character => character > 127 || character == ' '))
        {
            return null;
        }

        body = body.ToLowerInvariant();

        while (body.Contains("**", StringComparison.Ordinal))
        {
            body = body.Replace("**", "*", StringComparison.Ordinal);
        }

        if (domainAnchor && body.StartsWith('*'))
        {
            return null;
        }

        if (!domainAnchor && !startAnchor)
        {
            body = body.TrimStart('*');
        }

        if (!endAnchor)
        {
            body = body.TrimEnd('*');
        }

        bool? thirdParty = null;
        AdResourceType types = AdResourceType.None;
        AdResourceType excludedTypes = AdResourceType.None;
        List<string> domains = [];
        List<string> excludedDomains = [];

        foreach (string option in options.ToLowerInvariant().Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            bool negated = option.StartsWith('~');
            string name = negated ? option[1..] : option;

            if (name is "third-party" or "3p")
            {
                thirdParty = !negated;
            }
            else if (name is "first-party" or "1p")
            {
                thirdParty = negated;
            }
            else if (name.StartsWith("domain=", StringComparison.Ordinal))
            {
                foreach (string domain in name[7..].Split('|', StringSplitOptions.RemoveEmptyEntries))
                {
                    (domain.StartsWith('~') ? excludedDomains : domains).Add(domain.TrimStart('~'));
                }
            }
            else if (typeNames.TryGetValue(name, out AdResourceType type))
            {
                if (negated)
                {
                    excludedTypes |= type;
                }
                else
                {
                    types |= type;
                }
            }
            else if (!name.StartsWith("redirect=", StringComparison.Ordinal) && name is not ("important" or "match-case" or "all"))
            {
                return null;
            }
        }

        if (body.Length == 0 && domains.Count == 0 && types == AdResourceType.None)
        {
            return null;
        }

        return (new NetworkRule
        {
            Pattern = body,
            DomainAnchor = domainAnchor,
            StartAnchor = startAnchor,
            EndAnchor = endAnchor,
            ThirdParty = thirdParty,
            Types = types,
            ExcludedTypes = excludedTypes,
            Domains = domains,
            ExcludedDomains = excludedDomains,
        }, isException);
    }
}
