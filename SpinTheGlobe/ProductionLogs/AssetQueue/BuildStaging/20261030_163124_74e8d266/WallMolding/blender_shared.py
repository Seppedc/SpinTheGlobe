"""DetectiveDesk procedural blockout/low-detail starter library. Requires Blender bpy.
Geometry is intentionally editable concept geometry, not a production photorealistic hero mesh.
"""
import bpy, math, os, json, random, shutil
from mathutils import Vector
from pathlib import Path

PALETTE={
'wood':('#3A2017',0.0,.36),'woodDark':('#20130F',0.0,.45),
'brass':('#98713C',.92,.32),'brassLight':('#C59B59',.95,.2),
'velvet':('#4A1723',0.0,.87),'stone':('#383942',.03,.75),
'enamel':('#9A172A',.03,.17),'paper':('#C7AD7C',0.0,.88),
'glass':('#678F82',0.0,.12),'whiskey':('#A65B1E',0.0,.22),
'black':('#131720',0.0,.6),'steel':('#788089',.82,.3),'green':('#174F3D',.0,.19),
'ice':('#DDECE9',0.0,.08),'red':('#9E2929',0.0,.4)}

def mat(name):
    key='DD_'+name
    if key in bpy.data.materials: return bpy.data.materials[key]
    hexval,metal,rough=PALETTE[name]
    rgb=tuple(int(hexval[i:i+2],16)/255 for i in (1,3,5))
    m=bpy.data.materials.new(key);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*rgb,1)
    bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
    if name in ('glass','ice'):
       bs.inputs['Transmission Weight'].default_value=.75
       bs.inputs['IOR'].default_value=1.46
    if name=='velvet':
       bs.inputs['Sheen Weight'].default_value=.5
    return m

def assign(o,m):
    o.data.materials.append(mat(m));return o

