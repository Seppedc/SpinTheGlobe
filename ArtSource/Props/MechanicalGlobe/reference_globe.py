"""Reference-driven globe revision: readable cartography and instrument-grade holder.
Reuses the original detailed model, geographic polygons, gear/gauge builders and
established Unity-local coordinates from legacy_globe.py. No downloaded assets.
"""
from pathlib import Path
import os, math, json, hashlib
import bpy, bmesh
from mathutils import Vector, Quaternion
import legacy_globe as g
TAU=math.tau
HERE=Path(__file__).resolve().parent
OUT=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT',HERE)).resolve()
DESK_WORLD_Y=.815
BASE_LOCAL_Y=(DESK_WORLD_Y-g.ROOT_POSITION_UNITY[1])/g.ROOT_SCALE_UNITY[1]

def surface_maps(folder,name,color,metal,rough,size=512):
 albedo=bytearray(size*size*4);normal=bytearray(size*size*4);packed=bytearray(size*size*4)
 for y in range(size):
  for x in range(size):
   q=(y*size+x)*4;cloud=math.sin(TAU*x/size*3+.5*math.sin(TAU*y/size*2))*math.cos(TAU*y/size*4);grain=math.sin(TAU*x/size*127+math.sin(TAU*y/size*2));v=1+.045*cloud+.014*grain
   albedo[q:q+4]=bytes((*[g.byte(c*v) for c in color],255));normal[q:q+4]=bytes((g.byte(128+2*grain),g.byte(128+cloud),255,255));packed[q:q+4]=bytes((g.byte(metal*255),0,0,g.byte((1-rough-.035*cloud)*255)))
 paths={}
 for key,data in [('BaseColor',albedo),('Normal',normal),('MetallicSmoothness',packed)]:
  p=folder/(name+'_'+key+'.png');g.png(p,size,size,data);paths[key]=p
 return paths

def world_maps(folder,polys,width=2048):
 """Real vector-derived coastlines, restrained graticules and reduced glare."""
 h=width//2;mask=g.raster_land(polys,width,h);a=bytearray(width*h*4);n=bytearray(width*h*4);p=bytearray(width*h*4)
 for y in range(h):
  latitude=90-(y+.5)*180/h
  for x in range(width):
   k=y*width+x;q=k*4;land=bool(mask[k]);lon=-180+(x+.5)*360/width
   coast=any(mask[k]!=mask[min(h-1,max(0,y+dy))*width+(x+dx)%width] for dx,dy in [(1,0),(-1,0),(0,1),(0,-1),(2,0),(-2,0)])
   cloud=math.sin(x*.051+math.sin(y*.029))*math.cos(y*.037);grain=math.sin(x*1.31+y*.023)
   color=(158,137,89) if land else (27,48,57)
   line=min(abs(lon/30-round(lon/30))*30,abs(latitude/30-round(latitude/30))*30)<.14
   if coast:color=(203,159,77)
   elif line:color=(134,113,72) if land else (43,64,68)
   a[q:q+4]=bytes((*[g.byte(c*(1+.025*cloud+.007*grain)) for c in color],255))
   n[q:q+4]=bytes((g.byte(128+grain),128,255,255))
   # Moderate metallic values keep silhouettes readable under the existing noir lights.
   metal=.38 if land else .24;rough=.51 if land else .55
   p[q:q+4]=bytes((g.byte(metal*255),0,0,g.byte((1-rough)*255)))
 paths={}
 for key,data in [('BaseColor',a),('Normal',n),('MetallicSmoothness',p)]:
  path=folder/('WorldCartography_'+key+'.png');g.png(path,width,h,data);paths[key]=path
 return paths,mask,width,h

def remove_mesh_children(parent):
 for o in list(parent.children_recursive):
  if o.type=='MESH':
   if o in g.OBJECTS:g.OBJECTS.remove(o)
   bpy.data.objects.remove(o,do_unlink=True)

