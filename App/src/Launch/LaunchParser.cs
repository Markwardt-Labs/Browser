namespace Markwardt.Browser.Launch;

/// <summary>
/// Reads the command line.
/// </summary>
internal interface ILaunchParser
{
    /// <summary>
    /// Reads the options out of the command line arguments. Arguments it does not know are ignored.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>What was asked for.</returns>
    LaunchOptions Parse(IReadOnlyList<string> args);
}

/// <inheritdoc />
internal sealed class LaunchParser : ILaunchParser
{
    /// <inheritdoc />
    public LaunchOptions Parse(IReadOnlyList<string> args)
    {
        LaunchOptions options = new();

        for (int index = 0; index < args.Count; index++)
        {
            string[] parts = args[index].Split('=', 2);
            string? value = parts.Length > 1 ? parts[1] : null;

            if (parts[0] == "--goto")
            {
                (value, index) = Value(args, index, value);
                options = options with { Goto = value };
            }
            else if (parts[0] == "--window")
            {
                (value, index) = Value(args, index, value);
                options = options with { Window = State(value) ?? options.Window };
            }
            else if (parts[0] == "--isolate")
            {
                options = options with { Isolate = true };
            }
            else if (parts[0] == "--stats")
            {
                options = options with { Stats = true };
            }
            else if (parts[0] == "--close-only")
            {
                options = options with { CloseOnly = true };
            }
            else if (!args[index].StartsWith('-') && options.Address is null)
            {
                options = options with { Address = args[index] };
            }
        }

        return options;
    }

    private (string? Value, int Index) Value(IReadOnlyList<string> args, int index, string? inline)
        => inline is not null || index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal)
            ? (inline, index)
            : (args[index + 1], index + 1);

    private WindowState? State(string? value)
        => value?.ToLowerInvariant() switch
        {
            "maximized" => WindowState.Maximized,
            "unmaximized" => WindowState.Normal,
            "minimized" => WindowState.Minimized,
            _ => null,
        };
}
