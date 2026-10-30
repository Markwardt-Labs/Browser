namespace Markwardt.Browser.Profile;

/// <summary>
/// The folder that holds everything that outlives a run: the web profile and the bookmarks.
/// </summary>
internal interface IProfileFolder
{
    /// <summary>
    /// Gets the absolute path of the folder.
    /// </summary>
    string Location { get; }
}

/// <inheritdoc />
internal sealed class ProfileFolder : IProfileFolder
{
    /// <inheritdoc />
    public string Location { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Markwardt.Browser");
}
