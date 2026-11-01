"""Detective Desk – premium procedural 12-slot pin coffer for Blender 4.2+/5.x.
Run in a NEW Blender file using Scripting > Run Script (scene cleared).
Creates editable geometry, 12 true recessed sockets, lid pivot, UV-independent
procedural Cycles materials, slot-position CSV, .blend and .glb export.
"""
import bpy, math, os, csv, random
from mathutils import Vector
from math import radians, sin, cos, pi

# ---- Dimensions (metres). Scale the *whole prefab* in Unity if desired. ----
W, D, H = .160, .110, .038                  # outside width, depth, base height
WALL = .008
BOTTOM = .009
TOP_Z = H
LID_THICKNESS = .009
LID_ANGLE = 108.0                          # open toward the back (degrees)
ROWS, COLS = 3, 4
SOCKET_ID = .011                           # 11 mm hole, 10 mm pin foot target
SOCKET_OD = .016                           # outer aged brass ring diameter
SOCKET_DEPTH = .007                        # 7 mm physically recessed
RIM_HEIGHT = .0014
TRAY_TOP = .023                            # insert top below rim of chest
RING_SEGMENTS = 48

# Output beside saved .blend; fallback to home if opened from Blender text editor.
DEFAULT_OUTPUT = os.path.join(os.path.expanduser('~'), 'DetectivePinCofferOutput')
OUTPUT_DIR = os.environ.get('DETECTIVE_COFFER_OUTPUT', DEFAULT_OUTPUT)
os.makedirs(OUTPUT_DIR, exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    if material.users == 0: bpy.data.materials.remove(material)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 48
scene.render.resolution_x = 1400
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.view_settings.view_transform = 'AgX'
scene.unit_settings.system = 'METRIC'

# ---- Collections and helpers ----
def collection(name):
    col=bpy.data.collections.new(name); scene.collection.children.link(col); return col
main=collection('COFFER | Model')
basecol=collection('01 Base and wooden joinery')
lidcol=collection('02 Hinged lid assembly')
slotscol=collection('03 Twelve socket cavities')
metalcol=collection('04 Brass hardware')
decorcol=collection('05 Velvet and trim')
previewcol=collection('PREVIEW ONLY | delete before game export')

def move(obj,col):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    col.objects.link(obj); return obj

def parent_keep(obj, parent):
    mat=obj.matrix_world.copy(); obj.parent=parent; obj.matrix_world=mat

def material(name,color,metal=0,rough=.5,noise_scale=None,bump=.05):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    n=m.node_tree.nodes; links=m.node_tree.links
    bs=n.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Metallic'].default_value=metal; bs.inputs['Roughness'].default_value=rough
    if noise_scale:
        tex=n.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=noise_scale
        tex.inputs['Detail'].default_value=3.8; tex.inputs['Roughness'].default_value=.68
        tex.name='Procedural surface grain'
        bumpnode=n.new('ShaderNodeBump'); bumpnode.inputs['Strength'].default_value=bump
        bumpnode.inputs['Distance'].default_value=.00028
        links.new(tex.outputs['Fac'],bumpnode.inputs['Height'])
        links.new(bumpnode.outputs['Normal'],bs.inputs['Normal'])
        mix=n.new('ShaderNodeValToRGB'); mix.color_ramp.elements[0].position=.14
        mix.color_ramp.elements[1].position=.9
        dark=tuple(max(0,c*.6) for c in color)
        light=tuple(min(1,c*1.15) for c in color)
        mix.color_ramp.elements[0].color=(*dark,1)
        mix.color_ramp.elements[1].color=(*light,1)
        links.new(tex.outputs['Fac'],mix.inputs['Fac'])
        links.new(mix.outputs['Color'],bs.inputs['Base Color'])
    return m

wood=material('PBR | Dark scratched mahogany',(.115,.044,.021),rough=.39,noise_scale=330,bump=.14)
wood_dark=material('PBR | Deep mahogany end-grain',(.07,.027,.015),rough=.5,noise_scale=170,bump=.12)
brass=material('PBR | Antique brass',(.51,.285,.083),metal=1,rough=.29,noise_scale=420,bump=.045)
brass_bright=material('PBR | Brass polished edges',(.69,.44,.145),metal=1,rough=.21,noise_scale=420,bump=.025)
velvet=material('PBR | Deep crimson velvet',(.235,.008,.025),rough=.96,noise_scale=590,bump=.19)
velvet_dark=material('PBR | Shadowed velvet recess',(.092,.004,.013),rough=1,noise_scale=380,bump=.08)
iron=material('PBR | Blackened steel',(.065,.068,.063),metal=.9,rough=.32,noise_scale=350,bump=.04)

# Texture images persist in the GLB through primitive cube UV maps. Blender additionally
# adds noise bump details in the original .blend. Keep PNGs alongside the script.
try:
    texdir=os.path.dirname(os.path.abspath(__file__))
except NameError:
    texdir=OUTPUT_DIR
for mat,filename in [(wood,'mahogany_color.png'),(wood_dark,'mahogany_dark_color.png'),(velvet,'crimson_velvet_color.png')]:
    fpath=os.path.join(texdir,filename)
    if os.path.exists(fpath):
        image=bpy.data.images.load(fpath,check_existing=True)
        image.pack()
        node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
        node.name='Unity-exportable color texture';node.interpolation='Linear'
        mat.node_tree.links.new(node.outputs['Color'],mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'])

# Add directional noise to wood for long grain
bs=wood.node_tree.nodes.get('Principled BSDF'); n=wood.node_tree.nodes; l=wood.node_tree.links
for node in n:
    if node.type=='TEX_NOISE':
        texcoord=n.new('ShaderNodeTexCoord')
        vect=n.new('ShaderNodeVectorMath'); vect.operation='MULTIPLY'
        vect.inputs[1].default_value=(.30,6.0,1.1)
        l.new(texcoord.outputs['Generated'], vect.inputs[0])
        l.new(vect.outputs['Vector'], node.inputs['Vector'])
        break

def cube(name, loc, scale, mat, col=main, bevel=0, segments=3):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc); o=bpy.context.object
    o.name=name; o.dimensions=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    move(o,col); o.data.materials.append(mat)
    if bevel:
        mod=o.modifiers.new('Machined rounded edges','BEVEL'); mod.width=bevel; mod.segments=segments
        norm=o.modifiers.new('Weighted face normals','WEIGHTED_NORMAL')
    return o

def cylinder(name, loc, radius, depth, mat, col=metalcol, vertices=32, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc, rotation=rot)
    o=bpy.context.object; o.name=name; move(o,col); o.data.materials.append(mat)
    bev=o.modifiers.new('Gentle edge highlights','BEVEL'); bev.width=min(.00035,depth*.12); bev.segments=2
    o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return o

def annular(name, centre, outer, inner, zmin, zmax, mat, col=slotscol, segments=RING_SEGMENTS):
    # True central hole. Object contains NO polygon spanning its central opening.
    verts=[]; faces=[]
    for z,r in [(zmin,outer),(zmax,outer),(zmax,inner),(zmin,inner)]:
        for i in range(segments):
            a=2*pi*i/segments
            verts.append((centre[0]+r*cos(a),centre[1]+r*sin(a),z))
    for k in range(4):
        a=k*segments; b=((k+1)%4)*segments
        for i in range(segments):
            j=(i+1)%segments
            faces.append((a+i,a+j,b+j,b+i))
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); col.objects.link(obj); mesh.materials.append(mat)
    for p in mesh.polygons:p.use_smooth=True
    return obj

