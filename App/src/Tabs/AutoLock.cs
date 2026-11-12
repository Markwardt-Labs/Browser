namespace Markwardt.Browser.Tabs;

/// <summary>
/// Works out from what the frames of a page report, and from the downloads running, whether the tab must be left alone.
/// </summary>
internal interface IAutoLock
{
    /// <summary>
    /// Gets what currently protects the tab.
    /// </summary>
    AutoLockReason Reasons { get; }

    /// <summary>
    /// Takes in a report from a frame.
    /// </summary>
    /// <param name="report">The report.</param>
    /// <returns>True if <see cref="Reasons"/> changed.</returns>
    bool Apply(PageReport report);

    /// <summary>
    /// Forgets what the frames of the old document reported, because the page navigated. Downloads are not affected, since they go on.
    /// </summary>
    /// <returns>True if <see cref="Reasons"/> changed.</returns>
    bool Reset();

    /// <summary>
    /// Notes that a download started or ended.
    /// </summary>
    /// <param name="id">The download's identifier.</param>
    /// <param name="inProgress">Whether it is running.</param>
    /// <returns>True if <see cref="Reasons"/> changed.</returns>
    bool Download(uint id, bool inProgress);
}

/// <inheritdoc />
internal sealed class AutoLock : IAutoLock
{
    private readonly Dictionary<string, PageReport> frames = [];
    private readonly HashSet<uint> downloads = [];

    /// <inheritdoc />
    public AutoLockReason Reasons
    {
        get
        {
            AutoLockReason reasons = AutoLockReason.None;

            if (frames.Values.Any(frame => frame.Typed))
            {
                reasons |= AutoLockReason.Typing;
            }

            if (downloads.Count > 0 || frames.Values.Any(frame => frame.Uploading))
            {
                reasons |= AutoLockReason.Transfer;
            }

            if (frames.Values.Any(frame => frame.Audible))
            {
                reasons |= AutoLockReason.Sound;
            }

            return reasons;
        }
    }

    /// <inheritdoc />
    public bool Apply(PageReport report)
    {
        AutoLockReason before = Reasons;

        if (report.IsBusy)
        {
            frames[report.Id] = report;
        }
        else
        {
            frames.Remove(report.Id);
        }

        return before != Reasons;
    }

    /// <inheritdoc />
    public bool Reset()
    {
        AutoLockReason before = Reasons;
        frames.Clear();
        return before != Reasons;
    }

    /// <inheritdoc />
    public bool Download(uint id, bool inProgress)
    {
        AutoLockReason before = Reasons;

        if (inProgress)
        {
            downloads.Add(id);
        }
        else
        {
            downloads.Remove(id);
        }

        return before != Reasons;
    }
}
