namespace Markwardt.Browser.Engine;

/// <summary>
/// Reads and writes the process environment that native libraries see. .NET keeps its own copy of the environment on Unix, so <see cref="Environment.SetEnvironmentVariable(string, string)"/> never reaches a driver that reads it with <c>getenv</c>.
/// </summary>
internal interface INativeEnvironment
{
    /// <summary>
    /// Reads a variable.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The value, or null when it is not set.</returns>
    string? Get(string name);

    /// <summary>
    /// Sets a variable, replacing any value, for native code and for processes started from now on.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    void Set(string name, string value);
}

/// <inheritdoc />
internal sealed class NativeEnvironment : INativeEnvironment
{
    /// <inheritdoc />
    public string? Get(string name)
        => OperatingSystem.IsWindows() ? Environment.GetEnvironmentVariable(name) : Marshal.PtrToStringUTF8(getenv(name));

    /// <inheritdoc />
    public void Set(string name, string value)
    {
        if (OperatingSystem.IsWindows())
        {
            Environment.SetEnvironmentVariable(name, value);
        }
        else
        {
            _ = setenv(name, value, 1);
        }
    }

    [DllImport("libc")]
    private static extern int setenv([MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string value, int overwrite);

    [DllImport("libc")]
    private static extern nint getenv([MarshalAs(UnmanagedType.LPUTF8Str)] string name);
}
