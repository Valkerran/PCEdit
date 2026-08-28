#!/usr/bin/env pwsh
# Build the self-contained win-x64 build of the PCEdit desktop head and zip it.
#
# Usage:
#   deploy/build-windows.ps1                 # version from Directory.Build.props
#   deploy/build-windows.ps1 -Version 1.2.0  # explicit version
#
# Output: artifacts/PCEdit-<version>-win-x64.zip
#
# Requirements: .NET 10 SDK. Runs on Windows, or on Linux/macOS with the SDK
# installed (cross-publish; the produced zip is still a Windows build).

[CmdletBinding()]
param([string]$Version)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot  = Split-Path -Parent $ScriptDir
$Artifacts = Join-Path $RepoRoot 'artifacts'

if (-not $Version) {
    $props = Get-Content (Join-Path $RepoRoot 'Directory.Build.props') -Raw
    if ($props -match '<VersionPrefix>([^<]+)</VersionPrefix>') {
        $Version = $Matches[1].Trim()
    } else {
        throw 'Could not read <VersionPrefix> from Directory.Build.props'
    }
}
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version '$Version' is not X.Y.Z" }

$publish = Join-Path $ScriptDir 'OUT/publish-win-x64'
if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }

Write-Host ">> dotnet publish win-x64 (self-contained) $Version"
dotnet publish (Join-Path $RepoRoot 'PCEdit.Desktop/PCEdit.Desktop.csproj') `
    -c Release -r win-x64 --self-contained true `
    -p:Version=$Version -p:DebugType=None -p:DebugSymbols=false -p:PublishTrimmed=false `
    -o $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

New-Item -ItemType Directory -Force -Path $Artifacts | Out-Null
$zip = Join-Path $Artifacts "PCEdit-$Version-win-x64.zip"
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $zip
Write-Host ">> $zip"
