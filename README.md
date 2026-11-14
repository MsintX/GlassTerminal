# GlassTerminal

A Windows terminal application with a Windows Vista / Windows 7 Aero glass-inspired interface.

## Goals

- Windows 7 SP1 and later
- WPF + C# on .NET Framework 4.8
- A floating glass-first window rather than a conventional opaque terminal frame
- CMD and Windows PowerShell support
- A real interactive shell session

## First runnable version

- Starts a real `cmd.exe` process when the application opens
- Displays the GlassTerminal welcome banner on every new session
- Starts in `C:\Windows\System32`
- Uses the custom `GT` prompt, followed by the current working directory
- Sends entered commands to CMD and streams standard output and error back into the terminal view
- Uses the native Windows title bar and a compact tab strip
- Uses the entire area below the tab strip as one terminal surface
- Accepts commands inline at the end of the current shell prompt, like traditional CMD
- Uses Microsoft YaHei with Segoe UI fallback for terminal text

The current shell transport uses redirected standard input/output. It is suitable for ordinary CMD commands and command-line tools that work with redirected streams. Full-screen console applications and programs that require a real console/PTY are not supported yet; a WinPTY-backed transport is a later step.

## Build

Open `GlassTerminal.sln` in Visual Studio 2022 with the .NET desktop development workload and the .NET Framework 4.8 Developer Pack installed.

All project and source files are kept in the repository root. No third-party UI framework or additional runtime is required for this first version.

## Compatibility notes

The project targets .NET Framework 4.8 to retain Windows 7 SP1 compatibility. DWM glass is enabled when desktop composition is available; the application should still start when composition is unavailable.
