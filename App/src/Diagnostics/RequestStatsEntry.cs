namespace Markwardt.Browser.Diagnostics;

/// <summary>
/// How often one kind of request to one host, made by one site, was blocked or let through.
/// </summary>
/// <param name="Page">The host of the page that made the request.</param>
/// <param name="Host">The host the request went to.</param>
/// <param name="Type">The kind of request.</param>
/// <param name="Blocked">Whether it was blocked.</param>
/// <param name="Count">How many times.</param>
internal sealed record RequestStatsEntry(string Page, string Host, string Type, bool Blocked, int Count);
