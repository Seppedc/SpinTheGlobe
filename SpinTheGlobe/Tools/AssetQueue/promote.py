"""Promote fully closed Blender outputs into Assets; never write partially copied model files."""
import argparse, json, os, shutil, subprocess, uuid
from request_guard import request
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def safe(path):
    p=(ROOT/path).resolve()
    if not p.is_relative_to(ROOT):raise ValueError('Path outside project')
    return p
p=argparse.ArgumentParser();p.add_argument('--job',required=True);args=p.parse_args()
job=json.loads(safe(args.job).read_text());source=safe(job['build_output_path']);target=safe(job['output_path'])
if not str(source).startswith(str(ROOT/'ProductionLogs/AssetQueue/BuildStaging')):raise ValueError('Unexpected staging directory')
request(args.job,'before-import')
if json.loads((source/'blender-validation.json').read_text()).get('status')!='PASS':raise ValueError('Missing validated Blender outputs')
target.mkdir(parents=True,exist_ok=True)
for path in source.iterdir():
    if not path.is_file():continue
    destination=target/path.name
    if destination.exists():
        if destination.read_bytes()==path.read_bytes():continue
        raise FileExistsError('Refusing different existing output '+str(destination))
    temp=destination.with_name(destination.name+'.'+uuid.uuid4().hex+'.tmp')
    shutil.copyfile(path,temp);os.replace(temp,destination)
print('Promoted closed Blender outputs to '+str(target))
