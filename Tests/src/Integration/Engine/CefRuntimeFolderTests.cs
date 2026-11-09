namespace Tests.Integration.Engine;

/// <summary>
/// Tests for <see cref="CefRuntimeFolder"/> against real folders.
/// </summary>
public sealed class CefRuntimeFolderTests : IDisposable
{
    /// <summary>
    /// Creates an empty executable folder and cache folder, and an archive that holds a library and a resource folder.
    /// </summary>
    public CefRuntimeFolderTests()
    {
        beside = Path.Combine(root, "app");
        cache = Path.Combine(root, "cache");
        Directory.CreateDirectory(beside);
        archive.Setup(a => a.Id).Returns("abc123");
        archive.Setup(a => a.Open()).Returns(() => Pack(library, "locales/en-US.pak"));
    }

    private readonly string root = Path.Combine(Path.GetTempPath(), $"runtime-{Guid.NewGuid():N}");
    private readonly string beside;
    private readonly string cache;
    private readonly string library = OperatingSystem.IsWindows() ? "libcef.dll" : "libcef.so";
    private readonly Mock<ICefRuntimeArchive> archive = new();

    /// <summary>
    /// Deletes the test folders.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    /// <summary>
    /// A runtime beside the executable is used as it is, without touching the archive.
    /// </summary>
    [Fact]
    public void Prepare_RuntimeBesideExecutable_UsesIt()
    {
        File.WriteAllText(Path.Combine(beside, library), string.Empty);
        CefRuntimeFolder folder = new(beside, cache, archive.Object);

        folder.Prepare();

        Assert.Equal(beside, folder.Location);
        archive.Verify(a => a.Open(), Times.Never);
        Assert.False(Directory.Exists(cache));
    }

    /// <summary>
    /// With nothing beside the executable, the archive is unpacked into a folder named after its id, keeping its folder structure.
    /// </summary>
    [Fact]
    public void Prepare_Packed_UnpacksIntoCache()
    {
        CefRuntimeFolder folder = new(beside, cache, archive.Object);

        folder.Prepare();

        Assert.Equal(Path.Combine(cache, "abc123"), folder.Location);
        Assert.True(File.Exists(Path.Combine(folder.Location, library)));
        Assert.True(File.Exists(Path.Combine(folder.Location, "locales", "en-US.pak")));
    }

    /// <summary>
    /// A runtime that was already unpacked is reused by later runs, and calling again does nothing.
    /// </summary>
    [Fact]
    public void Prepare_AlreadyUnpacked_IsNotUnpackedAgain()
    {
        new CefRuntimeFolder(beside, cache, archive.Object).Prepare();
        CefRuntimeFolder second = new(beside, cache, archive.Object);

        second.Prepare();
        second.Prepare();

        archive.Verify(a => a.Open(), Times.Once);
        Assert.Equal(Path.Combine(cache, "abc123"), second.Location);
    }

    /// <summary>
    /// Unpacking leaves no temporary folder behind.
    /// </summary>
    [Fact]
    public void Prepare_Packed_LeavesNoTemporaryFolder()
    {
        new CefRuntimeFolder(beside, cache, archive.Object).Prepare();

        Assert.Equal([Path.Combine(cache, "abc123")], Directory.GetDirectories(cache));
    }

    /// <summary>
    /// Unpacking a new version removes the runtime of an old one.
    /// </summary>
    [Fact]
    public void Prepare_NewVersion_RemovesOldRuntime()
    {
        string old = Path.Combine(cache, "old");
        Directory.CreateDirectory(old);
        File.WriteAllText(Path.Combine(old, library), string.Empty);

        new CefRuntimeFolder(beside, cache, archive.Object).Prepare();

        Assert.False(Directory.Exists(old));
    }

    /// <summary>
    /// A folder without the completion marker, left by an interrupted unpack, is not trusted.
    /// </summary>
    [Fact]
    public void Prepare_IncompleteFolder_IsUnpackedAgain()
    {
        string partial = Path.Combine(cache, "abc123");
        Directory.CreateDirectory(partial);
        File.WriteAllText(Path.Combine(partial, "stray.txt"), string.Empty);

        CefRuntimeFolder folder = new(beside, cache, archive.Object);

        folder.Prepare();

        archive.Verify(a => a.Open(), Times.Once);
        Assert.True(File.Exists(Path.Combine(folder.Location, library)));
    }

    /// <summary>
    /// With no runtime beside the executable and none packed, preparing fails with a clear error.
    /// </summary>
    [Fact]
    public void Prepare_NothingAvailable_Throws()
    {
        archive.Setup(a => a.Id).Returns((string?)null);
        CefRuntimeFolder folder = new(beside, cache, archive.Object);

        Assert.Throws<InvalidOperationException>(folder.Prepare);
    }

    private MemoryStream Pack(params string[] files)
    {
        MemoryStream stream = new();

        using (ZipArchive zip = new(stream, ZipArchiveMode.Create, true))
        {
            foreach (string file in files)
            {
                using StreamWriter writer = new(zip.CreateEntry(file).Open());
                writer.Write(file);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
