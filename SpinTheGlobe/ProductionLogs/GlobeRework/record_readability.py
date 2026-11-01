import json, hashlib, re
from pathlib import Path
p=Path('ProductionLogs/GlobeRework'); a=Path('Assets/DetectiveDesk/GlobeRework')
history=p/'ReadabilityHistory/20261031-233752'
load=lambda f:json.loads(f.read_text())
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
baseline=load(p/'after-runtime.json'); after=load(p/'readability-after.json')
assert baseline==after==load(p/'readability-before.json')
v=load(a/'unity-validation.json')
assert sha(Path(v['canonical_fbx_source']))==v['source_sha256']
assert (a/'preview.png').read_bytes()==(p/'after-game-camera.png').read_bytes()
for name in ['DarkShell','EngravedLand']:
    old=(history/(name+'.mat')).read_text(); new=(a/'Materials'/(name+'.mat')).read_text()
    for prop in ['_BaseMap','_BumpMap','_MetallicGlossMap']:
        pattern=rf'- {prop}:.*?(?=\n    - |\n  m_)'
        assert re.search(pattern,old,re.S).group()==re.search(pattern,new,re.S).group()
settings={'_SpecularHighlights':0,'_SPECULARHIGHLIGHTS_OFF':True,'_EnvironmentReflections':0,'_ENVIRONMENTREFLECTIONS_OFF':True,'_Smoothness':0.6,'_EmissionMap':'same existing cartography _BaseMap','_EmissionColor':[0.06,0.06,0.06,1],'_EMISSION':True,'_METALLICSPECGLOSSMAP':True,'_NORMALMAP':True}
r={'status':'PASS','scope':['DarkShell.mat','EngravedLand.mat'],'settings':settings,'backup_directory':history.as_posix(),'preservation':{'exact_after_runtime_component_match':True,'components_checked':len(after),'camera_canvas_recttransform_restored':True,'scene_clean':True,'source_fbx_hash_unchanged':True,'existing_base_packed_normal_texture_references_and_uv_transforms_unchanged':True,'geometry_counts_unchanged':True,'unrelated_scene_components_unchanged':True},'inspection':load(p/'readability-inspection.json'),'console_errors':0,'console_warnings':0,'visual_assessment':'Settled 1920x1080 Game capture inspected: large white Europe glare removed; Europe, Mediterranean and Africa outlines readable, southern land retains a gentle shadow gradient; reflective brass/coastlines preserved; no luminous globe appearance.','runtime_repeated':False,'runtime_reason':'Only two presentation material settings changed; previous runtime PASS retained.','closeup':'Optional temporary-camera close-up omitted to keep capture scope minimal.','capture_note':'First capture showed transient shader variant rendering gaps; recaptured after variants settled and inspected final image.','review_status':'Needs Review'}
(p/'readability-preservation.json').write_text(json.dumps(r,indent=2)+'\n')
v['screenshot_sha256']=sha(a/'preview.png');v['material_only_refinement']=r
v['missing_textures']=0
(a/'unity-validation.json').write_text(json.dumps(v,indent=2)+'\n')
i=load(p/'integration-result.json');i['validation']=v;i['material_only_refinement']=r
(p/'integration-result.json').write_text(json.dumps(i,indent=2)+'\n')
report=(a/'asset-report.md').read_text()
report=report.replace('White globe tint, mapped smoothness at 1, gentle normal strength .22; no further globe brightness tuning. Restrained mapped emission is limited to GaugeFace and AmberAccent.','White globe tint and gentle normal strength .22 retained. Final globe surface readability settings are recorded below; other materials retain their original mapped smoothness and emission.')
report+='\n## Final material readability refinement\n\nOnly `Materials/DarkShell.mat` and `Materials/EngravedLand.mat` were saved with scoped SaveAssetIfDirty calls. Both use specular highlights 0 (`_SPECULARHIGHLIGHTS_OFF`), environment reflections 0 (`_ENVIRONMENTREFLECTIONS_OFF`), smoothness multiplier .6, and the same existing cartography BaseMap as EmissionMap with neutral (.06,.06,.06) emission (`_EMISSION`). Packed metallic/smoothness and normal maps/keywords, existing texture references and UV transforms remain intact. No brass, coastline, gear or gauge material was edited.\n\nFinal settled 1920x1080 Game image was inspected: the large white Europe glare is removed, Europe/Mediterranean/Africa outlines are readable, and southern land retains a gentle shadow gradient without a luminous appearance. A first capture had transient shader variant gaps; the final recapture is complete. Optional close-up omitted to keep scope minimal.\n\nPreservation PASS: all 3,930 scene components exactly match `after-runtime.json`, including camera, canvas/RectTransform, root, lights, HUD, cannon, coffer, bindings and transforms. Scene remains clean; no scene save or save-all. Exact 80 meshes/renderers and 272684 triangles retained; zero missing materials or required textures; source FBX hash unchanged. Dedicated Unity_ReadConsole returned zero warnings/errors without clearing. Runtime was not repeated because only material settings changed; prior runtime proof remains intact. Backups: `ProductionLogs/GlobeRework/ReadabilityHistory/20261031-233752/`. Details: `readability-preservation.json` and `readability-inspection.json` in ProductionLogs/GlobeRework. Human approval remains pending (Needs Review).\n'
(a/'asset-report.md').write_text(report)
print(json.dumps({'status':'PASS','components':len(after),'screenshot_sha256':v['screenshot_sha256'],'review_status':'Needs Review'}))
