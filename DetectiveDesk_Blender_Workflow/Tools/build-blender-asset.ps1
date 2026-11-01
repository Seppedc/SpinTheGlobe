param(
    [Parameter(Mandatory=$true)][string]$AssetName,
    [string]$BlenderExe = "blender"
)
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$assetDir = Join-Path $projectRoot ("Assets/DetectiveDesk/ArtSource/" + $AssetName)
$scriptName = "build_" + ($AssetName.ToLowerInvariant()) + ".py"
$source = Join-Path $assetDir $scriptName
$outfile = Join-Path $assetDir ($AssetName + ".blend")
if (-not (Test-Path $source)) { throw "Generator not found: $source" }
$blenderCmd = Get-Command $BlenderExe -ErrorAction SilentlyContinue
if (-not $blenderCmd) { throw "Blender not found. Provide -BlenderExe 'C:\Program Files\Blender Foundation\Blender 4.5\blender.exe' (update to actual path)." }
& $BlenderExe --background --factory-startup --python $source -- --output $outfile
if ($LASTEXITCODE -ne 0) { throw "Blender failed with exit code $LASTEXITCODE" }
if (-not (Test-Path $outfile)) { throw "Blender returned success but output is absent: $outfile" }
$size = (Get-Item $outfile).Length
if ($size -lt 1024) { throw "Unexpectedly small .blend output ($size bytes)" }
Write-Host "Created/updated $outfile ($size bytes)"
