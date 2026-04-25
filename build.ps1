#Requires -Version 5.1
<#
.SYNOPSIS
    Build CheatTraits mod. Loads .env for machine-specific paths before invoking MSBuild.
.DESCRIPTION
    RimWorld DLL references are resolved via Directory.Build.props (relative paths).
    This script loads .env for path validation and consistency with other mods. Do NOT
    run 'dotnet build' directly if you want .env-driven validation.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = $PSScriptRoot
$EnvFile  = Join-Path $RepoRoot '.env'
$ProjFile = Join-Path $RepoRoot 'Source\CheatTraits\CheatTraits.csproj'

# ── Load .env ──────────────────────────────────────────────────────────────────
if (-not (Test-Path $EnvFile)) {
    Write-Error @"
.env file not found at: $EnvFile

Copy .env.example to .env and fill in your local paths:
  RIMWORLD_PATH=D:\Steam\steamapps\common\RimWorld
  RIMWORLD_DECOMP_PATH=F:\Development\rimworld-decomp
"@
}

Get-Content $EnvFile | Where-Object { $_ -match '^\s*[^#].*=.*' } | ForEach-Object {
    $parts = $_ -split '=', 2
    $key   = $parts[0].Trim()
    $value = $parts[1].Trim()
    Set-Item "Env:\$key" $value
    Write-Host "  $key = $value" -ForegroundColor DarkGray
}

# ── Validate required vars ─────────────────────────────────────────────────────
if (-not $env:RIMWORLD_PATH) {
    Write-Error 'RIMWORLD_PATH is not set in .env'
}
if (-not (Test-Path $env:RIMWORLD_PATH)) {
    Write-Error "RIMWORLD_PATH does not exist: $env:RIMWORLD_PATH"
}

$managedDir = Join-Path $env:RIMWORLD_PATH 'RimWorldWin64_Data\Managed'
if (-not (Test-Path (Join-Path $managedDir 'Assembly-CSharp.dll'))) {
    Write-Warning "Assembly-CSharp.dll not found in $managedDir — build may fail"
}

if ($env:RIMWORLD_DECOMP_PATH -and -not (Test-Path $env:RIMWORLD_DECOMP_PATH)) {
    Write-Warning "RIMWORLD_DECOMP_PATH does not exist (not required for build): $env:RIMWORLD_DECOMP_PATH"
}

# ── Build ──────────────────────────────────────────────────────────────────────
Write-Host "`nRestoring packages..." -ForegroundColor Cyan
dotnet restore $ProjFile
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet restore failed (exit $LASTEXITCODE)" }

Write-Host "`nBuilding Release..." -ForegroundColor Cyan
dotnet build $ProjFile -c Release --no-restore
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet build failed (exit $LASTEXITCODE)" }

# ── Verify output ──────────────────────────────────────────────────────────────
$assembliesDir = Join-Path $RepoRoot 'Assemblies'
Write-Host "`nAssemblies output:" -ForegroundColor Cyan
Get-ChildItem $assembliesDir -File | Sort-Object Name | ForEach-Object {
    $sizeKb = [math]::Round($_.Length / 1KB, 1)
    Write-Host ("  {0,-45} {1,8} KB" -f $_.Name, $sizeKb)
}

Write-Host "`nBuild succeeded." -ForegroundColor Green
