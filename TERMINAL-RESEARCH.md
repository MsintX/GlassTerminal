# Terminal and Aero implementation research

Research date: 2026-11-14

This document records compatibility findings for the current target: Windows 7 SP1, WPF, C#, and .NET Framework 4.8.

## Findings

### PTY transport

#### winpty.NET 0.4.2

- NuGet: https://www.nuget.org/packages/winpty.NET/0.4.2
- Source: https://github.com/IntelOrca/winpty.NET
- Targets .NET Framework 4.0 and is compatible with .NET Framework 4.8.
- The package contains a managed wrapper and native WinPTY payloads.
- Its packaging script extracts x86 and x64 `winpty.dll`, `winpty-agent.exe`, and `winpty-debugserver.exe`.
- The wrapper exposes PTY creation, process spawning, input/output pipe names, and resize APIs.
- Last published in 2017. The package is a credible compatibility candidate, but its native binaries and lifecycle behavior still need to be tested on clean Windows 7 SP1 x86/x64 systems.

#### Quick.PtyNet

- Source: https://github.com/aaasoft/Quick.PtyNet
- NuGet: https://www.nuget.org/packages/Quick.PtyNet/
- The project is a .NET wrapper abstraction around platform PTYs.
- The WinPty adapter is available separately: https://www.nuget.org/packages/Quick.PtyNet.WinPty/
- It is worth evaluating as a newer API surface, but package target compatibility alone does not prove the bundled WinPTY agent works on Windows 7.

### Terminal emulation and rendering

#### XTerm.NET

- NuGet: https://www.nuget.org/packages/XTerm.NET/
- Provides a headless VT/ANSI terminal engine, screen buffers, alternate buffer, scrollback, cursor state, keyboard/mouse sequence generation, and resize events.
- Version 1.0.12 targets .NET 6.0, not .NET Framework 4.8. The newer releases do not solve the current framework constraint.
- Its documented API and buffer model are useful references, but it cannot currently be installed directly into this project.

#### VirtualTerminal

- NuGet: https://www.nuget.org/packages/VirtualTerminal/
- Provides a UI-agnostic VT engine and WPF TerminalControl.
- The current package and its command-line session are built around newer .NET and ConPTY-oriented code.
- It is not a drop-in Windows 7 / .NET Framework 4.8 solution.

#### EasyWindowsTerminalControl and WPF-UI.Terminal

- EasyWindowsTerminalControl: https://www.nuget.org/packages/EasyWindowsTerminalControl/
- WPF-UI.Terminal: https://www.nuget.org/packages/WPF-UI.Terminal/
- Both provide a much more complete WPF terminal experience than a TextBox adapter.
- Their current backend depends on Microsoft ConPTY.
- ConPTY requires Windows 10 version 1809 or later, so these packages cannot satisfy the Windows 7 minimum despite NuGet listing .NET Framework 4.8 compatibility.

### Possible alternative frontend: xterm.js hosted in CefSharp

- CefSharp.Wpf 109.1.110 targets .NET Framework 4.5.2 and is compatible with .NET Framework 4.8.
- Chromium 109 is an old, security-unpatched browser engine and must not be treated as a harmless dependency.
- A local-only xterm.js frontend with a carefully restricted bridge to a WinPTY backend is technically plausible, but the old embedded Chromium security and maintenance costs make it a separate experiment, not the default choice.
- NuGet: https://www.nuget.org/packages/CefSharp.Wpf/109.1.110

## Aero glass

### Native DWM API

- Microsoft documentation: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/extend-glass-frame-into-a-wpf-application
- `DwmExtendFrameIntoClientArea` is the native API for extending the classic glass frame into a WPF client area.
- Use negative one for all four margins to extend glass through the entire client area.
- Keep `WindowStyle="SingleBorderWindow"` so Windows continues to draw the native title bar and caption buttons.
- Use transparent WPF surfaces over the extended frame.
- Classic Aero glass is a Vista / Windows 7 composition feature; newer Windows versions should use a deliberate fallback rather than claiming to display classic Aero.

### FluentWpfChromes

- NuGet: https://www.nuget.org/packages/FluentWpfChromes/
- Provides AeroGlassChrome, AcrylicChrome, and TransparentChrome.
- Its documented setup requires `WindowStyle="None"`, which conflicts with this project's requirement to retain the native title bar.
- It is therefore not selected for the current window architecture.

## Proposed architecture

1. PTY transport: WinPTY, behind an application-owned `ITerminalSession` abstraction.
2. Terminal engine: a VT/ANSI parser and screen buffer independent of the transport.
3. Terminal view: a WPF control that renders cells, attributes, cursor, selection, and scrollback, and converts keyboard/mouse input to terminal sequences.
4. Window: native WPF title bar, DWM glass extension on Windows 7, tab strip, and one terminal view filling the remaining client area.

Do not wire a ConPTY-only package into the Windows 7 build. Do not treat redirected standard input/output as a terminal implementation.

## Verification still required

- Restore and build the solution in Visual Studio.
- Test DWM glass on Windows 7 SP1 with Aero composition enabled and disabled.
- Test the WinPTY package and native agent on Windows 7 SP1 x86 and x64.
- Test terminal input, output, resize, Ctrl+C, Ctrl+Break, ANSI sequences, alternate screen, Unicode, and process shutdown.
