import bpy,json
from pathlib import Path
from mathutils import Matrix,Vector

source=Path.home()/'Downloads/image__pixal3d__20261030-000307.glb'
out=Path(__file__).resolve().parent;out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)!=1:raise RuntimeError('Expected one imported mesh')
obj=meshes[0];bpy.context.view_layer.objects.active=obj;bpy.context.view_layer.update()
corners=[obj.matrix_world@Vector(p) for p in obj.bound_box]
low=Vector(tuple(min(p[a] for p in corners) for a in range(3)));high=Vector(tuple(max(p[a] for p in corners) for a in range(3)))
offset=Vector(((low.x+high.x)/2,(low.y+high.y)/2,low.z))
obj.data.transform(Matrix.Translation(-offset)@obj.matrix_world);obj.matrix_world=Matrix.Identity(4)
obj.name='Imported_PinBox';obj.data.name='PinBox_OptimizedMesh'
source_faces=len(obj.data.polygons)
decimate=obj.modifiers.new('Unity display mesh optimization','DECIMATE');decimate.ratio=min(1,48000/source_faces);decimate.use_collapse_triangulate=True
bpy.ops.object.modifier_apply(modifier=decimate.name)
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.export_scene.fbx(filepath=str(out/'PinBox_Unity.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
bpy.ops.wm.save_as_mainfile(filepath=str(out/'PinBox_Unity.blend'))
report={'source':str(source),'source_triangles':source_faces,'unity_triangles':len(obj.data.polygons),'source_dimensions':list(high-low),'pivot':'bottom center','materials':[m.name for m in obj.data.materials]}
(out/'ConversionReport.json').write_text(json.dumps(report,indent=2))
target=Vector((0,0,(high.z-low.z)/2));extent=max(high-low)
data=bpy.data.cameras.new('ReviewCamera');camera=bpy.data.objects.new('ReviewCamera',data);bpy.context.scene.collection.objects.link(camera)
camera.location=target+Vector((extent*1.7,-extent*3,extent*1.5));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=extent*1.45;bpy.context.scene.camera=camera
for name,delta,power,color in [('Key',(-2,-3,4),650,(1,.8,.6)),('Fill',(3,-1,2),450,(.65,.8,1)),('Rim',(0,2,4),650,(1,.9,.7))]:
    light_data=bpy.data.lights.new(name,'AREA');light_data.energy=power;light_data.size=extent*3
    light=bpy.data.objects.new(name,light_data);bpy.context.scene.collection.objects.link(light);light.location=target+Vector(delta)*extent;light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(out/'Blender_PinBoxPreview.png');bpy.ops.render.render(write_still=True)
print('PIN BOX EXPORTED',json.dumps(report))
