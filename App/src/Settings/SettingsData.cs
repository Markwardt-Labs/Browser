namespace Markwardt.Browser.Settings;

/// <summary>
/// The choices that are remembered between runs, as saved.
/// </summary>
internal sealed record SettingsData
{
    /// <summary>
    /// Gets a value indicating whether the top bar also shows outside the overview.
    /// </summary>
    public bool TopBarVisible { get; init; } = true;
}
