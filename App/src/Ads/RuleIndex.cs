namespace Markwardt.Browser.Ads;

/// <summary>
/// A set of rules that can be searched without trying every one.
/// </summary>
internal interface IRuleIndex
{
    /// <summary>
    /// Gets the number of rules in the index.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Checks whether any rule applies to a request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>True if at least one rule applies.</returns>
    bool Matches(AdRequest request);
}

/// <inheritdoc />
internal sealed class RuleIndex : IRuleIndex
{
    /// <summary>
    /// Builds the index.
    /// </summary>
    /// <param name="rules">The rules to index.</param>
    public RuleIndex(IEnumerable<NetworkRule> rules)
    {
        foreach (NetworkRule rule in rules)
        {
            Count++;

            if (rule.FindToken() is { } token)
            {
                if (!byToken.TryGetValue(token, out List<NetworkRule>? bucket))
                {
                    byToken[token] = bucket = [];
                }

                bucket.Add(rule);
            }
            else
            {
                loose.Add(rule);
            }
        }
    }

    private readonly Dictionary<string, List<NetworkRule>> byToken = [];
    private readonly List<NetworkRule> loose = [];

    /// <inheritdoc />
    public int Count { get; }

    /// <inheritdoc />
    public bool Matches(AdRequest request)
    {
        foreach (string token in request.Tokens)
        {
            if (byToken.TryGetValue(token, out List<NetworkRule>? bucket) && bucket.Any(rule => rule.Matches(request)))
            {
                return true;
            }
        }

        return loose.Any(rule => rule.Matches(request));
    }
}
