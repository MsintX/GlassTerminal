# Terminal frontend

The terminal UI uses local browser bundles of xterm.js 5.5.0 and
@xterm/addon-fit 0.10.0.

## Build preparation

No Node.js or npm installation is required.

When the project is built in Visual Studio or MSBuild, the project automatically
downloads the three pinned browser files into `Terminal/vendor` if they are not
already present:

- `xterm.js`
- `xterm.css`
- `addon-fit.js`

The first build therefore requires an internet connection and access to
jsDelivr. Later builds reuse the downloaded files. The application itself loads
only local files and does not contact a CDN at runtime.

To refresh the cached files, delete the three files from `Terminal/vendor` and
build the project again.

## Compatibility and security

CefSharp 109.1.110 is used to retain the Windows 7 compatibility target. Its
Chromium engine is obsolete and the NuGet package has published security
advisories. The embedded browser is intended only for this local terminal page;
do not use it as a general-purpose web browser or load untrusted remote pages.

The xterm.js browser bundles are distributed under the MIT License.
