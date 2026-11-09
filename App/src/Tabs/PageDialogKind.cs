namespace Markwardt.Browser.Tabs;

/// <summary>
/// The kinds of question a page can block on.
/// </summary>
internal enum PageDialogKind
{
    /// <summary>
    /// A message with an OK button (<c>alert</c>).
    /// </summary>
    Alert,

    /// <summary>
    /// A question answered with OK or Cancel (<c>confirm</c>).
    /// </summary>
    Confirm,

    /// <summary>
    /// A question answered with text and OK, or Cancel (<c>prompt</c>).
    /// </summary>
    Prompt,

    /// <summary>
    /// The question a page asks before it is left, by navigating away, going back or reloading (<c>beforeunload</c>).
    /// </summary>
    LeavePage,
}
