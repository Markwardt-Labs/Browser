namespace Markwardt.Browser.Tabs;

/// <summary>
/// A question a page is blocked on until the user answers it.
/// </summary>
internal interface IPageDialog
{
    /// <summary>
    /// Gets the kind of question.
    /// </summary>
    PageDialogKind Kind { get; }

    /// <summary>
    /// Gets the site that asks.
    /// </summary>
    string Origin { get; }

    /// <summary>
    /// Gets the text of the question.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the text a prompt starts with.
    /// </summary>
    string DefaultText { get; }

    /// <summary>
    /// Gets a value indicating whether the question has been answered.
    /// </summary>
    bool IsAnswered { get; }

    /// <summary>
    /// Answers the question and lets the page go on. Only the first answer counts.
    /// </summary>
    /// <param name="accepted">Whether the user agreed (OK, Leave), rather than refused (Cancel, Stay).</param>
    /// <param name="text">What the user typed, for a prompt.</param>
    void Answer(bool accepted, string text);
}

/// <inheritdoc />
/// <param name="kind">The kind of question.</param>
/// <param name="origin">The site that asks.</param>
/// <param name="message">The text of the question.</param>
/// <param name="defaultText">The text a prompt starts with.</param>
/// <param name="answered">Called once with the answer, to hand it to the page.</param>
internal sealed class PageDialog(PageDialogKind kind, string origin, string message, string defaultText, Action<bool, string> answered) : IPageDialog
{
    private int isAnswered;

    /// <inheritdoc />
    public PageDialogKind Kind => kind;

    /// <inheritdoc />
    public string Origin => origin;

    /// <inheritdoc />
    public string Message => message;

    /// <inheritdoc />
    public string DefaultText => defaultText;

    /// <inheritdoc />
    public bool IsAnswered => Volatile.Read(ref isAnswered) == 1;

    /// <inheritdoc />
    public void Answer(bool accepted, string text)
    {
        if (Interlocked.Exchange(ref isAnswered, 1) == 0)
        {
            answered(accepted, text);
        }
    }
}