def tube(name,parent,points,r,mat,cyclic=False):
 c=bpy.data.curves.new(name+'_Curve','CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=1
 s=c.splines.new('POLY');s.points.add(len(points)-1);s.use_cyclic_u=cyclic
 for p,co in zip(s.points,points):p.co=(*g.u(co),1)
 o=bpy.data.objects.new(name,c);g.COLLECTION.objects.link(o);o.parent=parent;o.data.materials.append(mat);bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object
 for poly in o.data.polygons:poly.use_smooth=True
 g.OBJECTS.append(o);return o

def text(name,parent,body,position,size,mat,angle=0):
 data=bpy.data.curves.new(name,'FONT');data.body=body;data.align_x='CENTER';data.align_y='CENTER';data.size=size;data.extrude=.00025;data.bevel_depth=.00005;data.bevel_resolution=1
 o=bpy.data.objects.new(name,data);g.COLLECTION.objects.link(o);o.parent=parent;o.location=g.u(position);o.rotation_mode='QUATERNION';o.rotation_quaternion=g.unity_rotation('Z',angle)@Quaternion((1,0,0),math.pi/2);data.materials.append(mat)
 bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.ops.object.convert(target='MESH');g.OBJECTS.append(bpy.context.object);return bpy.context.object

def make_meridian(root):
 m=bpy.data.objects['Meridian_Assembly'];remove_mesh_children(m);m.rotation_mode='QUATERNION';m.rotation_quaternion=g.unity_rotation('Y',-25)
 # Player-facing left semicircle only: top pole -> left -> bottom pole.
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
 # Top and bottom pole bearings are separate replaceable meshes, not a thin wire loop.
 g.lathe('Meridian_NorthPoleBearing',m,[(.026,.682),(.033,.694),(.036,.713),(.045,.726),(.045,.741),(.038,.752),(.032,.764)],g.MATERIALS['AntiqueBrass'],segments=64)
 g.lathe('Meridian_AcornFinial',m,[(.032,.764),(.044,.774),(.047,.785),(.040,.795),(.027,.804),(.018,.814),(.013,.825),(0,.835)],g.MATERIALS['PolishedBrass'],segments=64)
 g.lathe('Meridian_SouthPoleBearing',m,[(.034,-.810),(.047,-.795),(.047,-.777),(.034,-.759),(.030,-.713),(.022,-.686)],g.MATERIALS['AntiqueBrass'],segments=64)
 for y,r in [(.724,.044),(.746,.039),(-.777,.046)]:
  o=g.torus('Meridian_BearingCollar_'+str(y),m,r,.003,g.MATERIALS['PolishedBrass'],(0,y,0),n=64,sides=8);o.rotation_quaternion=g.unity_rotation('X',90)

def make_pedestal():
 p=bpy.data.objects['Pedestal_Assembly'];remove_mesh_children(p)
 # Extend the lower molding to the actual desk; retain every established root,
 # sphere, gear and gauge pivot instead of lowering the complete instrument.
 foot=BASE_LOCAL_Y-.86
 profile=[(.302,foot),(.316,foot+.006),(.319,-.022),(.307,-.008),(.305,0),(.335,.018),(.340,.040),(.340,.068),(.323,.085),(.320,.102),(.290,.122),(.290,.168),(.272,.187),(.262,.205),(.259,.229),(.230,.250),(.201,.275),(.170,.307),(.143,.342),(.123,.378),(.111,.409),(.118,.437),(.148,.460),(.161,.477),(.159,.500),(.122,.522),(.081,.540)]
 g.lathe('Pedestal_SteppedSculptedBrass',p,profile,g.MATERIALS['AntiqueBrass'],segments=128)
 for idx,(r,y) in enumerate([(.332,.025),(.328,.080),(.288,.127),(.285,.168),(.258,.223),(.157,.480)]):
  band=g.torus('Pedestal_PolishedMolding_%02d'%idx,p,r,.0036,g.MATERIALS['PolishedBrass'],(0,y,0),n=128,sides=8);band.rotation_quaternion=g.unity_rotation('X',90)
 for r,y in [(.320,.091),(.289,.147),(.264,.202)]:
  band=g.torus('Pedestal_RecessedDarkInlay_'+str(y),p,r,.0021,g.MATERIALS['EngravingInlay'],(0,y,0),n=128,sides=6);band.rotation_quaternion=g.unity_rotation('X',90)
 amber=g.torus('Pedestal_RestrainedAmberInlay',p,.159,.002,g.MATERIALS['AmberAccent'],(0,.486,0),n=96,sides=6);amber.rotation_quaternion=g.unity_rotation('X',90)
 # A single foliate frieze hugs the cylindrical drum, sharing its real surface.
 points=[]
 for i in range(481):
  a=TAU*i/480;y=.146+.012*math.sin(a*24);points.append((.2905*math.cos(a),y,.2905*math.sin(a)))
 tube('Pedestal_EngravedFoliateFrieze',p,points,.00085,g.MATERIALS['PolishedBrass'])
 text('Pedestal_MakerMark',p,'T E R R A',(0,.146,-.294),.018,g.MATERIALS['EngravingInlay'])
 # Compact support tucks the existing-sized RPM instrument against the base.
 # Gauge center is (.22,.32,-.47) from the pedestal origin, matching the user's
 # blue-circle placement; original sphere, pedestal and root remain unchanged.
 points=[];border=[]
 for i in range(65):
  t=i/64;a=Vector((.15,.29,-.12));b=Vector((.26,.24,-.28));c=Vector((.22,.27,-.43));v=(1-t)**2*a+2*(1-t)*t*b+t*t*c;points.append(tuple(v));border.append((v.x,v.y+.011,v.z))
 tube('Pedestal_GaugeSupportArch',p,points,.010,g.MATERIALS['AntiqueBrass'])
 tube('Pedestal_GaugeSupportPolishedEdge',p,border,.0022,g.MATERIALS['PolishedBrass'])
 g.lathe('Pedestal_GaugeMountingShoe',p,[(.025,0),(.033,.004),(.033,.012),(.021,.025)],g.MATERIALS['AntiqueBrass'],position=(.22,.255,-.43),segments=48)


def coastlines(sphere,polys):
 # Existing public-domain geographic data, no invented continent silhouettes.
 for batch in range(4):
  curve=bpy.data.curves.new('CoastlineBatch_%02d'%batch,'CURVE');curve.dimensions='3D';curve.bevel_depth=.00115;curve.bevel_resolution=1
  for poly in polys[batch::4]:
   ring=poly[0]
   if len(ring)<12:continue
   chunks=[];chunk=[];last=None
   for lon,lat,*_ in ring:
    if last is not None and abs(lon-last)>180:
     if len(chunk)>1:chunks.append(chunk)
     chunk=[]
    chunk.append(g.spherical(lon,lat,.683));last=lon
   if len(chunk)>1:chunks.append(chunk)
   for chunk in chunks:
    s=curve.splines.new('POLY');s.points.add(len(chunk)-1)
    for p,co in zip(s.points,chunk):p.co=(*g.u(co),1)
  o=bpy.data.objects.new('Sphere_RaisedCoastlineInlay_%02d'%batch,curve);g.COLLECTION.objects.link(o);o.parent=sphere;curve.materials.append(g.MATERIALS['PolishedBrass']);bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.ops.object.convert(target='MESH');g.OBJECTS.append(bpy.context.object)

def clean_geometry():
 for obj in list(g.OBJECTS):
  if obj.type!='MESH':continue
  bpy.context.view_layer.objects.active=obj
  for modifier in list(obj.modifiers):
   was_decimate=modifier.type=='DECIMATE';bpy.ops.object.modifier_apply(modifier=modifier.name)
   if was_decimate and obj.name.startswith('Sphere_EngravedContinentSections'):
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.triangulate(bm,faces=list(bm.faces))
    for v in bm.verts:v.co=v.co.normalized()*.6800
    for _ in range(9):
     edges=[e for e in bm.edges if e.calc_length()>.045]
     if not edges:break
     bmesh.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=True)
     for v in bm.verts:v.co=v.co.normalized()*.6800
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
  bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-8);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-10);bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free();obj.data.update()
  if obj.name.startswith(('Sphere_DarkMetalShell','Sphere_EngravedContinentSections')):
   for p in obj.data.polygons:p.use_smooth=True
   obj.data.normals_split_custom_set_from_vertices([v.co.normalized() for v in obj.data.vertices])
   # Original plate subdivision bends large planar geographic triangles onto
   # the sphere. Recompute UVs from their final spherical coordinates instead
   # of interpolating the old planar UVs, which creates northern map slivers.
   uv=obj.data.uv_layers.active or obj.data.uv_layers.new(name='UVMap')
   for face in obj.data.polygons:
    values=[]
    for loop_index in face.loop_indices:
     v=obj.data.vertices[obj.data.loops[loop_index].vertex_index].co.normalized();lon=math.atan2(v.x,-v.y);lat=math.asin(max(-1,min(1,v.z)));values.append(((lon+math.pi)/TAU,lat/math.pi+.5))
    seam=max(v[0] for v in values)-min(v[0] for v in values)>.5
    for loop_index,(u,v) in zip(face.loop_indices,values):uv.data[loop_index].uv=(u+1 if seam and u<.5 else u,v)
  if not obj.data.uv_layers:
   layer=obj.data.uv_layers.new(name='UVMap')
   for loop in obj.data.loops:
    v=obj.data.vertices[loop.vertex_index].co;layer.data[loop.index].uv=(v.x*2,v.z*2)

