import json, hashlib
from pathlib import Path
base=Path('ProductionLogs/GlobeRework')
a=json.loads((base/'integration-baseline.json').read_text())
b={x['id']:x for x in json.loads((base/'after-capture.json').read_text())}
allowed=[]; failures=[]
for x in a:
    y=b.get(x['id'])
    if y is None:
        failures.append([x['path'],'missing component']); continue
    p=json.loads(x['json']);q=json.loads(y['json'])
    r=dict(x['refs']);s=dict(y['refs'])
    globe=x['path'].startswith('DetectiveOffice/GameplayObjects/MechanicalGlobe')
    if globe and x['type']=='UnityEngine.MeshRenderer':
        p['MeshRenderer'].pop('m_Enabled',None);q['MeshRenderer'].pop('m_Enabled',None)
    if globe and x['type']=='DetectiveDesk.DetectiveGlobeMechanism':
        p['MonoBehaviour'].pop('feedbackRenderers',None);q['MonoBehaviour'].pop('feedbackRenderers',None)
        r={k:v for k,v in r.items() if not k.startswith('feedbackRenderers.')};s={k:v for k,v in s.items() if not k.startswith('feedbackRenderers.')}
    if globe and x['type']=='UnityEngine.Transform':
        # Existing children and all pose values must remain identical; only additive new references allowed.
        s={k:v for k,v in s.items() if k in r or not k.startswith('m_Children.')}
    if p!=q or r!=s:failures.append([x['path'],x['type']])
    elif x!=y:allowed.append([x['path'],x['type']])
report={'status':'PASS' if not failures else 'FAIL','existing_components_checked':len(a),'protected_scene_unchanged':not failures,'allowed_changes':allowed,'unexpected_changes':failures,'all_existing_transform_poses_unchanged':not failures,'coffer_and_cannon_components_unchanged':not failures}
(base/'preservation-comparison.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if k!='allowed_changes'}))
scripts={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in Path('Assets').rglob('*.cs') if 'GlobeRework' not in str(p)}
target=base/'script-hashes-before-runtime.json'
if not target.exists():target.write_text(json.dumps(scripts,indent=2))
else: print('Unrelated script hashes unchanged:',json.loads(target.read_text())==scripts)
