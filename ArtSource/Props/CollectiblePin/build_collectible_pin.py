"""DetectiveDesk collectible pin | Blender 4.2+ / 5.x
Run in a NEW empty Blender file (clears scene). Creates ONE exportable
Unity-ready pin with separated brass foot, neck, enamel dome, trim and needle.
Coordinates: local Z=0 is the coffer cavity FLOOR / PinSlot transform.
Designed for original coffer script: 11 mm socket ID, 7 mm recess depth.
"""
import bpy, math, os, csv
from mathutils import Vector
from math import pi

# ---------- CUSTOMIZATION ----------
ENAMEL_COLOR = 'Ruby'   # Ruby, Sapphire, Emerald, Amber, Amethyst, Ivory, Obsidian, Turquoise, Copper, Gold, Silver, Midnight
COLORS = {
    'Ruby': (0.45,0.012,0.025,1), 'Sapphire': (0.010,0.075,0.48,1),
    'Emerald': (0.012,0.31,0.105,1), 'Amber': (0.95,0.34,0.018,1),
    'Amethyst': (0.30,0.025,0.38,1), 'Ivory': (0.86,0.78,0.58,1),
    'Obsidian': (0.014,0.014,0.018,1), 'Turquoise': (0.014,0.56,0.51,1),
    'Copper': (0.56,0.19,0.07,1), 'Gold': (0.70,0.46,0.11,1),
    'Silver': (0.55,0.64,0.69,1), 'Midnight': (0.018,0.048,0.145,1),
}
SOCKET_ID_MM = 11.0
SOCKET_DEPTH_MM = 7.0
FOOT_DIAMETER_MM = 9.2   # 1.8 mm total clearance
FOOT_HEIGHT_MM = 5.9    # from the cavity floor; shorter than socket depth
HEAD_DIAMETER_MM = 12.6 # visible above the brass socket rim
MODEL_NAME = 'CollectiblePin_Ruby'
OUTPUT_DIR = os.environ.get('DETECTIVE_PIN_OUTPUT',os.path.join(os.path.expanduser('~'),'DetectivePinOutput'))
os.makedirs(OUTPUT_DIR, exist_ok=True)
assert ENAMEL_COLOR in COLORS, f'Unsupported color: {ENAMEL_COLOR}'
assert FOOT_DIAMETER_MM < SOCKET_ID_MM, 'Foot must fit inside socket'
assert FOOT_HEIGHT_MM < SOCKET_DEPTH_MM, 'Foot must be shallower than socket'

# Blender units are metres: multiply millimetres by 0.001
mm=lambda x: x/1000.0
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.unit_settings.system='METRIC'
scene.unit_settings.length_unit='MILLIMETERS'
scene.render.engine='CYCLES'
scene.cycles.samples=96
scene.render.resolution_x=1152; scene.render.resolution_y=1152
scene.view_settings.view_transform='AgX'

def coll(name):
    c=bpy.data.collections.new(name); scene.collection.children.link(c); return c
parts=coll('EXPORT | One collectible pin')
preview=coll('PREVIEW ONLY | not exported')

def material(name,color,metal=0,rough=0.3,coat=0,coat_rough=.18):
    m=bpy.data.materials.new(name);m.diffuse_color=color;m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=color
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    p.inputs['Coat Weight'].default_value=coat
    p.inputs['Coat Roughness'].default_value=coat_rough
    return m
brass=material('MAT | AgedBrass',(.42,.25,.075,1),1,.31)
brass_high=material('MAT | PolishedBrass',(.76,.49,.18,1),1,.18)
brass_dark=material('MAT | PatinaBrass',(.18,.105,.04,1),.86,.48)
enamel=material('MAT | EnamelColor | '+ENAMEL_COLOR,COLORS[ENAMEL_COLOR],.03,.15,.5,.12)
steel=material('MAT | SteelNeedle',(.43,.46,.50,1),.9,.24)

def link(ob,name,mat):
    ob.name=name
    for c in list(ob.users_collection):c.objects.unlink(ob)
    parts.objects.link(ob)
    ob.data.materials.append(mat)
    return ob

def bevel(ob,amount,segments=3):
    b=ob.modifiers.new('Soft manufactured edges','BEVEL'); b.width=mm(amount);b.segments=segments
    n=ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL')

def cyl(name,rad_mm,height_mm,z_mm,mat,verts=64,radius2=None):
    if radius2 is None:
        bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=mm(rad_mm),depth=mm(height_mm),location=(0,0,mm(z_mm)))
    else:
        bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=mm(rad_mm),radius2=mm(radius2),depth=mm(height_mm),location=(0,0,mm(z_mm)))
    ob=link(bpy.context.object,name,mat)
    for p in ob.data.polygons:p.use_smooth=True
    return ob

