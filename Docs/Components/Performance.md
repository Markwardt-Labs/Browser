# Performance

What was measured, on a virtual display with a 1080p H.264 video and a CSS animation filling the window
(60 frames a second reaching the app, about 2,000 CPU ticks per 10 seconds across all processes), so that
what helps and what does not is known rather than assumed. The virtual display has no usable GPU path for
Avalonia, so numbers for the part of the browser process that draws the window are not representative of a
real desktop; the Chromium side and startup are.

## Adopted

**Precompiled startup.** Publishing compiles the app ahead of time (ReadyToRun), which cuts the time from
launch to the window appearing from about 1.6 s to about 0.9 s and the CPU used while starting by about a
fifth, for about 40 MB more on disk. Plain builds are not precompiled, so development builds stay fast.

## Measured and left alone

- **Chromium GPU modes.** GPU compositing, software compositing and no GPU at all differ by under 5% in
  total CPU for the same video, and all deliver the same 60 frames a second. Frames are read back to memory for
  the app in every mode, which is the real cost.
- **.NET garbage collector settings** (non-concurrent, retained memory): no difference.
- **Coalescing pointer events** to one every 8 ms for a gaming mouse (500 events a second): about 6% less CPU
  in the browser process and no change in input-to-frame latency (about 15 ms either way, against about 10 ms
  at an ordinary event rate), because most of the added latency is Avalonia's own input handling. Not worth an
  override of CefGlue's input path.
- **Trimming Chromium's background services** (component updates, domain reliability, pings): an idle page
  already costs about 0.2% of a core.
- **Hardware video decoding** through VA-API on the Intel integrated GPU, with the whole GPU stack moved onto
  it: no reduction in decoding CPU, and not verifiable without a real display. The NVIDIA card has no VA-API
  driver, and decoding on one GPU while compositing on another is not supported.
- **Ad blocking's cost:** about 30 ms more to load a page of 18,000 elements and about 13% more for a full
  restyle of such a page, because the hiding rules for every site are one large stylesheet. Typical pages are
  a tenth of that size.
- **Replacing the frame copy** (each paint is copied twice in full, however small the change). The copy
  happens inside CefGlue's non-overridable render path and takes about 5% of the UI thread for full-window
  video, so the only way to remove it is a different render path.

## What would make a real difference

Frames travel from Chromium's GPU process to memory and back into a texture for every paint: at 4K that is
tens of megabytes per frame. Chromium can instead hand the compositor's texture to the host without a copy
(shared-texture painting); CefGlue's Avalonia control does not use it, and Avalonia 11.3 has no Linux import
for such textures, so this means a new rendering control rather than a setting.

When Avalonia cannot create an OpenGL context, it draws the window in software and registers every frame
buffer with the garbage collector, which forces roughly eleven full collections a second. That was seen on the
virtual display; whether it happens on a given desktop depends on its OpenGL support.

## The stats log

`--stats` records what is needed to tell these cases apart on a real desktop. Frames are counted by listening
to CefGlue's paint event through reflection (a failure shows as `painthook=FAILED` in the log), and the time
the UI thread takes to pick up a frame is measured by posting a marker to it, one at a time. The UI thread's
own lateness comes from a 20 ms timer. CPU use is read from the process list for the browser and each kind of
Chromium helper, so GPU, renderer and browser costs stay separate. The header says whether Avalonia is drawing
in software or on the GPU, which decides how to read everything else. Nothing is collected when the switch is
off, and only host names of pages are logged.

Slow video that comes and goes needs to be placed on one side of the chain: Chromium producing frames, the app
receiving them, or the window showing them. `video` and `dropped` come from a script run in every page that
reads the largest playing video's own playback counters and reports through the console, so they are what
Chromium produced; `paints` is what reached the app; `painttick` is how long Avalonia's render loop then took
to run, which is the window's own cadence. `active`, `pointer` and `screen` are recorded so that lag can be
correlated with focus and with where the pointer is, which cannot be known afterwards. The pointer is read
straight from the X server, so it is only known on X11. Findings so far: on the virtual display neither the
pointer's place nor another window having focus changed frame delivery at all (60 frames a second, no extra
delay), the page does not even learn that the window lost focus (Avalonia keeps its own focus inside the
window), and in a four-day real log the slow stretches were not explained by CPU use, UI stalls or GC.

