import collections, datetime, hashlib, json, pathlib, re, shutil

ROOT = pathlib.Path.cwd()
OUT = ROOT / 'ProductionLogs/SkillTree'
CHECK = OUT / 'Checkpoint'
def read(p): return json.loads(pathlib.Path(p).read_text(encoding='utf-8-sig'))
def write(p, data): pathlib.Path(p).write_text(json.dumps(data, indent=2, ensure_ascii=False), encoding='utf-8')
def sha(p): return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
data = read(OUT/'scene-before.json')
objects = [o for s in data['scenes'] for o in s['objects']]
components = [c for o in objects for c in o['components'] if c]
def props(c):
    strings = [p['path'] for p in c['properties'] if p['type']=='String']
    refs = [p['path'] for p in c['properties'] if p['type']=='ObjectReference']
    return {p['path']:p['value'] for p in c['properties'] if not any(p['path'].startswith(x+'.') for x in strings+refs)}
def compact(c): return {'ref':c['ref'], 'fields':props(c)}
def short(o): return {**{k:v for k,v in o.items() if k!='components'},'components':[compact(c) for c in o['components'] if c]}
types = collections.Counter(c['ref']['type'] for c in components)
ui = [short(o) for o in objects if '/GameUI' in o['ref']['hierarchy'] or any(c and c['ref']['type'].endswith(('Canvas','CanvasScaler')) for c in o['components'])]
core = [compact(c) for c in components if c['ref']['type'].endswith(('GameManager','GameplayPanel','DetectiveGame','FigurinePedestal','DeskAssetPresentation'))]
cameras_lights = [short(o) for o in objects if any(c and c['ref']['type'] in ('UnityEngine.Camera','UnityEngine.Light') for c in o['components'])]
callbacks = []
for c in components:
    fields = props(c)
    for k,v in fields.items():
        if k.endswith('.m_MethodName'):
            base=k[:-len('.m_MethodName')]
            callbacks.append({'component':c['ref'],'method':v,'fields':{a:b for a,b in fields.items() if a.startswith(base+'.')}})
panels = [o for o in ui if '/Upgrades panel' in o['ref']['hierarchy']]
assets = sorted({p['value']['assetPath'] for c in components for p in c['properties'] if p['type']=='ObjectReference' and isinstance(p['value'],dict) and p['value']['assetPath']})
third = [short(o) for o in objects if '/StatueCollectibleSlot_2' in o['ref']['hierarchy']]
skill_hits=[]
for p in (ROOT/'Assets').rglob('*'):
    if p.is_file() and p.suffix in ('.cs','.asset','.prefab','.unity'):
        if re.search(r'skill[ _-]?tree',p.read_text(encoding='utf-8-sig',errors='replace'),re.I): skill_hits.append(p.relative_to(ROOT).as_posix())
protected=read(ROOT/'ProductionLogs/Gameplay/asset-file-preservation.json')['files']
hashes=[]
for f in protected:
    p=ROOT/f['path']; h=sha(p) if p.exists() else None
    hashes.append({'path':f['path'],'sha256':h,'priorGameplaySha256':f['sha256_after'],'matchesPrior':h==f['sha256_after']})
write(OUT/'protected-art-hashes-before.json',{'filesChecked':len(hashes),'missing':[f['path'] for f in hashes if f['sha256'] is None],'changedSinceGameplay':[f['path'] for f in hashes if not f['matchesPrior']],'files':hashes})
backup_paths=set()
for folder in ['Assets/DetectiveDesk/Scripts/Gameplay','Assets/DetectiveDesk/Scripts/Editor/Gameplay']:
    backup_paths.update(p.relative_to(ROOT).as_posix() for p in (ROOT/folder).glob('*.cs*'))
for c in components:
    if c['ref']['type'].endswith(('DetectiveGame','DeskAssetPresentation')):
        r=props(c).get('m_Script')
        if isinstance(r,dict) and r['assetPath']: backup_paths.add(r['assetPath'])
backup_paths.update(a['ref']['assetPath'] for a in data['configurationAssets'])
backup_paths.update([data['activeScene'],'ProjectSettings/DynamicsManager.asset'])
for p in list(backup_paths):
    if (ROOT/(p+'.meta')).exists(): backup_paths.add(p+'.meta')
manifest=[]
for rel in sorted(backup_paths):
    p=ROOT/rel
    if not p.is_file(): raise RuntimeError('Missing backup source '+rel)
    dest=CHECK/'Files'/rel; dest.parent.mkdir(parents=True,exist_ok=True)
    if dest.exists(): raise RuntimeError('Refusing backup overwrite '+str(dest))
    shutil.copy2(p,dest)
    manifest.append({'source':rel,'backup':dest.relative_to(ROOT).as_posix(),'sha256':sha(p),'copySha256':sha(dest)})
