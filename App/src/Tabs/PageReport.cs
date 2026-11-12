namespace Markwardt.Browser.Tabs;

/// <summary>
/// What a page's own script says about the state of one of its frames. It is sent through the console, so anything the page prints could pose as one; a report only ever changes the tab it came from.
/// </summary>
internal sealed record PageReport
{
    /// <summary>
    /// Gets what a report's console message starts with.
    /// </summary>
    public static readonly string Prefix = "MWSTATE ";

    /// <summary>
    /// Reads a report from a console message.
    /// </summary>
    /// <param name="message">The console message.</param>
    /// <param name="report">The report, when the message is one.</param>
    /// <returns>True if the message was a well formed report.</returns>
    public static bool TryParse(string? message, out PageReport report)
    {
        report = null!;

        if (message is null || !message.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(message.AsMemory(Prefix.Length));
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("id", out JsonElement id) || id.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            double scrollX = 0;
            double scrollY = 0;

            if (root.TryGetProperty("scroll", out JsonElement scroll) && scroll.ValueKind == JsonValueKind.Array && scroll.GetArrayLength() == 2)
            {
                scrollX = scroll[0].GetDouble();
                scrollY = scroll[1].GetDouble();
            }

            MediaPosition? media = null;

            if (root.TryGetProperty("video", out JsonElement video) && video.ValueKind == JsonValueKind.Object)
            {
                media = new MediaPosition(video.GetProperty("t").GetDouble(), video.GetProperty("d").GetDouble(), video.GetProperty("p").GetBoolean());
            }

            report = new PageReport
            {
                Id = id.GetString() ?? string.Empty,
                IsTop = Flag(root, "top"),
                Typed = Flag(root, "typed"),
                Uploading = Flag(root, "uploading"),
                Audible = Flag(root, "audible"),
                ScrollX = scrollX,
                ScrollY = scrollY,
                Media = media,
            };

            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return false;
        }
    }

    private static bool Flag(JsonElement root, string name)
        => root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.True;

    /// <summary>
    /// Gets an identifier of the frame's document, so that a later report replaces an earlier one.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets a value indicating whether the report comes from the top frame of the page.
    /// </summary>
    public bool IsTop { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user has typed text that is still in a field.
    /// </summary>
    public bool Typed { get; init; }

    /// <summary>
    /// Gets a value indicating whether the page is uploading something.
    /// </summary>
    public bool Uploading { get; init; }

    /// <summary>
    /// Gets a value indicating whether the page is playing sound.
    /// </summary>
    public bool Audible { get; init; }

    /// <summary>
    /// Gets how far the top frame was scrolled across, in pixels.
    /// </summary>
    public double ScrollX { get; init; }

    /// <summary>
    /// Gets how far the top frame was scrolled down, in pixels.
    /// </summary>
    public double ScrollY { get; init; }

    /// <summary>
    /// Gets the position of the long video or audio in the top frame, if there is one.
    /// </summary>
    public MediaPosition? Media { get; init; }

    /// <summary>
    /// Gets a value indicating whether the frame is doing anything that matters to the tab's lock.
    /// </summary>
    public bool IsBusy => Typed || Uploading || Audible;
}
