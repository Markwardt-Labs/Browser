# Ad blocking

Ads are handled at four levels, because no single one catches everything: the network, the page's elements,
popups, and YouTube's video ads. Extensions are not an option: this build of CEF runs without the extension
system (it renders off-screen so Avalonia can draw over the page), so the blocker is part of the app.

## Network

`BrowserRequests` is CEF's request handler. For every request that is not the page itself it asks the blocker
whether to block, and if so returns a handler that cancels the request. Page navigations are never cancelled,
so a blocked address cannot leave a blank error page; it only loses its ads. The check runs on a CEF thread
for every request, so the blocker holds its rules as an immutable set that a reload swaps in whole.

The rules are the usual EasyList syntax, read by `FilterParser`. Each rule is a pattern (with `||` for a
domain start, `|` for an address start or end, `*` for any text and `^` for a separator) plus options: third
party or not, request types, sites. A rule that needs more than that (regular expressions, redirects to stub
resources, scriptlets, extended selectors, parameter stripping) is skipped, never approximated, because a
rule applied more broadly than written breaks sites while one that is skipped only lets an ad through.

Finding the rules that could match an address would be too slow if it meant trying all of them (the lists
hold some 50,000). Each rule is filed under the longest run of letters and digits in its pattern that has to
appear as a whole run in any address it matches; a request looks up its own runs. Rules without such a run
are tried every time. Exception rules (`@@`) are checked only once a blocking rule has matched. A lookup
takes tens of microseconds. Only web addresses are filtered: `file:`, `data:`, internal pages and the
overview are never touched.

## Lists

`FilterLists` keeps the downloaded lists as files in the profile and adds a built-in list (embedded in the
executable) of the biggest ad networks. At start the lists on disk are read and parsed before the first
window opens (a fraction of a second), so the first page is already protected; then a background refresh
downloads any list that is missing or older than three days, checks it looks like a filter list (so an error
page cannot replace a good list), writes it atomically and, if anything changed, rebuilds the rules. A failed
download is silent and leaves the old list. The lists are fetched by the user's own copy and not shipped
with the app, which keeps their licenses (GPL / CC BY-SA) out of its distribution.

## Elements

Blocking requests leaves empty slots and ad markup that is part of the page. The cosmetic rules of the lists
(`##selector`) hide those. `AdBlocker.FrameScript` builds, for each frame, a script that adds one stylesheet:
the selectors limited to the frame's site (and its parent domains, minus the sites a rule excludes and the
selectors a site is excepted from), and, in the main frame only, the selectors that apply everywhere. The
plain ones (a class, an id, a tag) are joined into a few large rules, which is safe because they cannot be
invalid; the others get a rule each, so that one the browser cannot parse costs only itself. The shared part
is built once per set of lists. The script runs when a frame starts loading, so the slots are hidden before
they paint, and again when it finishes (it only acts once).

## Popups

`BrowserPopups` refuses a new window the page opens without a user gesture, which is how pop-ups and
pop-unders are started by ad scripts, and one whose address the blocker matches as an ad, which catches the
ones started from a click on the page. Popup rules are the ordinary rules plus the lists' `popup` ones.

## YouTube video ads

YouTube serves its ads from its own domains, so address rules cannot separate them from videos. Two things are
done instead. Before the page's own scripts run, a script removes the ad fields (`adPlacements`, `playerAds`,
`adSlots`) from the player data: from the data embedded in the page, by catching the assignment of
`ytInitialPlayerResponse`, and from the data it fetches, by wrapping `JSON.parse` and `Response.json`. With no ad
schedule the player has nothing to play. If an ad still plays (the player marks itself as showing one), a timer
presses the skip button, mutes the ad and plays it at 16 times the speed to its end, then restores the
viewer's volume and speed. A stylesheet hides the ad slots and promotions around the page. This depends on
YouTube's data format and page structure, which change without notice; the watch-page behavior in
`YouTube.js` (quality, theater mode, video mode) is separate and waits out ads.

## Limits

No switch turns the blocker off, and there is no count or list of what was blocked. Sites that detect
blockers and refuse to work are not handled. Trackers that the ad-focused lists leave out, such as
analytics, are not blocked.
