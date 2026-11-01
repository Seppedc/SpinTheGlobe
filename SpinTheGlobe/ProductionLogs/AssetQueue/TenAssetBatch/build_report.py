from pathlib import Path
import hashlib, html, json, os
from urllib.parse import quote

ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
queue=json.loads((ROOT/'asset_queue.json').read_text())
slugs=['FabricLeatherPBR','MechanicalAnim','InteractionAnim','CityBackdrop','FloorBackground',
       'CollectibleStatues','KintsugiMaterial','JewelerHammer','WhiskeyGlass','WhiskeyBottle']
limits={
 'FabricLeatherPBR':'Four material variants; final sheen and texture scale need review.',
 'MechanicalAnim':'Standalone clips and demo; production-rig behavior and gameplay integration are unverified.',
 'InteractionAnim':'Endpoint and sampled-pose checks passed; screenshot does not demonstrate production-model motion.',
 'CityBackdrop':'Standalone texture; window placement and final lighting need review.',
 'FloorBackground':'Model candidate; surface finish and gameplay-camera fit need review.',
 'CollectibleStatues':'Modular bust candidates; source proportions and materials need review.',
 'KintsugiMaterial':'Gold seam material; emission keyword was subsequently removed and glow remains unverified.',
 'JewelerHammer':'Standalone model; final finish and gameplay integration need review.',
 'WhiskeyGlass':'Transparent-alpha correction shows fill and ice; physical refraction remains unverified.',
 'WhiskeyBottle':'Model and preview passed after export correction; final glass response needs review.'}
def link(path):return quote(os.path.relpath(ROOT/path,OUT).replace('\\','/'),safe='/.-_')
records=[];cards=[]
assert queue['run_state']=='Stopped' and queue['active_worker'] is None
for slug in slugs:
    asset=next(x for x in queue['assets'] if x['slug']==slug)
    assert asset['status']=='Needs Review' and asset['validated'] and not asset['human_approved']
    folder=ROOT/asset['output_path'];review=json.loads((folder/'unity-validation.json').read_text())
    assert review['status']=='PASS' and review['console_errors']==0 and review['protected_scene_unchanged']
    picture=ROOT/review['screenshot_path']
    assert hashlib.sha256(picture.read_bytes()).hexdigest()==review['screenshot_sha256']
    report=ROOT/asset['report_path'];assert report.is_file()
    record={key:asset[key] for key in ['id','slug','name','pipeline','priority','status','attempt_count','source_path','output_path','screenshot_path','report_path']}
    record['review_limitation']=limits[slug]
    if (folder/'blender-validation.json').exists():
        geometry=json.loads((folder/'blender-validation.json').read_text())
        record['mesh_count']=geometry['mesh_count'];record['triangles']=geometry['triangles']
    records.append(record)
    cards.append(f'''<article><a href="{link(review['screenshot_path'])}"><img loading="lazy" src="{link(review['screenshot_path'])}" alt="{html.escape(asset['name'])} isolated Unity preview"></a>
<div class="details"><small>{html.escape(asset['id'])} · {html.escape(asset['pipeline'])}</small><h2>{html.escape(asset['name'])}</h2><b>Needs Review</b>
<p>{html.escape(limits[slug])}</p><a href="{link(asset['report_path'])}">Read asset report</a> · <a href="{link(asset['output_path'])}/">Open output folder</a></div></article>''')
summary={'status':'Stopped','requested_groups':10,'verified_groups':len(records),'human_approved':False,
         'notes':['Technical evidence passed; no human visual approval or gameplay integration claimed.',
                  'Usage interruption and asset-specific retries retained in queue logs.',
                  'Kintsugi keyword change audited without restoring older bytes.',
                  'Whiskey glass material corrected and re-reviewed with a single writer lock.'],
         'assets':records}
(OUT/'results.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>DetectiveDesk — ten-asset review batch</title><style>
body{margin:0;background:#171719;color:#e7dfd0;font:16px/1.5 system-ui,sans-serif}main{max-width:1380px;margin:auto;padding:36px 24px}
h1{font-size:32px;margin:0 0 8px}header p{max-width:850px;color:#bfb6a8}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:22px;margin-top:28px}
article{background:#232225;border:1px solid #454039;border-radius:10px;overflow:hidden}img{display:block;width:100%;aspect-ratio:4/3;object-fit:contain;background:#090b0c}.details{padding:18px}h2{margin:5px 0 10px;font-size:21px}b{font-size:13px;color:#e9bd70}small{color:#aaa194}a{color:#d9b978}p{color:#c3bdb4}
</style><main><header><h1>Ten-asset review batch</h1><p>All ten groups passed their recorded automated checks and await human review. The queue is stopped. These are isolated candidates; gameplay placement and integration are not approved.</p></header><section class="grid">'''+''.join(cards)+'''</section></main></html>'''
(OUT/'index.html').write_text(page,encoding='utf-8')
print('PASS: ten verified groups; report and preview gallery generated; queue remains stopped.')
