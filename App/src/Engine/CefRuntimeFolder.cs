namespace Markwardt.Browser.Engine;

/// <summary>
/// Finds the CEF runtime (libcef and its resource files). A single-file build carries it packed inside the executable, which libcef cannot load from, so it is unpacked once into a cache folder and loaded from there.
/// </summary>
internal interface ICefRuntimeFolder
{
    /// <summary>
    /// Gets the folder that holds libcef and its resource files. Set by <see cref="Prepare"/>.
    /// </summary>
    string Location { get; }

    /// <summary>
    /// Finds the runtime beside the executable, or unpacks the embedded one on first use. Does nothing if already done.
    /// </summary>
    /// <exception cref="InvalidOperationException">The runtime is neither beside the executable nor packed into it.</exception>
    void Prepare();

    /// <summary>
    /// Makes libcef loadable from <see cref="Location"/> when it is not beside the executable. Call after <see cref="Prepare"/>, before CEF is used.
    /// </summary>
    void Activate();
}

/// <inheritdoc />
/// <param name="beside">The folder of the executable, where a build that does not pack the runtime keeps it.</param>
/// <param name="cache">The folder under which unpacked runtimes are kept, one folder per archive id.</param>
/// <param name="archive">The packed runtime.</param>
internal sealed class CefRuntimeFolder(string beside, string cache, ICefRuntimeArchive archive) : ICefRuntimeFolder
{
    private readonly string library = OperatingSystem.IsWindows() ? "libcef.dll" : "libcef.so";
    private readonly string complete = ".complete";

    /// <inheritdoc />
    public string Location { get; private set; } = string.Empty;

    /// <inheritdoc />
    public void Prepare()
    {
        if (Location.Length > 0)
        {
            return;
        }

        if (File.Exists(Path.Combine(beside, library)))
        {
            Location = beside;
            return;
        }

        string id = archive.Id ?? throw new InvalidOperationException($"{library} was not found beside the executable and no runtime is packed into it.");
        string target = Path.Combine(cache, id);

        if (!File.Exists(Path.Combine(target, complete)))
        {
            Unpack(target);
            Remove(target);
        }

        Location = target;
    }

    /// <inheritdoc />
    public void Activate()
    {
        if (Location.Length == 0 || Location == beside)
        {
            return;
        }

        string path = Path.Combine(Location, library);
        AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, name) => name == "libcef" ? NativeLibrary.Load(path) : IntPtr.Zero;

        // CefGlue looks for libcef in the current directory before it falls back to searching the folders above the executable.
        Environment.CurrentDirectory = Location;
    }

    private void Unpack(string target)
    {
        Directory.CreateDirectory(cache);
        string temporary = $"{target}.{Guid.NewGuid():N}.tmp";

        try
        {
            using (Stream stream = archive.Open())
            using (ZipArchive zip = new(stream))
            {
                zip.ExtractToDirectory(temporary);
            }

            File.WriteAllText(Path.Combine(temporary, complete), string.Empty);

            if (Directory.Exists(target) && !File.Exists(Path.Combine(target, complete)))
            {
                Directory.Delete(target, true);
            }

            try
            {
                Directory.Move(temporary, target);
            }
            catch (IOException) when (File.Exists(Path.Combine(target, complete)))
            {
            }
        }
        finally
        {
            if (Directory.Exists(temporary))
            {
                Directory.Delete(temporary, true);
            }
        }
    }

    private void Remove(string keep)
    {
        foreach (string folder in Directory.GetDirectories(cache))
        {
            if (folder != keep && !folder.EndsWith(".tmp", StringComparison.Ordinal))
            {
                try
                {
                    Directory.Delete(folder, true);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                }
            }
        }
    }
}
