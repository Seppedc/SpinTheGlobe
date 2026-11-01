"""Deterministic coffer example. Re-running rebuilds this scene and saves a .blend.
Usage: blender -b --factory-startup --python build_examplecoffer.py -- --output /path/ExampleCoffer.blend
All dimensions are meters (Unity-friendly). Edit this file, not the generated .blend.
"""
import bpy, math, sys, argparse
from pathlib import Path
from mathutils import Vector

ASSET = 'ExampleCoffer'
COLLECTION = 'ART_ExampleCoffer'
MM = 0.001
W,D,H = .160,.110,.038

def material(name, color, metallic=0.0, roughness=.6):
    mat=bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color=(*color,1)
    mat.use_nodes=True
    principled=mat.node_tree.nodes.get('Principled BSDF')
    principled.inputs['Base Color'].default_value=(*color,1)
    principled.inputs['Metallic'].default_value=metallic
    principled.inputs['Roughness'].default_value=roughness
    return mat

wood=material('MAT_AgedMahogany',(0.16,.055,.028),roughness=.35)
velvet=material('MAT_CrimsonVelvet',(.26,.009,.026),roughness=.91)
brass=material('MAT_AgedBrass',(.50,.29,.075),metallic=1,roughness=.29)

# Only rebuild the asset's own collection: other scene objects remain untouched.
old=bpy.data.collections.get(COLLECTION)
if old:
    for obj in list(old.objects): bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.collections.remove(old)
coll=bpy.data.collections.new(COLLECTION)
bpy.context.scene.collection.children.link(coll)

def to_collection(obj):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    coll.objects.link(obj)
    return obj

def box(name,loc,dims,mat, bevel=.0007,parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o=to_collection(bpy.context.object)
    o.name=name
    o.dimensions=dims
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat)
    if bevel:
        mod=o.modifiers.new('Rounded edges','BEVEL'); mod.width=bevel; mod.segments=3
        o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
    if parent: o.parent=parent; o.matrix_parent_inverse=parent.matrix_world.inverted()
    return o

def empty(name,loc):
    o=bpy.data.objects.new(name,None); coll.objects.link(o); o.location=loc; return o

def torus(name,loc,major,minor,mat):
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=48,minor_segments=12,location=loc)
    o=to_collection(bpy.context.object); o.name=name; o.data.materials.append(mat)
    return o

# Shell bottom, side and back rail; open top to show insert.
base=box('Base_Bottom',(0,0,.004),(W,D,.008),wood)
for sign in [-1,1]:
    box('Base_Side_'+str(sign),(sign*(W/2-.005),0,.023),(.010,D,.03),wood)
box('Base_Front',(0,-D/2+.005,.023),(W-.017,.01,.03),wood)
box('Base_Back',(0,D/2-.005,.023),(W-.017,.01,.03),wood)

# Velvet insert with 12 physical recessed bores cut into its mesh.
tray=box('Velvet_12_Recess_Tray',(0,0,.012),(.142,.090,.012),velvet,bevel=.001)
# Apply the tray's existing modifiers before socket booleans, in stack order.
bpy.context.view_layer.objects.active=tray
for initial_modifier in list(tray.modifiers):
    bpy.ops.object.modifier_apply(modifier=initial_modifier.name)
for row in range(3):
    for col in range(4):
        x=(col-1.5)*.031; y=(row-1)*.026
        n=row*4+col+1
        bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=.0055,depth=.008,location=(x,y,.018))
        cutter=bpy.context.object; cutter.name='TMP_CUT';
        cutter.data.materials.append(velvet)  # Keep boolean faces/material slots valid.
        bpy.context.view_layer.objects.active=tray
        modifier=tray.modifiers.new('Socket_cut_%02d'%n,'BOOLEAN'); modifier.operation='DIFFERENCE'; modifier.object=cutter
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        bpy.data.objects.remove(cutter,do_unlink=True)
        torus('Socket_Rim_%02d'%n,(x,y,.01815),.0068,.00085,brass)
        marker=empty('PinSlot_%02d'%n,(x,y,.014))
        marker.empty_display_type='SPHERE'; marker.empty_display_size=.003

# Hinge pivot, allowing lid rotation animation in Unity.
pivot=empty('Lid_HingePivot',(0,D/2,.036))
# Lid is at 108-degree open angle, relative to closed (0) position, around the X axis.
# Use local y relative to hinge as the outward direction of the lid.
box('Lid_Panel',(0,D/2+.046,.036),(.16,.092,.008),wood,parent=pivot)
box('Lid_Velvet',(0,D/2+.046,.041),(.142,.077,.002),velvet,parent=pivot)
for x in [-.056,.056]:
    box('Lid_BrassCorner_%.2f'%x,(x,D/2+.089,.040),(.012,.01,.003),brass,parent=pivot)
# Apply lid rotation: open toward back. The plane remains visible.
pivot.rotation_euler.x=math.radians(70)

for x in [-.056,.056]:
    box('Hinge_%.2f'%x,(x,D/2,.035),(.012,.008,.005),brass)
box('Latch',(0,-D/2-.001,.026),(.017,.003,.018),brass)

# Embed an exact snapshot of this generator in the .blend file.
script=Path(__file__).resolve()
script_text=script.read_text(encoding='utf-8')
text_name='GENERATE_'+ASSET+'.py'
txt=bpy.data.texts.get(text_name) or bpy.data.texts.new(text_name)
txt.clear(); txt.write(script_text)

scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1.0
scene.render.engine='CYCLES'
scene.render.resolution_x=1000; scene.render.resolution_y=700

args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
parser=argparse.ArgumentParser(); parser.add_argument('--output',type=Path,default=script.with_name(ASSET+'.blend'))
opt=parser.parse_args(args)
opt.output.parent.mkdir(parents=True,exist_ok=True)
try: bpy.ops.file.pack_all()
except Exception as e: print('Pack warning:',e)
bpy.ops.wm.save_as_mainfile(filepath=str(opt.output.resolve()))
print('Saved',opt.output,'Mesh count:',sum(o.type=='MESH' for o in coll.objects))
