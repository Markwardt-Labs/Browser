# Usage

Walkthroughs of Browser in different situations, in place of runnable code samples - there's no
programmatic API to call, just the application itself.

## The top bar and the overview

The window is a top bar over the page. The bar holds the address box with back, forward, reload and new
tab buttons, the bookmark star, a new window button at its left edge, and the minimize, maximize and
close buttons. Its toggle button (left of minimize) chooses whether the bar always shows or only shows in
the overview; the choice is remembered between runs and starts on.

F1 brings up the overview: a sidebar of bookmarks and a grid of cards, one per open tab, under the top bar.
Press F1 or Esc again, or choose a card, to go back to the page; clicking empty space does nothing. The
window opens in the overview. Drag the top bar to move the window, double-click it to maximize, and drag
the window's edges to resize it.

## Command line

Each option applies to the window the launch opens.

| Option | Does |
| --- | --- |
| `<address>` | Opens the address as the first page, with the overview raised over it |
| `--goto <url>` | Goes straight to the page instead of showing the overview |
| `--isolate` | Switches the overview off and keeps the top bar always on with only the window controls in it; the bar's toggle is ignored and not saved, F1, Ctrl+L and new tabs do nothing, and links that ask for a new tab load in the current one |
| `--window maximized\|unmaximized\|minimized` | Sets the state the window starts in |
| `--stats` | Writes a performance log (see Performance below); it covers the whole run, so only the launch that starts the browser decides it |
| `--close-only` | Hides the minimize and maximize buttons and stops double-clicking the bar from maximizing |

There is one running copy per user. Launching the program again, with or without options, opens a new window
in the running copy and comes forward; the options on that command line apply to that window only, so
`--isolate` or `--close-only` on a later launch does not change the windows that are already open.

## Open a page or search

Type into the address box (it shows in the overview, and outside it when the top bar is kept visible), then press Enter. Text that starts with a colon is an address:
`:youtube.com` loads `https://youtube.com` in the current tab, and `:http://localhost:5000` is used as
typed. Anything else is a search: `avalonia ui docs` opens a Google search in a new tab. While you type
a search, bookmarks whose name matches appear under the box; press Down to move onto one and Enter to
open it in the current tab instead. Esc closes the suggestions, and a second Esc closes the overview, or, outside the overview, puts the page's address back and returns to the page.
The box is empty on a blank tab and shows the current page's address behind a colon otherwise. Ctrl+L focuses the box, or opens the
overview when the top bar is hidden. An address on the command line needs no colon.

## Work with tabs

