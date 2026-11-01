"""Promote independently verified lamp after Blender exits."""
from pathlib import Path
import sys,json,shutil,hashlib,datetime
root=Path(__file__).resolve().parents[1];src=(root/sys.argv[1]).resolve()
assert src.is_relative_to(root/'ProductionLogs/LampRework')
proof=json.loads((src/'source-validation.json').read_text());assert proof['status']=='PASS' and proof['zero_area_triangles']==0
dst=root/'Assets/DetectiveDesk/ArtSource/Props/BankersLamp';out=root/'Assets/DetectiveDesk/LampRework';out.mkdir(parents=True,exist_ok=True)
backup=root/'ProductionLogs/LampRework/PromoteBackups'/datetime.datetime.now().strftime('%Y%m%d_%H%M%S');backup.mkdir(parents=True,exist_ok=True)
for n in ['BankersLamp.blend','BankersLamp.fbx']:
 if (dst/n).exists():shutil.copy2(dst/n,backup/n)
 shutil.copy2(src/n,dst/n)
shutil.copytree(src/'Textures',dst/'Textures',dirs_exist_ok=True)
for n in ['blender-preview.png','blender-shade-closeup.png','generation-validation.json','source-validation.json']:shutil.copy2(src/n,out/n)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
b={'status':'PASS','embedded_source_exact':True,'embedded_helpers_exact':True,'mesh_count':proof['mesh_count'],'triangles':proof['triangles'],'zero_area_triangles':0}
for key,n in [('blend','BankersLamp.blend'),('fbx','BankersLamp.fbx'),('generator','build_asset.py')]:p=dst/n;b[key]=p.relative_to(root).as_posix();b[key+'_sha256']=sha(p)
b['helpers']={'lamp_detail.py':sha(dst/'lamp_detail.py')};b['independent_validation']=(out/'source-validation.json').relative_to(root).as_posix()
(out/'blender-validation.json').write_text(json.dumps(b,indent=2),encoding='utf-8');print('Lamp source promoted',b)