def torus(name,r_mm,tube_mm,z_mm,mat):
    bpy.ops.mesh.primitive_torus_add(major_segments=80,minor_segments=12,
         location=(0,0,mm(z_mm)),major_radius=mm(r_mm),minor_radius=mm(tube_mm))
    ob=link(bpy.context.object,name,mat)
    for p in ob.data.polygons:p.use_smooth=True
    return ob

# Pin's origin/root is the socket's FLOOR so it plugs into PinSlot_01 etc.
root=bpy.data.objects.new('PIN_ROOT | align to PinSlot cavity floor',None)
parts.objects.link(root);root.empty_display_type='SPHERE';root.empty_display_size=mm(2)

def parent(ob):ob.parent=root;return ob

# Tapered plug, 9.2 mm across; bottom and sides are clean solid geometry.
parent(cyl('01 | Socket foot tapered brass',FOOT_DIAMETER_MM/2,FOOT_HEIGHT_MM,
           FOOT_HEIGHT_MM/2,brass,64,radius2=FOOT_DIAMETER_MM*.445))
# Jewel holder is above the 7 mm-deep cavity: 6.4-9.6 mm.
parent(cyl('02 | Brass socket shoulder',5.1,.8,6.45,brass_high))
parent(cyl('03 | Ornate brass collar',4.4,2.4,8.1,brass))
parent(torus('04 | Neck decorative ring',4.28,.35,8.75,brass_high))
parent(cyl('05 | Enamel bezel cup',6.35,1.3,10.2,brass_dark))
parent(torus('06 | Polished head rim',6.10,.32,10.55,brass_high))

# Smooth jewel-like dome, visually clear and easy to click even from desk distance.
bpy.ops.mesh.primitive_uv_sphere_add(segments=64,ring_count=32,radius=mm(HEAD_DIAMETER_MM/2),location=(0,0,mm(15.2)))
head=link(bpy.context.object,'07 | RECOLOR THIS | Enamel head',enamel)
# Slightly squashed vertical pearl, keeping body under the lid's closing clearance
head.scale=(1,1,.77);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
for face in head.data.polygons:face.use_smooth=True
parent(head)
# No protruding needle below z=0, so the collectible is safe to set in the socket.
# Separate needle hidden within tapered plug. Optionally show it in gameplay during launch.
needle=parent(cyl('08 | Hidden detachable steel needle',1.05,5.0,3.0,steel,32,radius2=.15))
needle.hide_render=True;needle.hide_viewport=True
needle['description']='Optional show on launch; hidden while pin is stored'

root['pin_color']=ENAMEL_COLOR
root['socket_compatible_inner_diameter_mm']=SOCKET_ID_MM
root['socket_compatible_depth_mm']=SOCKET_DEPTH_MM
root['foot_outer_diameter_mm']=FOOT_DIAMETER_MM
root['origin']='Bottom center of mounting foot: attach to PinSlot transform'

# Studio preview without exporting these objects.
def light(name,loc,energy,size,color):
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.shape='DISK';d.size=size;d.color=color
    ob=bpy.data.objects.new(name,d);preview.objects.link(ob);ob.location=loc
    direction=Vector((0,0,.012))-ob.location;ob.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
light('Preview warm key',(.12,-.14,.19),60,.15,(1.0,.78,.56))
light('Preview cool fill',(-.12,.10,.13),35,.13,(.56,.72,1))
camd=bpy.data.cameras.new('Preview camera');cam=bpy.data.objects.new('Preview camera',camd)
preview.objects.link(cam);cam.location=(.055,-.09,.065)
cam.rotation_euler=(Vector((0,0,.010))-cam.location).to_track_quat('-Z','Y').to_euler()
camd.type='ORTHO';camd.ortho_scale=.043;scene.camera=cam
scene.world.color=(.09,.09,.09)

# Save entire editable scene; export ONLY root and its children.
scene.view_settings.look='Medium High Contrast'
for ob in bpy.context.selected_objects:ob.select_set(False)
root.select_set(True)
for ob in parts.objects:
    if ob!=root:ob.select_set(True)
bpy.context.view_layer.objects.active=head
outfile=os.path.join(OUTPUT_DIR,f'CollectiblePin_{ENAMEL_COLOR}.glb')
bpy.ops.export_scene.gltf(filepath=outfile,export_format='GLB',use_selection=True,export_yup=True)
for ob in parts.objects:ob.select_set(False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUTPUT_DIR,f'CollectiblePin_{ENAMEL_COLOR}.blend'))

with open(os.path.join(OUTPUT_DIR,'pin_specification.txt'),'w',encoding='utf-8') as f:
    f.write(f'Collectible pin {ENAMEL_COLOR}\nSocket ID 11 mm, depth 7 mm\nFoot OD 9.2 mm, height 5.9 mm\nOrigin: socket floor at z=0\nHead OD 12.6 mm\nColor variants: '+', '.join(COLORS)+'\n')
print('Saved:',outfile)
print('Tip: change ENAMEL_COLOR at script top and rerun to make another color. GLB contains exactly ONE pin.')
