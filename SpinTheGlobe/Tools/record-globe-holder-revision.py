"""Record executed holder evidence; preserve partial sphere approval only."""
from pathlib import Path
import sys,shutil,datetime
sys.path.insert(0,str(Path(__file__).parent/'AssetQueue'))
import queue as q
with q.Lock():
 state=q.read(q.QUEUE);assert state.get('active_worker') is None,'Queue worker active'
 out='Assets/DetectiveDesk/GlobeHolderRevision';ev=q.verify_evidence({'output_path':out,'pipeline':'Blender'})
 b=q.read(q.safe(out)/'blender-validation.json')
 for name,digest in b.get('helpers',{}).items():assert q.digest(q.safe('Assets/DetectiveDesk/ArtSource/Gameplay/MechanicalGlobe')/name)==digest
 backup=q.ROOT/'ProductionLogs/GlobeHolderRevision/TrackerCheckpoints'/datetime.datetime.now().strftime('%Y%m%d_%H%M%S');backup.mkdir(parents=True)
 for n in ['asset_queue.json','asset_registry.json','Asset_Tracker.xlsx','assets.csv']:shutil.copy2(q.ROOT/n,backup/n)
 job=next(a for a in state['assets'] if a['slug']=='MechanicalGlobe');previous=job['output_path'];stamp=q.now()
 job.update(status='Needs Review',validated=True,human_approved=False,existing_binding=ev['prefab_path'],output_path=out,screenshot_path=ev['screenshot_path'],report_path=out+'/asset-report.md',last_updated=stamp,last_error='')
 job['notes']='User-approved world sphere and current colors preserved. Requested left half-circle holder, removal of exposed gears and base-adjacent RPM gauge integrated through existing visual modules. See executed source/Unity/preservation/runtime/Console evidence and Game image in report. Whole revised asset awaits visual approval.'
 job['history'].append({'at':stamp,'status':'Needs Review','event':'holder_revision_integrated','previous_output':previous,'evidence':out})
 pending=job.pop('pending_revision',None)
 job['approved_parts']=['World sphere geometry and current colors (user statement before holder revision)']
 if pending:job['history'].append({'at':stamp,'event':'staged_revision_resolved','prior_staging':pending})
 job['scene_integration']={'scene':'Assets/DetectiveDesk/Scenes/DetectiveOffice.unity','report':job['report_path'],'screenshot':job['screenshot_path'],'at':stamp}
 registry=q.read(q.ROOT/'asset_registry.json');r=next(a for a in registry if a['slug']=='MechanicalGlobe');r.update(status='Detailed source; holder revision needs visual review',unity=ev['prefab_path'],check='Executed source preservation/embedded scripts/half holder/no exposed gears/relocated gauge/FBX validation; existing Unity module refresh, preservation and Console checks. Exact runtime checks and limits recorded in report.')
 q.atomic(q.ROOT/'asset_registry.json',registry);q.save(state)
 assert len(q.read(q.QUEUE)['assets'])==42
 print('Globe holder evidence recorded; 42 rows synchronized. Whole-asset visual approval pending. Backup:',backup)
