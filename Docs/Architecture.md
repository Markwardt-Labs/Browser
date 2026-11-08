# Architecture

This document explains the high-level design decisions behind Browser's implementation - *why* it's
built the way it is, not the class-by-class mechanics of *how*.

## CefGlue for the CEF binding

CefGlue.Next.Avalonia provides an Avalonia control that renders a CEF browser and is the most
current binding with Avalonia support (Chromium 156); CefNet.Avalonia is stuck at Chromium 105 and
the older CefGlue.Avalonia at Chromium 120. The trade-off it accepts is that the binding is built
against Avalonia 11.3, so the app stays on that line (see below) instead of Avalonia 12.

## Avalonia stays on 11.3

Avalonia 12 moved types the binding references (`GotFocusEventArgs`), so the control fails with a
`TypeLoadException` the first time it is created. Pinning Avalonia 11.3 costs the Avalonia 12
features and is revisited when the binding is rebuilt against 12.

## Off-screen rendering

CEF can either embed a native window for the page or render frames off-screen for Avalonia to draw.
The native window is the binding's default, and it is the wrong fit here: on X11 it sits in the
window as a child that receives keystrokes before Avalonia whenever the pointer is over the page, so
shortcuts and typing into the address box intermittently go to the page, and it brings Chromium's own
UI along (a native context menu that never closes, accelerators like Ctrl+T opening native Chrome
windows). Off-screen mode makes Avalonia the only owner of input and of everything drawn on screen,
at the cost of copying each finished frame from memory into the Avalonia control.

CefGlue's off-screen loader also turns the GPU off for Chromium (`--disable-gpu`,
`--disable-gpu-compositing`), which would mean no hardware compositing, video decoding, WebGL or
WebGPU. `CefStarter` therefore initializes CEF itself with `BrowserApp`, which keeps the other
switches the loader adds, apart from the scrolling one, and leaves the GPU on; the control only skips loading CEF when it is already
loaded, and picks off-screen mode from an internal flag that the starter sets by reflection. The GPU
process uses Chromium's own choice of graphics backend: forcing ANGLE's Vulkan backend booted on a headless X
server but, on the NVIDIA machine, lost the Vulkan device and failed to import video frames, which crashed the
GPU process over and over (frozen page, then a repaint, and hover preview videos that did not play), so Vulkan
is only an opt-in for headless testing. Where the GPU is unusable, Chromium drops back to software rendering by itself. Frames
still reach Avalonia through CPU memory, so the GPU accelerates the page, not the final blit. Frames are
requested at 60 per second and Chromium's smooth scrolling is left on, which is what makes wheel
scrolling animate instead of jumping a notch at a time; the loader's `--disable-smooth-scrolling` is
not kept.
The CEF build from NuGet is Chromium's default build, without the proprietary codecs, so H.264, AAC and
HEVC video does not play: sites that offer only those, or pair a playable video codec with AAC audio (Steam's
trailers are AV1 with AAC), show an unsupported-format message, because a page needs both tracks. Decoding
cannot be added at run time or in the page, since H.264 and AAC are compiled in or absent, so
`Scripts/Codecs.cs` swaps in a build of the same Chromium version made with `proprietary_codecs=true
ffmpeg_branding=Chrome`. No official CEF distribution has one, so it is a community build, fetched on
demand, pinned by hash and applied to local installs only; any other CEF version needs a matching build, so
the pin moves with the CEF package. HEVC stays unavailable: Chromium only offers it through hardware
decoders.

## Pages are always dark

Dark mode is forced for every page by CEF switches rather than injected per site: `force-dark-mode`
makes pages report `prefers-color-scheme: dark`, so sites with a dark theme use it, and the
`WebContentsForceDark` feature darkens pages that have none. The two do not fight, because Chromium
leaves pages that already adapt to the preference alone. The browser background color is set dark
too, so there is no white flash before a page paints.

## The context menu is drawn by Avalonia

