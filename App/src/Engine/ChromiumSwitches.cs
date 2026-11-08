namespace Markwardt.Browser.Engine;

/// <summary>
/// The Chromium command line switches the browser starts with.
/// </summary>
internal interface IChromiumSwitches
{
    /// <summary>
    /// Builds the switches.
    /// </summary>
    /// <param name="angle">The graphics backend Chromium's ANGLE layer should use (for instance <c>vulkan</c>), or null or empty for Chromium's own default.</param>
    /// <returns>The switches as name and value (empty for none).</returns>
    IReadOnlyList<KeyValuePair<string, string>> Build(string? angle);
}

/// <inheritdoc />
internal sealed class ChromiumSwitches : IChromiumSwitches
{
    /// <inheritdoc />
    public IReadOnlyList<KeyValuePair<string, string>> Build(string? angle)
    {
        List<KeyValuePair<string, string>> switches =
        [
            new("force-dark-mode", string.Empty),
            new("enable-features", angle == "vulkan" ? "Vulkan,WebContentsForceDark" : "WebContentsForceDark"),
        ];

        if (!string.IsNullOrEmpty(angle))
        {
            switches.Add(new("use-angle", angle));
        }

        return switches;
    }
}
