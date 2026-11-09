namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// How late the UI thread has been when asked to run something, since the last time it was taken.
/// </summary>
/// <param name="MaxLateMilliseconds">The longest delay.</param>
/// <param name="Stalls">How many times the delay was over 50 ms.</param>
internal sealed record UiProbeSnapshot(double MaxLateMilliseconds, int Stalls);
