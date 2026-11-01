$ErrorActionPreference = 'Stop'
$projectPath = 'C:\Users\decra\source\Antigravity\SpinTheGlobe\SpinTheGlobe'
$editorPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe'
$lockPath = Join-Path $projectPath 'Temp\UnityLockfile'
$statePath = Join-Path $projectPath 'ProductionLogs\Gameplay\editor-relaunch-state.json'
@{ state = 'Waiting for authorized current project to close'; project = $projectPath; executable = $editorPath } | ConvertTo-Json | Set-Content -LiteralPath $statePath
$deadline = (Get-Date).AddSeconds(60)
while ((Test-Path -LiteralPath $lockPath) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 1 }
if (Test-Path -LiteralPath $lockPath) {
    @{ state = 'Stopped: current project lock still exists; no additional editor started'; project = $projectPath } | ConvertTo-Json | Set-Content -LiteralPath $statePath
    exit 1
}
$startedEditor = Start-Process -FilePath $editorPath -ArgumentList @('-projectPath', ('"' + $projectPath + '"')) -WindowStyle Hidden -PassThru
@{ state = 'Started same project with same Unity version'; project = $projectPath; executable = $editorPath; pid = $startedEditor.Id } | ConvertTo-Json | Set-Content -LiteralPath $statePath
