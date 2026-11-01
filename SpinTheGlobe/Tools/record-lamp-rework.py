"""Record executed lamp evidence without approving appearance or scheduling assets."""
from pathlib import Path
import sys,shutil,datetime
sys.path.insert(0,str(Path(__file__).parent/'AssetQueue'))
import queue as q
with q.Lock():
 state=q.read(q.QUEUE);assert state.get('active_worker') is None,'Queue worker active'
 out='Assets/DetectiveDesk/LampRework';ev=q.verify_evidence({'output_path':out,'pipeline':'Blender'})
 b=q.read(q.safe(out)/'blender-validation.json')
 for name,digest in b['helpers'].items():assert q.digest(q.safe('Assets/DetectiveDesk/ArtSource/Props/BankersLamp')/name)==digest
 backup=q.ROOT/'ProductionLogs/LampRework/TrackerCheckpoints'/datetime.datetime.now().strftime('%Y%m%d_%H%M%S');backup.mkdir(parents=True)
 for n in ['asset_queue.json','asset_registry.json','Asset_Tracker.xlsx','assets.csv']:shutil.copy2(q.ROOT/n,backup/n)
 job=next(a for a in state['assets'] if a['slug']=='BankersLamp');previous=job['output_path'];stamp=q.now()
 job.update(status='Needs Review',validated=True,human_approved=False,existing_binding=ev['prefab_path'],output_path=out,screenshot_path=ev['screenshot_path'],report_path=out+'/asset-report.md',last_updated=stamp,last_error='')
 job['notes']='Reference detail revision built and independently checked in Blender/FBX, integrated at original lamp anchor with explicit URP materials. Camera, gameplay and unrelated scene components preserved. New restrained bulb-local spot; original four room lights unchanged. See lamp report and Game view. Human visual approval pending.'
 job['history'].append({'at':stamp,'status':'Needs Review','event':'reference_lamp_rework','previous_output':previous,'evidence':out})
 job['scene_integration']={'scene':'Assets/DetectiveDesk/Generated/DetectiveOffice.unity','report':job['report_path'],'screenshot':job['screenshot_path'],'at':stamp}
 registry=q.read(q.ROOT/'asset_registry.json');r=next(a for a in registry if a['slug']=='BankersLamp');r.update(status='Detailed source; visual review pending',unity=ev['prefab_path'],check='Executed Blender source/embedded helper/closed hood/48 beads/UV/PBR/FBX reimport; Unity URP import, placement, preservation, Game image and Console checks. See evidence for exact results.')
 q.atomic(q.ROOT/'asset_registry.json',registry);q.save(state)
 synced=q.read(q.QUEUE);assert len(synced['assets'])==42;assert next(a for a in synced['assets'] if a['slug']=='BankersLamp')['status']=='Needs Review'
 print('Recorded lamp evidence; 42 rows synchronized; appearance remains Needs Review. Backup:',backup)
