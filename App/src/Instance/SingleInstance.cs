namespace Markwardt.Browser.Instance;

/// <summary>
/// Makes sure only one copy of the browser runs per user, since a profile on disk cannot be shared. A later launch hands its command line to the running copy instead, which opens a window for it.
/// </summary>
internal interface ISingleInstance
{
    /// <summary>
    /// Claims the right to be the running copy, or hands a command line to the copy that already is.
    /// </summary>
    /// <param name="args">The command line arguments of this launch.</param>
    /// <param name="cancellation">Cancels the attempt.</param>
    /// <returns>True when this launch is now the running copy, false when it handed over and should exit.</returns>
    Task<bool> Claim(IReadOnlyList<string> args, CancellationToken cancellation = default);

    /// <summary>
    /// Starts reporting the command lines that later launches hand over.
    /// </summary>
    /// <param name="received">Called, on a background thread, with the arguments of each launch.</param>
    /// <param name="cancellation">Stops listening.</param>
    void Listen(Action<IReadOnlyList<string>> received, CancellationToken cancellation = default);
}

/// <inheritdoc />
/// <param name="name">The name of the pipe that later launches connect to.</param>
internal sealed class SingleInstance(string name) : ISingleInstance
{
    private NamedPipeServerStream? server;

    /// <inheritdoc />
    public async Task<bool> Claim(IReadOnlyList<string> args, CancellationToken cancellation = default)
    {
        try
        {
            await using NamedPipeClientStream client = new(".", name, PipeDirection.Out);
            await client.ConnectAsync(300, cancellation);
            await using StreamWriter writer = new(client);
            await writer.WriteLineAsync(JsonSerializer.Serialize(args).AsMemory(), cancellation);
            return false;
        }
        catch (Exception exception) when (exception is TimeoutException or IOException)
        {
            server = Create();
            return true;
        }
    }

    /// <inheritdoc />
    public void Listen(Action<IReadOnlyList<string>> received, CancellationToken cancellation = default)
        => _ = Task.Run(() => Accept(received, cancellation), cancellation);

    private NamedPipeServerStream Create()
        => new(name, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

    private async Task Accept(Action<IReadOnlyList<string>> received, CancellationToken cancellation = default)
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

    private async Task Receive(NamedPipeServerStream connected, Action<IReadOnlyList<string>> received, CancellationToken cancellation = default)
    {
        await using (connected)
        {
            using StreamReader reader = new(connected);

            try
            {
                received(JsonSerializer.Deserialize<string[]>(await reader.ReadLineAsync(cancellation) ?? "[]") ?? []);
            }
            catch (JsonException)
            {
                received([]);
            }
        }
    }
}
