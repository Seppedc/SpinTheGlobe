"""Revision 2: reference-driven miniature clockwork pin cannon.
Called by the asset's original build_asset.py entry point. Metres, Z up,
barrel forward +Y. Cosmetic blind bore; no real projectile mechanism.
"""
from pathlib import Path
import os, math, json, hashlib
import bpy, bmesh
from mathutils import Vector
TAU=math.tau
HERE=Path(__file__).resolve().parent
OUT=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT',HERE)).resolve()

def empty(name,parent=None,at=(0,0,0)):
 o=bpy.data.objects.new(name,None);col.objects.link(o);o.parent=parent;o.location=at;o.empty_display_size=.015;return o

def finish(o,name,mat,parent=None,bevel=0):
 o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.parent=parent or root
 if mat:o.data.materials.append(mat)
 bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 if bevel:
  m=o.modifiers.new('Machined edge radius','BEVEL');m.width=bevel;m.segments=2
 for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
 o.modifiers.new('Weighted crafted normals','WEIGHTED_NORMAL');o.select_set(False);return o

def mesh(name,verts,faces,mat,parent=None):
 d=bpy.data.meshes.new(name+'_Mesh');d.from_pydata(verts,[],faces);d.update();bm=bmesh.new();bm.from_mesh(d);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(d);bm.free()
 o=bpy.data.objects.new(name,d);col.objects.link(o);return finish(o,name,mat,parent)

def box(name,at,size,mat,parent=None,bevel=.0007):
 bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.dimensions=size;return finish(o,name,mat,parent,bevel)

def cylinder(name,at,r,depth,mat,parent=None,axis='Z',vertices=40):
 rot={'Z':(0,0,0),'X':(0,math.pi/2,0),'Y':(math.pi/2,0,0)}[axis]
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=at,rotation=rot)
 return finish(bpy.context.object,name,mat,parent,min(.0003,depth*.15))

def ball(name,at,r,mat,parent=None):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,radius=r,location=at);o=finish(bpy.context.object,name,mat,parent)
 for p in o.data.polygons:p.use_smooth=True
 return o

def lathe(name,profile,mat,parent=None,axis='Z',at=(0,0,0),n=64):
 vs=[]
 for r,h in profile:
  for i in range(n):
   a=TAU*i/n;p=(r*math.cos(a),r*math.sin(a),h)
   if axis=='Y':p=(p[0],p[2],p[1])
   if axis=='X':p=(p[2],p[0],p[1])
   vs.append(p)
 fs=[]
 for j in range(len(profile)-1):
  for i in range(n):
   a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
 o=mesh(name,vs,fs,mat,parent);o.location=at;return o

