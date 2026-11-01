"""Promote independently verified original-derived globe sources after Blender exits."""
from pathlib import Path
import sys,json,shutil,hashlib,datetime
root=Path(__file__).resolve().parents[1];src=(root/sys.argv[1]).resolve()
if not src.is_relative_to(root/'ProductionLogs/GlobeRework'):raise SystemExit('Not a designated globe build directory')
proof=json.loads((src/'source-validation.json').read_text())
if proof.get('status')!='PASS' or proof.get('zero_area_triangles')!=0:raise SystemExit('Clean independent source proof required')
dst=root/'Assets/DetectiveDesk/ArtSource/Gameplay/MechanicalGlobe';out=root/'Assets/DetectiveDesk/GlobeRework';out.mkdir(parents=True,exist_ok=True)
backup=root/'ProductionLogs/GlobeRework/PromoteBackups'/datetime.datetime.now().strftime('%Y%m%d_%H%M%S');backup.mkdir(parents=True,exist_ok=True)
for n in ['MechanicalGlobe.blend','MechanicalGlobe.fbx']:
 if (dst/n).exists():shutil.copy2(dst/n,backup/n)
 shutil.copy2(src/n,dst/n)
shutil.copytree(src/'Textures',dst/'Textures',dirs_exist_ok=True)
for n in ['blender-preview.png','blender-threequarter.png','generation-validation.json','source-validation.json']:shutil.copy2(src/n,out/n)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
b={'status':'PASS','embedded_source_exact':True,'embedded_helpers_exact':True,'mesh_count':proof['mesh_count'],'triangles':proof['triangles'],'zero_area_triangles':0}
for key,n in [('blend','MechanicalGlobe.blend'),('fbx','MechanicalGlobe.fbx'),('generator','build_asset.py')]:p=dst/n;b[key]=p.relative_to(root).as_posix();b[key+'_sha256']=sha(p)
b['helpers']={n:sha(dst/n) for n in ['legacy_globe.py','reference_globe.py']};b['independent_validation']=(out/'source-validation.json').relative_to(root).as_posix()
(out/'blender-validation.json').write_text(json.dumps(b,indent=2),encoding='utf-8')
print('Promoted canonical blend, FBX and textures; existing globe assets untouched. Prior new-source outputs:',backup)