## The Vulkan backend

The GPU was first enabled by forcing ANGLE's Vulkan backend, because that was the only way the GPU process
booted on the virtual display used for testing. On the real NVIDIA machine it proved unstable: Chromium's log
showed `VK_ERROR_DEVICE_LOST` from Skia and failed DMA-BUF imports of video frames, then the GPU process
restarting three times in seven seconds, during which the page delivered no frames and hover previews did not
play. Chromium's default backend is used now. What cannot be known from the virtual display is whether the
default one gets the GPU on the real session, which is why `--stats` records GPU process restarts and the
backend that was asked for.

## Swap sync and several windows

Found on the real machine with two maximized windows, one per monitor, a video playing in one: the video lagged
when the pointer was in the other window and was fine over the video. Sampling the running browser's threads (a
.NET sampling trace attached to the live process, `dotnet-trace collect --profile dotnet-sampled-thread-time`)
showed Avalonia's render thread, which serves every window, spending 92 to 95% of its time inside the OpenGL
buffer swap of `GlxGlPlatformSurface`, and completing only 27 to 40 swaps a second for both windows together,
each swap blocking one to two screen refreshes (90th percentile 33 to 63 ms) with about a millisecond between
swaps. Avalonia never sets a swap interval, so the driver's default of waiting for the refresh applies, and
because the waits happen one after another on one thread, the windows split the frames: any redraw in the
other window (hover, scrolling, a caret, an animation) takes frames from the video. Chromium was producing
frames normally, and the app received them at the same rate whichever screen the pointer was on, which is why
the frame counts of the stats log did not show it.

The fix is to turn the swap wait off before the first window exists: `__GL_SYNC_TO_VBLANK=0` for the NVIDIA
driver and `vblank_mode=0` for Mesa, set through the C library's `setenv` because .NET's own environment on
Unix never reaches native code (checked: Chromium's helper processes inherit them). On a composited desktop
this cannot tear, since the compositor presents each frame itself; a variable the user has already set, or
`MARKWARDT_VSYNC=1`, keeps the driver's behavior. This could not be reproduced on the virtual display, which
draws Avalonia in software, so the proof is the same trace taken again on the real machine: swaps per second
and swap duration should rise and fall respectively, and `painttick` in the stats log should drop.

## Video that goes choppy when the player hides its controls

Reported on the real machine, on YouTube and other sites alike: video is fluid while the pointer is over it and
the player's controls are showing, and goes choppy after a few still seconds when controls and cursor hide. The
old log shows two different regimes of frames reaching the app: with the controls showing about 84 frames a
second averaging 0.64 Mpx changed (about 28 full-window picture updates a second plus small ones), and with them
hidden 60 frames a second averaging 0.07 Mpx, which is the equivalent of a few picture updates a second.
It could not be reproduced on the virtual display, where a video with native controls delivered 60 frames a
second of 1.9 Mpx with the controls shown or hidden, so the difference lies in the GPU-composited path of the
real machine. Ruled out with data: pointer position on its own, window focus, CPU frequency (steady 4.7 GHz),
CPU starvation, UI stalls, GC, and Chromium's capture oracle refusing frames (two warnings in the whole log).
Seen but not proven: the GPU swinging between P5 (memory 810 MHz) and P3 (memory 5000 MHz) at 22 to 35%
utilization, a known source of low-load stutter. The log now records `big` (full-size picture updates a
second), `idle` (seconds the pointer has been still), the page's own `video` frame rate and `dropped` frames, so
the next capture can say whether Chromium stopped producing video frames when the controls hid, or produced
them and they did not arrive.
