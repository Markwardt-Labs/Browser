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
