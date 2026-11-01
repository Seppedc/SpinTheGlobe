import json,struct
from pathlib import Path
from PIL import Image,ImageOps
from io import BytesIO

source=Path.home()/'Downloads/image__pixal3d__20261030-000307.glb';data=source.read_bytes();n,kind=struct.unpack_from('<II',data,12);doc=json.loads(data[20:20+n]);offset=20+n+8
out=Path(__file__).resolve().parent/'Textures';out.mkdir(parents=True,exist_ok=True)
textures=[]
for spec in doc['images']:
    view=doc['bufferViews'][spec['bufferView']];start=offset+view.get('byteOffset',0)
    textures.append(Image.open(BytesIO(data[start:start+view['byteLength']])).convert('RGB'))
def image_source(texture):
    if 'source' in texture:return texture['source']
    return texture['extensions']['EXT_texture_webp']['source']
pbr=doc['materials'][0]['pbrMetallicRoughness'];base=image_source(doc['textures'][pbr['baseColorTexture']['index']]);mr=image_source(doc['textures'][pbr['metallicRoughnessTexture']['index']])
textures[base].save(out/'PinBox_BaseColor.png');r,g,b=textures[mr].split();zero=Image.new('L',g.size,0)
Image.merge('RGBA',(b,zero,zero,ImageOps.invert(g))).save(out/'PinBox_MetallicSmoothness.png')
(out.parent/'TextureReport.json').write_text(json.dumps({'base_color_dimensions':textures[base].size,'metallic_smoothness_dimensions':g.size,'packing':'glTF B metallic -> URP R; 1-G roughness -> URP A','normal_map_supplied':False},indent=2))
print('Extracted original GLB textures; normal map was not supplied.')
