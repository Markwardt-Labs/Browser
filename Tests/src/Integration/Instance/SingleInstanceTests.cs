namespace Tests.Integration.Instance;

/// <summary>
/// Tests for <see cref="SingleInstance"/> over a real named pipe.
/// </summary>
public sealed class SingleInstanceTests
{
    private readonly string name = $"Tests.{Guid.NewGuid():N}";

    /// <summary>
    /// The first launch becomes the running copy.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Claim_FirstLaunch_BecomesRunningCopy()
        => Assert.True(await new SingleInstance(name).Claim([]));

    /// <summary>
    /// A later launch hands its whole command line to the running copy and is told to exit.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Claim_SecondLaunch_HandsArgumentsToRunningCopy()
    {
        using CancellationTokenSource stop = new();
        SingleInstance first = new(name);
        TaskCompletionSource<IReadOnlyList<string>> received = new();
        Assert.True(await first.Claim([]));
        first.Listen(received.SetResult, stop.Token);

        bool second = await new SingleInstance(name).Claim(["--isolate", "--goto", "https://example.com/a b"]);

        Assert.False(second);
        Assert.Equal(["--isolate", "--goto", "https://example.com/a b"], await received.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        await stop.CancelAsync();
    }

    /// <summary>
    /// A launch without arguments hands over an empty command line.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Claim_SecondLaunchWithoutArguments_HandsOverNone()
    {
        using CancellationTokenSource stop = new();
        SingleInstance first = new(name);
        TaskCompletionSource<IReadOnlyList<string>> received = new();
        await first.Claim([]);
        first.Listen(received.SetResult, stop.Token);

        await new SingleInstance(name).Claim([]);

        Assert.Empty(await received.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        await stop.CancelAsync();
    }
}
