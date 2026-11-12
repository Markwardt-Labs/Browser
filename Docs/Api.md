# API

Browser has no library API to consume - it's a desktop application, and its primary surface is the
application window itself. This document covers the *design and flow* of that surface - how the
pieces fit together and the order things happen in - the same role it would play for a library's
public types, but for the UI a user actually drives.

## Shape

One frameless window: a top bar (navigation buttons, the address box with the new-tab button on its left
and the bookmark star on its right, and the window controls) over the active page. The user can hide the
bar outside the overview, leaving only the page. F1 raises an overview over the page, with the bar above
it: a sidebar listing the bookmarks and a grid of cards, one per open tab, each with a thumbnail and the page
title. Avalonia owns the window, the
overview and all input; Chromium Embedded Framework owns everything inside a page (rendering,
networking, JavaScript). The two meet in `BrowserTab`, which wraps one CEF browser and mirrors its
address and title into properties the UI binds to; CEF raises those on its own threads, so each is
posted to the UI thread first.

`BrowserTabs` holds the open tabs and which one is active. Only the active tab is visible; a background
tab keeps running at first, then is frozen, suspended and finally closed as it sits unused, unless it is
locked (by the user, or automatically while its page has typed text, a transfer or sound). Showing a tab wakes it.

## Flow

1. **Start** - `Program.Main` first checks whether this process is a CEF helper (renderer, GPU,
   ...) and, if so, runs as one and exits. Otherwise it checks whether the browser is already running
   and, if so, hands it the command line and exits. Otherwise it reads the command line (`LaunchParser`),
   loads the remembered settings and builds the Avalonia app, initializing CEF in off-screen mode, on the
   persistent profile, with dark-mode switches before the first window. `App` opens a window for the
   command line (and later for every command line handed over): its first tab is on the `--goto` address or
   the address given on its own if any, and the window is in the state `--window` asked for. It opens in
   the overview unless `--goto` or `--isolate` was given. Further windows come from the overview's new window button.
2. **Overview** - F1 (or Ctrl+L, or Ctrl+T) captures a thumbnail of the active page, shows the
   overview and moves keyboard focus to the address box. F1, Esc, or
   choosing a card hides it again and hands focus back to the page.
3. **Navigate** - what is typed into the address box is interpreted by `IAddressResolver`. Text starting
   with `:` is an address, loaded in the active tab with `https://` added when it has no scheme; anything
   else is a Google search, opened in a new tab. While a search is being typed, the bookmarks whose title
   contains it are offered under the box: Down and Up choose one, and Enter opens it in the active tab
   instead of searching. The box is empty on a blank page and otherwise shows the page's address behind
   a colon, so submitting it unchanged reloads the page. Launch arguments are plain addresses and need no colon.
4. **Bookmarks** - the star in the top bar bookmarks the active page, or removes its bookmark when it
   already has one, and shows filled for a bookmarked page. Choosing a bookmark in the sidebar loads it
   in the active tab, a middle click opens it in a new tab, its pencil renames it in place (Enter or
   leaving the field saves, Esc discards), and its cross removes it. Every change is
   saved straight away to a file in the profile folder, and the list is read back at start.
5. **Tabs** - choosing a card makes its tab active; the close button or a middle click closes it. "Open
   link in new tab", "View page source" and a middle or Alt click on a link become tabs through the tab's
   new-tab request, and F2, Ctrl+T or the new-tab button opens a blank one with the overview up for an
   address. A page's own request for a window (`target=_blank`, `window.open`) loads in the current tab
   instead, so tabs are only created by the user.
6. **Right-click** - CEF hands over its context menu model, which is shown as an Avalonia menu at
   the click position; choosing an entry runs it, and dismissing the menu cancels it.
7. **Later launches** - while the browser runs, starting it again opens a new window in the running
   copy for the given command line (a blank tab with the overview up if it names no address) and brings
   it forward.
8. **Shortcuts** - a key press resolves through `ShortcutMap` to a command that the window runs,
   whether it arrived through Avalonia or through CEF's keyboard handler.
9. **Page questions** - when a page blocks on an alert, confirm, prompt or "leave this page?" check, the
   tab raises it as a question and the window shows it over the page until it is answered.