F2, Ctrl+T, or the plus button beside the address box in the overview, opens a blank tab and puts you in
the address box. In the overview, click a card to switch to that tab, and close one with its cross or a
middle click. Closing the last tab leaves a blank one.
Clicking a link always follows it in the current tab, even when the page asks for a new window; tabs are
only created by you. Windows a page opens without you having clicked anything are refused as ad popups. The one exception is a script popup (`window.open` with a size, as the "Sign in with
Google" dialog uses): it opens as a small separate window that stays connected to the page that opened
it, since the sign-in would otherwise never finish. Middle-clicking a link opens it in a new tab, and Alt+clicking or Ctrl+clicking it
opens it in a background tab, which loads without taking you away from the page.

The overview's top bar has back, forward and reload buttons before the plus button, and a new window
button at its left edge. A new window starts with a blank tab and keeps its own tabs; windows share
bookmarks and sign-ins, and closing one leaves the others running.

| Shortcut | Does |
| --- | --- |
| F1 | Show or hide the overview |
| F2 or Ctrl+T / Ctrl+W | New tab / close tab |
| Ctrl+L | Focus the address box (opening the overview if the top bar is hidden) |
| Ctrl+R or F5 | Reload |
| Ctrl+F | Find in page |
| Alt+Left / Alt+Right | Back / forward |
| F4 | Video mode on or off |

## Find in page

Ctrl+F opens a find box at the top right of the page. Matches are highlighted as you type and the box
shows which one is active out of how many. Enter (or the down arrow) goes to the next match, Shift+Enter
(or the up arrow) to the previous one, Aa makes the search case sensitive, and Esc or the cross closes
it. The box goes away on its own when you open the overview or switch tabs.

## Ads

Ads are blocked without any setup. Requests to ad networks are cancelled before they load, elements that
are left behind (empty ad slots) are hidden, windows that a page opens by itself, or that lead to an ad, are
refused, and YouTube's video ads are removed from the player and skipped when one still gets through. Clicking
a link or button yourself always works, unless the window it opens goes to a known ad address.

The rules come from EasyList, the uBlock Origin filter lists, Peter Lowe's ad server list and EasyList's
adult-site lists, which are downloaded when the browser first runs with a connection and refreshed every few
days (a small built-in list, which also recognizes video ad requests by their shape, covers the time before that
and offline use). They are kept in the `filters` folder of the profile, so deleting a file there forces a fresh
download. The blocker catches what the lists describe: a new ad network, or one a site serves from its own
domain, can get through until the lists catch up. A site that breaks because of the blocking can only be
fixed by changing the lists for now, as there is no switch.

## Video mode and YouTube

F4 makes the page's main video fill the whole page view, controls included, and puts it back. YouTube
watch pages enter video mode by themselves, and YouTube videos, including embedded players on other
sites, play at a fixed 1080p (or the best quality below it that the video has) instead of automatic
quality; watch pages also switch to theater mode, which is what you see after leaving video mode.
Choosing another quality or leaving video mode sticks until the next video starts.

## Use the context menu

Right-click a page for Back, Forward, Reload, View page source and Inspect; right-clicking a link
adds Open, Open in new tab and Open in background tab, and a text field shows the editing commands (Cut, Copy, Paste, ...) instead of the page entries. The menu closes when you choose
an entry or click anywhere else. View page source opens in a tab, and Inspect opens the developer
tools in their own window.

## Bookmark pages

Open the overview and press the star next to the address box to bookmark the page you were on; the
star is filled for a page that already has a bookmark, and pressing it again removes it. Bookmarks
are listed in the sidebar on the left: click one to load it in the current tab, middle-click to open
it in a new tab. On hover a bookmark shows a pencil, which turns its name into a field you can edit
(Enter or clicking away saves, Esc cancels), and a cross, which removes it. A blank tab cannot be
bookmarked.

## Stay signed in

Sign-ins, cookies, site data and bookmarks are kept between runs in the `Markwardt.Browser` folder under your
local application data folder (`~/.local/share` on Linux, `%LocalAppData%` on Windows). Only one copy of
the browser runs at a time: starting it again opens the address you gave it, as a tab, in the copy that
is already running.

## Pages are dark

Every page is shown dark: sites with a dark theme use it, and the rest are darkened automatically.
There is no setting to turn this off.

## Install it on this machine

On Linux, `dotnet run Scripts/Install.cs` from the repository root builds the app and installs it for the
current user. It then shows up as "Browser" in the application launcher; run the script again to update it.
A path or `file://` address given when launching opens that file.

The install is a single executable (about 330 MB) with the Chromium runtime inside it; the first launch unpacks
the runtime into your local application data folder, which takes a few seconds. The install also downloads
(once, about 140 MB) a Chromium runtime with H.264 and AAC support to pack in, so video on sites such as Steam
plays. If that download fails the install still completes, without those codecs, and running the script again
retries. HEVC video does not play either way.

`dotnet run Scripts/Install.cs -- --stats` makes the launcher entry start the browser with `--stats`.

## Performance log

With `--stats` the browser writes `stats/stats-<date>-<time>.log` in the profile folder (the newest ten are kept,
and a log is closed at 20 MB and a new one started): comment lines describing the machine, screens and how the
window is drawn, then a line every two seconds with the frames delivered per second and their size, how long the
window's render loop took to run after a frame arrived (`painttick`), CPU use of the browser, GPU, renderer and
other helper processes (100% is one core), how late the UI thread ran, garbage collections, memory, whether the
window has focus (`active`), where the mouse pointer is (`pointer`: over the window, beside it on the same
screen, or on another screen), how long it has been still (`idle`) and which screen the window is on, how many
frames a second changed at least a quarter of the window (`big`, which is what a playing video does), how the playing video is doing as the page sees
it (`video` frames per second shown, `dropped` frames) and the site being viewed.
The ad blocker's decisions go to `requests-<date>-<time>.log` beside it: what was blocked, which popups were
refused and which requests to other sites were let through. It records host names only, never full addresses.

## Run it from source

```
dotnet run Scripts/Run.cs
```

Run from the repository root. Over SSH or in CI, run it under a virtual display with a window
manager instead, for example `xvfb-run -a sh -c 'openbox & dotnet run Scripts/Run.cs'`.
