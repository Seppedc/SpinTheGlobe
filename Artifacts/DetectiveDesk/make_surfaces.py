from pathlib import Path
import json, math, random
from PIL import Image, ImageDraw, ImageFont

base = Path(__file__).resolve().parent
out = base.parent.parent / 'SpinTheGlobe/Assets/DetectiveDesk/Generated/Refined'
out.mkdir(parents=True, exist_ok=True)
random.seed(29)
w,h = 2048,900
im=Image.new('RGB',(w,h)); p=im.load()
for y in range(h):
    for x in range(w):
        edge=min(x,y,w-1-x,h-1-y)
        tone=int(5*math.sin(x*.003)*math.sin(y*.009)) + random.randint(-2,2)-int(max(0,30-edge)*.3)
        p[x,y]=(223+tone,204+tone,165+tone)
d=ImageDraw.Draw(im)
ink=(99,91,65); faint=(194,180,145)
d.rectangle((22,22,w-23,h-23),outline=ink,width=3)
d.rectangle((31,31,w-32,h-32),outline=faint,width=2)
def xy(lon,lat): return (88+(lon+180)/360*(w-176),135+(85-lat)/145*(h-220))
for lon in range(-180,181,30):
    a,b=xy(lon,85),xy(lon,-60); d.line((a,b),fill=faint,width=1)
for lat in range(-60,86,30): d.line((xy(-180,lat),xy(180,lat)),fill=faint,width=1)
data=json.loads((base/'ne_110m_land.geojson').read_text())
for feat in data['features']:
    geom=feat['geometry']; polys=geom['coordinates'] if geom['type']=='MultiPolygon' else [geom['coordinates']]
    for poly in polys:
        ring=poly[0]
        if max(pt[1] for pt in ring)<-60: continue
        coords=[xy(lon,max(-60,lat)) for lon,lat in ring]
        d.polygon(coords,fill=(155,151,112),outline=ink,width=2)
font=ImageFont.truetype('C:/Windows/Fonts/georgia.ttf',36)
small=ImageFont.truetype('C:/Windows/Fonts/georgia.ttf',18)
d.text((w/2,62),'W O R L D   A T L A S',font=font,fill=ink,anchor='mm')
d.text((w/2,h-51),'C A R T O G R A P H I C   S U R V E Y',font=small,fill=ink,anchor='mm')
im.save(out/'ParchmentWorldMap.png')
# Subdued longitudinal mahogany grain; generated separately to keep backup materials intact.
w,h=1024,512
wood=Image.new('RGB',(w,h));p=wood.load()
for y in range(h):
    for x in range(w):
        grain=math.sin(y*.65+math.sin(x*.008)*2)+.4*math.sin(y*2.4+x*.004)
        board=-7 if y%128<3 else 0
        v=int(grain*3)+random.randint(-1,1)+board
        p[x,y]=(max(0,83+v),max(0,37+v//2),max(0,24+v//3))
wood.save(out/'MahoganyGrain.png')
(out/'CartographySource.txt').write_text('World map land outlines: Natural Earth 1:110m land, public domain.\nSource: https://github.com/nvkelso/natural-earth-vector/blob/master/geojson/ne_110m_land.geojson\nParchment and mahogany surfaces drawn procedurally.\n')
print(out)
