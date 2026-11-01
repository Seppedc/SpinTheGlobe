"""Request a read-only process-safety check from the existing coordinator; no elevation."""
import argparse,json,time,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def request(job_file,stage):
    path=(ROOT/job_file).resolve()
    if not path.is_relative_to(ROOT/'ProductionLogs/AssetQueue/runs'):raise ValueError('Unexpected job location')
    job=json.loads(path.read_text());folder=path.parent;nonce=uuid.uuid4().hex
    temporary=folder/'guard-request.tmp'
    temporary.write_text(json.dumps({'nonce':nonce,'stage':stage,'run_id':job['run_id']}))
    temporary.replace(folder/'guard-request.json')
    for _ in range(120):
        response=folder/'guard-response.json'
        if response.exists():
            result=json.loads(response.read_text())
            if result.get('nonce')==nonce:
                if result.get('status')!='PASS':raise RuntimeError('Coordinator process check failed: '+result.get('error',''))
                print('Coordinator writer guard PASS: '+stage);return
        time.sleep(.5)
    raise RuntimeError('Coordinator unavailable; do not launch another writer')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--job',required=True);p.add_argument('--stage',choices=['before-blender','before-validation','before-import'],required=True)
    args=p.parse_args();request(args.job,args.stage)
