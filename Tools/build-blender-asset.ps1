<#
.SYNOPSIS
  Rebuild one asset's .blend from its generator, optionally export FBX and publish it into Unity.

.DESCRIPTION
  Source layout  : ArtSource/<Category>/<Asset>/   (.blend, build_*.py, Textures/, References/)
  Unity layout   : SpinTheGlobe/Assets/DetectiveDesk/Art/<Category>/<Asset>/{Models,Textures,...}

  Generator lookup order inside the asset folder: build_<asset>.py, build_asset.py, then a single build_*.py.
  The .blend is written next to its generator. -ExportFbx writes <Asset>.fbx next to it.
  -Publish copies the FBX into Unity's Models/ folder (and Textures/*.png into Unity's Textures/),
  overwriting files in place so existing .meta GUIDs and scene references are kept.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-blender-asset.ps1 -Asset PinCoffer -ExportFbx -Publish
#>
param(
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z][A-Za-z0-9_-]*$')][Alias('AssetName', 'Slug')][string]$Asset,
    [string]$BlenderExe = "",
    [switch]$ExportFbx,
    [switch]$Publish,
    [switch]$PublishTextures
)
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$artSource = Join-Path $repo 'ArtSource'
$unityArt = Join-Path $repo 'SpinTheGlobe/Assets/DetectiveDesk/Art'

# ---- Locate the asset source folder -------------------------------------------------
$folders = @(Get-ChildItem -LiteralPath $artSource -Directory -Recurse | Where-Object { $_.Name -eq $Asset -and $_.FullName -notlike '*\References*' })
if ($folders.Count -ne 1) { throw "Expected exactly one ArtSource/<Category>/$Asset folder, found $($folders.Count)." }
$assetDir = $folders[0].FullName
$category = Split-Path (Split-Path $assetDir -Parent) -Leaf

$candidates = @("build_$($Asset.ToLowerInvariant()).py", 'build_asset.py') | ForEach-Object { Join-Path $assetDir $_ } | Where-Object { Test-Path -LiteralPath $_ }
if (-not $candidates) { $candidates = @(Get-ChildItem -LiteralPath $assetDir -Filter 'build_*.py' | ForEach-Object FullName) }
if (@($candidates).Count -lt 1) { throw "No generator (build_*.py) found in $assetDir" }
$generator = @($candidates)[0]

# ---- Blender --------------------------------------------------------------------------
if (-not $BlenderExe) {
    $configPath = Join-Path $PSScriptRoot 'blender-path.json'
    if (Test-Path -LiteralPath $configPath) { $BlenderExe = (Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json).BlenderExe }
    if (-not $BlenderExe) { $BlenderExe = 'blender' }
}
if (-not (Get-Command $BlenderExe -ErrorAction SilentlyContinue)) { throw "Blender not found: '$BlenderExe'. Set the path in Tools/blender-path.json." }

$blend = Join-Path $assetDir "$Asset.blend"
$previous = $env:DETECTIVEDESK_ASSET_OUTPUT
try {
    # Starter generators read DETECTIVEDESK_ASSET_OUTPUT; detailed ones accept --output.
    $env:DETECTIVEDESK_ASSET_OUTPUT = $assetDir
    if ((Split-Path $generator -Leaf) -eq 'build_asset.py') {
        & $BlenderExe --background --factory-startup --python-exit-code 1 --python $generator
    } else {
        & $BlenderExe --background --factory-startup --python-exit-code 1 --python-expr "import bpy; bpy.ops.wm.read_factory_settings(use_empty=True)" --python $generator -- --output $blend
    }
    if ($LASTEXITCODE -ne 0) { throw "Blender failed with exit code $LASTEXITCODE" }
} finally { $env:DETECTIVEDESK_ASSET_OUTPUT = $previous }
if (-not (Test-Path -LiteralPath $blend) -or (Get-Item -LiteralPath $blend).Length -lt 1024) { throw "Missing or empty output: $blend" }
Write-Host "Built $blend"

# ---- FBX ------------------------------------------------------------------------------
$fbx = Join-Path $assetDir "$Asset.fbx"
if ($ExportFbx -or $Publish) {
    & $BlenderExe --background $blend --python-exit-code 1 --python (Join-Path $PSScriptRoot 'export-blender-fbx.py') -- --asset $Asset --output $fbx
    if ($LASTEXITCODE -ne 0) { throw "FBX export failed with exit code $LASTEXITCODE" }
    Write-Host "Exported $fbx"
}

# ---- Publish into Unity -----------------------------------------------------------------
if ($Publish) {
    $unityAsset = if ($category -eq 'Materials') { Join-Path $unityArt "Shared/Materials/$Asset" } else { Join-Path $unityArt "$category/$Asset" }
    $models = Join-Path $unityAsset 'Models'
    New-Item -ItemType Directory -Force -Path $models | Out-Null
    Copy-Item -LiteralPath $fbx -Destination (Join-Path $models "$Asset.fbx") -Force
    Write-Host "Published FBX to $models"
    if ($PublishTextures) {
        $srcTextures = Join-Path $assetDir 'Textures'
        if (Test-Path -LiteralPath $srcTextures) {
            $dstTextures = Join-Path $unityAsset 'Textures'
            New-Item -ItemType Directory -Force -Path $dstTextures | Out-Null
            Get-ChildItem -LiteralPath $srcTextures -Filter '*.png' | Copy-Item -Destination $dstTextures -Force
            Write-Host "Published textures to $dstTextures"
        }
    }
    Write-Host "Return to Unity to import. For the pin coffer run Tools > Detective Desk > Blender Assets > Refresh Pin Coffer From Blender."
}
