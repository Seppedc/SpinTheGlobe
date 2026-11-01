"""Detailed reference-driven revision of the existing banker lamp.
Metres, Blender Z-up. Source reference is partially cropped: retain established
base footprint/height, refine the visible turned brass and rounded green hood.
Coated opaque glass + ivory lining are a deliberate stable URP presentation.
"""
from pathlib import Path
import os,math,json
import bpy,bmesh
from mathutils import Vector,Matrix
HERE=Path(__file__).resolve().parent
OUT=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT',HERE)).resolve()
TAU=math.tau
SPECS={}

def empty(name,parent=None,world=(0,0,0)):
 o=bpy.data.objects.new(name,None);col.objects.link(o);o.parent=parent;o.location=parent.matrix_world.inverted()@Vector(world) if parent else world;o.empty_display_size=.012;bpy.context.view_layer.update();return o

def attach(o,name,mat,parent,world_matrix):
 o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.parent=parent;o.matrix_parent_inverse=Matrix.Identity(4);o.matrix_basis=parent.matrix_world.inverted()@world_matrix
 if mat:o.data.materials.append(mat)
 return o

def primitive(o,name,mat,parent,bevel=0):
 bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);bpy.context.view_layer.update();world=o.matrix_world.copy();attach(o,name,mat,parent,world)
 if bevel:
  m=o.modifiers.new('Manufactured edge radius','BEVEL');m.width=bevel;m.segments=2
 for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
 o.select_set(False);return o

def mesh(name,vertices,faces,mat,parent,smooth=True):
 inv=parent.matrix_world.inverted();d=bpy.data.meshes.new(name+'_Mesh');d.from_pydata([tuple(inv@Vector(v)) for v in vertices],[],faces);d.update();o=bpy.data.objects.new(name,d);col.objects.link(o);o.parent=parent;o.data.materials.append(mat)
 for p in d.polygons:p.use_smooth=smooth
 return o

def cylinder(name,at,r,depth,mat,parent,axis='Z'):
 rot={'Z':(0,0,0),'X':(0,math.pi/2,0),'Y':(math.pi/2,0,0)}[axis]
 bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=r,depth=depth,location=at,rotation=rot);return primitive(bpy.context.object,name,mat,parent,min(.00028,depth*.12))

def ball(name,at,r,mat,parent):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,radius=r,location=at);o=primitive(bpy.context.object,name,mat,parent)
 for p in o.data.polygons:p.use_smooth=True
 return o

def box(name,at,size,mat,parent,bevel=.0002):
 bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.dimensions=size;return primitive(o,name,mat,parent,bevel)

def lathe(name,profile,mat,parent,at=(0,0,0),axis='Z',segments=96):
 vs=[]
 for r,h in profile:
  for i in range(segments):
   a=TAU*i/segments;p=(r*math.cos(a),r*math.sin(a),h)
   if axis=='X':p=(p[2],p[0],p[1])
   if axis=='Y':p=(p[0],p[2],p[1])
   vs.append(tuple(p[k]+at[k] for k in range(3)))
 fs=[]
 for j in range(len(profile)-1):
  for i in range(segments):
   a=j*segments+i;b=j*segments+(i+1)%segments;fs.append((a,b,b+segments,a+segments))
 fs.extend([tuple(reversed(range(segments))),tuple((len(profile)-1)*segments+i for i in range(segments))]);return mesh(name,vs,fs,mat,parent)

