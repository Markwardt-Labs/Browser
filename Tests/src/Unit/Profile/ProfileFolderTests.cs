namespace Tests.Unit.Profile;

/// <summary>
/// Tests for <see cref="ProfileFolder"/>.
/// </summary>
public sealed class ProfileFolderTests
{
    private readonly ProfileFolder folder = new();

    /// <summary>
    /// The profile lives under the user's local application data, so it survives restarts and is per user.
    /// </summary>
    [Fact]
    public void Location_IsUnderLocalApplicationData()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.True(Path.IsPathRooted(folder.Location));
        Assert.Equal(Path.Combine(root, "Markwardt.Browser"), folder.Location);
    }
}
