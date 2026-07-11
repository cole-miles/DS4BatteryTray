Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:SystemRoot 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path $root 'artifacts\tests'
$output = Join-Path $outputDirectory 'DS4BatteryTray.Tests.exe'

if (-not (Test-Path -LiteralPath $csc)) {
    throw "C# compiler not found at $csc"
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$sources = @(
    (Get-ChildItem -LiteralPath (Join-Path $root 'src\Core') -Filter '*.cs' -Recurse | ForEach-Object { $_.FullName })
    (Get-ChildItem -LiteralPath (Join-Path $root 'tests') -Filter '*.cs' -Recurse | ForEach-Object { $_.FullName })
)

& $csc /nologo /target:exe /platform:anycpu /optimize+ /out:$output $sources
if ($LASTEXITCODE -ne 0) {
    throw "Test compiler failed with exit code $LASTEXITCODE"
}

& $output
if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE"
}
