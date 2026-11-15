# Terminal frontend

The terminal UI is powered by local copies of xterm.js and its FitAddon.

## Prepare dependencies

1. In Visual Studio, restore the NuGet packages for the project.
2. From the repository root, run:

```powershell
npm install
npm run prepare-terminal
```

The script places the browser bundles in `Terminal/vendor`. The application loads
only these local files; it does not fetch terminal scripts from a CDN at runtime.

## Compatibility and security

CefSharp 109.1.110 is used to retain the Windows 7 compatibility target. Its
Chromium engine is obsolete and the NuGet package has published security
advisories. The embedded browser is intended only for this local terminal page;
do not use it as a general-purpose web browser or load untrusted remote pages.