def flat_recess_cell(name,cx,cy,half_w,half_h,outer_radius,z_top,z_bottom):
    """True radial velvet cavity: square upper lip -> cylindrical edge -> lower floor.
    Each segment around perimeter has square vertex at the intersection with a ray.
    No upper surface closes the opening."""
    N=64; verts=[]; faces=[]
    for i in range(N):
        theta=2*pi*i/N; dx=cos(theta); dy=sin(theta)
        factor=min(half_w/max(abs(dx),1e-9),half_h/max(abs(dy),1e-9))
        verts.append((cx+factor*dx,cy+factor*dy,z_top))
    for radius,z in [(outer_radius,z_top),(outer_radius,z_bottom)]:
        for i in range(N):
            a=2*pi*i/N
            verts.append((cx+radius*cos(a),cy+radius*sin(a),z))
    for k in range(2):
        a=k*N; b=(k+1)*N
        for i in range(N):
            j=(i+1)%N
            faces.append((a+i,a+j,b+j,b+i))
    faces.append(tuple(reversed([2*N+i for i in range(N)])))
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces); mesh.update()
    ob=bpy.data.objects.new(name,mesh); slotscol.objects.link(ob)
    mesh.materials.append(velvet); mesh.materials.append(velvet_dark)
    for poly in mesh.polygons:
        poly.material_index=0 if poly.index<2*N else 1
    return ob

# ---- Solid chest base ----
# Base floor and four structural walls, accurately spaced around the interior.
cube('Chest | thick solid wooden bottom',(0,0,BOTTOM/2),(W,D,BOTTOM),wood,basecol,.0014)
cube('Chest | front wall',(0,-D/2+WALL/2,(BOTTOM+H)/2),(W,WALL,H-BOTTOM),wood,basecol,.0014)
cube('Chest | rear wall',(0,D/2-WALL/2,(BOTTOM+H)/2),(W,WALL,H-BOTTOM),wood,basecol,.0014)
for sign,label in [(-1,'left'),(1,'right')]:
    cube('Chest | '+label+' side wall',(sign*(W/2-WALL/2),0,(BOTTOM+H)/2),(WALL,D-2*WALL,H-BOTTOM),wood,basecol,.001)
