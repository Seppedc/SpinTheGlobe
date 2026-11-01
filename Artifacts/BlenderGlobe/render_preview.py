import bpy
from mathutils import Vector
from pathlib import Path
base=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(base/'Export/MechanicalGlobe_Blender.blend'))
scene=bpy.context.scene
for obj in scene.objects:
    obj.hide_render=obj.name not in bpy.data.collections['DetectiveGlobe_Export'].objects
camera_data=bpy.data.cameras.new('AssetReviewCamera');camera=bpy.data.objects.new('AssetReviewCamera',camera_data);scene.collection.objects.link(camera)
camera.location=(-1.12,-5.5,2.12);target=Vector((-1.12,-.33,1.86));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera_data.type='ORTHO';camera_data.ortho_scale=2.4;scene.camera=camera
for name,pos,power,color,size in [('WarmKey',(-3,-3.5,4.5),650,(1,.77,.50),3),('CoolFill',(2,-2.5,3),400,(.45,.65,1),3),('Rim',(-1,2,4),750,(1,.65,.3),2)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.color=color;data.shape='DISK';data.size=size
    obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=pos;obj.rotation_euler=(target-obj.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(base/'Blender_ModelPreview.png')
bpy.ops.render.render(write_still=True)