def run():
 OUT.mkdir(parents=True,exist_ok=True);textures=OUT/'Textures';textures.mkdir(exist_ok=True)
 old=bpy.data.collections.get('DetectiveGlobe_Export')
 if old:
  for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
  bpy.data.collections.remove(old)
 if not bpy.data.filepath:
  for name in ['Cube','Camera','Light']:
   o=bpy.data.objects.get(name)
   if o:bpy.data.objects.remove(o,do_unlink=True)
 g.OBJECTS.clear();g.COLLECTION=bpy.data.collections.new('DetectiveGlobe_Export');bpy.context.scene.collection.children.link(g.COLLECTION);bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
 land=g.polygons();sets,_,_,_=g.generate_textures(textures,land,1024)
 world,mask,w,h=world_maps(textures,land,2048);sets['DarkShell']=world;sets['EngravedLand']=dict(world)
 sets['AntiqueBrass']=surface_maps(textures,'AntiqueBrass',(169,119,51),.84,.37)
 sets['PolishedBrass']=surface_maps(textures,'PolishedBrass',(196,148,67),.88,.31)
 sets['EngravingInlay']=surface_maps(textures,'EngravingInlay',(18,23,23),.25,.62)
 sets['DarkMechanism']=surface_maps(textures,'DarkMechanism',(25,32,34),.72,.46)
 g.MATERIALS={name:g.make_material(name,paths) for name,paths in sets.items()}
 root,specs=g.build(land,mask,w,h,192)
 # The earth map belongs only on the sphere, never on gauge housings or rear gears.
 for obj in g.OBJECTS:
  if obj.type=='MESH' and not obj.name.startswith('Sphere_'):
   for slot in obj.material_slots:
    if slot.material==g.MATERIALS['DarkShell']:slot.material=g.MATERIALS['DarkMechanism']
 # Remove exposed gear meshes only; retain semantic empty pivots for later
 # Unity binding preservation. No sphere geometry or materials are changed.
 remove_mesh_children(bpy.data.objects['Clockwork_Assembly'])
 gauge=bpy.data.objects['RPMGauge_Assembly'];gauge.location=g.u((-1.23,1.18,-.80))
 make_meridian(root);make_pedestal();coastlines(bpy.data.objects['Sphere_RotationPivot'],land);clean_geometry()
 for image in bpy.data.images:
  if image.source=='FILE' and image.filepath:
   image.use_fake_user=True;image.pack();image.filepath='//Textures/'+Path(image.filepath).name
 sources={'GENERATE_MechanicalGlobe.py':(HERE/'build_asset.py').read_text(encoding='utf-8'),'legacy_globe.py':(HERE/'legacy_globe.py').read_text(encoding='utf-8'),'reference_globe.py':Path(__file__).read_text(encoding='utf-8')}
 for name,source in sources.items():
  t=bpy.data.texts.get(name) or bpy.data.texts.new(name);t.clear();t.write(source)
  if OUT!=HERE:(OUT/('build_asset.py' if name.startswith('GENERATE') else name)).write_text(source,encoding='utf-8')
 bpy.context.view_layer.update();meshes=[o for o in g.OBJECTS if o.type=='MESH'];pts=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box];bounds=[[min(p[k] for p in pts),max(p[k] for p in pts)] for k in range(3)]
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'MechanicalGlobe.blend'));bpy.ops.object.select_all(action='DESELECT')
 for o in g.OBJECTS:o.select_set(True)
 bpy.context.view_layer.objects.active=root;bpy.ops.export_scene.fbx(filepath=str(OUT/'MechanicalGlobe.fbx'),use_selection=True,object_types={'MESH','EMPTY'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_space_transform=True,bake_space_transform=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
 for o in meshes:o.data.calc_loop_triangles()
 report={'status':'PASS','mesh_count':len(meshes),'triangles':sum(len(o.data.loop_triangles) for o in meshes),'zero_area_triangles':sum(t.area<1e-18 for o in meshes for t in o.data.loop_triangles),'bounds_blender_m':bounds,'sphere_pivot_unity':g.SPHERE_CENTER,'sphere_radius':.683,'base_bottom_unity_y':BASE_LOCAL_Y,'desk_world_y':DESK_WORLD_Y,'geographic_polygons':len(land),'world_texture_size':[w,h],'geography_source':g.SOURCE_URL,'source_provenance':'Original ../Artifacts/BlenderGlobe/build_mechanical_globe.py reused as legacy_globe.py; original file preserved','embedded_sources_exact':all(bpy.data.texts[n].as_string()==s for n,s in sources.items()),'materials':{n:{k:'Textures/'+p.name for k,p in paths.items()} for n,paths in sets.items()},'packed_images':sum(bool(i.packed_file) for i in bpy.data.images),'holder_span_degrees':180,'exposed_gear_meshes':0,'gauge_pivot_unity':[-1.23,1.18,-.80],'unity_integration':'PENDING - Unity MCP bridge usage limit; staged only','gear_specs':[{'pivot':n+'_Pivot','ratio':r} for n,_,_,_,r in specs],'needle':'RPMGauge_NeedlePivot','uv_meshes':sum(bool(o.data.uv_layers) for o in meshes),'limits':['Stylized Natural Earth 1:110m coastline data, not survey/terrain detail','Original gameplay motion and lighting require separate Unity binding validation']}
 (OUT/'generation-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');preview();print('REFERENCE_GLOBE_GENERATED '+json.dumps(report))

def preview():
 scene=bpy.context.scene;c=bpy.data.collections.new('PREVIEW_ONLY');scene.collection.children.link(c)
 def add(o):c.objects.link(o);return o
 data=bpy.data.cameras.new('PreviewCamera');camera=add(bpy.data.objects.new('PreviewCamera',data));target=Vector((-1.13,-.33,1.91));camera.location=(-.76,-5.5,2.70);camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=2.55;scene.camera=camera
 mat=bpy.data.materials.new('PreviewFloor');mat.diffuse_color=(.018,.023,.025,1);mesh=bpy.data.meshes.new('PreviewFloor');floor=BASE_LOCAL_Y-.002;mesh.from_pydata([(-4,-4,floor),(2,-4,floor),(2,2,floor),(-4,2,floor)],[],[(0,1,2,3)]);mesh.materials.append(mat);add(bpy.data.objects.new('PreviewFloor',mesh))
 for name,pos,power,color,size in [('WarmKey',(-3,-3.5,4.5),420,(1,.81,.59),3),('NeutralFill',(2,-3,3.3),330,(.79,.87,1),3),('Rim',(-1,2,4),550,(1,.73,.42),2)]:
  d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape='DISK';d.size=size;o=add(bpy.data.objects.new(name,d));o.location=pos;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
 scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.resolution_x=1500;scene.render.resolution_y=1500;scene.render.resolution_percentage=100;scene.world.color=(.08,.08,.08);scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'blender-preview.png');bpy.ops.render.render(write_still=True)
 camera.location=(-2.6,-4.9,2.8);camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/'blender-threequarter.png');bpy.ops.render.render(write_still=True)
 for o in list(c.objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.collections.remove(c)
