"""Deterministic walnut tabletop; DD-01 only. No legs or gameplay integration.
Reference: References/SceneApproved.png (art direction only).
"""
from pathlib import Path
import os, math, shutil
import bpy
import numpy as np
HERE=Path(__file__).resolve().parent
OUT=Path(os.environ['DETECTIVEDESK_ASSET_OUTPUT']).resolve()
OUT.mkdir(parents=True,exist_ok=True)
# Isolated factory-startup process only; leave any unrelated objects untouched.
for o in list(bpy.data.objects):
    if o.name in {'Cube','Camera','Light'} or o.name.startswith('DD01_') or o.name=='ROOT_DetectiveDesk': bpy.data.objects.remove(o,do_unlink=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
col=bpy.data.collections.new('Collection_DetectiveDesk');scene.collection.children.link(col)
root=bpy.data.objects.new('ROOT_DetectiveDesk',None);col.objects.link(root)
def material(name,color,metal,rough):
    m=bpy.data.materials.new(name);m.use_nodes=True
    b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=rough
    return m
wood=material('DD01_Walnut',(.12,.047,.021),0,.34)
dark=material('DD01_Endgrain',(.07,.025,.012),0,.4)
brass=material('DD01_AgedBrass',(.36,.23,.085),.82,.32)
# Authored raster maps, no Blender-only procedural dependence. UVs are metric planar.
N=1024
y,x=np.mgrid[0:N,0:N].astype(np.float32);x/=N;y/=N
rng=np.random.default_rng(701)
warp=y+.011*np.sin(x*19)+.005*np.sin(x*43+y*7)
grain=np.sin(warp*850+np.sin(x*15)*2)*.035+np.sin(warp*230+np.sin(x*8)*3)*.055
cloud=.06*np.sin(x*9+y*4)*np.sin(y*17)
noise=rng.normal(0,.009,(N,N))
v=np.clip(.55+grain+cloud+noise,.25,.85)
scratches=np.zeros((N,N),dtype=np.float32)
for i in range(90):
    px=int(rng.integers(20,N-80));py=int(rng.integers(10,N-10));length=int(rng.integers(8,70))
    scratches[py,px:px+length]=rng.uniform(.025,.09)
v+=scratches
texdir=OUT/'textures';texdir.mkdir(exist_ok=True)
def image(name,rgb):
    im=bpy.data.images.new(name,width=N,height=N,alpha=True)
    rgba=np.ones((N,N,4),dtype=np.float32);rgba[:,:,:3]=rgb
    im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(texdir/(name+'.png'));im.file_format='PNG';im.save();im.pack();return im
base=image('DD01_Walnut_BaseColor',v[:,:,None]*np.array([.38,.19,.09],dtype=np.float32))
rough=image('DD01_Walnut_Roughness',np.repeat(np.clip(.34+grain*.8+scratches,.24,.55)[:,:,None],3,axis=2));rough.colorspace_settings.name='Non-Color'
bs=wood.node_tree.nodes.get('Principled BSDF')
for im,socket in [(base,'Base Color'),(rough,'Roughness')]:
    n=wood.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;wood.node_tree.links.new(n.outputs['Color'],bs.inputs[socket])
def box(name,at,dim,mat,bevel):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name='DD01_'+name;o.dimensions=dim
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    col.objects.link(o);o.parent=root;o.data.materials.append(mat)
    # XY projection gives continuous long grain over the top; side UVs are separate.
    for p in o.data.polygons:
        for li in p.loop_indices:
            co=o.data.vertices[o.data.loops[li].vertex_index].co
            if abs(p.normal.z)>.5: uv=((co.x+at[0])/2.8+.5,(co.y+at[1])/1.15+.5)
            elif abs(p.normal.y)>.5:uv=((co.x+at[0])/2.8+.5,(co.z+at[2])/.105)
            else:uv=((co.y+at[1])/1.15+.5,(co.z+at[2])/.105)
            o.data.uv_layers.active.data[li].uv=uv
    if bevel:
        m=o.modifiers.new('Crafted edge radius','BEVEL');m.width=bevel;m.segments=4
        o.modifiers.new('Face weighted normals','WEIGHTED_NORMAL')
    return o
box('Walnut_ThickTop',(0,0,.0575),(2.8,1.15,.095),wood,.012)
box('Lower_OgeeStep',(0,0,.0075),(2.75,1.10,.015),dark,.005)
# Fine metal band is inset below the wood perimeter, leaving a generous work surface.
for s in [-1,1]:
    box('Brass_LongBorder_'+str(s),(0,s*.538,.105),(2.64,.006,.002),brass,.001)
    box('Brass_EndBorder_'+str(s),(s*1.317,0,.105),(.006,1.07,.002),brass,.001)
    box('Brass_Fascia_'+str(s),(0,s*.573,.053),(2.76,.002,.006),brass,.0006)
# No moving parts. Root origin is the underside, centered in XY.
source=Path(__file__).read_text(encoding='utf-8-sig')
t=bpy.data.texts.new('GENERATE_DetectiveDesk.py');t.write(source)
(OUT/'build_asset.py').write_text(source,encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DetectiveDesk.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in col.objects:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(OUT/'DetectiveDesk.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True)
print('DD-01 tabletop build finished')

