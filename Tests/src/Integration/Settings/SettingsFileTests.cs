namespace Tests.Integration.Settings;

/// <summary>
/// Tests for <see cref="SettingsFile"/> against a real folder.
/// </summary>
public sealed class SettingsFileTests : IDisposable
{
    /// <summary>
    /// Creates a file in a folder that does not exist yet.
    /// </summary>
    public SettingsFileTests()
        => file = new SettingsFile(Path.Combine(folder, "settings.json"));

    private readonly string folder = Path.Combine(Path.GetTempPath(), $"settings-{Guid.NewGuid():N}");
    private readonly SettingsFile file;

    /// <summary>
    /// Deletes the test folder.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>
    /// Nothing has been saved yet, so the defaults apply.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Load_NothingSaved_ReturnsDefaults()
        => Assert.True((await file.Load()).TopBarVisible);

    /// <summary>
    /// A saved choice comes back, including after the folder had to be created.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Save_ThenLoad_ReturnsSavedChoice()
    {
        await file.Save(new SettingsData { TopBarVisible = false });

        Assert.False((await file.Load()).TopBarVisible);
        Assert.Single(Directory.GetFiles(folder));
    }

    /// <summary>
    /// A damaged file falls back to the defaults.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Load_DamagedFile_ReturnsDefaults()
    {
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "settings.json"), "{ not json");

        Assert.True((await file.Load()).TopBarVisible);
    }
}
