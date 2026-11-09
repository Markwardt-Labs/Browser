namespace Tests.Unit.Tabs;

/// <summary>
/// Tests for <see cref="PageDialog"/>.
/// </summary>
public sealed class PageDialogTests
{
    private readonly List<(bool Accepted, string Text)> answers = [];

    private PageDialog Create(PageDialogKind kind = PageDialogKind.Prompt)
        => new(kind, "example.com", "Name?", "default", (accepted, text) => answers.Add((accepted, text)));

    /// <summary>
    /// The dialog exposes what the page asked.
    /// </summary>
    [Fact]
    public void Properties_ReflectTheQuestion()
    {
        PageDialog dialog = Create(PageDialogKind.Confirm);

        Assert.Equal(PageDialogKind.Confirm, dialog.Kind);
        Assert.Equal("example.com", dialog.Origin);
        Assert.Equal("Name?", dialog.Message);
        Assert.Equal("default", dialog.DefaultText);
        Assert.False(dialog.IsAnswered);
    }

    /// <summary>
    /// The answer reaches the page and marks the dialog answered.
    /// </summary>
    [Fact]
    public void Answer_HandsItToThePage()
    {
        PageDialog dialog = Create();
        dialog.Answer(true, "Ada");

        Assert.Equal([(true, "Ada")], answers);
        Assert.True(dialog.IsAnswered);
    }

    /// <summary>
    /// A second answer is ignored, so the page's callback is only completed once.
    /// </summary>
    [Fact]
    public void Answer_Twice_CountsOnlyTheFirst()
    {
        PageDialog dialog = Create();
        dialog.Answer(false, string.Empty);
        dialog.Answer(true, "late");

        Assert.Equal([(false, string.Empty)], answers);
    }
}
