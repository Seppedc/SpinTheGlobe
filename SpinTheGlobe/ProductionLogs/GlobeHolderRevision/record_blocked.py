import hashlib,json
from pathlib import Path
R=Path.cwd(); L=R/'ProductionLogs/GlobeHolderRevision'; E=R/'Assets/DetectiveDesk/GlobeHolderRevision'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
baseline=json.loads((L/'file-baseline.json').read_text()); changes=[];missing=[]
for name,digest in baseline.items():
    p=R/name
    if not p.exists():missing.append(name)
    elif sha(p)!=digest:changes.append(name)
sphere=[p for p in baseline if '/NativeMeshes/' in p and '_Sphere_' in p and p.endswith('.asset')]
materials=[p for p in baseline if '/GlobeRework/Materials/' in p and p.endswith('.mat')]
runtime=[p for p in baseline if p.endswith('.cs') and '/Editor/' not in p]
prefabs=[p for p in changes if p.endswith('.prefab')]
files={'status':'PASS' if not missing and len(sphere)==13 and len(materials)==9 and all(p not in changes for p in sphere+materials+runtime) else 'FAIL','sphere_files':len(sphere),'sphere_byte_equal':all(p not in changes for p in sphere),'material_files':len(materials),'materials_byte_equal':all(p not in changes for p in materials),'runtime_script_files':len(runtime),'runtime_scripts_byte_equal':all(p not in changes for p in runtime),'changed_existing_files':changes,'missing_files':missing,'scene_file_unchanged':sha(R/'Assets/DetectiveDesk/Generated/DetectiveOffice.unity')==sha(L/'FreshCheckpoint/DetectiveOffice.unity')}
write(L/'file-preservation-validation.json',files)
error='This action was rejected due to unacceptable risk.\nReason: The command clears and rewrites twelve serialized mesh assets while omitting channels such as colors and higher UV sets, creating a significant corruption risk beyond the explicitly authorized visual refresh; reimport or inspect without destructive rewrites instead.'
v={'status':'BLOCKED','reason':'Automatic approval review rejected explicit native mesh rendering-buffer rewrite; user instructed stop on permissions failure','exact_error':error,'unity_connection':'PASS','source_promoted':True,'scene_saved':False,'edit_mode':True,'scene_dirty_at_last_write':True,'runtime_executed':False,'protected_scene_unchanged':False,'protected_scene_comparison_status':'PASS except specifically authorized changes, 4324 components; comparison completed before rejection','visual_validation':'FAIL: captured Game camera and closeup continue to render old full holder and old offset gauge although CPU native mesh vertices/bounds contain revised source','source_sha256':sha(R/'Assets/DetectiveDesk/ArtSource/Gameplay/MechanicalGlobe/MechanicalGlobe.fbx'),'prefab_path':'Assets/DetectiveDesk/GlobeRework/Prefabs/GlobeRework_Static.prefab','screenshot_path':'Assets/DetectiveDesk/GlobeHolderRevision/final-game-camera.png','screenshot_sha256':sha(E/'final-game-camera.png'),'mesh_count_retained_modular':80,'active_modular_mesh_count_at_inspection':51,'missing_materials_at_inspection':0,'sphere_preservation':files['sphere_byte_equal'],'material_preservation':files['materials_byte_equal'],'console_read_before_revision':{'warnings':0,'errors':0},'console_final_not_read':True,'known_tooling_error':'ArgumentNullException in preservation verifier due to Unity 6 JSON wrapper; verifier corrected, subsequent complete preservation passed; retained Console was not cleared','backup':'ProductionLogs/GlobeHolderRevision/FreshCheckpoint','changed_existing_files':changes,'new_helper':'Assets/DetectiveDesk/GlobeRework/Editor/GlobeHolderRefresh.cs','limitations':['No final scene save','No runtime or final Console verification','No successful visual validation','Helper Apply is single-use and must not be rerun on partially updated scene','Initial authoring Build never called; no duplicate modules; no Blender/agents/queue/tracker changes']}
write(E/'unity-validation.json',v);write(L/'integration-result.json',v)
write(L/'writer-guard.json',{'status':'released','reason':'Stopped immediately after automatic approval rejection','unity_writer_active':False,'no_background_worker_started':True})
report='''# Globe holder revision — partial integration, blocked

Unity MCP connection and read-only inspection succeeded. DetectiveOffice was initially clean in Edit Mode, with 4,324 components. Fresh scene COPY, canonical source, six prefabs, native meshes and original root prefab backups are in `ProductionLogs/GlobeHolderRevision/FreshCheckpoint/`.

Promoted the existing staged reference_globe.py, blend and FBX only. No Blender rerun occurred. Handoff hashes were verified against original canonical files and checkpoint; actual staged hashes are recorded separately. Original entry/legacy, textures and meta files retained. Twelve existing native mesh assets updated by source-object name and stable GUID; old native assets retained. Existing Static prefab disables five absent right labels; three existing gear prefabs disable 24 gear renderers. Needle prefab retained identity. Existing original needle moved by (-.74,-.25,-.05) in globe-root coordinates, correctly converted through its actual RPMGauge_Assembly parent. Feedback list retains 51 enabled existing renderer references in original order.

All 4,324 existing components were compared by GlobalObjectId, serialized field data and independent serialized object references. Only the 29 renderer flags, original needle local position and feedback list differ. All 13 sphere native mesh files, all nine URP material files and all runtime scripts remain byte-identical. Camera, lamp, lights, HUD and unrelated components passed comparison. Source sphere/pedestal/gauge imported anchors passed within 0.000001m. No sphere rebind, texture/material/light/camera edit, initial Build, duplicate module, gameplay target deletion or tracker edit occurred.

Visual verification FAILED: actual inspected 1920x1080 Game capture with HUD and additive-camera closeup show stale full-holder/old-offset-dial rendering even though CPU mesh vertices/bounds contain the staged revisions. Native mesh reimport and explicit UploadMeshData did not resolve this. Capture camera/canvas/RectTransform state was restored in finally, additive scenes closed, subsequent preservation passed.

Automatic approval review rejected a proposed explicit Clear/rewrite of mesh render buffers because it omitted colors/higher UV channels and could corrupt assets. The rejected command did not execute. Unity writes stopped immediately; no workaround attempted. Scene remains in Edit Mode with this revision's unsaved authorized changes. Existing scene file remains unchanged. Updated source/native/prefab files ARE on disk; this is a partial integration, not a completed asset.

Runtime NOT executed. Final Console NOT read after rejection; no zero-error claim. Initial dedicated ReadConsole returned zero warnings/errors. A preservation-verifier ArgumentNullException was corrected for Unity 6 JSON wrappers; later full preservation passed. This tooling error may remain in the Console; Console was never cleared.

Approval or parent-directed recovery is required before further Unity writes. Preserve current changes and backups; do not rerun Apply (would move needle again), initial Build or generic generators. Next investigation should examine static-batching/render cache and all native mesh vertex channels before choosing a complete-channel update or safe scene reload. This is a recommendation only; no such operation was executed.

Exact changed existing files:

'''+''.join('- `'+p+'`\n' for p in changes)+'''
New files: scoped `Assets/DetectiveDesk/GlobeRework/Editor/GlobeHolderRefresh.cs` (+Unity meta); preparation/capture/block-report scripts and detailed logs under ProductionLogs/GlobeHolderRevision; dedicated evidence directory contains original staged validation and previews, blender-validation.json, blocked unity-validation.json, and inspected Game/closeup captures (including v2/v3 diagnostic copies). No status was marked complete. See integration-result.json and file-preservation-validation.json for machine-readable evidence.
'''
(E/'asset-report.md').write_text(report,encoding='utf-8')
print(json.dumps({'status':'BLOCKED','file_preservation':files['status'],'sphere_files':len(sphere),'materials':len(materials),'changed_files':len(changes),'prefab_changes':prefabs,'scene_file_unchanged':files['scene_file_unchanged'],'guard':'released'},indent=2))
