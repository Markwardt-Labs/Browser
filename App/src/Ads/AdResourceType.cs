namespace Markwardt.Browser.Ads;

/// <summary>
/// The kinds of request that filter rules can be limited to.
/// </summary>
[Flags]
internal enum AdResourceType
{
    /// <summary>
    /// No type; as a rule's restriction, any type.
    /// </summary>
    None = 0,

    /// <summary>
    /// A top level page.
    /// </summary>
    Document = 1,

    /// <summary>
    /// A frame or iframe.
    /// </summary>
    SubDocument = 2,

    /// <summary>
    /// A stylesheet.
    /// </summary>
    Stylesheet = 4,

    /// <summary>
    /// A script.
    /// </summary>
    Script = 8,

    /// <summary>
    /// An image.
    /// </summary>
    Image = 16,

    /// <summary>
    /// A font.
    /// </summary>
    Font = 32,

    /// <summary>
    /// Audio or video.
    /// </summary>
    Media = 64,

    /// <summary>
    /// An object or embed.
    /// </summary>
    Object = 128,

    /// <summary>
    /// A fetch or XMLHttpRequest.
    /// </summary>
    XmlHttpRequest = 256,

    /// <summary>
    /// A ping or beacon.
    /// </summary>
    Ping = 512,

    /// <summary>
    /// Anything else.
    /// </summary>
    Other = 1024,

    /// <summary>
    /// A new window opened by a page.
    /// </summary>
    Popup = 2048,
}
