from pathlib import Path
import json, math
from PIL import Image, ImageDraw, ImageFont

base=Path(__file__).resolve().parent
out=base.parents[1]/'SpinTheGlobe/Assets/DetectiveDesk/Globe/Textures'
out.mkdir(parents=True,exist_ok=True)
w,h=2048,1024
im=Image.new('RGB',(w,h),(27,43,47));d=ImageDraw.Draw(im)
def project(p):return ((p[0]+180)/360*w,(90-p[1])/180*h)
data=json.loads((base/'ne_110m_land.geojson').read_text())
for f in data['features']:
    g=f['geometry'];polys=g['coordinates'] if g['type']=='MultiPolygon' else [g['coordinates']]
    for poly in polys:d.polygon([project(p) for p in poly[0]],fill=(117,112,75),outline=(174,151,94),width=2)
for lon in range(-180,181,30):
    x=(lon+180)/360*w;d.line((x,0,x,h),fill=(70,83,74),width=1)
for lat in range(-60,61,30):
    y=(90-lat)/180*h;d.line((0,y,w,y),fill=(70,83,74),width=1)
im.save(out/'GlobeCartography.png')

size=1024;c=size/2
im=Image.new('RGB',(size,size),(216,205,166));d=ImageDraw.Draw(im)
font=ImageFont.truetype('C:/Windows/Fonts/georgiab.ttf',85)
small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',46)
for i in range(51):
    a=math.radians(225-i/50*270);outer=435;inner=370 if i%10==0 else (390 if i%5==0 else 410)
    d.line((c+math.cos(a)*inner,c-math.sin(a)*inner,c+math.cos(a)*outer,c-math.sin(a)*outer),fill=(31,34,31),width=9 if i%10==0 else 4)
for i in range(6):
    a=math.radians(225-i/5*270)
    d.text((c+math.cos(a)*303,c-math.sin(a)*303),str(i),font=font,fill=(29,32,30),anchor='mm')
d.text((c,660),'RPM',font=font,fill=(43,40,30),anchor='mm')
d.text((c,742),'CLOCKWORK',font=small,fill=(85,73,51),anchor='mm')
d.ellipse((52,52,972,972),outline=(102,87,58),width=4)
im.save(out/'RPMGaugeFace.png')
print('Saved globe cartography and analog RPM dial:',out)
