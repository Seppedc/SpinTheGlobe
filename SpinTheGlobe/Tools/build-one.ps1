param(
 [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z][A-Za-z0-9_-]*$')][string]$Slug,
 [string]$Blender="",
 [string]$OutputDirectory="",
 [switch]$Rebuild
)
$ErrorActionPreference='Stop'
$Root=Split-Path -Parent $PSScriptRoot
if(-not(Test-Path (Join-Path $Root 'ProjectSettings/ProjectVersion.txt'))){throw 'Run the installed script from a Unity project.'}
if($Slug -in @('PinCoffer','CollectiblePin')){throw "Protected completed asset '$Slug': read its PREFER_EXISTING.md and use the detailed generator, not the generic starter."}
if(-not $Blender){$config=Join-Path $PSScriptRoot 'blender-path.json';if(Test-Path -LiteralPath $config){$Blender=(Get-Content -LiteralPath $config -Raw|ConvertFrom-Json).BlenderExe};if(-not $Blender){$Blender='blender'}}
if(-not(Get-Command $Blender -ErrorAction SilentlyContinue)){throw "Blender executable not found: $Blender"}
$assetGenerators=@(Get-ChildItem (Join-Path $Root "Assets/DetectiveDesk/ArtSource") -Recurse -Filter build_asset.py | Where-Object { (Split-Path (Split-Path $_.FullName -Parent) -Leaf) -eq $Slug -and $_.FullName -notlike '*\_Validation\*' })
if ($assetGenerators.Count -ne 1) { throw "Expected one production starter for '$Slug'; found $($assetGenerators.Count)." }
$assetOutput=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{$assetGenerators[0].DirectoryName}
if((Test-Path (Join-Path $assetOutput ($Slug+'.blend'))) -or (Test-Path (Join-Path $assetOutput ($Slug+'.fbx')))){
 if(-not $Rebuild){throw 'Output exists. Inspect it before an explicitly requested -Rebuild, or use a new output directory for tests.'}
 $assetBackup=Join-Path $Root ('ProductionLogs/AssetBuildBackups/'+$Slug+'_'+[guid]::NewGuid().ToString('N'))
 New-Item -ItemType Directory -Path $assetBackup -Force | Out-Null
 Get-ChildItem -LiteralPath $assetOutput -File | Copy-Item -Destination $assetBackup
 Write-Output "Existing outputs backed up to $assetBackup"
}
$previousOutput=$env:DETECTIVEDESK_ASSET_OUTPUT
try {
 $env:DETECTIVEDESK_ASSET_OUTPUT=$assetOutput
 & $Blender --background --factory-startup --python-exit-code 1 --python $assetGenerators[0].FullName
 if ($LASTEXITCODE -ne 0) { throw "Blender build failed ($LASTEXITCODE)" }
 foreach($extension in @('.blend','.fbx')){$out=Join-Path $assetOutput ($Slug+$extension);if(-not(Test-Path -LiteralPath $out) -or (Get-Item -LiteralPath $out).Length -lt 1024){throw "Missing or empty output: $out"}}
 Write-Output "Built and checked outputs for $Slug in $assetOutput"
} finally { $env:DETECTIVEDESK_ASSET_OUTPUT=$previousOutput }