def cube(name,at,dim,material,bev=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=name
    o.dimensions=dim;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bev:
       mod=o.modifiers.new('Soft machined edges','BEVEL');mod.width=min(bev,min(dim)/3);mod.segments=3
       mod2=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return assign(o,material)

def cylinder(name,at,r,depth,material,vertices=40,rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=at);o=bpy.context.object;o.name=name
    if rotation:o.rotation_euler=rotation
    m=o.modifiers.new('Bevel machined rim','BEVEL');m.width=min(.006,r/5,depth/5);m.segments=2
    o.modifiers.new('Normals','WEIGHTED_NORMAL');return assign(o,material)

def torus(name,at,major,minor,material,rotation=None):
    bpy.ops.mesh.primitive_torus_add(major_radius=major,minor_radius=minor,major_segments=64,minor_segments=12,location=at)
    o=bpy.context.object;o.name=name
    if rotation:o.rotation_euler=rotation
    return assign(o,material)

def sphere(name,at,r,material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,radius=r,location=at)
    o=bpy.context.object;o.name=name;bpy.ops.object.shade_smooth();return assign(o,material)

def curve_between(name,a,b,r,material):
    v=Vector(b)-Vector(a);mid=(Vector(a)+Vector(b))/2
    o=cylinder(name,mid,r,v.length,material,rotation=(0,0,0));o.rotation_euler=v.to_track_quat('Z','Y').to_euler();return o

def ring_socket(prefix,x,y,z,outer=.012,inner=.009,depth=.008):
    torus(prefix+'_Rim',(x,y,z),outer,.0016,'brassLight')
    # sleeve has an empty center; recessed floor visibly below the ring
    torus(prefix+'_InnerLip',(x,y,z-depth*.4),inner,.0013,'brass')
    cylinder(prefix+'_RecessFloor',(x,y,z-depth),inner, .0014,'woodDark')

def paper(name,x,y,z,w=.15,d=.1):return cube(name,(x,y,z),(w,d,.0017),'paper',.001)

def make_asset(slug):
    """Distinct deterministic geometry blocks. Everything stays separate for later refinement."""
    if slug=='DetectiveDesk':
        cube('Mahogany_ThickTop',(0,0,.75),(2.8,1.15,.09),'wood',.018)
        for xx in [-1.32,1.32]:
          cube('Brass_EndInlay',(xx,0,.8),(.012,1.07,.002),'brass')
        for yy in [-.54,.54]:cube('Brass_FrontRearEdge',(0,yy,.795),(2.72,.009,.005),'brass')
        for i in range(7): cube('WoodPlankSeam_'+str(i),(-1.1+i*.36,0,.798),(.001,1.04,.0006),'woodDark')
    elif slug=='DeskStructure':
        for x in [-1.27,1.27]:
          for y in [-.45,.45]:cube('CarvedLeg', (x,y,.37),(.14,.14,.74),'wood',.015)
        for y in [-.52,.52]:cube('Apron', (0,y,.64),(2.5,.05,.18),'woodDark',.015)
        for x in [-1.27,1.27]:cube('SideApron',(x,0,.64),(.06,.9,.18),'woodDark',.015)
        for x in [-.9,0,.9]:cube('Panel_'+str(x),(x,-.544,.64),(.72,.015,.12),'wood',.006)
    elif slug=='BackWall':
        cube('WallBacking',(0,0,1.35),(4.5,.12,2.7),'woodDark')
        for i in range(8):cube('InsetWallPanel',(i*.52-1.82,-.067,1.5),(.45,.025,1.9),'wood',.015)
    elif slug=='WallMolding':
        for z in [.15,1.0,2.43]:cube('MoldingHorizontal',(0,-.1,z),(4.4,.065,.055),'brass',.01)
        for x in [-2.0,-1.0,0,1.0,2.0]:cube('MoldingVertical',(x,-.1,1.7),(.032,.055,1.3),'woodDark',.006)
    elif slug=='WorldMapFrame':
        cube('MapInsert_Placeholder_TextureMe',(0,0,0),(.94,.015,.58),'paper')
        for z in [-.31,.31]:cube('FrameHorizontal',(0,-.015,z),(1.05,.065,.045),'woodDark',.006)
        for x in [-.51,.51]:cube('FrameVertical',(x,-.015,0),(.045,.065,.65),'woodDark',.006)
        for x in [-.51,.51]:
          for z in [-.31,.31]:sphere('BrassCornerPin',(x,-.047,z),.01,'brass')
    elif slug=='RainyWindow':
        cube('WindowGlass',(0,0,0),(1.1,.013,1.45),'glass')
        for x in [-.57,0,.57]:cube('WindowVertical',(x,-.022,0),(.045,.06,1.5),'wood',.009)
        for z in [-.76,0,.76]:cube('WindowHorizontal',(0,-.022,z),(1.2,.06,.045),'wood',.009)
        cube('ExteriorSill',(0,-.14,-.77),(1.25,.28,.07),'wood',.015)
    elif slug=='FloorBackground':
        cube('Floor',(0,0,0),(5.0,3.1,.08),'woodDark',.015)
        for x in range(-4,5):cube('PlankGap',(x*.55,0,.043),(.003,3,.001),'black')
    elif slug=='MechanicalGlobe':
        sphere('RotatingWorldSphere',(0,0,.45),.32,'stone')
        for i in range(4):torus('MeridianBand_'+str(i),(0,0,.45),.34,.008,'brass',rotation=(math.pi/2,0,i*math.pi/4))
        cylinder('GlobeStem',(0,0,.13),.045,.3,'brass')
        cylinder('Pedestal',(0,0,.035),.21,.07,'wood',64)
        # Continent medallions are non-geographic placeholder geometry.
        for i in range(9):
            a=i*2.399;sphere('ContinentProxy_'+str(i),(.32*math.cos(a),.32*math.sin(a),.45+.1*math.sin(i)),.035,'brass')
    elif slug=='GlobeGears':
        for i in range(3):
            x=(i-1)*.18;z=.19+i*.07
            cylinder('GearHub_'+str(i),(x,0,z),.07,.022,'brass')
            for j in range(12):
                theta=j*math.tau/12
                cube(f'Gear_{i}_Tooth_{j}',(x+.084*math.cos(theta),.084*math.sin(theta),z),(.019,.02,.027),'brassLight',.002)
        curve_between('Axle',(-.25,0,.19),(.25,0,.19),.014,'steel')
    elif slug=='GlobeControl':
        cylinder('RoundGaugeHousing',(0,0,.18),.125,.04,'brass')
        cylinder('RoundGaugeFace',(0,0,.205),.103,.003,'paper')
        curve_between('GaugeNeedle', (0,0,.21),(.075,.055,.212),.005,'red')
        cylinder('Pedestal',(0,0,.07),.2,.14,'wood')
    elif slug=='PinCoffer':
        cube('Base',(0,0,.017),(.16,.11,.034),'wood',.008)
        cube('VelvetBottom',(0,0,.037),(.143,.094,.004),'velvet',.002)
        for r in range(3):
          for c in range(4):ring_socket('Slot_%02d'%(r*4+c+1),(c-1.5)*.028,(r-1)*.026,.041,.008,.0055,.007)
        cube('Lid',(0,.055,.12),(.16,.009,.11),'wood',.007)
        cube('LidVelvet',(0,.048,.12),(.143,.002,.093),'velvet')
        for x in [-.055,.055]:cylinder('Hinge',(x,.054,.053),.006,.027,'brass',rotation=(math.pi/2,0,0))
    elif slug=='CollectiblePin':
        cylinder('SocketCompatibleFoot',(0,0,.003),.0046,.006,'brass')
        cylinder('PinStem',(0,0,.015),.002,.023,'steel')
        torus('HeadBrassNeck',(0,0,.029),.0058,.001,'brassLight')
        sphere('EnamelHead',(0,0,.036),.0063,'enamel')
    elif slug=='PinLauncher':
        cube('ToyLauncherBase',(0,0,.035),(.24,.14,.07),'wood',.013)
        cylinder('Pivot',(0,0,.11),.04,.09,'brass')
        curve_between('PinLaunchTube',(-.05,0,.14),(.18,0,.23),.025,'brass')
        cylinder('MuzzleRing',(.18,0,.23),.032,.014,'brassLight',rotation=(0,1.19,0))
        cube('PullLever',(-.1,0,.15),(.045,.025,.04),'wood')
    elif slug=='CaseFiles':
        cube('CaseFolder',(0,0,0),(.29,.21,.006),'paper',.002)
        for j in range(5):paper('CaseDocument_'+str(j),.0,j*.007-.016,.009+j*.001,.25,.17)
        for x in [-.08,.08]:cube('FoldedClip',(x,-.082,.016),(.02,.003,.004),'steel')
    elif slug=='SolvedStamp':
        cube('StampPadTray',(.15,0,.01),(.12,.08,.02),'woodDark',.006)
        cube('RedInkPad',(.15,0,.023),(.105,.064,.006),'red')
        cube('StampBase',(0,0,.02),(.085,.055,.03),'brass',.007)
        cylinder('StampNeck',(0,0,.065),.015,.075,'wood')
        sphere('StampKnob',(0,0,.118),.027,'wood')
    elif slug=='StatueSlots':
        for j in range(4):
            x=(j-1.5)*.13
            cylinder('StatuePedestal_%02d'%j,(x,0,.017),.047,.034,'wood',48)
            cylinder('PedestalBrassPlate_%02d'%j,(x,0,.037),.039,.005,'brass',48)
    elif slug=='CollectibleStatues':
        cylinder('BustPlinth',(0,0,.025),.055,.05,'wood')
        sphere('MiniatureHead',(0,0,.21),.06,'stone')
        cylinder('Shoulders',(0,0,.115),.065,.13,'stone')
    elif slug=='JewelerHammer':
        curve_between('MalletWoodHandle',(0,0,.01),(0,0,.2),.011,'wood')
        curve_between('BrassHammerHead',(-.065,0,.19),(.065,0,.19),.026,'brass')
    elif slug=='WhiskeyGlass':
        cylinder('CrystalRocksGlass',(0,0,.055),.046,.11,'glass',8)
        cylinder('AmberWhiskey',(0,0,.034),.039,.06,'whiskey',32)
        cube('SquareIceCube',(0,0,.064),(.035,.035,.035),'ice',.006)
    elif slug=='WhiskeyBottle':
        cylinder('BottleBase',(0,0,.105),.047,.21,'glass',24)
        cylinder('LiquorFill',(0,0,.095),.039,.18,'whiskey',24)
        cylinder('LongNeck',(0,0,.24),.019,.11,'glass',24)
        cylinder('CorkStopper',(0,0,.3),.021,.033,'wood')
    elif slug=='BankersLamp':
        cylinder('LampBase',(0,0,.022),.11,.045,'brass',64)
        curve_between('BentBrassStem',(0,0,.044),(.0,0,.4),.013,'brass')
        cylinder('EmeraldGlassShade',(0,0,.415),.19,.09,'green',64)
        for x in [-.16,.16]:sphere('BrassShadeEnd',(x,0,.42),.013,'brassLight')
        curve_between('PullChain',(.09,0,.4),(.09,0,.24),.0025,'brass')
    else: raise ValueError('Not implemented '+slug)

def _prepare_export_root(slug):
    """Anchor one editable asset at world XY=0 with its lowest point at Z=0.

    Keep each mesh's origin at its meaningful pivot; only the parent asset root is
    required to be at the absolute base. This preserves future lid/gear animation.
    """
    from mathutils import Vector
    bpy.context.view_layer.update()
    mesh_objs=[o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not mesh_objs:
        raise RuntimeError('No mesh geometry was built for '+slug)
    pts=[]
    for o in mesh_objs:
        for corner in o.bound_box:
            pts.append(o.matrix_world @ Vector(corner))
    minx,maxx=min(p.x for p in pts),max(p.x for p in pts)
    miny,maxy=min(p.y for p in pts),max(p.y for p in pts)
    minz=min(p.z for p in pts)
    offset=Vector(((minx+maxx)/2, (miny+maxy)/2, minz))
    for o in mesh_objs:
        o.location -= offset
        # Explicit active selection provides valid context for transform_apply.
        bpy.ops.object.select_all(action='DESELECT')
        o.select_set(True)
        bpy.context.view_layer.objects.active=o
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    collection=bpy.data.collections.new('Collection_'+slug)
    bpy.context.scene.collection.children.link(collection)
    root=bpy.data.objects.new('ROOT_'+slug, None)
    collection.objects.link(root)
    for o in mesh_objs:
        # Move generated geometry into a single collection; preserve visual transforms.
        for old in list(o.users_collection):old.objects.unlink(o)
        collection.objects.link(o)
        world=o.matrix_world.copy()
        o.parent=root
        o.matrix_world=world
    return root,mesh_objs

def save(slug,asset_dir,source_file):
    """Build a deterministic editable .blend and optional Unity-aligned FBX.

    Run in a new Blender process; destructive cleanup is contained to that process.
    PBR materials use Principled slots, with no Blender-only shader dependencies.
    """
    asset_dir=Path(os.environ.get('DETECTIVEDESK_ASSET_OUTPUT') or asset_dir).resolve()
    asset_dir.mkdir(parents=True,exist_ok=True)
    random.seed(0)
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for material in list(bpy.data.materials):
        if material.users == 0:bpy.data.materials.remove(material)
    bpy.context.scene.unit_settings.system='METRIC'
    bpy.context.scene.unit_settings.scale_length=1.0
    make_asset(slug)
    root,objects=_prepare_export_root(slug)
    bpy.context.scene.render.engine='CYCLES'
    bpy.context.scene.cycles.samples=32
    name='GENERATE_'+slug+'.py'
    if name in bpy.data.texts:bpy.data.texts.remove(bpy.data.texts[name])
    txt=bpy.data.texts.new(name)
    txt.write(Path(source_file).read_text(encoding='utf-8'))
    helper=bpy.data.texts.get('GENERATE_blender_shared.py') or bpy.data.texts.new('GENERATE_blender_shared.py')
    helper.clear();helper.write(Path(__file__).read_text(encoding='utf-8'))
    # Retain both real sources beside isolated test outputs as well as embedding them.
    for source_path in (Path(source_file),Path(__file__)):
        destination=asset_dir/source_path.name
        if source_path.resolve()!=destination.resolve():
            if destination.exists() and destination.read_bytes()!=source_path.read_bytes():
                raise RuntimeError('Refusing to replace a different source: '+str(destination))
            if not destination.exists():shutil.copyfile(source_path,destination)
    bpy.ops.wm.save_as_mainfile(filepath=str(asset_dir/(slug+'.blend')))
    # Use explicit object selection and a root empty to export only this asset.
    try:
        bpy.ops.object.select_all(action='DESELECT')
        for obj in [root]+objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=root
        bpy.ops.export_scene.fbx(filepath=str(asset_dir/(slug+'.fbx')),
            use_selection=True, axis_forward='-Z', axis_up='Y',
            apply_unit_scale=True, bake_space_transform=True,
            add_leaf_bones=False)
        print('FBX exported')
    except Exception as e:
        print('FBX export failed, .blend source preserved:',e)
        raise
    return str(asset_dir/(slug+'.blend'))
