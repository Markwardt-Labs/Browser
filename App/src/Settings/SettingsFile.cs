namespace Markwardt.Browser.Settings;

/// <summary>
/// Where the remembered choices are kept between runs.
/// </summary>
internal interface ISettingsFile
{
    /// <summary>
    /// Reads the saved choices.
    /// </summary>
    /// <param name="cancellation">Cancels the read.</param>
    /// <returns>The saved choices, or the defaults when nothing usable has been saved.</returns>
    Task<SettingsData> Load(CancellationToken cancellation = default);

    /// <summary>
    /// Replaces the saved choices.
    /// </summary>
    /// <param name="data">The choices to save.</param>
    /// <param name="cancellation">Cancels the write.</param>
    /// <returns>A task that completes when the choices are saved.</returns>
    Task Save(SettingsData data, CancellationToken cancellation = default);
}

/// <summary>
/// Keeps the remembered choices in a JSON file.
/// </summary>
/// <param name="path">The path of the file.</param>
internal sealed class SettingsFile(string path) : ISettingsFile
{
    /// <inheritdoc />
    public async Task<SettingsData> Load(CancellationToken cancellation = default)
    {
        if (!File.Exists(path))
        {
            return new SettingsData();
        }

        try
        {
            await using FileStream stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<SettingsData>(stream, cancellationToken: cancellation) ?? new SettingsData();
        }
        catch (JsonException)
        {
            return new SettingsData();
        }
    }

    /// <inheritdoc />
    public async Task Save(SettingsData data, CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = $"{path}.{Guid.NewGuid():N}.tmp";

        await using (FileStream stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true }, cancellation);
        }

        File.Move(temporary, path, true);
    }
}
