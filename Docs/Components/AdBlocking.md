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
rule applied more broadly than written breaks sites while one that is skipped only lets an ad through. The one
rule kind that is read as something close to what it says is `$redirect`: it blocks the request and hands the
page a stand-in, and blocking without the stand-in is the nearest thing this blocker can do.

Finding the rules that could match an address would be too slow if it meant trying all of them (the lists
hold some 50,000). Each rule is filed under the longest run of letters and digits in its pattern that has to
appear as a whole run in any address it matches; a request looks up its own runs. Rules without such a run
are tried every time. Exception rules (`@@`) are checked only once a blocking rule has matched. A lookup
takes tens of microseconds. Only web addresses are filtered: `file:`, `data:`, internal pages and the
overview are never touched.

## Lists

`FilterLists` keeps the downloaded lists as files in the profile and adds a built-in list (embedded in the
executable) of the biggest ad networks and of the shape of ad requests whatever their domain: video ad
requests (`/vast.php?` and similar, limited to scripted and media requests) and the cam-site widgets that adult
sites embed. The shape rules exist because ad networks change domains faster than any list follows: a video ad
request to a domain no list had seen was the first leak found in real use. The downloaded lists are EasyList,
uBlock Origin's filters and unbreak list, Peter Lowe's ad server list, uBlock's badware list and EasyList's
adult-site lists; the pure tracking lists (EasyPrivacy, uBlock's privacy list) are left out on purpose, since
they block things like YouTube's playback statistics and break more than they hide. At start the lists on disk are read and parsed before the first
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
rules written for a site under any country domain (`example.*`, 1,300 in the lists) are kept by the site's
name and match the host with its top level part removed. The plain ones (a class, an id, a tag) are joined into a few large rules, which is safe because they cannot be
invalid; the others get a rule each, so that one the browser cannot parse costs only itself. The shared part
is built once per set of lists. The script runs when a frame starts loading, so the slots are hidden before
they paint, and again when it finishes (it only acts once).

## Popups

`BrowserPopups` refuses a new window the page opens without a user gesture, which is how pop-ups and
pop-unders are started by ad scripts, and one whose address the blocker matches as an ad, which catches the
ones started from a click on the page. Popup rules are the ordinary rules plus the lists' `popup` ones.

## YouTube video ads

YouTube serves its ads from its own domains, so address rules cannot separate them from videos. Two things are
done instead. Before the page's own scripts run (see below), a script removes the ad fields (`adPlacements`, `playerAds`,
`adSlots`) from the player data: from the data embedded in the page, by catching the assignment of
`ytInitialPlayerResponse`, and from the data it fetches, by wrapping `JSON.parse` and `Response.json`. With no ad
schedule the player has nothing to play. If an ad still plays (the player marks itself as showing one), a timer
presses the skip button, mutes the ad and plays it at 16 times the speed to its end, then restores the
viewer's volume and speed. A stylesheet hides the ad slots and promotions around the page. This depends on
YouTube's data format and page structure, which change without notice; the watch-page behavior in
`YouTube.js` (quality, theater mode, video mode) is separate and waits out ads.

"Before the page's own scripts run" needs the renderer process. A script sent from the browser process when a
frame starts loading arrives after the page's first inline scripts have already run (measured: it was late in
every attempt), so the player data embedded in a freshly loaded watch page was never stripped and its pre-roll
ad played until the fallback skipped it. The helper processes therefore run through `HelperProcess` rather than
CefGlue's own helper entry point, with a renderer handler that evaluates the script in each YouTube frame as its
JavaScript context is created. That replaces CefGlue's renderer-side object binding, which this app does not
use. `HelperProcess` also reproduces what CefGlue's entry point did besides: helpers exit when the browser does.

## Limits

No switch turns the blocker off. Sites that detect blockers and refuse to work are not handled. Trackers that
the ad-focused lists leave out, such as analytics, are not blocked. Rules that run script in the page (uBlock's
scriptlets, extended selectors) are not supported, so what only they remove stays. Popups opened by a click
anywhere on a page, to an address no list knows, are not recognized; refused popups and the sites that open
them are in the diagnostics.

## Diagnosing leaks

With `--stats` the blocker counts what it decided, per page host and request host, and writes
`requests-<date>-<time>.log` beside the stats log: blocked requests, refused popups (with the reason), and
every request to another site that was let through. What an ad does that no list catches shows up there as a
host with an ad-like name or a video ad request, and the fix is a rule for its shape or domain.
