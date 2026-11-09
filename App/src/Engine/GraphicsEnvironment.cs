namespace Markwardt.Browser.Engine;

/// <summary>
/// Sets the environment variables the graphics drivers read when the first window is created.
/// </summary>
internal interface IGraphicsEnvironment
{
    /// <summary>
    /// Turns off waiting for the screen refresh in buffer swaps. Avalonia draws every window from one render thread and never sets a swap interval, so with the drivers' default of waiting, each window's swap blocks that thread for a full refresh: with two windows both redrawing, each gets about half the frames, and a video playing in one lags whenever the other is being used. Does nothing off Linux, and nothing for a variable the user has already set or when <c>MARKWARDT_VSYNC</c> is set.
    /// </summary>
    /// <returns>The variables that were set, as name and value.</returns>
    IReadOnlyList<KeyValuePair<string, string>> DisableSwapSync();
}

/// <inheritdoc />
/// <param name="isLinux">Whether this is Linux.</param>
/// <param name="get">Reads an environment variable of the process, as native code sees it; null when unset.</param>
/// <param name="set">Sets an environment variable of the process, for native code and for processes started from now on.</param>
internal sealed class GraphicsEnvironment(bool isLinux, Func<string, string?> get, Action<string, string> set) : IGraphicsEnvironment
{
    private readonly List<KeyValuePair<string, string>> variables =
    [
        new("__GL_SYNC_TO_VBLANK", "0"),
        new("vblank_mode", "0"),
    ];

    /// <inheritdoc />
    public IReadOnlyList<KeyValuePair<string, string>> DisableSwapSync()
    {
        List<KeyValuePair<string, string>> applied = [];

        if (!isLinux || get("MARKWARDT_VSYNC") is not null)
        {
            return applied;
        }

        foreach (KeyValuePair<string, string> variable in variables)
        {
            if (get(variable.Key) is null)
            {
                set(variable.Key, variable.Value);
                applied.Add(variable);
            }
        }

        return applied;
    }
}