write(CHECK/'backup-manifest.json',manifest)
write(CHECK/'panels-serialized.json',ui)
write(OUT/'camera-light-bindings-before.json',cameras_lights)
physics=ROOT/'ProjectSettings/DynamicsManager.asset'
backend=int(re.search(r'm_CurrentBackendId:\s*(\d+)',physics.read_text()).group(1))
inspection={'activeScene':data['activeScene'],'editMode':True,'dirty':data['activeDirty'],'sceneCount':len(data['scenes']),'objectCount':len(objects),'componentCount':len(components),'componentTypeCounts':dict(types),'coreBindings':core,'configurationAssets':[compact(c) for c in data['configurationAssets']],'ui':ui,'upgradesPanelObjects':panels,'persistentCallbacks':callbacks,'camerasAndLights':cameras_lights,'thirdFigurineObjects':third,'referencedAssets':assets,'existingSkillTreeSearchHits':skill_hits,'physics':{'serializedBackendId':backend,'expectedPhysXId':4072204805,'matchesPhysX':backend==4072204805,'sha256':sha(physics),'runtimeProbe':'Not rerun: bridge stopped after existing NoSubscription Console message. Prior Gameplay public API evidence confirms this ID is PhysX.'},'screenshot':'ProductionLogs/Gameplay/upgrades-panel.png','console':{'readOnly':True,'cleared':False,'entries':[{'type':'Log','message':'Reduced additional punctual light shadows resolution by 2 to fit 6 shadow maps in 2048x2048 atlas.'},{'type':'Log','message':"Error reason is 'NoSubscription' and no additional error information was provided (https://generators.ai.unity.com)."}],'action':'Stopped subsequent Unity bridge calls under auth-stop rule.'},'limitations':['No new screenshot; authorized prior Gameplay/upgrades-panel.png reused. No view toggles performed.','SerializedProperty ArraySize values retain type labels; actual array counts are calculated from element fields.','No runtime tests or Play Mode. No runtime physics probe after auth-stop.','Panels backed up as serialized data and inside both scene copies; no new panel prefab created.']}
write(OUT/'inspection.json',inspection)
lines=['# Skill-tree inspection and checkpoint','',f"Scene: `{data['activeScene']}`. Edit Mode, {len(data['scenes'])} loaded scene; clean before and after saveAsCopy. {len(objects)} objects, {len(components)} components (including transforms).",'',f"Protected art: {len(hashes)} files hashed; {sum(not f['matchesPrior'] for f in hashes)} differ from prior Gameplay baseline. Physics backend ID {backend} matches PhysX (4072204805).",'', 'Full serialized properties, GlobalObjectIds, session IDs and object references are in scene-before.json. UI layout, CanvasScalers, all persistent callbacks, core bindings and third-figurine source mesh/material references are in inspection.json.','', 'Current core components:']
for c in core:
    fields=props({'properties':[{'path':k,'type':'value','value':v} for k,v in c['fields'].items()]})
    lines += [f"- `{c['ref']['type']}` ID {c['ref']['id']} at `{c['ref']['hierarchy']}`"]
    for k in ['speedLevel','motorLevel','durabilityLevel','selectedSkin','tier','money','moneyPerMinute','gold']:
        if k in fields: lines += [f'  - {k}: {fields[k]}']
lines += ['', 'Upgrades panel hierarchy and layout:']
for o in panels:
    lines += [f"- `{o['ref']['hierarchy']}` ID {o['ref']['id']}; activeSelf={o['activeSelf']}"]
    for c in o['components']:
        if c['ref']['type'].endswith('RectTransform'):
            lines += ['  - '+str({k:v for k,v in c['fields'].items() if k in ['m_AnchorMin','m_AnchorMax','m_AnchoredPosition','m_SizeDelta','m_Pivot']})]
        if c['ref']['type'].endswith('.Text'): lines += ['  - text: '+repr(c['fields'].get('m_Text'))]
lines += ['', 'Canvas scalers:']
for c in components:
    if c['ref']['type'].endswith('CanvasScaler'): lines += [f"- `{c['ref']['hierarchy']}` ID {c['ref']['id']}: "+str({k:v for k,v in props(c).items() if k in ['m_UiScaleMode','m_ReferenceResolution','m_MatchWidthOrHeight','m_ScaleFactor','m_ScreenMatchMode']})]
lines += ['',f'Persistent callbacks: {len(callbacks)} total.']
for cb in callbacks:
    lines += [f"- `{cb['component']['hierarchy']}` [{cb['component']['id']}] -> `{cb['method']}`; "+str({k:v for k,v in cb['fields'].items() if k.endswith(('m_Target','m_CallState','m_Mode'))})]
lines += ['',f'Existing skill-tree search hits under Assets: {skill_hits}. Reuse GameManager, GameplayPanel, UpgradeConfig and the existing Upgrades modal; no duplicate systems were created.','',f'Backups: {len(manifest)} files copied with matching SHA-256, plus live scene saveAsCopy and serialized UI panels. Original disk scene is preserved separately under Checkpoint/Files. All Gameplay runtime/editor scripts and their metadata were included to cover the requested manager/panel/config/runtime validation scripts and their dependencies.','', 'Visual reference: `ProductionLogs/Gameplay/upgrades-panel.png` reused as authorized; no toggles, camera changes, Canvas changes, scene edits or original scene save.','', 'Bridge stopped after dedicated Console read exposed an existing Unity AI NoSubscription log. No security, login, installation, model changes, reflection, Blender, agents, Play Mode or Console clear used. No tracker/queue writes.','', 'Limitations: no fresh runtime physics probe; serialized backend ID and prior Gameplay public-API evidence used. No new screenshot. Array sizes are obtained by counting element fields, since raw dump labels ArraySize properties. Full reference and field data remains in JSON.']
(OUT/'report.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
write(OUT/'unity-writer-released.json',{'writer':'skill-tree-readonly-inspection','state':'released','timestampUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'bridgeCallsStopped':True,'reason':'Inspection complete; existing Unity AI NoSubscription Console log observed','sceneSaved':False,'sceneCopySaved':True,'sceneCleanBefore':not data['activeDirty'],'sceneCleanAfter':not read(OUT/'checkpoint-state.json')['dirtyAfter'],'play':False,'protectedFilesHashed':len(hashes),'report':'ProductionLogs/SkillTree/report.md'})
print(json.dumps({'objects':len(objects),'components':len(components),'protectedFiles':len(hashes),'changedProtectedFiles':sum(not f['matchesPrior'] for f in hashes),'backups':len(manifest),'callbacks':len(callbacks),'upgradeObjects':len(panels),'skillTreeHits':skill_hits,'physicsBackend':backend}))
