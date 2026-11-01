param(
 [ValidateSet('Initialize','DryRun','Run','Resume','Pause','Stop','Status','Test')][string]$Action='Status',
 [ValidateRange(0,42)][int]$MaxAssetsPerRun=0,
 [ValidateRange(1,42)][int]$PlanCount=10,
 [string]$TestSlug='WallMolding'
)
$ErrorActionPreference='Stop'
$queueProjectRoot=Split-Path -Parent $PSScriptRoot
if(-not(Test-Path (Join-Path $queueProjectRoot 'ProjectSettings/ProjectVersion.txt'))){throw 'Runner must live in this Unity project Tools directory.'}
$queueArguments=@((Join-Path $PSScriptRoot 'AssetQueue/queue.py'),$Action.ToLowerInvariant(),'--max-assets',[string]$MaxAssetsPerRun,'--plan-count',[string]$PlanCount,'--test-slug',$TestSlug)
& python @queueArguments
if($LASTEXITCODE -ne 0){throw "Asset queue exited with code $LASTEXITCODE. Read ProductionLogs/AssetQueue/queue.log and asset_queue.json."}
