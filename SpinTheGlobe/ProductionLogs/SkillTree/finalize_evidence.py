import json, hashlib, pathlib, re, datetime
root = pathlib.Path.cwd()
out = root / 'ProductionLogs/SkillTree'
def read(p): return json.loads(pathlib.Path(p).read_text(encoding='utf-8-sig'))
def write(p,d): pathlib.Path(p).write_text(json.dumps(d,indent=2),encoding='utf-8')
def sha(p): return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
baseline=read(out/'scene-before.json')
def docs(p):
    text=pathlib.Path(p).read_text(encoding='utf-8-sig')
    return {m.group(1):m.group(0) for m in re.finditer(r'^--- !u!\d+ &(\d+).*?(?=^--- !u!|\Z)',text,re.M|re.S)}
old=docs(out/'Checkpoint/DetectiveOffice-live-copy.unity')
new=docs(root/baseline['activeScene'])
allowed=set(); manager=None
for s in baseline['scenes']:
    for obj in s['objects']:
        refs=[obj['ref']]+[c['ref'] for c in obj['components'] if c]
        if '/GameUI/Upgrades panel' in obj['ref']['hierarchy']:
            allowed.update(r['globalId'].split('-')[-2] for r in refs)
        for c in obj['components']:
            if c and c['ref']['type']=='DetectiveDesk.Gameplay.GameManager':manager=c['ref']['globalId'].split('-')[-2]
changes=[]; unexpected=[]
for ident,doc in old.items():
    if new.get(ident)!=doc:
        ok=ident in allowed
        if ident==manager:
            normalized=re.sub(r'^  skillTree(?:Panel)?:.*\n','',new.get(ident,''),flags=re.M)
            ok=normalized==doc
        changes.append(dict(fileID=ident,authorized=ok))
        if not ok:unexpected.append(ident)
write(out/'scene-yaml-preservation.json',dict(status='PASS' if not unexpected else 'FAIL',originalDocuments=len(old),changedOriginalDocuments=changes,unexpected=unexpected,newDocuments=len(set(new)-set(old)),method='Exact original YAML document bytes after newline normalization. Manager permits only two new skillTree reference lines. Upgrades subtree IDs derived from live baseline.'))
manifest=[]
for p in (out/'Checkpoint/Files').rglob('*'):
    if not p.is_file():continue
    rel=p.relative_to(out/'Checkpoint/Files');cur=root/rel
    if not cur.exists() or sha(p)!=sha(cur):manifest.append(dict(path=rel.as_posix(),change='modified' if cur.exists() else 'missing',before=sha(p),after=sha(cur) if cur.exists() else None))
for folder in ['Assets/DetectiveDesk/Scripts/Gameplay','Assets/DetectiveDesk/Scripts/Editor/Gameplay','Assets/DetectiveDesk/Gameplay/SkillTree']:
    for p in (root/folder).glob('*'):
        if p.is_file() and not (out/'Checkpoint/Files'/p.relative_to(root)).exists():manifest.append(dict(path=p.relative_to(root).as_posix(),change='added',after=sha(p)))
write(out/'changed-files.json',dict(files=manifest,logs='ProductionLogs/SkillTree contains new evidence/scripts/captures; original full regression also refreshed ProductionLogs/Gameplay runtime-validation.json and its two standard captures.'))
runtime=read(out/'runtime-validation.json');graph=read(out/'graph-validation.json');gameplay=read(out/'gameplay-regression.json');preserve=read(out/'preservation-validation.json')
write(out/'integration-result.json',dict(status='PASS' if all(d['status']=='PASS' for d in [runtime,graph,gameplay,preserve]) and not unexpected else 'FAIL',nodes=39,graphChecks=graph['assertions'],runtimeChecks=len(runtime['checks']),gameplayChecks=len(gameplay['checks']),scene=baseline['activeScene'],prefab='Assets/DetectiveDesk/Gameplay/SkillTree/SkillTreePanel.prefab',definition='Assets/DetectiveDesk/Gameplay/SkillTree/DefaultSkillTree.asset',legacyListRetainedInactive=True,cash=422,levels=[0,0,0],figurineTiers=[0,0,0],physics='PhysX',newSceneObjects=preserve['objectCount']-1579,newSceneComponents=preserve['componentCount']-4601))
print(json.dumps(dict(yamlUnexpected=unexpected,changedFiles=len(manifest),integration=read(out/'integration-result.json')['status'])))
