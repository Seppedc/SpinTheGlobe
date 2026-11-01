import bpy,json
from pathlib import Path

workspace=Path(__file__).resolve().parents[2]
source=workspace/'SpinTheGlobe/Assets/Objects/Pinbox.blend'
out=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(source))
bpy.ops.object.select_all(action='DESELECT')
objects=[o for o in bpy.context.scene.objects if o.type in {'MESH','EMPTY'}]
for obj in objects:obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/'DetectivePinCoffer.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
(out/'ExportReport.json').write_text(json.dumps({'source':str(source),'export':str(out/'DetectivePinCoffer.fbx'),'blender_version':bpy.app.version_string,'mesh_count':sum(o.type=='MESH' for o in objects),'authored_slots':[o.name for o in objects if o.name.startswith('PinSlot_')],'source_file_modified':False},indent=2))
print('REAL COFFER EXPORTED',out/'DetectivePinCoffer.fbx')
