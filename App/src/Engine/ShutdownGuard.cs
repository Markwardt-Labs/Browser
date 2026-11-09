namespace Markwardt.Browser.Engine;

/// <summary>
/// Makes sure the browser process ends when its last window is gone. Avalonia's own lifetime is meant to do this but on the real desktop it was seen to leave the process running, with every page of the last window still loaded, for hours and across days (the next launch simply reused it); this guard works from the application's own list of windows and does not depend on it.
/// </summary>
internal interface IShutdownGuard
{
    /// <summary>
    /// Tells the guard that the last window has closed. If no window has been opened again once the grace period is over, an orderly shutdown is requested, and if the process is still alive after the force delay it is ended.
    /// </summary>
    void LastWindowClosed();
}

/// <inheritdoc />
/// <param name="hasWindows">Tells whether any window is open.</param>
/// <param name="shutDown">Requests an orderly shutdown of the application.</param>
/// <param name="exit">Ends the process.</param>
/// <param name="schedule">Runs an action once after a delay.</param>
/// <param name="grace">How long to wait after the last window closed before shutting down, in case a new one is about to open.</param>
/// <param name="force">How long after requesting the shutdown to end the process if it has not ended by itself.</param>
internal sealed class ShutdownGuard(Func<bool> hasWindows, Action shutDown, Action exit, Action<Action, TimeSpan> schedule, TimeSpan grace, TimeSpan force) : IShutdownGuard
{
    private int generation;

    /// <inheritdoc />
    public void LastWindowClosed()
    {
        int current = Interlocked.Increment(ref generation);

        schedule(() =>
        {
            if (hasWindows() || current != Volatile.Read(ref generation))
            {
                return;
            }

            shutDown();

            schedule(() =>
            {
                if (!hasWindows() && current == Volatile.Read(ref generation))
                {
                    exit();
                }
            }, force);
        }, grace);
    }
}
