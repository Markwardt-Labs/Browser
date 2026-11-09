namespace Tests.Unit.Settings;

/// <summary>
/// Tests for <see cref="UserSettings"/>.
/// </summary>
public sealed class UserSettingsTests
{
    /// <summary>
    /// Creates the settings under test over a mock storage that has nothing saved.
    /// </summary>
    public UserSettingsTests()
    {
        storage.Setup(s => s.Load(It.IsAny<CancellationToken>())).ReturnsAsync(new SettingsData());
        storage.Setup(s => s.Save(It.IsAny<SettingsData>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        settings = new UserSettings(storage.Object);
    }

    private readonly Mock<ISettingsStorage> storage = new();
    private readonly UserSettings settings;

    /// <summary>
    /// The top bar shows outside the overview until chosen otherwise.
    /// </summary>
    [Fact]
    public void TopBarVisible_Default_IsTrue()
        => Assert.True(settings.TopBarVisible);

    /// <summary>
    /// Loading brings in what was saved.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Load_SavedChoice_IsApplied()
    {
        storage.Setup(s => s.Load(It.IsAny<CancellationToken>())).ReturnsAsync(new SettingsData { TopBarVisible = false });

        await settings.Load();

        Assert.False(settings.TopBarVisible);
    }

    /// <summary>
    /// Changing the choice notifies listeners and saves it.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task TopBarVisible_Changed_NotifiesAndSaves()
    {
        List<string?> raised = [];
        TaskCompletionSource<SettingsData> saved = new();
        settings.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        storage.Setup(s => s.Save(It.IsAny<SettingsData>(), It.IsAny<CancellationToken>())).Callback<SettingsData, CancellationToken>((data, _) => saved.TrySetResult(data)).Returns(Task.CompletedTask);

        settings.TopBarVisible = false;

        Assert.Equal([nameof(IUserSettings.TopBarVisible)], raised);
        Assert.False((await saved.Task.WaitAsync(TimeSpan.FromSeconds(5))).TopBarVisible);
    }

    /// <summary>
    /// Setting the value it already has does nothing.
    /// </summary>
    [Fact]
    public void TopBarVisible_Unchanged_DoesNotSave()
    {
        settings.TopBarVisible = true;

        storage.Verify(s => s.Save(It.IsAny<SettingsData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// A storage that cannot write does not break the setting.
    /// </summary>
    [Fact]
    public void TopBarVisible_SaveFails_StillChanges()
    {
        storage.Setup(s => s.Save(It.IsAny<SettingsData>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException());

        settings.TopBarVisible = false;

        Assert.False(settings.TopBarVisible);
    }
}
