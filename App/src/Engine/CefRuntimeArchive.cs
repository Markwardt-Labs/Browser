namespace Markwardt.Browser.Engine;

/// <summary>
/// The CEF runtime files as packed into the executable at publish time.
/// </summary>
internal interface ICefRuntimeArchive
{
    /// <summary>
    /// Gets a name that changes whenever the packed files do, or null when nothing is packed (a build that keeps the runtime files beside the executable).
    /// </summary>
    string? Id { get; }

    /// <summary>
    /// Opens the zip archive of the runtime files.
    /// </summary>
    /// <returns>A readable, seekable stream over the archive.</returns>
    Stream Open();
}

/// <summary>
/// Reads the runtime archive from the resources of an assembly.
/// </summary>
/// <param name="assembly">The assembly the build packed the archive into.</param>
internal sealed class CefRuntimeArchive(Assembly assembly) : ICefRuntimeArchive
{
    private readonly string archiveName = "Markwardt.Browser.CefRuntime.zip";
    private readonly string idName = "Markwardt.Browser.CefRuntime.id";

    /// <inheritdoc />
    public string? Id
    {
        get
        {
            using Stream? stream = assembly.GetManifestResourceStream(idName);

            if (stream is null)
            {
                return null;
            }

            using StreamReader reader = new(stream);
            return reader.ReadToEnd().Trim();
        }
    }

    /// <inheritdoc />
    public Stream Open()
        => assembly.GetManifestResourceStream(archiveName) ?? throw new InvalidOperationException("The CEF runtime is not packed into this executable.");
}