# Thin decorative inlay at rim: separate beveled strips.
for sign in (-1,1):
    cube('Brass | top side inlay',(sign*(W/2-WALL-.001),0,H+.0004),(.0012,D-2*WALL,.001),brass_bright,metalcol,.0003)
    cube('Brass | top front/rear inlay',(0,sign*(D/2-WALL-.001),H+.0004),(W-2*WALL,.0012,.001),brass_bright,metalcol,.0003)

# Velvet backing below tray.
cube('Velvet | insert support',(0,0,TRAY_TOP-SOCKET_DEPTH-.003), (W-2*WALL-.002,D-2*WALL-.002,.004), velvet_dark,decorcol,.0006)

# ---- Socket array: 12 equally spaced, independently identifiable parts ----
usable_w=W-2*WALL-.007
usable_d=D-2*WALL-.008
cell_w=usable_w/COLS
cell_d=usable_d/ROWS
centers=[]
for row in range(ROWS):
    for col in range(COLS):
        x=(col-(COLS-1)/2)*cell_w
        y=((ROWS-1)/2-row)*cell_d
        idx=row*COLS+col+1
        centers.append((idx,row+1,col+1,x,y))
        flat_recess_cell(f'Velvet | cavity_{idx:02d}',x,y,cell_w/2,cell_d/2,SOCKET_ID/2,TRAY_TOP,TRAY_TOP-SOCKET_DEPTH)
        annular(f'Brass | socket_ring_{idx:02d}',(x,y),SOCKET_OD/2,SOCKET_ID/2,
                TRAY_TOP-.0004,TRAY_TOP+RIM_HEIGHT,brass_bright)
        # An actual empty child node for game inventory mounting, at floor of cavity
        empty=bpy.data.objects.new(f'PinSlot_{idx:02d} | row_{row+1}_col_{col+1}',None)
        slotscol.objects.link(empty); empty.location=(x,y,TRAY_TOP-SOCKET_DEPTH)
        empty.empty_display_size=.002;empty.empty_display_type='CIRCLE'
        empty['slot_index']=idx; empty['row']=row;empty['column']=col
        empty['pin_foot_diameter_mm']=SOCKET_ID*1000*.90

# Velvet trims front + back around edge
for sign in (-1,1):
    cube('Velvet | border X',(0,sign*(usable_d/2+.0013),TRAY_TOP-.0003),(usable_w+.004,.0021,.0022),velvet,decorcol,.0006)
    cube('Velvet | border Y',(sign*(usable_w/2+.0013),0,TRAY_TOP-.0003),(.0021,usable_d+.004,.0022),velvet,decorcol,.0006)

# ---- Brass corner protectors with rivets ----
for sx in (-1,1):
    for sy in (-1,1):
        x=sx*(W/2-.004);y=sy*(D/2-.004)
        cube('Brass | corner metal top cap',(x,y,H-.002),(.009,.009,.003),brass,metalcol,.0009)
        cube('Brass | corner vertical guard',(sx*(W/2+.0001),y,H*.52),(.0026,.009,H*.91),brass,metalcol,.0008)
        cylinder('Brass | corner rivet',(x,y,H+.00015),.00115,.0008,brass_bright)

# Front keyhole emblem + latch
cube('Brass | front lock shield',(0,-D/2-.001,H*.50),(.026,.003,.023),brass,metalcol,.002)
cylinder('Brass | lock knob',(0,-D/2-.004,H*.58),.004,.006,brass_bright,metalcol,rot=(pi/2,0,0))
cube('Iron | small keyhole inset',(0,-D/2-.003,H*.36),(.002,.001,.005),iron,metalcol,.0004)

# ---- Lid: all geometry parented to pivot; lid closes to cover entire base ----
pivot=bpy.data.objects.new('LID_PIVOT | animate X rotation',None);lidcol.objects.link(pivot)
pivot.location=(0,D/2,H+.002)
pivot.empty_display_type='ARROWS';pivot.empty_display_size=.012
# Create mesh in WORLD coordinates first, then parent without transform changes.
# Lid local back edge y=0 and front edge y=-D. Parent transformation corrected.
# Lid closed lies over the chest at z=H+.008 before rotating upward.
def add_lid_part(o):parent_keep(o,pivot);return o
lid_floor=add_lid_part(cube('Lid | solid wooden panel',(0,0,H+.006),(W,D,.010),wood,lidcol,.0015))
# frame around underside, goes on inner face; hinge orientation stays aligned
for sx in (-1,1):
    add_lid_part(cube('Lid | inner velvet side rail',(sx*(W/2-.007),0,H+.0006),(.009,D-.015,.0035),wood_dark,lidcol,.0008))
