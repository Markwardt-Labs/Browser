# Site rules and video mode

A site rule adjusts how one website behaves by running a script in each of its frames after they
load. Rules exist so that site-specific behavior is a script and a match, not code scattered through
the tab and window types.

## Injection

`BrowserTab` runs the scripts of every matching rule in each frame that finishes loading, main or
embedded, in rule order, with the frame's own address as the match: a YouTube player embedded in
another site is a frame of its own and is matched on its own address. A full page load replaces the
document and the scripts run again; every script installs itself once per document, because YouTube
navigates without reloading and a frame can load more than once. Scripts that need to keep working
across such navigations poll on a short timer rather than hook the site's own events, which change
without notice.

## Video mode

`VideoModeRule` runs on every web and file page and exposes a small API on the page. Entering finds the
largest visible video (playing ones preferred) and the box that holds it together with its controls,
then fixes that box to the viewport. A site can name the box itself; otherwise it is the highest
ancestor that is not much bigger than the video. The ancestors lose the properties that would trap a
fixed element (transforms, filters, containment) and the siblings along the chain are hidden, since
paint order alone did not keep YouTube's masthead and description from drawing over the player.
Leaving undoes exactly what was added. The page is told to relayout after both, because players size
their video from script.

F4 toggles it. A video inside an iframe is handled by messaging: a frame that enters tells its parent,
which fills the iframe element the same way, up to the top, and a toggle fans out to child frames the
same way. Video mode also ends by itself when its video element leaves the page.

## YouTube

`YouTubeRule` matches `youtube.com` and `youtube-nocookie.com` and their subdomains. Per video, found
from the player's own video id, it:

- sets a fixed quality through the player API (1080p, or the best level below it that the video offers)
  and checks that the player took it, retrying a few times, because it may be refused while an ad plays
  or before the video has loaded;
- on watch pages, clicks the player's size button once when the page is not already in theater mode;
- on watch pages, enters video mode.

Each step is tried a limited number of times and never repeated for the same video, so the viewer's
own change of quality, theater mode or video mode is not undone. Leaving the watch page ends video mode.

## Page state

`PageStateRule` matches every web and file frame. Its script reports to the browser through the console what
the tab's lock and restore need: typed text still in a field, an upload (a `fetch` or `XMLHttpRequest` with a
file or large body, or a form with a file chosen), media that is playing unmuted, and, for the top frame, the
scroll position and the position of the longest video or audio. Reports are sent only when something changes,
with the video position at most every five seconds and at once on pause or seek, so a quiet page sends none.
A report from a frame the page itself spoofs only affects that tab.