CEF's menu is described by a model, not drawn by the app. Reading that model into plain entries and
showing them in an Avalonia menu keeps CEF's own commands (back, forward, text editing) while the menu
matches the rest of the window and, importantly, always closes: the callback CEF waits on is completed
with a command on a choice and cancelled when the menu closes any other way. Commands that would open
a native window (view source, DevTools aside) are routed to tabs instead, and entries CEF cannot serve
off-screen (print) are removed.

## Shortcuts have one map and two paths

Which keys do what lives in one `ShortcutMap`. Keys reach the app through Avalonia when the UI has
focus and through CEF's keyboard handler when the page has focus (CEF sees them first and would
otherwise run its own accelerators), so both paths resolve through the same map rather than each
keeping a list.

## Sign-ins persist, so there is one profile and one running copy

Pages' cookies and storage live in a profile folder under the user's local application data, so signing
in to a site survives restarts. Chromium batches its cookie writes and this process leaves without
giving it a chance to finish them, so the cookie store is flushed on a timer and once more as the window
closes, with shutdown held until that flush completes (and a short timeout in case it never does).

A profile on disk cannot be used by two processes, and CEF's own answer to a second process on one is to
open a native Chrome window in the first. The app therefore allows one running copy per user: a later
launch sends its whole command line to the running copy over a named pipe and exits before CEF starts, and
the running copy opens a new window for it and comes forward.

## The address bar searches by default

One box does two jobs, and the common one, searching, should need no prefix, so only text that opts in
with a leading colon is treated as an address. This avoids guessing from the text (is `localhost`, or
`my notes`, or `v1.2` an address?), which is what makes a combined box surprising. A search goes to a
new tab so the page being read is not lost, and bookmark suggestions bring the quickest route to a
saved page into the same box. The address of the current page is shown behind the colon so that the
box stays consistent: whatever it shows, submitting it does what it looks like. Everything that is
known to be an address (launch arguments, including handed-over ones) skips the box and
goes through the plain address path.

## Page tooltips have one owner

A page's tooltips arrive from CEF as text. CefGlue's own handling sets that text on the control as an
Avalonia tooltip and leaves it there, so Avalonia's tooltip service later shows the stale text on its
own whenever the pointer enters the control, in whatever place it happens to be. The app therefore
takes tooltips over: `BrowserTooltips` hands every text to `PageBrowser`, which shows it only while the
pointer is over a visible page, puts the text on the control only while the tooltip is open, and closes
it (clearing the text) on an empty text from CEF, the pointer leaving or moving away from where it
appeared, a press, the wheel, a key, and the tab being hidden. Only non-empty texts are held back around
a context menu, since the page gets no mouse events while one is open and would otherwise show a
tooltip afterwards over nothing; the empty ones that clear a tooltip must still arrive.

## The pointer press must not look like leaving the page

Pressing a mouse button makes CefGlue's control capture the pointer, and Avalonia reports that as the
pointer exiting the control. CefGlue forwards it to Chromium as the mouse leaving the page, so a page
that reacts to a mouse leave loses the element under the click between press and release: YouTube's
hover preview closes, the release lands on the thumbnail beneath, and the click goes to a common
ancestor instead of the link. `PageBrowser` swallows an exit that happens while it holds the capture.

## Find is the one thing drawn over the page

Everything else lives in the overview, but finding has to be used while reading the page, so Ctrl+F
shows a small box floating over the page's corner. It exists only while a search is going on, and
leaves when the overview opens or the tab changes, so the window is still only the page the rest of
the time. CEF does the searching and highlighting inside the page; the box just drives it and shows the
counts CEF reports.

## Several windows share one process

A new window gets its own tab collection, so tabs are separate, and shares the bookmark list and the
profile with the rest. `App` creates windows, one for each launch and one for each press of the new window
button, and releases a window's pages when it closes while others remain. Launch options are per window
rather than per process, which is what lets a later launch ask for an isolated or close-only window without
changing the ones already open. The
app ends when the last window closes; the cookie flush hold applies to that.