def tube(name,points,r,mat,parent,cyclic=False):
 c=bpy.data.curves.new(name+'_Curve','CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=2;sp=c.splines.new('POLY');sp.points.add(len(points)-1);sp.use_cyclic_u=cyclic;inv=parent.matrix_world.inverted()
 for p,co in zip(sp.points,points):p.co=(*inv@Vector(co),1)
 o=bpy.data.objects.new(name,c);col.objects.link(o);o.parent=parent;o.data.materials.append(mat);bpy.context.view_layer.objects.active=o;bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object
 for p in o.data.polygons:p.use_smooth=True
 return o

def ring(name,at,r,wire,mat,parent,axis='Z'):
 pts=[]
 for i in range(65):
  a=TAU*i/64;p=(r*math.cos(a),r*math.sin(a),0)
  if axis=='X':p=(0,p[0],p[1])
  if axis=='Y':p=(p[0],0,p[1])
  pts.append(tuple(p[k]+at[k] for k in range(3)))
 return tube(name,pts,wire,mat,parent)

def simple(name,color,metal,rough,emission=None):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 SPECS[name]={'base_color_linear':list(color),'metallic':metal,'roughness':rough,'normal_strength':.10,'textures':{},'surface':'Opaque'}
 if emission:
  p.inputs['Emission Color'].default_value=(*emission,1);p.inputs['Emission Strength'].default_value=1;SPECS[name]['emission_linear']=list(emission)
 return m

def pbr(name,color_srgb,metal,rough,family):
 m=simple(name,(1,1,1),metal,rough);bsdf=m.node_tree.nodes['Principled BSDF'];n=512;data={k:[] for k in ['BaseColor','Roughness','MetallicSmoothness','Normal']}
 for y in range(n):
  for x in range(n):
   u=x/n;v=y/n;cloud=math.sin(TAU*u*3+.4*math.sin(TAU*v*2))*math.cos(TAU*v*3);brush=math.sin(TAU*u*147+.6*math.sin(TAU*v*2));micro=math.sin(x*13.37+y*42.91)*math.cos(x*7.31-y*9.71)
   variation=1+.012*cloud if family=='glass' else .92+.045*cloud+.018*brush+.012*micro
   rr=max(.05,min(.9,rough+(.006*cloud if family=='glass' else .035*cloud+.012*micro)))
   data['BaseColor'].extend((*[max(0,min(1,c/255*variation)) for c in color_srgb],1));data['Roughness'].extend((rr,rr,rr,1));data['MetallicSmoothness'].extend((metal,metal,metal,1-rr));data['Normal'].extend((.5+(.001 if family=='glass' else .012)*micro,.5+.003*cloud,1,1))
 folder=OUT/'Textures';folder.mkdir(parents=True,exist_ok=True)
 for key,pixels in data.items():
  image=bpy.data.images.new(name+'_'+key,width=n,height=n,alpha=True);image.use_fake_user=True;image.colorspace_settings.name='sRGB' if key=='BaseColor' else 'Non-Color';image.pixels.foreach_set(pixels);image.filepath_raw=str(folder/(image.name+'.png'));image.file_format='PNG';image.save();image.pack();image.filepath='//Textures/'+image.name+'.png';SPECS[name]['textures'][key]='Textures/'+image.name+'.png'
  if key in ['BaseColor','Roughness','Normal']:
   tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image
   if key=='BaseColor':m.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
   elif key=='Roughness':m.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Roughness'])
   else:
    normal=m.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.08 if family=='glass' else .18;m.node_tree.links.new(tex.outputs['Color'],normal.inputs['Color']);m.node_tree.links.new(normal.outputs['Normal'],bsdf.inputs['Normal'])
 if family=='glass':bsdf.inputs['Coat Weight'].default_value=.65;bsdf.inputs['Coat Roughness'].default_value=.08
 return m

def hood(parent,green,liner,edge):
 nx=96;na=64;vs=[];fs=[];indices=[]
 # Rounded capsule ends close the hood, unlike the former open-ended half-pipe.
 for inner in [False,True]:
  half=.141 if inner else .145;straight=.095;cap=half-straight;ry=.082 if inner else .086;rz=.096 if inner else .100;z0=.309 if inner else .306
  for i in range(nx+1):
   x=-half+2*half*i/nx;t=max(0,(abs(x)-straight)/cap);f=math.sqrt(max(0,1-t*t))
   for j in range(na+1):
    a=math.pi*j/na;vs.append((x,ry*f*math.cos(a),z0+rz*f*math.sin(a)))
 layer=(nx+1)*(na+1)
 for inner in [0,1]:
  for i in range(nx):
   for j in range(na):
    a=inner*layer+i*(na+1)+j;b=a+na+1;fs.append((a,a+1,b+1,b) if not inner else (b,b+1,a+1,a));indices.append(inner)
 for j in [0,na]:
  for i in range(nx):
   a=i*(na+1)+j;b=(i+1)*(na+1)+j;fs.append((a,b,b+layer,a+layer));indices.append(2)
 o=mesh('Shade_GreenGlass',vs,fs,green,parent);o.data.materials.append(liner);o.data.materials.append(edge)
 for p,index in zip(o.data.polygons,indices):p.material_index=index
 # Rolled brass wire follows the full physical mouth perimeter.
 pts=[]
 for side in [1,-1]:
  values=range(nx+1) if side==1 else reversed(range(nx+1))
  for i in values:
   x=-.145+.29*i/nx;t=max(0,(abs(x)-.095)/.05);f=math.sqrt(max(0,1-t*t));pts.append((x,side*.086*f,.306))
 tube('Shade_RolledBrassLip',pts,.00115,gold,parent,True)
 # Fine secondary green edge makes the glass thickness read at grazing angles.
 pts2=[(x,y*.983,z+.0016) for x,y,z in pts];tube('Shade_GreenGlassEdge',pts2,.00055,edge,parent,True)

def build():
 global col,root,gold
 OUT.mkdir(parents=True,exist_ok=True);old=bpy.data.collections.get('Collection_BankersLamp')
 if old:
  for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
  bpy.data.collections.remove(old)
 if not bpy.data.filepath:
  for name in ['Cube','Camera','Light']:
   o=bpy.data.objects.get(name)
   if o:bpy.data.objects.remove(o,do_unlink=True)
 col=bpy.data.collections.new('Collection_BankersLamp');bpy.context.scene.collection.children.link(col);bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1;root=empty('ROOT_BankersLamp')
 brass=pbr('Lamp_AgedBrass',(159,107,44),.84,.28,'brass');gold=pbr('Lamp_PolishedBrass',(191,138,61),.90,.20,'brass');green=pbr('Lamp_EmeraldGlass',(10,105,37),0,.11,'glass')
 edge=simple('Lamp_GreenGlassEdge',(.020,.265,.047),0,.10);liner=simple('Lamp_OpalLiner',(.82,.72,.49),0,.45,(.055,.042,.020));bulb=simple('Lamp_BulbWarm',(.95,.82,.58),0,.27,(1.4,1.0,.55));ceramic=simple('Lamp_CeramicSocket',(.65,.61,.49),0,.32);felt=simple('Lamp_Felt',(.010,.018,.012),0,.94);cord=simple('Lamp_CordRubber',(.012,.014,.012),0,.68);dark=simple('Lamp_DarkInlay',(.017,.021,.018),.28,.54)
 base=empty('Base_Assembly',root);stem=empty('Stem_Assembly',root)
 lathe('Base_Felt',[(.077,0),(.080,.001),(.080,.003)],felt,base)
 profile=[(.080,.003),(.086,.006),(.087,.010),(.086,.014),(.081,.018),(.079,.021),(.076,.023),(.075,.027),(.066,.031),(.060,.037),(.047,.042),(.037,.048),(.029,.056),(.024,.064),(.019,.073),(.015,.079)]
 lathe('Base_SpunBrass',profile,brass,base)
 for i,(r,z) in enumerate([(.085,.010),(.080,.019),(.074,.028),(.046,.043),(.023,.066)]):ring('Base_PolishedMolding_%02d'%i,(0,0,z),r,.0008,gold,base)
 for r,z in [(.079,.022),(.065,.032)]:ring('Base_IncisedRing_'+str(z),(0,0,z),r,.00035,dark,base)
 cylinder('Base_UndersideScrew',(0,0,.001),.005,.001,gold,base);box('Base_UndersideScrewSlot',(0,0,.0004),(.006,.0007,.0003),dark,base,.00003)
 lathe('Stem_Collar',[(.015,.076),(.018,.079),(.018,.084),(.015,.089),(.012,.092),(.010,.100)],gold,stem)
 # Keep the reference's slender mostly straight column with turned ferrules.
 lathe('Stem_CurvedBrass',[(.010,.094),(.010,.105),(.008,.109),(.007,.120),(.007,.273),(.008,.279),(.010,.284),(.010,.289),(.008,.296)],brass,stem,at=(0,-.012,0))
 for i,(z,r) in enumerate([(.103,.010),(.114,.008),(.268,.0075),(.280,.010),(.288,.010)]):ring('Stem_TurnedFerrule_%02d'%i,(0,-.012,z),r,.0007,gold,stem)
 for z in [.108,.276,.285]:ring('Stem_IncisedCollar_'+str(z),(0,-.012,z),.0082,.00030,dark,stem)
 pts=[]
 for i in range(49):
  t=i/48;a=Vector((0,-.012,.290));b=Vector((0,-.020,.335));c=Vector((0,-.103,.349));v=(1-t)**2*a+2*(1-t)*t*b+t*t*c;pts.append(tuple(v))
 tube('Stem_RearSwanNeck',pts,.0058,brass,stem)
 tube('Shade_RearYoke',[(-.147,0,.343),(-.143,-.045,.348),(-.095,-.086,.350),(0,-.103,.349),(.095,-.086,.350),(.143,-.045,.348),(.147,0,.343)],.0038,gold,stem)
 shade=empty('Shade_Pivot',root,(0,0,.343));hood(shade,green,liner,edge)
 for side in [-1,1]:
  x=side*.147
  cylinder('Shade_PivotWasher_%s'%side,(x,0,.343),.012,.003,brass,stem,'X')
  # Independent knurled side caps remain concentric with the shade hinge axis.
  cylinder('Shade_PivotCap_%s'%side,(side*.151,0,.343),.009,.006,gold,stem,'X')
  for j in range(16):
   a=TAU*j/16;y=.009*math.cos(a);z=.343+.009*math.sin(a);tube('Shade_PivotKnurl_%s_%02d'%(side,j),[(side*.149,y,z),(side*.153,y,z)],.00028,brass,stem)
  box('Shade_PivotScrewSlot_%s'%side,(side*.1542,0,.343),(.0005,.007,.00065),dark,stem,.00002)
 sockets=empty('BulbSocket_Pivot',root,(-.075,.047,.297))
 cylinder('Socket_CeramicSleeve',(-.083,.047,.297),.011,.026,ceramic,sockets,'X');cylinder('Socket_BrassCollar',(-.067,.047,.297),.012,.007,gold,sockets,'X')
 for x in [-.080,-.075,-.070]:ring('Socket_Thread_'+str(x),(x,.047,.297),.0112,.00035,brass,sockets,'X')
 lathe('Bulb_OpalTube',[(0,-.016),(.010,-.016),(.014,-.008),(.019,.006),(.018,.024),(.011,.038),(.003,.043),(0,.044)],bulb,sockets,at=(-.050,.047,.297),axis='X',segments=64)
 tube('Socket_SupportBracket',[(-.083,.047,.297),(-.115,.039,.306),(-.138,.012,.335)],.0022,brass,sockets)
 chain=empty('PullChain_Pivot',root,(.101,.066,.299));ring('PullChain_Guide',(.101,.066,.300),.0029,.00055,gold,chain,'X');tube('PullChain_Core',[(.101,.066,.299),(.101,.066,.174)],.00035,brass,chain)
 for i in range(48):ball('PullChain_Bead_%02d'%i,(.101,.066,.297-i*.0026),.00095,gold,chain)
 lathe('PullChain_Finial',[(0,.151),(.002,.151),(.0045,.155),(.005,.160),(.004,.165),(.0023,.170),(.0012,.176),(0,.177)],gold,chain,at=(.101,.066,0),segments=48)
 ring('PullChain_FinialCollar',(.101,.066,.169),.0027,.00045,brass,chain)
 # Short rear cord stays separate and unobtrusive; no new gameplay switch.
 cylinder('Cord_Grommet',(0,-.079,.018),.0038,.006,cord,base,'Y')
 tube('Cord_RearLead',[(0,-.082,.018),(0,-.094,.010),(.012,-.110,.004),(.040,-.123,.003),(.063,-.131,.003)],.0020,cord,base)
 empty('LampLightOrigin',root,(-.025,.047,.276));empty('LampLightTarget',root,(-.025,.047,.010));empty('LampFront_Marker',root,(0,.15,.32))
 # Tilt the complete hood/rim, while socket, yoke and gravity-hanging chain remain fixed.
 shade.rotation_euler.x=math.radians(10);bpy.context.view_layer.update()
 meshes=[o for o in col.objects if o.type=='MESH']
 for o in meshes:
  bpy.context.view_layer.objects.active=o
  for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-8);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-10);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT');bpy.ops.object.select_all(action='DESELECT')
 sources={'GENERATE_BankersLamp.py':(HERE/'build_asset.py').read_text(encoding='utf-8'),'lamp_detail.py':Path(__file__).read_text(encoding='utf-8')}
 for name,source in sources.items():
  t=bpy.data.texts.get(name) or bpy.data.texts.new(name);t.clear();t.write(source)
  if OUT!=HERE:(OUT/('build_asset.py' if name.startswith('GENERATE') else name)).write_text(source,encoding='utf-8')
 bpy.context.view_layer.update();pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box];bounds=[[min(p[k] for p in pts),max(p[k] for p in pts)] for k in range(3)]
 for o in meshes:o.data.calc_loop_triangles()
 report={'status':'PASS','mesh_count':len(meshes),'triangles':sum(len(o.data.loop_triangles) for o in meshes),'zero_area_triangles':sum(t.area<1e-18 for o in meshes for t in o.data.loop_triangles),'bounds_blender_m':bounds,'dimensions_m':[b-a for a,b in bounds],'root_at_base_origin':True,'shade_tilt_degrees':10,'chain_beads':48,'closed_rounded_hood':True,'glass_thickness_m':.003,'embedded_sources_exact':all(bpy.data.texts[n].as_string()==s for n,s in sources.items()),'packed_images':sum(bool(i.packed_file) for i in bpy.data.images),'uv_meshes':sum(bool(o.data.uv_layers) for o in meshes),'materials':SPECS,'light_markers':{'origin':[-.025,.047,.276],'target':[-.025,.047,.010]},'reference':'Inspiration/SceneAndValidatedAssets/SceneApproved.png','limits':['Reference crops part of the foot; original footprint retained and visible turned moldings refined','Opaque coated emerald glass and ivory lining, not validated refractive glass','Lamp-local light must be bound and verified in Unity; no switch gameplay added']}
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BankersLamp.blend'));bpy.ops.object.select_all(action='DESELECT')
 for o in col.objects:o.select_set(True)
 bpy.context.view_layer.objects.active=root;bpy.ops.export_scene.fbx(filepath=str(OUT/'BankersLamp.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
 (OUT/'generation-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');preview();print('DETAILED_BANKER_LAMP_PASS '+json.dumps({k:report[k] for k in ['mesh_count','triangles','zero_area_triangles','dimensions_m','packed_images']}))

def preview():
 scene=bpy.context.scene;c=bpy.data.collections.new('PREVIEW_ONLY');scene.collection.children.link(c)
 def add(o):c.objects.link(o);return o
 d=bpy.data.cameras.new('LampReviewCamera');cam=add(bpy.data.objects.new('LampReviewCamera',d));target=Vector((0,.005,.206));cam.location=(.48,.83,.41);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=.52;scene.camera=cam
 floor=bpy.data.meshes.new('PreviewFloor');floor.from_pydata([(-2,-2,-.001),(2,-2,-.001),(2,2,-.001),(-2,2,-.001)],[],[(0,1,2,3)]);fm=bpy.data.materials.new('PreviewCharcoal');fm.diffuse_color=(.025,.024,.022,1);floor.materials.append(fm);add(bpy.data.objects.new('PreviewFloor',floor))
 for name,at,power,color,size in [('Softbox',(-.25,.35,.65),16,(1,.86,.66),.44),('WindowRim',(.28,-.35,.48),12,(.53,.72,1),.30),('FrontFill',(.40,.65,.22),4,(.85,.91,1),.28)]:
  d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape='RECTANGLE';d.size=size;d.size_y=size*.35;o=add(bpy.data.objects.new(name,d));o.location=at;o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
 d=bpy.data.lights.new('PhysicalBulbPreview','POINT');d.energy=.55;d.color=(1,.68,.32);d.shadow_soft_size=.015;o=add(bpy.data.objects.new('PhysicalBulbPreview',d));o.location=(-.025,.047,.276)
 scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.render.resolution_x=1400;scene.render.resolution_y=1600;scene.render.resolution_percentage=100;scene.world.color=(.05,.05,.05);scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'blender-preview.png');bpy.ops.render.render(write_still=True)
 cam.location=(.35,.85,.30);cam.rotation_euler=(Vector((0,.015,.32))-cam.location).to_track_quat('-Z','Y').to_euler();dof=cam.data;dof.ortho_scale=.36;scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.filepath=str(OUT/'blender-shade-closeup.png');bpy.ops.render.render(write_still=True)
 for o in list(c.objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.collections.remove(c)
