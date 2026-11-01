"""Deterministic tabletop pin toy. Local source; no shared generator dependencies.
Metric, +Y launch direction. Pull assembly translates along Y; no pin proxy supplied.
"""
from pathlib import Path
import os, math, shutil
import bpy
from mathutils import Vector
SLUG='PinLauncher'
def material(name,color,metal,rough):
    m=bpy.data.materials.new(name); m.use_nodes=True
    p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    return m
def finish(o,name,mat):
    o.name=name;o.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    bevel=o.modifiers.new('Rounded crafted edges','BEVEL');bevel.width=.0012;bevel.segments=3
    o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
    for c in list(o.users_collection):c.objects.unlink(o)
    collection.objects.link(o);o.parent=root
    return o
def box(name,pos,size,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.dimensions=size
    return finish(o,name,mat)
def cylinder(name,pos,radius,depth,mat,rotation=(0,0,0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=radius,depth=depth,location=pos,rotation=rotation)
    return finish(bpy.context.object,name,mat)
def build():
    global root,collection
    # Only factory scene primitives are removed, in an isolated background process.
    for o in list(bpy.context.scene.objects):bpy.data.objects.remove(o,do_unlink=True)
    scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
    collection=bpy.data.collections.new('Collection_'+SLUG);scene.collection.children.link(collection)
    root=bpy.data.objects.new('ROOT_'+SLUG,None);collection.objects.link(root)
    wood=material('PL_Mahogany',(.105,.035,.018),0,.38)
    brass=material('PL_AgedBrass',(.48,.29,.09),.82,.3)
    green=material('PL_GreenEnamel',(.018,.16,.095),.08,.24)
    rubber=material('PL_RubberFeet',(.015,.018,.016),0,.8)
    steel=material('PL_SpringSteel',(.33,.38,.4),.85,.27)
    ivory=material('PL_IvoryMarks',(.76,.63,.38),0,.48)
    for x in [-.073,.073]:
        for y in [-.112,.112]:cylinder('Foot_%s_%s'%(x,y),(x,y,.004),.012,.008,rubber)
    box('Mahogany_Platform',(0,0,.023),(.18,.28,.03),wood)
    box('Brass_Plinth',(0,0,.04),(.174,.274,.004),brass)
    box('Enamel_Deck',(0,0,.045),(.164,.264,.006),green)
    # Wide open launch lane, with no enclosed barrel or firearm grip.
    box('Open_Tray_Floor',(0,.034,.061),(.058,.175,.01),wood)
    for x in [-.035,.035]:
        box('Tray_Rail_'+str(x),(x,.034,.073),(.009,.175,.026),brass)
        box('Rail_Enamel_'+str(x),(x,.034,.087),(.01,.175,.003),green)
    for x in [-.066,.066]:
        for y in [-.11,.11]:
            cylinder('Deck_Rivet_%s_%s'%(x,y),(x,y,.05),.003,.003,brass)
    box('Spring_Backstop',(0,-.086,.074),(.075,.012,.043),brass)
    cylinder('Plunger_Rod',(0,-.06,.079),.003,.13,steel,(math.pi/2,0,0))
    # A true continuous coil rather than stacked decorative rings.
    curve=bpy.data.curves.new('Spring_Coil_Geometry','CURVE');curve.dimensions='3D';curve.bevel_depth=.0014;curve.bevel_resolution=3
    spline=curve.splines.new('POLY');spline.points.add(360)
    for i,p in enumerate(spline.points):
        t=i/360;p.co=(.010*math.cos(t*math.tau*9),-.076+t*.063,.079+.010*math.sin(t*math.tau*9),1)
    coil=bpy.data.objects.new('Exposed_Compression_Spring',curve);collection.objects.link(coil)
    bpy.ops.object.select_all(action='DESELECT');coil.select_set(True);bpy.context.view_layer.objects.active=coil
    bpy.ops.object.convert(target='MESH');finish(bpy.context.object,'Exposed_Compression_Spring',steel)
    moving=bpy.data.objects.new('Plunger_Translation_Pivot',None);collection.objects.link(moving);moving.parent=root;moving.location=(0,-.012,.079)
    for o in [box('Pin_Pusher',(0,-.009,.079),(.05,.012,.025),green),box('Pull_Tab',(0,-.124,.079),(.065,.014,.025),wood),bpy.data.objects['Plunger_Rod']]:
        world=o.matrix_world.copy();o.parent=moving;o.matrix_world=world
    # Decorative star badge on the broad deck conveys a clockwork tabletop toy.
    cylinder('Toy_Badge',(0,.112,.052),.017,.003,brass)
    verts=[]
    for i in range(10):
        a=i*math.pi/5;r=.012 if i%2==0 else .005
        verts.append((r*math.sin(a),.112+r*math.cos(a),.054))
    mesh=bpy.data.meshes.new('StarBadge');mesh.from_pydata(verts,[],[list(range(10))]);mesh.update()
    o=bpy.data.objects.new('Five_Point_Toy_Star',mesh);collection.objects.link(o);o.parent=root;o.data.materials.append(green)
    for i in range(5):box('Power_Mark_'+str(i),(.058,-.05+i*.018,.05),(.012,.002,.002),ivory)
    output=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT',Path(__file__).parent)).resolve();output.mkdir(parents=True,exist_ok=True)
    source=Path(__file__).read_text(encoding='utf-8');text=bpy.data.texts.new('GENERATE_'+SLUG+'.py');text.write(source)
    if output/Path(__file__).name != Path(__file__).resolve():(output/Path(__file__).name).write_text(source,encoding='utf-8')
    bpy.context.view_layer.update()
    bpy.ops.wm.save_as_mainfile(filepath=str(output/(SLUG+'.blend')))
    bpy.ops.object.select_all(action='DESELECT')
    for o in collection.objects:o.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(output/(SLUG+'.fbx')),use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
if __name__=='__main__':build()
