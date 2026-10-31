namespace Markwardt.Browser.Settings;

/// <summary>
/// The choices that are remembered between runs and shared by every window.
/// </summary>
internal interface IUserSettings : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets a value indicating whether the top bar also shows outside the overview. Changes are saved.
    /// </summary>
    bool TopBarVisible { get; set; }

    /// <summary>
    /// Reads the saved choices.
    /// </summary>
    /// <param name="cancellation">Cancels the read.</param>
    /// <returns>A task that completes when the choices are in place.</returns>
    Task Load(CancellationToken cancellation = default);
}

/// <inheritdoc cref="IUserSettings" />
/// <param name="storage">Where the choices are kept.</param>
internal sealed class UserSettings(ISettingsStorage storage) : ObservableObject, IUserSettings
{
    private readonly SemaphoreSlim saving = new(1, 1);
    private bool topBarVisible = true;

    /// <inheritdoc />
    public bool TopBarVisible
    {
        get => topBarVisible;
        set
        {
            if (topBarVisible != value)
            {
                Set(ref topBarVisible, value);
                _ = Save();
            }
        }
    }

    /// <inheritdoc />
    public async Task Load(CancellationToken cancellation = default)
        => Set(ref topBarVisible, (await storage.Load(cancellation)).TopBarVisible, nameof(TopBarVisible));

    private async Task Save()
    {
        await saving.WaitAsync();

        try
        {
            await storage.Save(new SettingsData { TopBarVisible = topBarVisible });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
        finally
        {
            saving.Release();
        }
    }
}
