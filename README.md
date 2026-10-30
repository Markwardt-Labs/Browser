# Browser

[![Release](https://img.shields.io/github/v/release/Markwardt-Labs/Browser?label=Release)](https://github.com/Markwardt-Labs/Browser/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Build](https://github.com/Markwardt-Labs/Browser/actions/workflows/build.yml/badge.svg)](https://github.com/Markwardt-Labs/Browser/actions/workflows/build.yml)
[![Coverage](.github/badges/badge_linecoverage.svg)](.github/badges/badge_linecoverage.svg)

Browser is a self-contained desktop web browser written in C#/.NET 10. The window and its controls
are [Avalonia](https://avaloniaui.net/); pages are rendered by the
[Chromium Embedded Framework](https://github.com/chromiumembedded/cef) through
[CefGlue](https://github.com/Deon-Berlin/CefGlue). It redistributes third-party components, whose
licenses are reproduced in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).

## Requirements

- Windows 10/11, 64-bit, or Linux, x86-64 (glibc-based distro with an X11 or XWayland session). No
  separate .NET runtime install needed - the executable is self-contained.

## Installing

Download the archive for your platform from the latest
[Release](https://github.com/Markwardt-Labs/Browser/releases/latest) and extract it to a folder of
its own - the executable needs the Chromium files extracted beside it:

- **Windows** - extract `Markwardt.Browser-win-x64.zip` and run `Markwardt.Browser.exe`.
- **Linux** - extract `Markwardt.Browser-linux-x64.zip`, mark `Markwardt.Browser` executable
  (`chmod +x Markwardt.Browser`), and run it.

## Documentation

| File | Covers |
| --- | --- |
| [Docs/Api.md](Docs/Api.md) | The application window and the flow of a navigation |
| [Docs/Usage.md](Docs/Usage.md) | Using the browser and running it from source |
| [Docs/Architecture.md](Docs/Architecture.md) | Why it is built the way it is |
| [Docs/Project.md](Docs/Project.md) | This repository's scripts, publishing and CI |
