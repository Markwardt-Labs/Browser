namespace Markwardt.Browser.Instance;

/// <summary>
/// Makes sure only one copy of the browser runs per user, since a profile on disk cannot be shared. A later launch hands its address to the running copy instead.
/// </summary>
internal interface ISingleInstance
{
    /// <summary>
    /// Claims the right to be the running copy, or hands an address to the copy that already is.
    /// </summary>
    /// <param name="address">The address this launch was asked to open; empty for none.</param>
    /// <param name="cancellation">Cancels the attempt.</param>
    /// <returns>True when this launch is now the running copy, false when it handed over and should exit.</returns>
    Task<bool> Claim(string address, CancellationToken cancellation = default);

    /// <summary>
    /// Starts reporting the addresses that later launches hand over.
    /// </summary>
    /// <param name="received">Called, on a background thread, with each address; empty when there was none.</param>
    /// <param name="cancellation">Stops listening.</param>
    void Listen(Action<string> received, CancellationToken cancellation = default);
}

/// <inheritdoc />
/// <param name="name">The name of the pipe that later launches connect to.</param>
internal sealed class SingleInstance(string name) : ISingleInstance
{
    private NamedPipeServerStream? server;

    /// <inheritdoc />
    public async Task<bool> Claim(string address, CancellationToken cancellation = default)
    {
        try
        {
            await using NamedPipeClientStream client = new(".", name, PipeDirection.Out);
            await client.ConnectAsync(300, cancellation);
            await using StreamWriter writer = new(client);
            await writer.WriteLineAsync(address.AsMemory(), cancellation);
            return false;
        }
        catch (Exception exception) when (exception is TimeoutException or IOException)
        {
            server = Create();
            return true;
        }
    }

    /// <inheritdoc />
    public void Listen(Action<string> received, CancellationToken cancellation = default)
        => _ = Task.Run(() => Accept(received, cancellation), cancellation);

    private NamedPipeServerStream Create()
        => new(name, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

    private async Task Accept(Action<string> received, CancellationToken cancellation)
    {
        NamedPipeServerStream current = server ?? throw new InvalidOperationException("Claim must succeed before listening.");

        while (!cancellation.IsCancellationRequested)
        {
            await current.WaitForConnectionAsync(cancellation);
            NamedPipeServerStream connected = current;
            current = Create();
            _ = Receive(connected, received, cancellation);
        }
    }

    private async Task Receive(NamedPipeServerStream connected, Action<string> received, CancellationToken cancellation)
    {
        await using (connected)
        {
            using StreamReader reader = new(connected);
            received(await reader.ReadLineAsync(cancellation) ?? string.Empty);
        }
    }
}
