namespace Tests.Unit.Engine;

/// <summary>
/// Tests for <see cref="CefRuntimeArchive"/>.
/// </summary>
public sealed class CefRuntimeArchiveTests
{
    private readonly CefRuntimeArchive archive = new(typeof(CefRuntimeArchiveTests).Assembly);

    /// <summary>
    /// An assembly without a packed runtime has no id.
    /// </summary>
    [Fact]
    public void Id_NothingPacked_IsNull()
        => Assert.Null(archive.Id);

    /// <summary>
    /// Opening the archive of an assembly without a packed runtime fails with a clear error.
    /// </summary>
    [Fact]
    public void Open_NothingPacked_Throws()
        => Assert.Throws<InvalidOperationException>(archive.Open);
}
