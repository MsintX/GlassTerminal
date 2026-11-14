# GlassTerminal

A Windows terminal application with a Windows Vista / Windows 7 Aero glass-inspired interface.

## Goals

- Windows 7 SP1 and later
- WPF + C# on .NET Framework 4.8
- Keep the native Windows title bar
- Extend classic DWM Aero glass through the WPF client area on Windows 7
- Use the entire client area below the tab strip for the terminal
- Support real interactive console sessions

## Current prototype

- Starts a real `cmd.exe` process
- Displays the welcome banner on every new session
- Starts in `C:\Windows\System32`
- Uses the custom `GT` prompt followed by the current working directory
- Keeps the native Windows title bar and fills the client area with a single terminal session
- Uses Microsoft YaHei for terminal text and Segoe UI for interface text
- Restores full-client-area DWM glass on Windows 7 when desktop composition is enabled

The shell transport is still based on redirected standard input/output, and its TextBox input adapter is only a temporary prototype. It is **not a complete terminal emulator**. Interactive console applications, VT cursor movement, alternate screen buffers, terminal resize events, and reliable control-key handling require a PTY transport and a VT terminal engine.

## Terminal architecture investigation

See [TERMINAL-RESEARCH.md](TERMINAL-RESEARCH.md) for the compatibility review and the proposed PTY / terminal-rendering architecture.

## Build

Open `GlassTerminal.sln` in Visual Studio 2022 with the .NET desktop development workload and the .NET Framework 4.8 Developer Pack installed.

All project and source files are kept in the repository root. No third-party UI framework is used.

## Compatibility notes

Classic DWM Aero glass is available on Windows Vista / Windows 7 when desktop composition is enabled. This prototype enables the full client-area glass frame on Windows 7 and falls back to an opaque dark surface when classic glass is unavailable.
