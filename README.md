# GlassTerminal

A WPF terminal emulator with a Windows Vista / Windows 7 Aero glass-inspired interface.

## Goals

- Windows 7 SP1 and later
- WPF + C# on .NET Framework 4.8
- A floating, glass-first window rather than a conventional opaque terminal frame
- A real interactive shell session, not a text-output mockup
- CMD and Windows PowerShell support

## Current status

The initial application shell is in place:

- WPF solution and .NET Framework 4.8 project
- Custom frameless window with DWM glass extension
- Custom caption controls and draggable title area

**The terminal session is not wired up yet.** The next implementation step is the WinPTY-backed process session and terminal input/output pipeline. ConPTY is intentionally not used because it is unavailable on Windows 7.

## Build

Open `GlassTerminal.sln` in Visual Studio 2022 with the .NET desktop development workload and the .NET Framework 4.8 Developer Pack installed.

The application currently has no bundled WinPTY binaries, so this initial shell build does not launch a shell yet.

## Compatibility notes

The project targets .NET Framework 4.8 rather than modern .NET so that Windows 7 SP1 remains a supported target. DWM glass is enabled when desktop composition is available; the application should still be able to start when composition is unavailable.
