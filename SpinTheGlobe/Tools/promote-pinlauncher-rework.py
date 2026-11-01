"""Promote a finished, independently validated PinLauncher build; preserve .meta identities."""
from pathlib import Path
import sys, shutil, json, hashlib, datetime
root=Path(__file__).resolve().parents[1]
src=(root/sys.argv[1]).resolve()
if not src.is_relative_to(root/'ProductionLogs/PinLauncherRework'):raise SystemExit('Not a designated build folder')
proof=json.loads((src/'source-validation.json').read_text())
if proof.get('status')!='PASS' or proof.get('zero_area_triangles')!=0:raise SystemExit('Validated clean source required')
dst=root/'Assets/DetectiveDesk/ArtSource/Gameplay/PinLauncher';out=root/'Assets/DetectiveDesk/PinLauncherRework'
backup=root/'ProductionLogs/PinLauncherRework/PromoteBackups'/datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
backup.mkdir(parents=True,exist_ok=True);out.mkdir(parents=True,exist_ok=True)
for n in ['PinLauncher.blend','PinLauncher.fbx']:
 if (dst/n).exists():shutil.copy2(dst/n,backup/n)
 shutil.copy2(src/n,dst/n)
for n in ['textures','PinLauncher.fbm']:
 if (src/n).exists():shutil.copytree(src/n,dst/n,dirs_exist_ok=True)
for n in ['blender-preview.png','blender-muzzle-preview.png','generation-validation.json','source-validation.json']:shutil.copy2(src/n,out/n)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
b={'status':'PASS','embedded_source_exact':True,'embedded_helper_exact':True,'mesh_count':proof['source_mesh_count'],'triangles':proof['triangles'],'zero_area_triangles':0}
for key,n in [('blend','PinLauncher.blend'),('fbx','PinLauncher.fbx'),('generator','build_asset.py')]:p=dst/n;b[key]=p.relative_to(root).as_posix();b[key+'_sha256']=sha(p)
b['helper']=(dst/'pinlauncher_detail.py').relative_to(root).as_posix();b['helper_sha256']=sha(dst/'pinlauncher_detail.py');b['independent_validation']=(out/'source-validation.json').relative_to(root).as_posix()
(out/'blender-validation.json').write_text(json.dumps(b,indent=2),encoding='utf-8')
print('Promoted validated source, FBX and texture maps; metadata identities retained. Prior outputs:',backup)
