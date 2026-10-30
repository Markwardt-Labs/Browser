# Usage

Walkthroughs of Browser in different situations, in place of runnable code samples - there's no
programmatic API to call, just the application itself.

## See only the page

The window shows nothing but the page. Press F1 to bring up the overview: a top bar with the address
box and the minimize, maximize and close buttons, over a grid of cards, one per open tab. Press F1 or
Esc again, or choose a card, to go back to the page; clicking empty space does nothing. The window itself opens in the overview. Drag the top bar to
move the window, double-click it to maximize, and drag the window's edges to resize it.

## Open a page or search

Open the overview and type into the box, then press Enter. Text that starts with a colon is an address:
`:youtube.com` loads `https://youtube.com` in the current tab, and `:http://localhost:5000` is used as
typed. Anything else is a search: `avalonia ui docs` opens a Google search in a new tab. While you type
a search, bookmarks whose name matches appear under the box; press Down to move onto one and Enter to
open it in the current tab instead. Esc closes the suggestions, and a second Esc closes the overview.
The box is empty on a blank tab and shows the current page's address behind a colon otherwise. A first
command-line argument is opened as the starting page, with no colon needed.

## Work with tabs

F2, Ctrl+T, or the plus button beside the address box in the overview, opens a blank tab and puts you in
the address box. In the overview, click a card to switch to that tab, and close one with its cross or a
middle click. Closing the last tab leaves a blank one.
Clicking a link always follows it in the current tab, even when the page asks for a new window; tabs are
only created by you. Middle-clicking a link opens it in a new tab, and Alt+clicking or Ctrl+clicking it
opens it in a background tab, which loads without taking you away from the page.

The overview's top bar has back, forward and reload buttons before the plus button, and a new window
button at its left edge. A new window starts with a blank tab and keeps its own tabs; windows share
bookmarks and sign-ins, and closing one leaves the others running.

| Shortcut | Does |
| --- | --- |
| F1 | Show or hide the overview |
| F2 or Ctrl+T / Ctrl+W | New tab / close tab |
| Ctrl+L | Show the overview with the address box focused |
| Ctrl+R or F5 | Reload |
| Ctrl+F | Find in page |
| Alt+Left / Alt+Right | Back / forward |
| F4 | Video mode on or off |

## Find in page

Ctrl+F opens a find box at the top right of the page. Matches are highlighted as you type and the box
shows which one is active out of how many. Enter (or the down arrow) goes to the next match, Shift+Enter
(or the up arrow) to the previous one, Aa makes the search case sensitive, and Esc or the cross closes
it. The box goes away on its own when you open the overview or switch tabs.

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

## Run it from source

```
dotnet run Scripts/Run.cs
```

Run from the repository root. Over SSH or in CI, run it under a virtual display with a window
manager instead, for example `xvfb-run -a sh -c 'openbox & dotnet run Scripts/Run.cs'`.
