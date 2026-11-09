namespace Tests.Unit.Engine;

/// <summary>
/// Tests for <see cref="ShutdownGuard"/>.
/// </summary>
public sealed class ShutdownGuardTests
{
    private readonly List<(Action Action, TimeSpan Delay)> scheduled = [];
    private readonly TimeSpan grace = TimeSpan.FromSeconds(3);
    private readonly TimeSpan force = TimeSpan.FromSeconds(8);
    private bool hasWindows;
    private int shutDowns;
    private int exits;

    private ShutdownGuard CreateGuard()
        => new(() => hasWindows, () => shutDowns++, () => exits++, (action, delay) => scheduled.Add((action, delay)), grace, force);

    /// <summary>
    /// Closing the last window schedules a check after the grace period and does nothing yet.
    /// </summary>
    [Fact]
    public void LastWindowClosed_SchedulesGraceCheck()
    {
        CreateGuard().LastWindowClosed();

        Assert.Single(scheduled);
        Assert.Equal(grace, scheduled[0].Delay);
        Assert.Equal(0, shutDowns);
    }

    /// <summary>
    /// With no window after the grace period the application is asked to shut down and the exit is scheduled.
    /// </summary>
    [Fact]
    public void GraceElapsed_NoWindows_ShutsDownAndSchedulesExit()
    {
        CreateGuard().LastWindowClosed();
        scheduled[0].Action();

        Assert.Equal(1, shutDowns);
        Assert.Equal(2, scheduled.Count);
        Assert.Equal(force, scheduled[1].Delay);
        Assert.Equal(0, exits);
    }

    /// <summary>
    /// A window opened during the grace period cancels the shutdown.
    /// </summary>
    [Fact]
    public void GraceElapsed_WindowReopened_DoesNothing()
    {
        CreateGuard().LastWindowClosed();
        hasWindows = true;
        scheduled[0].Action();

        Assert.Equal(0, shutDowns);
        Assert.Single(scheduled);
    }

    /// <summary>
    /// A newer close supersedes the check of an earlier one.
    /// </summary>
    [Fact]
    public void GraceElapsed_SupersededByLaterClose_DoesNothing()
    {
        ShutdownGuard guard = CreateGuard();
        guard.LastWindowClosed();
        guard.LastWindowClosed();
        scheduled[0].Action();

        Assert.Equal(0, shutDowns);
    }

    /// <summary>
    /// If the process still runs after the shutdown request it is exited.
    /// </summary>
    [Fact]
    public void ForceElapsed_StillNoWindows_Exits()
    {
        CreateGuard().LastWindowClosed();
        scheduled[0].Action();
        scheduled[1].Action();

        Assert.Equal(1, exits);
    }

    /// <summary>
    /// A window opened after the shutdown request but before the force deadline prevents the exit.
    /// </summary>
    [Fact]
    public void ForceElapsed_WindowOpened_DoesNotExit()
    {
        CreateGuard().LastWindowClosed();
        scheduled[0].Action();
        hasWindows = true;
        scheduled[1].Action();

        Assert.Equal(0, exits);
    }
}