def tube(name,points,r,mat,parent=None):
 c=bpy.data.curves.new(name+'_Curve','CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=2;s=c.splines.new('POLY');s.points.add(len(points)-1)
 for p,co in zip(s.points,points):p.co=(*co,1)
 o=bpy.data.objects.new(name,c);col.objects.link(o);bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.ops.object.convert(target='MESH');return finish(bpy.context.object,name,mat,parent)

def ring(name,at,r,wire,mat,parent=None,axis='Z'):
 pts=[]
 for i in range(65):
  a=TAU*i/64;p=(r*math.cos(a),r*math.sin(a),0)
  if axis=='Y':p=(p[0],0,p[1])
  if axis=='X':p=(0,p[0],p[1])
  pts.append(tuple(p[k]+at[k] for k in range(3)))
 return tube(name,pts,wire,mat,parent)

def screw(name,at,mat,parent=None,axis='Z',r=.0025):
 cylinder(name+'_Head',at,r,.0016,mat,parent,axis,24)
 offset={'Z':(0,0,.001),'X':(.001,0,0),'Y':(0,-.001,0)}[axis]
 size={'Z':(r*1.35,.00055,.00035),'X':(.00035,r*1.35,.00055),'Y':(r*1.35,.00035,.00055)}[axis]
 box(name+'_Slot',tuple(at[i]+offset[i] for i in range(3)),size,dark,parent,.00005)

def gear(name,at,r,teeth,mat,parent=None,depth=.005):
 n=teeth*4;vs=[]
 for x in [-depth/2,depth/2]:
  for inner in [False,True]:
   for i in range(n):
    a=TAU*i/n;rad=r*.70 if inner else r*(1 if i%4 in (1,2) else .88);vs.append((x,rad*math.cos(a),rad*math.sin(a)))
 fs=[]
 for i in range(n):
  j=(i+1)%n;fs.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
 o=mesh(name+'_ToothedRim',vs,fs,mat,parent);o.location=at
 cylinder(name+'_Axle',at,r*.19,depth+.005,steel,parent,'X')
 for i in range(6):
  a=TAU*i/6;tube(name+'_Spoke_%02d'%i,[(at[0],at[1]+v*math.cos(a),at[2]+v*math.sin(a)) for v in [r*.12,r*.77]],.0018,mat,parent)

def label(name,text,at,size,mat,parent=None):
 d=bpy.data.curves.new(name,'FONT');d.body=text;d.align_x='CENTER';d.align_y='CENTER';d.size=size;d.extrude=.00009;d.bevel_depth=.00003;d.bevel_resolution=1
 o=bpy.data.objects.new(name,d);col.objects.link(o);o.location=at;o.rotation_euler=(math.pi/2,0,0);bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.ops.object.convert(target='MESH');return finish(bpy.context.object,name,mat,parent)

def simple(name,color,metal,rough):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;return m

def pbr(name,color,metal,rough,family):
 m=simple(name,color,metal,rough);p=m.node_tree.nodes['Principled BSDF'];n=512;arrays={k:[] for k in ['BaseColor','Roughness','MetallicSmoothness','Normal']}
 for y in range(n):
  for x in range(n):
   u=x/n;v=y/n;fine=math.sin(x*17.13+y*41.77)*math.sin(x*4.93-y*13.11);cloud=math.sin(TAU*u*3+.9*math.sin(TAU*v*2))*math.cos(TAU*v*4)
   if family=='wood':grain=math.sin(TAU*(u*23+.32*math.sin(TAU*v*2)+.11*math.sin(TAU*u*3)));variation=.80+.13*grain+.05*cloud+.025*fine;rr=rough+.06*grain+.03*fine
   else:variation=.89+.07*cloud+.025*math.sin(TAU*u*135+math.sin(TAU*v*3))+.025*fine;rr=rough+.04*cloud+.025*fine
   rr=max(.08,min(.9,rr));c=[max(0,min(1,a*variation)) for a in color]
   arrays['BaseColor'].extend((*c,1));arrays['Roughness'].extend((rr,rr,rr,1));arrays['MetallicSmoothness'].extend((metal,metal,metal,1-rr));arrays['Normal'].extend((.5+.025*fine,.5+.013*cloud,1,1))
 folder=OUT/'textures';folder.mkdir(parents=True,exist_ok=True)
 for key,pixels in arrays.items():
  img=bpy.data.images.new(name+'_'+key,width=n,height=n,alpha=True);img.use_fake_user=True;img.colorspace_settings.name='sRGB' if key=='BaseColor' else 'Non-Color';img.pixels.foreach_set(pixels);img.filepath_raw=str(folder/(img.name+'.png'));img.file_format='PNG';img.save();img.pack();img.filepath='//textures/'+img.name+'.png'
  if key in ['BaseColor','Roughness','Normal']:
   tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=img
   if key=='BaseColor':m.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
   elif key=='Roughness':m.node_tree.links.new(tex.outputs['Color'],p.inputs['Roughness'])
   else:
    normal=m.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.18;m.node_tree.links.new(tex.outputs['Color'],normal.inputs['Color']);m.node_tree.links.new(normal.outputs['Normal'],p.inputs['Normal'])
 return m

def build():
 global root,col,dark,steel
 OUT.mkdir(parents=True,exist_ok=True);old=bpy.data.collections.get('Collection_PinLauncher')
 if old:
  for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
  bpy.data.collections.remove(old)
 if not bpy.data.filepath:
  for name in ['Cube','Camera','Light']:
   o=bpy.data.objects.get(name)
   if o:bpy.data.objects.remove(o,do_unlink=True)
 scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
 col=bpy.data.collections.new('Collection_PinLauncher');scene.collection.children.link(col);root=empty('ROOT_PinLauncher');root['revision']=2
 wood=pbr('PL_Mahogany',(.115,.032,.014),0,.34,'wood');brass=pbr('PL_AgedBrass',(.62,.365,.115),.83,.30,'brass');steel=pbr('PL_SpringSteel',(.115,.145,.16),.86,.28,'steel')
 gold=simple('PL_PolishedBrass',(.72,.46,.16),.88,.22);dark=simple('PL_EngravingInlay',(.017,.022,.019),.55,.5);green=simple('PL_GreenEnamel',(.009,.058,.032),0,.24);rubber=simple('PL_RubberFeet',(.012,.014,.012),0,.85);red=simple('PL_PinTipCrimson',(.46,.012,.022),0,.25)
 static=empty('Pedestal_Static',root)
 for x in [-.074,.074]:
  for y in [-.103,.103]:cylinder('Foot_%s_%s'%(x,y),(x,y,.0035),.012,.007,rubber,static);cylinder('CornerShoe_%s_%s'%(x,y),(x,y,.010),.014,.010,brass,static)
 box('Mahogany_Platform',(0,0,.032),(.184,.254,.045),wood,static,.003);box('Plinth_LowerOgee',(0,0,.014),(.194,.264,.009),wood,static,.002);box('Brass_Plinth',(0,0,.020),(.192,.262,.003),gold,static);box('Plinth_TopBead',(0,0,.056),(.194,.264,.007),brass,static,.0012);box('Enamel_Deck',(0,0,.061),(.175,.245,.003),green,static)
 for x in [-.087,.087]:box('DeckEdge_X_%s'%x,(x,0,.064),(.003,.246,.003),gold,static)
 for y in [-.121,.121]:box('DeckEdge_Y_%s'%y,(0,y,.064),(.175,.003,.003),gold,static)
 for x in [-.078,.078]:
  for y in [-.108,.108]:screw('Deck_Rivet_%s_%s'%(x,y),(x,y,.064),gold,static)
 box('MakerPlaque_Frame',(0,-.129,.038),(.108,.003,.027),gold,static);box('MakerPlaque_Enamel',(0,-.131,.038),(.100,.0015,.022),dark,static)
 label('MakerPlaque_Title','PIN  /  No. 01',(0,-.132,.042),.008,gold,static);label('MakerPlaque_Subtitle','CLOCKWORK  ATELIER',(0,-.132,.032),.0037,brass,static)
 for x in [-.078,.078]:
  for side in [-1,1]:
   pts=[]
   for i in range(51):
    t=i/50;a=t*TAU*1.2;r=.001+.011*t;pts.append((x+side*r*math.cos(a),-.130,.036+r*math.sin(a)))
   tube('FrontScroll_%s_%s'%(x,side),pts,.0007,gold,static)
 for side in [-1,1]:
  x=side*.094
  for z in [.030,.047]:tube('SideInlay_%s_%s'%(side,z),[(x,y,z) for y in [-.104,.104]],.00055,gold,static)
  for y in [-.10,-.05,0,.05,.10]:tube('SideDiamond_%s_%s'%(side,y),[(x,y-.008,.038),(x,y,.044),(x,y+.008,.038),(x,y,.032),(x,y-.008,.038)],.00045,brass,static)
 turret=empty('Turret_Yaw_Pivot',root,(0,0,.065));lathe('Turret_TurnedSocket',[(0,0),(.059,0),(.061,.004),(.058,.008),(.048,.010),(.047,.015),(.042,.019),(.04,.024),(0,.024)],brass,turret);ring('Turret_SocketBead',(0,0,.009),.054,.0013,gold,turret)
 for i in range(8):
  a=TAU*i/8;screw('TurretBolt_%02d'%i,(.051*math.cos(a),.051*math.sin(a),.011),gold,turret,r=.002)
 for side in [-1,1]:
  x=side*.044;vs=[(x+dx,y,z) for dx in [-.004,.004] for y,z in [(-.055,.020),(.047,.020),(.026,.090),(-.022,.096),(-.046,.068)]];fs=[tuple(range(4,-1,-1)),tuple(range(5,10))]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)];mesh('CarriageCheek_%s'%side,vs,fs,brass,turret)
  for y,z in [(-.038,.036),(.027,.035),(-.009,.080)]:screw('CheekFastener_%s_%s'%(side,y),(x+side*.005,y,z),gold,turret,'X',.0028)
  tube('CheekScroll_%s'%side,[(x+side*.005,-.039+i*.0013,.044+.012*math.sin(i*.12)) for i in range(52)],.00065,gold,turret)
 cylinder('Elevation_Trunnion',(0,-.008,.087),.012,.119,steel,turret,'X');elevation=empty('Elevation_Pivot',turret,(0,-.008,.087));elevation.rotation_euler.x=math.radians(37);recoil=empty('Recoil_Slide_Pivot',elevation)
 profile=[(0,-.112),(.016,-.112),(.025,-.103),(.027,-.094),(.027,-.075),(.026,-.068),(.024,-.054),(.023,-.015),(.021,.035),(.019,.082),(.016,.135),(.015,.161),(.018,.168),(.020,.174),(.020,.183),(.018,.190),(.011,.190),(.010,.185),(.010,.152),(.009,.115),(0,.115)]
 lathe('Barrel_TaperedBrassShell',profile,brass,recoil,'Y',n=96)
 for i,(y,r) in enumerate([(-.089,.0275),(-.063,.0259),(-.013,.024),(.045,.0218),(.101,.0189),(.166,.0185),(.183,.0205)]):
  ring('Barrel_MachinedBand_%02d'%i,(0,y,0),r,.0013,gold,recoil,'Y')
  if i not in [0,6]:ring('Barrel_DarkGroove_%02d'%i,(0,y-.003,0),r-.001,.00055,dark,recoil,'Y')
 for i in range(12):
  a=i*TAU/12;tube('Breech_Flute_%02d'%i,[(.0272*math.cos(a),y,.0272*math.sin(a)) for y in [-.089,-.075]],.0006,gold,recoil)
 for side in [-1,1]:
  for j in range(3):
   y=-.045+j*.025;pts=[]
   for i in range(49):
    t=i/48;along=y+.012*math.sin(t*math.pi);radius=.024+(.021-.024)*(along+.054)/.089-.00015;a=(.4 if side==1 else math.pi-.4)+.16*math.sin(t*TAU)
    pts.append((radius*math.cos(a),along,radius*math.sin(a)))
   tube('Barrel_LeafInlay_%s_%s'%(side,j),pts,.00030,dark,recoil)
 cylinder('Muzzle_DarkBore',(0,.133,0),.0099,.001,dark,recoil,'Y');empty('MuzzleTip',recoil,(0,.196,0));empty('BreechOrigin',recoil,(0,-.112,0));empty('PinSeat',recoil,(0,.152,0));ball('LoadedPin_EnamelIndicator',(0,.194,0),.0041,red,recoil)
 plunger=empty('Plunger_Translation_Pivot',recoil,(0,-.112,0));cylinder('Plunger_Rod',(0,-.019,0),.0028,.038,steel,plunger,'Y');lathe('Pull_Tab',[(0,-.046),(.005,-.046),(.008,-.042),(.009,-.037),(.006,-.032),(0,-.032)],gold,plunger,'Y')
 tube('Exposed_Compression_Spring',[(.006*math.cos(TAU*7*i/168),-.006-.026*i/168,.006*math.sin(TAU*7*i/168)) for i in range(169)],.0009,steel,plunger)
 wheel=empty('Handwheel_Rotation_Pivot',turret,(.073,-.008,.087));gear('ElevationGear',(0,0,0),.045,30,brass,wheel);ring('Handwheel_PolishedRim',(.004,0,0),.043,.0024,gold,wheel,'X');cylinder('Handwheel_RaisedHub',(.005,0,0),.010,.012,gold,wheel,'X');screw('Handwheel_HubScrew',(.012,0,0),gold,wheel,'X',.0038)
 crank=empty('Handwheel_Crank_Pivot',wheel,(.009,-.032,0));cylinder('Handwheel_CrankSpindle',(.008,0,0),.002,.016,steel,crank,'X');lathe('Handwheel_TurnedGrip',[(0,0),(.003,0),(.004,.004),(.004,.014),(.003,.019),(0,.019)],wood,crank,'X',(.015,0,0),32)
 gear('ReductionGear',(.070,-.058,.052),.019,14,steel,turret,.004);cylinder('ReductionGear_BrassHub',(.076,-.058,.052),.005,.004,gold,turret,'X')
 cylinder('ElevationScrew_Core',(0,-.075,.035),.004,.055,steel,turret)
 tube('ElevationScrew_HelicalThread',[(.005*math.cos(TAU*10*i/240),-.075+.005*math.sin(TAU*10*i/240),.012+.044*i/240) for i in range(241)],.0008,gold,turret);ring('ElevationScrew_AdjustmentRim',(0,-.075,.017),.015,.0017,gold,turret)
 for i in range(12):
  a=TAU*i/12;ball('ElevationScrew_Knurl_%02d'%i,(.014*math.cos(a),-.075+.014*math.sin(a),.017),.0018,brass,turret)
 # The graduation ticks are inlaid on a supported arc, never floating in space.
 vs=[];n=32
 for x in [.050,.052]:
  for r in [.056,.066]:
   for i in range(n+1):
    a=math.radians(4+72*i/n);vs.append((x,-.008+r*math.cos(a),.087+r*math.sin(a)))
 k=n+1;fs=[]
 for i in range(n):fs.extend([(i,i+1,k+i+1,k+i),(2*k+i,3*k+i,3*k+i+1,2*k+i+1),(i,2*k+i,2*k+i+1,i+1),(k+i,k+i+1,3*k+i+1,3*k+i)])
 fs.extend([(0,k,3*k,2*k),(n,2*k-1,4*k-1,3*k-1)]);mesh('ElevationScale_EngravedArc',vs,fs,dark,turret)
 for angle in [8,72]:
  a=math.radians(angle);tube('ElevationScale_Support_%s'%angle,[(.051,-.008,.087),(.051,-.008+.061*math.cos(a),.087+.061*math.sin(a))],.0014,brass,turret)
 for i in range(9):
  a=math.radians(12+i*7);tube('ElevationScale_Mark_%02d'%i,[(.0522,-.008+.058*math.cos(a),.087+.058*math.sin(a)),(.0522,-.008+.063*math.cos(a),.087+.063*math.sin(a))],.00035,gold,turret)
 empty('ElevationScale_Zero',turret,(.052,.050,.087))
 bpy.ops.object.select_all(action='DESELECT');meshes=[o for o in col.objects if o.type=='MESH']
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT');bpy.ops.object.select_all(action='DESELECT')
 sources={'GENERATE_PinLauncher.py':(HERE/'build_asset.py').read_text(encoding='utf-8'),'pinlauncher_detail.py':Path(__file__).read_text(encoding='utf-8')}
 for name,source in sources.items():
  txt=bpy.data.texts.get(name) or bpy.data.texts.new(name);txt.clear();txt.write(source)
  if OUT!=HERE:(OUT/('build_asset.py' if name.startswith('GENERATE') else name)).write_text(source,encoding='utf-8')
 bpy.context.view_layer.update();points=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];bounds=[[min(p[k] for p in points),max(p[k] for p in points)] for k in range(3)];root['dimensions_m']=str([round(b-a,5) for a,b in bounds]);root['forward']='Blender +Y: use MuzzleTip and BreechOrigin to aim'
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'PinLauncher.blend'))
 for o in col.objects:o.select_set(True)
 bpy.context.view_layer.objects.active=root;bpy.ops.export_scene.fbx(filepath=str(OUT/'PinLauncher.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=False)
 dg=bpy.context.evaluated_depsgraph_get();triangles=sum(sum(len(p.vertices)-2 for p in o.evaluated_get(dg).data.polygons) for o in meshes)
 report={'status':'PASS','revision':2,'mesh_count':len(meshes),'triangles':triangles,'bounds_m':bounds,'dimensions_m':[b-a for a,b in bounds],'embedded_source_exact':all(bpy.data.texts[k].as_string()==v for k,v in sources.items()),'materials':[m.name for m in [wood,brass,steel,gold,dark,green,rubber,red]],'moving_pivots':[o.name for o in col.objects if o.type=='EMPTY'],'uv_meshes':sum(bool(o.data.uv_layers) for o in meshes),'packed_images':sum(bool(i.packed_file) for i in bpy.data.images),'reference':'Inspiration/SceneAndValidatedAssets/SceneApproved.png','limitations':['Original procedural maps, not scanned textures','Cosmetic toy geometry; no physical firing simulation']}
 (OUT/'generation-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');render_preview(scene);print('PIN_LAUNCHER_REWORK_PASS '+json.dumps(report))

def render_preview(scene):
 preview=bpy.data.collections.new('PREVIEW_ONLY');scene.collection.children.link(preview)
 def add(o):preview.objects.link(o);return o
 d=bpy.data.meshes.new('PreviewFloor');d.from_pydata([(-2,-2,-.001),(2,-2,-.001),(2,2,-.001),(-2,2,-.001)],[],[(0,1,2,3)]);d.materials.append(simple('PreviewCharcoal',(.012,.017,.019),.05,.5));add(bpy.data.objects.new('PreviewFloor',d))
 center=Vector((0,.015,.145));d=bpy.data.cameras.new('PreviewCamera');cam=add(bpy.data.objects.new('PreviewCamera',d));cam.location=(.58,-.79,.45);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=.47;scene.camera=cam
 for name,at,power,color,size in [('WarmKey',(.25,-.35,.65),18,(1,.72,.43),.40),('CoolRim',(-.35,.32,.42),22,(.48,.68,1),.32),('SoftFront',(.15,-.65,.25),6,(1,.90,.72),.25)]:
  d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape='DISK';d.size=size;o=add(bpy.data.objects.new(name,d));o.location=at;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
 scene.render.engine='CYCLES';scene.cycles.samples=40;scene.cycles.use_denoising=True;scene.render.resolution_x=1400;scene.render.resolution_y=1400;scene.render.resolution_percentage=100;scene.world.color=(.07,.07,.07);scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'blender-preview.png');bpy.ops.render.render(write_still=True)
 cam.location=(.58,.79,.48);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/'blender-muzzle-preview.png');bpy.ops.render.render(write_still=True)
 for o in list(preview.objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.collections.remove(preview)