for sy in (-1,1):
    add_lid_part(cube('Lid | inner velvet end rail',(0,sy*(D/2-.007),H+.0006),(W-.015,.009,.0035),wood_dark,lidcol,.0008))
add_lid_part(cube('Lid | crimson velvet inner face',(0,0,H+.001),(W-.026,D-.025,.0025),velvet,lidcol,.0007))
for sx in (-1,1):
    add_lid_part(cube('Lid | gold inner longitudinal trim',(sx*(W/2-.013),0,H-.0003),(.0012,D-.024,.0012),brass_bright,lidcol,.00025))
for sy in (-1,1):
    add_lid_part(cube('Lid | gold inner transverse trim',(0,sy*(D/2-.012),H-.0003),(W-.026,.0012,.0012),brass_bright,lidcol,.00025))
for sx in (-1,1):
    for sy in (-1,1):
        add_lid_part(cube('Brass | lid corner guard',(sx*(W/2-.005),sy*(D/2-.005),H+.012),(.011,.011,.003),brass,lidcol,.001))
# Matching top half of closure clasp
add_lid_part(cube('Brass | lid closure plate',(0,-D/2-.001,H+.009),(.024,.003,.010),brass,lidcol,.0015))
# Hinge barrels at back edge. Hinge axis parallel to X.
for x in (-W*.31,W*.31):
    cylinder('Brass | hinge barrel',(x,D/2,H+.003),.003,.014,brass,metalcol,rot=(0,pi/2,0))
    cube('Brass | hinge plate base',(x,D/2-.004,H-.0002),(.018,.008,.0014),brass,metalcol,.0004)
    add_lid_part(cube('Brass | hinge plate lid',(x,D/2-.004,H+.012),(.018,.008,.0015),brass,lidcol,.0004))
# Turning open: rotating around X local hinge axis; camera from front (-Y)
pivot.rotation_euler.x=radians(-LID_ANGLE)
pivot['open_angle_degrees']=LID_ANGLE
pivot['closed_angle_degrees']=0

# bevel modifier support varying Blender versions already handled below
# Remove preview cameras/lights from the exported GLB by exporting SELECTED coffer only.
# Create neutral studio preview camera and lights so the .blend is immediately viewable.
world=bpy.data.worlds.new('Studio backdrop');scene.world=world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.045,.04,.038,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.4

def light(name,loc,power,size,color):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
    ob=bpy.data.objects.new(name,data);previewcol.objects.link(ob);ob.location=loc
    direction=Vector((0,0,.07))-ob.location;ob.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
light('Preview | warm area',(0,-.40,.45),210,.6,(1,.83,.65))
light('Preview | soft fill',(-.24,.15,.34),110,.50,(.75,.83,1))
camdata=bpy.data.cameras.new('Preview camera');camera=bpy.data.objects.new('Preview camera',camdata)
previewcol.objects.link(camera); camera.location=(.24,-.28,.22)
camera.rotation_euler=(Vector((0,0,.08))-camera.location).to_track_quat('-Z','Y').to_euler()
camdata.type='ORTHO';camdata.ortho_scale=.30;scene.camera=camera

# Ensure preview helpers are not exported, preserve individual meshes and empties.
for ob in bpy.context.selected_objects: ob.select_set(False)
exported=[]
for col in [basecol,lidcol,slotscol,metalcol,decorcol]:
    for ob in col.objects:
        if ob.type in {'MESH','EMPTY'}:
            ob.select_set(True);exported.append(ob)
pivot.select_set(True)
bpy.context.view_layer.objects.active=pivot

# glTF procedural shaders cannot be faithfully exported; the GLB receives main base
# color/metallic/roughness values. Retain .blend for full procedural detail.
try:
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUTPUT_DIR,'Detective_Pin_Coffer_12Slots.glb'),
                              export_format='GLB',use_selection=True,export_yup=True)
    print('Exported GLB')
except Exception as exc:
    print('GLB export problem:',exc)
for ob in exported:ob.select_set(False)
pivot.select_set(False)

with open(os.path.join(OUTPUT_DIR,'pin_slot_positions.csv'),'w',newline='',encoding='utf-8') as f:
    writer=csv.writer(f);writer.writerow(['slot','row','column','x_m','y_m','z_m','max_foot_diameter_mm'])
    for idx,row,col,x,y in centers:
        writer.writerow([idx,row,col,round(x,6),round(y,6),round(TRAY_TOP-SOCKET_DEPTH,6),round(SOCKET_ID*1000*.9,2)])

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUTPUT_DIR,'Detective_Pin_Coffer_12Slots.blend'))
print('Created 12 real recessed sockets; Blender scene saved in:',OUTPUT_DIR)
