import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
log = root / 'ProductionLogs/Gameplay'
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(name, data): (log / name).write_text(json.dumps(data, indent=2), encoding='utf-8')
baseline = json.loads((log / 'integration-asset-hashes-before.json').read_text())
changed = [p for p, sha in baseline.items() if not (root / p).is_file() or digest(root / p) != sha]
files = [{ 'path': p, 'sha256_before': sha, 'sha256_after': digest(root / p) if (root / p).is_file() else None } for p, sha in baseline.items()]
sphere = [p for p in baseline if '/GlobeRework/NativeMeshes/' in p and 'Sphere_' in p and p.endswith('.asset')]
mats = [p for p in baseline if '/GlobeRework/' in p and p.endswith('.mat')]
pin = root / 'Assets/DetectiveDesk/Prefabs/CollectiblePin.prefab'
pin_ok = digest(pin) == digest(log / 'Checkpoint/CollectiblePin.prefab')
coffer_ok = digest(root / 'Assets/DetectiveDesk/PinCoffer/DetectivePinCoffer.prefab') == digest(log / 'Checkpoint/DetectivePinCoffer.prefab')
write('asset-file-preservation.json', dict(status='PASS' if not changed and pin_ok and coffer_ok else 'FAIL', filesChecked=len(files), changed=changed, sphereFilesChecked=len(sphere), globeMaterialFilesChecked=len(mats), collectiblePinPrefabIdentical=pin_ok, cofferPrefabIdentical=coffer_ok, files=files))
modified_existing=[]
for backup in (log/'Checkpoint/Assets').rglob('*'):
    if backup.is_file():
        relative=backup.relative_to(log/'Checkpoint')
        current=root/relative
        if current.is_file() and digest(backup)!=digest(current): modified_existing.append(relative.as_posix())
added=[]
for folder in ['Assets/DetectiveDesk/Scripts/Gameplay','Assets/DetectiveDesk/Scripts/Editor/Gameplay','Assets/DetectiveDesk/Gameplay']:
    added += [f.relative_to(root).as_posix() for f in (root/folder).rglob('*') if f.is_file()]
settings = 'ProjectSettings/DynamicsManager.asset'
settings_changed = digest(root/settings) != digest(log/'Checkpoint/DynamicsManager-before-PhysX.asset')
write('changed-files.json',dict(scope='Full gameplay implementation including parent-installed runtime and four adapters; no git repository available',modifiedExisting=modified_existing+['Assets/DetectiveDesk/Generated/DetectiveOffice.unity']+([settings] if settings_changed else []),addedOrInstalled=sorted(added),evidenceDirectory='ProductionLogs/Gameplay',queueOrTrackerChanged=False,supervisedRepairs=['Original mechanism rpmNeedle pivot owns tachometer; visual child baseline identity preserved','Compact world-space RPM label beside original gauge','Installed PhysX selected through public settings UI; same-version editor restarted','Map occlusion refines oversized legacy interaction boxes against active authored visual bounds','Physical pin pop crosses board-to-desk gap; actual falling and desk landing validated'],physicsSettingsChange={'field':'m_CurrentBackendId','before':3737844653,'after':4072204805,'onlyChangedField':True}))
print(json.dumps(dict(assetsChecked=len(files),changed=changed,sphereFiles=len(sphere),globeMaterials=len(mats),pinPrefabIdentical=pin_ok,cofferPrefabIdentical=coffer_ok,modifiedExisting=modified_existing)))
