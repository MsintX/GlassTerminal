$ErrorActionPreference = "Stop"

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDirectory
$vendorDirectory = Join-Path $projectRoot "Terminal\vendor"

$assets = @(
    @{
        Name = "xterm.js"
        Url = "https://cdn.jsdelivr.net/npm/@xterm/xterm@5.5.0/lib/xterm.js"
    },
    @{
        Name = "xterm.css"
        Url = "https://cdn.jsdelivr.net/npm/@xterm/xterm@5.5.0/css/xterm.css"
    },
    @{
        Name = "addon-fit.js"
        Url = "https://cdn.jsdelivr.net/npm/@xterm/addon-fit@0.10.0/lib/addon-fit.js"
    }
)

if (-not (Test-Path $vendorDirectory)) {
    New-Item -ItemType Directory -Path $vendorDirectory -Force | Out-Null
}

# Windows 7 may use an older PowerShell/.NET default protocol set.
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]3072
} catch {
    throw "Unable to enable TLS 1.2. Install .NET Framework 4.8 and Windows updates before building."
}

$client = New-Object System.Net.WebClient

try {
    foreach ($asset in $assets) {
        $destination = Join-Path $vendorDirectory $asset.Name

        if ((Test-Path $destination) -and ((Get-Item $destination).Length -gt 1024)) {
            Write-Host ("Using cached terminal asset: " + $asset.Name)
            continue
        }

        $temporary = $destination + ".download"
        Write-Host ("Downloading " + $asset.Name + " from the pinned xterm.js release...")

        try {
            $client.DownloadFile($asset.Url, $temporary)

            if (-not (Test-Path $temporary) -or (Get-Item $temporary).Length -le 1024) {
                throw "The downloaded file is missing or unexpectedly small."
            }

            Move-Item -Path $temporary -Destination $destination -Force
            Write-Host ("Prepared " + $asset.Name)
        } catch {
            if (Test-Path $temporary) {
                Remove-Item $temporary -Force
            }

            throw ("Failed to download " + $asset.Name + ": " + $_.Exception.Message)
        }
    }
} finally {
    $client.Dispose()
}

Write-Host "Terminal browser assets are ready."
