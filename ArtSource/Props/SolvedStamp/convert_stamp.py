"""Run with Blender --background --factory-startup; converts only the downloaded asset."""
import bpy
import json
from pathlib import Path
from mathutils import Matrix, Vector

source=Path.home()/'Downloads/image_20k.glb'
output=Path(__file__).resolve().parent
output.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(source))
meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH']
if len(meshes)!=1:raise RuntimeError('Expected one stamp mesh, found '+str(len(meshes)))
obj=meshes[0];bpy.context.view_layer.update()
corners=[obj.matrix_world@Vector(p) for p in obj.bound_box]
low=Vector(tuple(min(p[a] for p in corners) for a in range(3)))
high=Vector(tuple(max(p[a] for p in corners) for a in range(3)))
offset=Vector(((low.x+high.x)/2,(low.y+high.y)/2,low.z))
obj.data.transform(Matrix.Translation(-offset)@obj.matrix_world);obj.matrix_world=Matrix.Identity(4)
obj.name='AssetFurnace_SOLVED_Stamp';obj.data.name='AssetFurnace_StampMesh'
for modifier in list(obj.modifiers):bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=modifier.name)
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
bpy.ops.export_scene.fbx(filepath=str(output/'AssetFurnace_SOLVED_Stamp.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=True,add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
bpy.ops.wm.save_as_mainfile(filepath=str(output/'AssetFurnace_SOLVED_Stamp.blend'))
report={'source':str(source),'blender_version':bpy.app.version_string,'source_bounds_min':list(low),'source_bounds_max':list(high),'normalized_dimensions':list(high-low),'vertices':len(obj.data.vertices),'faces':len(obj.data.polygons),'pivot':'bottom center','material_names':[m.name for m in obj.data.materials]}
(output/'ConversionReport.json').write_text(json.dumps(report,indent=2))

target=Vector((0,0,(high.z-low.z)*.5));extent=max(high-low)
camera_data=bpy.data.cameras.new('StampReviewCamera');camera=bpy.data.objects.new('StampReviewCamera',camera_data);bpy.context.scene.collection.objects.link(camera)
camera.location=target+Vector((extent*1.8,-extent*3,extent*1.2));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera_data.type='ORTHO';camera_data.ortho_scale=extent*1.4;bpy.context.scene.camera=camera
for name,delta,energy,color in [('Key',(-2,-3,4),500,(1,.8,.6)),('Fill',(3,-1,2),350,(.65,.8,1)),('Rim',(0,2,4),500,(1,.9,.7))]:
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.size=extent*3
    light=bpy.data.objects.new(name,data);bpy.context.scene.collection.objects.link(light);light.location=target+Vector(delta)*extent;light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=900;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(output/'Blender_StampPreview.png');bpy.ops.render.render(write_still=True)
print('STAMP CONVERSION COMPLETE',json.dumps(report))
