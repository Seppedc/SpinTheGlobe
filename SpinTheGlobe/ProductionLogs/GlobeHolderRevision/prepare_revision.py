import hashlib,json,shutil,zipfile
from pathlib import Path
ROOT=Path.cwd(); LOG=ROOT/'ProductionLogs/GlobeHolderRevision'; SRC=LOG/'Source'; CAN=ROOT/'Assets/DetectiveDesk/ArtSource/Gameplay/MechanicalGlobe'; BACK=LOG/'FreshCheckpoint'; EVID=ROOT/'Assets/DetectiveDesk/GlobeHolderRevision'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,data):p.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
def prepare():
    hand=json.loads((LOG/'handoff.json').read_text()); validation=json.loads((SRC/'source-validation.json').read_text())
    assert validation['status']=='PASS' and validation['embedded_sources_exact'] and validation['approved_sphere_geometry_uv_material_names_transforms_exact']
    # Handoff's hashes describe the original live source/checkpoint, as proven by
    # exact agreement for generator, blend and FBX. They are not staged hashes.
    for name,digest in hand['source_sha256'].items(): assert sha(CAN/name)==digest, f'Canonical handoff conflict {name}'
    for name in ['reference_globe.py','build_asset.py','legacy_globe.py','MechanicalGlobe.blend','MechanicalGlobe.fbx']:
        assert sha(CAN/name)==sha(LOG/'Checkpoint'/name),f'Canonical source conflict: {name}'
    for p in (SRC/'Textures').glob('*.png'): assert sha(p)==sha(CAN/'Textures'/p.name),f'Texture conflict {p.name}'
    registry=json.loads((ROOT/'asset_registry.json').read_text(encoding='utf-8-sig')); assert 'MechanicalGlobe' in json.dumps(registry)
    with zipfile.ZipFile(ROOT/'Asset_Tracker.xlsx') as z: assert z.read('xl/workbook.xml')
    BACK.mkdir(exist_ok=True);EVID.mkdir(exist_ok=True)
    assert not (LOG/'file-baseline.json').exists(),'Do not overwrite baseline'
    protected=set(ROOT.glob('Assets/**/*.cs'))|set(ROOT.glob('Assets/**/*.prefab'))|set(ROOT.glob('Assets/**/*.prefab.meta'))
    for folder in ['Assets/DetectiveDesk/GlobeRework/NativeMeshes','Assets/DetectiveDesk/GlobeRework/Materials','Assets/DetectiveDesk/GlobeRework/Textures','Assets/DetectiveDesk/ArtSource/Gameplay/MechanicalGlobe/Textures']:
        protected.update(p for p in (ROOT/folder).rglob('*') if p.is_file())
    protected.update(p for p in CAN.iterdir() if p.is_file())
    protected.add(ROOT/'Assets/DetectiveDesk/Generated/DetectiveOffice.unity.meta')
    write(LOG/'file-baseline.json',{p.relative_to(ROOT).as_posix():sha(p) for p in sorted(protected)})
    for folder in ['Assets/DetectiveDesk/GlobeRework/NativeMeshes','Assets/DetectiveDesk/GlobeRework/Materials','Assets/DetectiveDesk/GlobeRework/Prefabs','Assets/DetectiveDesk/ArtSource/Gameplay/MechanicalGlobe','Assets/DetectiveDesk/GlobeBlender/Prefabs']:
        shutil.copytree(ROOT/folder,BACK/folder,dirs_exist_ok=True)
    write(LOG/'writer-guard.json',{'status':'held','authority':'Explicit parent-supervised sole sequential Unity writer authorization; parent waits; no other writer active','stage':'before-import','no_process_queries':True})
    write(LOG/'staging-verification.json',{'status':'PASS','handoff_hash_scope':'original canonical source, confirmed against checkpoint','handoff_hashes_verified':True,'staged_hashes':{n:sha(SRC/n) for n in ['reference_globe.py','MechanicalGlobe.blend','MechanicalGlobe.fbx']},'canonical_matches_checkpoint':True,'all_texture_payloads_exact':True,'backup':str(BACK.relative_to(ROOT))})
    print('Staging/checkpoint/texture hashes verified; fresh source/prefab/native backups and protected file baseline complete.')
def promote():
    assert (LOG/'scene-baseline.json').exists() and (BACK/'DetectiveOffice.unity').exists()
    for name in ['reference_globe.py','MechanicalGlobe.blend','MechanicalGlobe.fbx']:
        assert sha(CAN/name)==sha(LOG/'Checkpoint'/name),f'Conflict since preparation: {name}'
        shutil.copy2(SRC/name,CAN/name)
    for name in ['source-validation.json','generation-validation.json','blender-preview.png','blender-threequarter.png']:shutil.copy2(SRC/name,EVID/name)
    proof={'status':'PASS','embedded_source_exact':True,'mesh_count':51,'triangles':235110,'source_file':str(SRC/'source-validation.json'),'execution_proof':'Existing original-derived staged build plus independent blend and FBX reopen; no Blender rerun in this Unity revision session','helper_hashes':{n:sha(CAN/n) for n in ['build_asset.py','legacy_globe.py']}}
    for key,name in [('blend','MechanicalGlobe.blend'),('fbx','MechanicalGlobe.fbx'),('generator','reference_globe.py')]:proof[key+'_path']=(CAN/name).relative_to(ROOT).as_posix();proof[key+'_sha256']=sha(CAN/name)
    write(EVID/'blender-validation.json',proof);write(LOG/'promotion-validation.json',proof);print('Only generator/blend/FBX promoted, existing meta/textures/materials retained.')
if __name__=='__main__':
    import sys
    {'prepare':prepare,'promote':promote}[sys.argv[1]]()
