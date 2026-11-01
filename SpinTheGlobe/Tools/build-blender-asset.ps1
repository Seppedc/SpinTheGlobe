param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z][A-Za-z0-9_-]*$')][string]$AssetName,
    [string]$BlenderExe = "",
    [switch]$ExportFbx
)
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'))) { throw 'Tools must be located in a Unity project root.' }
if (-not $BlenderExe) {
    $configPath = Join-Path $PSScriptRoot 'blender-path.json'
    if (Test-Path -LiteralPath $configPath) { $BlenderExe = (Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json).BlenderExe }
    if (-not $BlenderExe) { $BlenderExe = 'blender' }
}
$assetDir = Join-Path $projectRoot ("Assets/DetectiveDesk/ArtSource/" + $AssetName)
$scriptName = "build_" + ($AssetName.ToLowerInvariant()) + ".py"
$source = Join-Path $assetDir $scriptName
$outfile = Join-Path $assetDir ($AssetName + ".blend")
if (-not (Test-Path $source)) { throw "Generator not found: $source" }
$blenderCmd = Get-Command $BlenderExe -ErrorAction SilentlyContinue
if (-not $blenderCmd) { throw "Blender not found. Provide -BlenderExe 'C:\Program Files\Blender Foundation\Blender 4.5\blender.exe' (update to actual path)." }
& $BlenderExe --background --factory-startup --python-exit-code 1 --python-expr "import bpy; bpy.ops.wm.read_factory_settings(use_empty=True)" --python $source -- --output $outfile
if ($LASTEXITCODE -ne 0) { throw "Blender failed with exit code $LASTEXITCODE" }
if (-not (Test-Path $outfile)) { throw "Blender returned success but output is absent: $outfile" }
$size = (Get-Item $outfile).Length
if ($size -lt 1024) { throw "Unexpectedly small .blend output ($size bytes)" }
Write-Host "Created/updated $outfile ($size bytes)"
if ($ExportFbx) {
    $exportScript = Join-Path $PSScriptRoot 'export-blender-fbx.py'
    $fbxOutput = Join-Path $assetDir ($AssetName + '.fbx')
    & $BlenderExe --background $outfile --python-exit-code 1 --python $exportScript -- --asset $AssetName --output $fbxOutput
    if ($LASTEXITCODE -ne 0) { throw "FBX export failed with exit code $LASTEXITCODE" }
    if (-not (Test-Path -LiteralPath $fbxOutput)) { throw 'FBX output is missing' }
}