## Bookmarks live in the overview and in a plain file

Bookmarks are a flat list in the overview's sidebar, beside the tab cards, because the overview is
where the user already goes to decide what to look at; no other UI is added for them. They are saved
as JSON in the profile folder after every change, written to a temporary file and moved into place so
a crash cannot leave half a file, and a file that cannot be parsed is treated as empty rather than
blocking start. Saving is best effort and off the UI thread: a full or read-only disk leaves the
bookmarks working in memory instead of failing the click. Loading merges with anything added before it
finished, so an early click is never lost or duplicated. Pages are matched by address ignoring case,
which is what makes the star a toggle rather than a way to bookmark one page twice.

## One executable, two roles

CEF runs its renderer and GPU work in helper processes. Rather than shipping a second helper
executable, the app launches itself with CEF's `--type=...` arguments and `HelperProcess`, the
first call in `Main`, takes over before any Avalonia code runs. This keeps the release to a single
managed executable and avoids a helper that has to be found and versioned alongside it.

## One executable, with the CEF runtime packed inside

A published build is a single self-contained file. The managed app and Avalonia's native libraries are bundled
by .NET, but `libcef` and Chromium's resource files (a few hundred MB) cannot be loaded from inside that bundle,
so publishing zips them (with the package's `Resources/` folder flattened, since CEF only finds `icudtl.dat` next
to `libcef`) and embeds the zip as a resource, named by its hash. On start, `CefRuntimeFolder` unpacks it once
into `Markwardt.Browser.Runtime/<hash>` under the local application data folder, writing to a temporary folder and
renaming it so that an interrupted or concurrent unpack never leaves a half-filled runtime, and deleting runtimes of
other versions. A first launch takes a couple of seconds longer; later ones find the folder.

The unpacked folder has to be found by both kinds of process. Chromium starts each renderer, GPU and utility
process from the same executable, so each one does the same check before CEF runs. libcef is then loaded from the
folder: an unmanaged-library resolver covers the import, and the process's working directory is set to the folder
because CefGlue otherwise goes looking for libcef through the directories above the executable, which on a
user's machine can mean scanning their home folder. CEF's resource and locale paths point at the folder too.

A plain build (development and tests) does not pack anything; it copies the runtime next to the executable, and
`CefRuntimeFolder` uses it from there. Publishing for a runtime with extra codecs passes its folder in, replacing
the packed files. CEF still creates an empty `extensions` folder beside the executable when it can, which is
harmless and optional: the app runs from a read-only folder.

## The top bar, the overview and isolation

A browser's own chrome competes with the page for space, so the top bar can be hidden outside the overview,
which then appears on demand in the manner of a desktop "activities" overview, with the tabs as cards and the
bookmarks beside them. The bar is a separate element from the overview so that it can show without it: it
overlays the page, and the page is inset by its height only while it stays visible, so opening the overview
over a hidden bar does not resize the page. The choice is a setting shared by every window and saved, with the
bar on by default. The overview is an Avalonia overlay on top of the page, which only works because pages
render off-screen.

For embedding the browser as a single-purpose window, `--isolate` removes the overview altogether and fixes
the bar on with only the window controls, ignoring and never saving the setting. Without an overview there is
no way to switch tabs, so requests for a new tab load in the current one. Minimize and maximize can be
hidden separately with `--close-only`, and a window's starting state and page come from `--window` and
`--goto`.

The window is frameless and draws its own controls, with invisible edge strips for resizing, because a native
frame would be permanent chrome. The palette is that of the Auto Remote client (the same dark editor colors),
so the apps read as one family.

## Tab thumbnails are captured, not live

Tabs that are not showing are hidden and do not render, so their cards cannot mirror them live. Each
tab instead keeps a bitmap of how it last looked, captured when it stops being the active tab and when
the overview opens (for the active one). The thumbnail is therefore as fresh as the last time the user
looked at that tab, which is what a card is for.
