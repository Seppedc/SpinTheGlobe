"""Explicit human review/status command; never invoked by unattended workers."""
import argparse
import importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('detective_asset_queue',Path(__file__).with_name('queue.py'))
q=importlib.util.module_from_spec(spec);spec.loader.exec_module(q)
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--slug',required=True);p.add_argument('--status',choices=q.STATUSES,required=True)
p.add_argument('--human-approve',action='store_true');p.add_argument('--retry-blocked',action='store_true')
p.add_argument('--notes',required=True)
a=p.parse_args()
with q.Lock():
    state=q.read(q.QUEUE)
    if state.get('active_worker'):raise SystemExit('Worker still recorded; Resume/recover safely before changing status.')
    asset=next(j for j in state['assets'] if j['slug']==a.slug)
    if a.status=='Complete' and (not a.human_approve or not asset['validated']):
        raise SystemExit('Complete requires verified production evidence AND explicit --human-approve. A smoke test is insufficient.')
    if a.retry_blocked:
        if asset['protected_existing']:raise SystemExit('Protected existing assets require a separate explicit migration task.')
        if a.status!='Not Started':raise SystemExit('Use --status "Not Started" with --retry-blocked')
        asset['attempt_count']=0;asset['last_error']=''
    if a.status in ['Generated','Imported','Validated','Needs Review'] and not asset['validated'] and not asset['protected_existing']:
        raise SystemExit('Automation evidence is required; do not manually promote an untested asset.')
    asset['status']=a.status;asset['human_approved']=a.human_approve
    asset['notes']=a.notes;asset['last_updated']=q.now()
    asset['history'].append({'at':q.now(),'status':a.status,'human_review':True,'notes':a.notes})
    q.save(state);q.log('Explicit human status update: '+a.slug+' -> '+a.status)
