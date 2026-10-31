namespace Tests.Unit.Launch;

/// <summary>
/// Tests for <see cref="LaunchParser"/>.
/// </summary>
public sealed class LaunchParserTests
{
    private readonly LaunchParser parser = new();

    /// <summary>
    /// No arguments ask for nothing.
    /// </summary>
    [Fact]
    public void Parse_NoArguments_ReturnsDefaults()
    {
        LaunchOptions options = parser.Parse([]);

        Assert.Null(options.Address);
        Assert.Null(options.Goto);
        Assert.False(options.Isolate);
        Assert.False(options.CloseOnly);
        Assert.Null(options.Window);
    }

    /// <summary>
    /// An address on its own is the starting page.
    /// </summary>
    [Fact]
    public void Parse_PlainAddress_SetsAddress()
        => Assert.Equal("example.com", parser.Parse(["example.com"]).Address);

    /// <summary>
    /// The value of --goto is the address to go to, and is not mistaken for a starting page.
    /// </summary>
    /// <param name="first">The first argument.</param>
    /// <param name="second">The second argument; empty for none.</param>
    [Theory]
    [InlineData("--goto", "https://example.com")]
    [InlineData("--goto=https://example.com", "")]
    public void Parse_Goto_SetsGotoOnly(string first, string second)
    {
        LaunchOptions options = parser.Parse(second.Length == 0 ? [first] : [first, second]);

        Assert.Equal("https://example.com", options.Goto);
        Assert.Null(options.Address);
    }

    /// <summary>
    /// A --goto with no value is ignored, and does not swallow the next option.
    /// </summary>
    [Fact]
    public void Parse_GotoWithoutValue_DoesNotConsumeNextOption()
    {
        LaunchOptions options = parser.Parse(["--goto", "--isolate"]);

        Assert.Null(options.Goto);
        Assert.True(options.Isolate);
    }

    /// <summary>
    /// The flags switch their options on.
    /// </summary>
    [Fact]
    public void Parse_Flags_AreSet()
    {
        LaunchOptions options = parser.Parse(["--isolate", "--close-only"]);

        Assert.True(options.Isolate);
        Assert.True(options.CloseOnly);
    }

    /// <summary>
    /// --window takes the three states, in either spelling of the argument and in any letter case.
    /// </summary>
    /// <param name="argument">The arguments, space separated.</param>
    /// <param name="expected">The expected state.</param>
    [Theory]
    [InlineData("--window maximized", Avalonia.Controls.WindowState.Maximized)]
    [InlineData("--window=Unmaximized", Avalonia.Controls.WindowState.Normal)]
    [InlineData("--window MINIMIZED", Avalonia.Controls.WindowState.Minimized)]
    public void Parse_Window_SetsState(string argument, Avalonia.Controls.WindowState expected)
        => Assert.Equal(expected, parser.Parse(argument.Split(' ')).Window);

    /// <summary>
    /// An unknown window state is ignored.
    /// </summary>
    [Fact]
    public void Parse_UnknownWindowState_IsIgnored()
        => Assert.Null(parser.Parse(["--window", "sideways"]).Window);

    /// <summary>
    /// Options may come in any order around an address, and unknown options are ignored.
    /// </summary>
    [Fact]
    public void Parse_MixedArguments_ReadsEverything()
    {
        LaunchOptions options = parser.Parse(["--unknown", "--window", "maximized", "example.com", "--close-only", "--goto", "https://a.example"]);

        Assert.Equal("example.com", options.Address);
        Assert.Equal("https://a.example", options.Goto);
        Assert.Equal(Avalonia.Controls.WindowState.Maximized, options.Window);
        Assert.True(options.CloseOnly);
    }
}
