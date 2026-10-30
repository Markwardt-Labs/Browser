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
        => Assert.True(await new SingleInstance(name).Claim(string.Empty));

    /// <summary>
    /// A later launch hands its address to the running copy and is told to exit.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Claim_SecondLaunch_HandsAddressToRunningCopy()
    {
        using CancellationTokenSource stop = new();
        SingleInstance first = new(name);
        TaskCompletionSource<string> received = new();
        Assert.True(await first.Claim(string.Empty));
        first.Listen(received.SetResult, stop.Token);

        bool second = await new SingleInstance(name).Claim("https://example.com");

        Assert.False(second);
        Assert.Equal("https://example.com", await received.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        await stop.CancelAsync();
    }

    /// <summary>
    /// A launch without an address hands over an empty one.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    [Fact]
    public async Task Claim_SecondLaunchWithoutAddress_HandsOverEmpty()
    {
        using CancellationTokenSource stop = new();
        SingleInstance first = new(name);
        TaskCompletionSource<string> received = new();
        await first.Claim(string.Empty);
        first.Listen(received.SetResult, stop.Token);

        await new SingleInstance(name).Claim(string.Empty);

        Assert.Equal(string.Empty, await received.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        await stop.CancelAsync();
    }
}
