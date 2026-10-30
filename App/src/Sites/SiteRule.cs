namespace Markwardt.Browser.Sites;

/// <summary>
/// Adjusts how one website behaves by running a script in each of its pages and embedded frames.
/// </summary>
internal interface ISiteRule
{
    /// <summary>
    /// Gets the script to run in every matching frame once it has loaded. It must be safe to run again in a document it already ran in.
    /// </summary>
    string Script { get; }

    /// <summary>
    /// Decides whether the rule applies to a frame.
    /// </summary>
    /// <param name="address">The address the frame has loaded.</param>
    /// <returns>True if <see cref="Script"/> should run in it.</returns>
    bool Matches(string address);
}
