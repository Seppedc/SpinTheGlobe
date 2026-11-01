"""Stage a requested holder revision without changing live Unity assets."""
from pathlib import Path
import shutil,json,hashlib
root=Path(__file__).resolve().parents[1]
log=root/'ProductionLogs/GlobeHolderRevision';checkpoint=log/'Checkpoint';stage=log/'Source'
if stage.exists():raise SystemExit('Existing staged revision: inspect instead of overwriting')
source=root/'Assets/DetectiveDesk/ArtSource/Gameplay/MechanicalGlobe'
checkpoint.mkdir(parents=True,exist_ok=True);stage.mkdir(parents=True)
for n in ['build_asset.py','reference_globe.py','legacy_globe.py','MechanicalGlobe.blend','MechanicalGlobe.fbx','README.md']:
 shutil.copy2(source/n,checkpoint/n)
 if n.endswith('.py'):shutil.copy2(source/n,stage/n)
shutil.copy2(root/'Assets/DetectiveDesk/Generated/DetectiveOffice.unity',checkpoint/'DetectiveOffice.unity')
for folder in ['Prefabs','Materials','NativeMeshes']:shutil.copytree(root/'Assets/DetectiveDesk/GlobeRework'/folder,checkpoint/folder)
for n in ['asset_queue.json','asset_registry.json','Asset_Tracker.xlsx','assets.csv']:shutil.copy2(root/n,checkpoint/n)
p=stage/'reference_globe.py';s=p.read_text(encoding='utf-8')
a=s.index(' inner=.738;outer=.786;depth=.014;n=192;vs=[];uv=[]')
b=s.index(' # Top and bottom pole bearings',a)
s=s[:a]+''' # Player-facing left semicircle only: top pole -> left -> bottom pole.
 inner=.738;outer=.786;depth=.014;n=96;count=n+1;vs=[];uv=[]
 for z in [-depth,depth]:
  for r in [inner,outer]:
   for i in range(count):
    a=math.pi/2+math.pi*i/n;vs.append((r*math.cos(a),r*math.sin(a),z));uv.append((i/n*2,(r-inner)/(outer-inner)))
 fs=[]
 for i in range(n):
  j=i+1;fs.extend([(i,count+i,count+j,j),(2*count+i,2*count+j,3*count+j,3*count+i),(i,j,2*count+j,2*count+i),(count+i,3*count+i,3*count+j,count+j)])
 fs.extend([(0,2*count,3*count,count),(n,count+n,3*count+n,2*count+n)])
 g.mesh('Meridian_BroadGraduatedBrassBand',vs,fs,m,g.MATERIALS['AntiqueBrass'],uv=uv,smooth=False)
 for name,r in [('Outer',outer),('Inner',inner)]:
  points=[(r*math.cos(math.pi/2+math.pi*i/n),r*math.sin(math.pi/2+math.pi*i/n),-.014) for i in range(count)]
  tube('Meridian_PolishedEdge_'+name,m,points,.0032,g.MATERIALS['PolishedBrass'])
 vs=[];fs=[]
 for i in range(36,109):
  a=TAU*i/144;major=i%12==0;medium=i%4==0;r0=.741;r1=.760 if major else (.754 if medium else .749);w=.001 if major else .00055
  for r,t in [(r0,-w),(r1,-w),(r1,w),(r0,w)]:vs.append((r*math.cos(a)-t*math.sin(a),r*math.sin(a)+t*math.cos(a),-.0143))
  k=len(vs)-4;fs.append((k,k+3,k+2,k+1))
 g.mesh('Meridian_EngravedGraduations',vs,fs,m,g.MATERIALS['EngravingInlay'],smooth=False)
 for i in range(3,10):
  a=TAU*i/12;r=.771;text('Meridian_DegreeLabel_%03d'%(i*30),m,str(i*30),(r*math.cos(a),r*math.sin(a),-.0145),.012,g.MATERIALS['EngravingInlay'],i*30-90)
''' +s[b:]
a=s.index(' # Retain the existing dial position, but connect it to the new plinth.')
b=s.index('\ndef coastlines(',a)
s=s[:a]+''' # Compact support tucks the existing-sized RPM instrument against the base.
 # Gauge center is (.22,.32,-.47) from the pedestal origin, matching the user's
 # blue-circle placement; original sphere, pedestal and root remain unchanged.
 points=[];border=[]
 for i in range(65):
  t=i/64;a=Vector((.15,.29,-.12));b=Vector((.26,.24,-.28));c=Vector((.22,.27,-.43));v=(1-t)**2*a+2*(1-t)*t*b+t*t*c;points.append(tuple(v));border.append((v.x,v.y+.011,v.z))
 tube('Pedestal_GaugeSupportArch',p,points,.010,g.MATERIALS['AntiqueBrass'])
 tube('Pedestal_GaugeSupportPolishedEdge',p,border,.0022,g.MATERIALS['PolishedBrass'])
 g.lathe('Pedestal_GaugeMountingShoe',p,[(.025,0),(.033,.004),(.033,.012),(.021,.025)],g.MATERIALS['AntiqueBrass'],position=(.22,.255,-.43),segments=48)

''' +s[b:]
needle=' make_meridian(root);make_pedestal();coastlines(bpy.data.objects[\'Sphere_RotationPivot\'],land);clean_geometry()'
assert needle in s
s=s.replace(needle,""" # Remove exposed gear meshes only; retain semantic empty pivots for later
 # Unity binding preservation. No sphere geometry or materials are changed.
 remove_mesh_children(bpy.data.objects['Clockwork_Assembly'])
 gauge=bpy.data.objects['RPMGauge_Assembly'];gauge.location=g.u((-1.23,1.18,-.80))
 make_meridian(root);make_pedestal();coastlines(bpy.data.objects['Sphere_RotationPivot'],land);clean_geometry()""")
s=s.replace("'gear_specs':[", "'holder_span_degrees':180,'exposed_gear_meshes':0,'gauge_pivot_unity':[-1.23,1.18,-.80],'unity_integration':'PENDING - Unity MCP bridge usage limit; staged only','gear_specs':[")
p.write_text(s,encoding='utf-8')
manifest={'status':'Staged source; Blender not yet run','source':stage.relative_to(root).as_posix(),'checkpoint':checkpoint.relative_to(root).as_posix(),'unity_applied':False,'reason':'Unity MCP bridge CLI usage limit','requested_changes':['Remove right meridian half only','Remove exposed gear visuals','Move existing-sized RPM dial down/in toward base'],'preserve':['World ball geometry/UV/materials','Camera','Root/sphere/pedestal placement','Existing gameplay identities'],'source_sha256':{n:hashlib.sha256((checkpoint/n).read_bytes()).hexdigest() for n in ['reference_globe.py','legacy_globe.py','MechanicalGlobe.blend','MechanicalGlobe.fbx']}}
(log/'handoff.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8');print('Staged original-derived holder revision without modifying live globe',stage)
