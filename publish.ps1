<#
.SYNOPSIS
    Produces PokerRanges.exe as a self-contained build.

.DESCRIPTION
    A single executable, with no .NET to install on the target machine. The tests run first:
    publishing a binary nobody has checked is asking to have to recall it.

.PARAMETER SkipTests
    Publishes without running the tests. For a quick round trip, not for a build you keep.

.PARAMETER ReadyToRun
    Precompiles to native code: noticeably snappier startup, bigger file. The binary becomes
    specific to win-x64, which it already is anyway.
#>
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$ReadyToRun
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$solution = Join-Path $root 'PokerRanges.slnx'
$project = Join-Path $root 'src\PokerRanges.App\PokerRanges.App.csproj'
$output = Join-Path $root 'publish\win-x64'

if (-not $SkipTests) {
    Write-Host 'Tests...' -ForegroundColor Cyan
    dotnet test $solution --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed: nothing has been published.' }
}

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

Write-Host 'Publishing...' -ForegroundColor Cyan

$arguments = @($project, '-p:PublishProfile=win-x64', '--nologo')
if ($ReadyToRun) { $arguments += '-p:PublishReadyToRun=true' }

dotnet publish @arguments
if ($LASTEXITCODE -ne 0) { throw 'Publishing failed.' }

$exe = Join-Path $output 'PokerRanges.exe'
if (-not (Test-Path $exe)) { throw "Publishing finished but $exe cannot be found." }

$size = [math]::Round((Get-Item $exe).Length / 1MB, 1)

Write-Host ''
Write-Host "PokerRanges.exe — $size MB" -ForegroundColor Green
Write-Host $exe
Write-Host ''
Write-Host 'Copy it as is: no .NET installation required on the target machine.'
Write-Host 'Settings and charts are created on first launch under %APPDATA%\PokerRanges.'
