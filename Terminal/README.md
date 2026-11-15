# Terminal frontend

The terminal UI is powered by local copies of xterm.js and its FitAddon.

From the repository root, run:

```powershell
npm install
npm run prepare-terminal
```

This places the browser bundles in `Terminal/vendor`. The application loads only
these local files; it does not fetch terminal scripts from a CDN at runtime.

The CefSharp 109 runtime is retained for the project's Windows 7 compatibility
goal. Chromium 109 is obsolete and has known security advisories. Do not use this
embedded browser as a general-purpose web browser or load untrusted remote pages.
