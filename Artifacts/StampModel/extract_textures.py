import json,struct
from pathlib import Path
from PIL import Image,ImageOps
from io import BytesIO

source=Path.home()/'Downloads/image_20k.glb';data=source.read_bytes();length,kind=struct.unpack_from('<II',data,12);doc=json.loads(data[20:20+length]);bin_offset=20+length+8
out=Path(__file__).resolve().parent/'Textures';out.mkdir(parents=True,exist_ok=True)
images={}
for spec in doc['images']:
    view=doc['bufferViews'][spec['bufferView']];start=bin_offset+view.get('byteOffset',0)
    image=Image.open(BytesIO(data[start:start+view['byteLength']])).convert('RGB');images[spec['name']]=image
images['retopo_albedo'].save(out/'Stamp_BaseColor.png');images['detail_normal'].save(out/'Stamp_Normal.png')
mr=images['detail_mr'];r,g,b=mr.split();zero=Image.new('L',mr.size,0)
Image.merge('RGBA',(b,zero,zero,ImageOps.invert(g))).save(out/'Stamp_MetallicSmoothness.png')
(out.parent/'TextureReport.json').write_text(json.dumps({'source_maps':{name:list(image.size) for name,image in images.items()},'packing':'glTF B metallic -> URP R; 1 - glTF G roughness -> URP A; no albedo/normal repainting','missing_textures':[]},indent=2))
print('Extracted all three supplied PBR maps; packed URP metallic/smoothness.')
