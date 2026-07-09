Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:SystemRoot 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = Join-Path $root 'src\DS4BatteryTray.cs'
$output = Join-Path $root 'DS4BatteryTray.exe'
$icon = Join-Path $root 'assets\app.ico'

if (-not (Test-Path -LiteralPath $csc)) {
    throw "C# compiler not found at $csc"
}

function New-AppIcon {
    param([Parameter(Mandatory = $true)][string]$Path)

    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    Add-Type -AssemblyName System.Drawing
    Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class BuildNativeIconMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
'@

    $bitmap = New-Object System.Drawing.Bitmap 32, 32
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $outline = [System.Drawing.Color]::FromArgb(32, 32, 32)
    $fill = [System.Drawing.Color]::FromArgb(16, 124, 16)
    $outlinePen = New-Object System.Drawing.Pen $outline, 2
    $fillBrush = New-Object System.Drawing.SolidBrush $fill
    $outlineBrush = New-Object System.Drawing.SolidBrush $outline

    try {
        $graphics.FillRectangle($fillBrush, 7, 12, 16, 8)
        $graphics.DrawRectangle($outlinePen, 4, 9, 22, 14)
        $graphics.FillRectangle($outlineBrush, 26, 13, 3, 6)

        $hIcon = $bitmap.GetHicon()
        try {
            $iconObject = [System.Drawing.Icon]::FromHandle($hIcon)
            $stream = [System.IO.File]::Create($Path)
            try {
                $iconObject.Save($stream)
            }
            finally {
                $stream.Dispose()
                $iconObject.Dispose()
            }
        }
        finally {
            [BuildNativeIconMethods]::DestroyIcon($hIcon) | Out-Null
        }
    }
    finally {
        $outlinePen.Dispose()
        $fillBrush.Dispose()
        $outlineBrush.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

New-AppIcon -Path $icon

$references = @(
    '/r:System.dll',
    '/r:System.Core.dll',
    '/r:System.Drawing.dll',
    '/r:System.Management.dll',
    '/r:System.Runtime.dll',
    '/r:System.Windows.Forms.dll',
    '/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.WindowsRuntime.dll'
)

& $csc /nologo /target:winexe /platform:x64 /optimize+ /win32icon:$icon /out:$output $references $source

if ($LASTEXITCODE -ne 0) {
    throw "C# compiler failed with exit code $LASTEXITCODE"
}

Write-Output "Built $output"
