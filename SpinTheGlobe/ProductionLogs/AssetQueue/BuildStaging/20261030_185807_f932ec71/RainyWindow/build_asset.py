"""RainyWindow: deterministic six-light mahogany frame; no glass or rain shader.
Run in an isolated empty Blender process. Output override is mandatory for queue staging.
Reference: References/NoirLighting.png, art direction only. No downloaded textures.
"""
from pathlib import Path
import os
import bpy
HERE=Path(__file__).resolve().parent

def build():
    out=Path(os.environ['DETECTIVEDESK_ASSET_OUTPUT']).resolve()
    out.mkdir(parents=True,exist_ok=True)
    # Only remove our own generated collection; unrelated scene content is untouched.
    old=bpy.data.collections.get('Collection_RainyWindow')
    if old:
        for obj in list(old.objects): bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(old)
    col=bpy.data.collections.new('Collection_RainyWindow')
    bpy.context.scene.collection.children.link(col)
    root=bpy.data.objects.new('ROOT_RainyWindow',None);col.objects.link(root)
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.context.scene.unit_settings.scale_length=1
    materials={}
    for name,color,rough in [('Mahogany',(0.085,0.032,0.018,1),.34),('Recess',(0.025,0.012,0.009,1),.5),('EdgeWood',(0.13,0.054,0.025,1),.32)]:
        m=bpy.data.materials.new('RainyWindow_'+name);m.use_nodes=True
        bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color
        bs.inputs['Roughness'].default_value=rough;m.diffuse_color=color;materials[name]=m
    meshes=[]
    def box(name,pos,size,mat='Mahogany',bevel=.003):
        bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
        o=bpy.context.object;o.name=name;o.dimensions=size
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
        for c in list(o.users_collection):c.objects.unlink(o)
        col.objects.link(o);o.parent=root;o.data.materials.append(materials[mat])
        mod=o.modifiers.new('Joinery edge easing','BEVEL');mod.width=bevel;mod.segments=3
        o.modifiers.new('Face normals','WEIGHTED_NORMAL');meshes.append(o)
    # Envelope 1.36 x .30 x 1.84 m. Front faces toward -Y; base Z=0.
    for side,x in [('Left',-.60),('Right',.60)]:
        box('Jamb_'+side,(x,.055,.95),(.12,.19,1.70))
        box('Casing_'+side,(x,-.045,.96),(.14,.045,1.72),'EdgeWood')
        box('Rebate_'+side,(x*.89,-.018,.95),(.018,.08,1.56),'Recess',.001)
        box('CasingBead_'+side,(x,-.072,.96),(.075,.018,1.70))
    box('Head',(0,.055,1.77),(1.08,.19,.14))
    box('HeadCasing',(0,-.045,1.78),(1.34,.045,.12),'EdgeWood')
    box('HeadBead',(0,-.072,1.78),(1.30,.018,.055))
    box('BottomRail',(0,.045,.14),(1.08,.17,.12))
    box('Sill',(0,-.005,.055),(1.36,.30,.11),'EdgeWood',.008)
    box('SillNose',(0,-.132,.065),(1.34,.035,.055),'Mahogany',.008)
    box('Apron',(0,-.045,.135),(1.20,.035,.05))
    box('MullionVertical',(0,.015,.95),(.045,.09,1.50))
    for i,z in enumerate((.70,1.20)):
        for side,x in [('Left',-.27375),('Right',.27375)]:
            box('MullionHorizontal_%d_%s'%(i,side),(x,.015,z),(.5025,.09,.04))
    # Narrow facing beads define pane edges without fabricating glass.
    box('MullionFace',(0,-.037,.95),(.019,.014,1.50),'EdgeWood',.001)
    for i,z in enumerate((.70,1.20)):
        box('CrossbarFace_'+str(i),(0,-.037,z),(1.05,.014,.016),'EdgeWood',.001)
    txt=bpy.data.texts.get('GENERATE_RainyWindow.py') or bpy.data.texts.new('GENERATE_RainyWindow.py')
    source=Path(__file__).read_text(encoding='utf-8');txt.clear();txt.write(source)
    (out/'build_asset.py').write_text(source,encoding='utf-8')
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'RainyWindow.blend'))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [root]+meshes:obj.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(out/'RainyWindow.fbx'),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)

if __name__=='__main__':
    build()
