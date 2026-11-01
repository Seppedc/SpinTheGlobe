"""Sequential crash-resumable asset queue. Python standard library only."""
import argparse, csv, datetime as dt, hashlib, importlib.util, io, json, os
from pathlib import Path
import shutil, subprocess, sys, time, uuid, xml.etree.ElementTree as ET, zipfile

ROOT=Path(__file__).resolve().parents[2]
CONFIG=json.loads((Path(__file__).with_name('config.json')).read_text())
QUEUE=ROOT/'asset_queue.json'
CONTROL=ROOT/'asset_queue.control.json'
LOG=ROOT/CONFIG['LogRoot']
STATUSES=['Not Started','In Progress','Generated','Imported','Validated','Needs Review','Blocked','Complete']
def now(): return dt.datetime.now(dt.timezone.utc).isoformat()
def safe(path):
    p=(ROOT/path).resolve()
    if not p.is_relative_to(ROOT): raise ValueError('Path outside Unity project: '+str(p))
    return p
def relative(path): return Path(path).resolve().relative_to(ROOT).as_posix()
def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def atomic(path,data):
    path=Path(path);path.parent.mkdir(parents=True,exist_ok=True)
    temp=path.with_name(path.name+'.'+uuid.uuid4().hex+'.tmp')
    with temp.open('w',encoding='utf-8',newline='\n') as f:
        json.dump(data,f,indent=2,ensure_ascii=False);f.write('\n');f.flush();os.fsync(f.fileno())
    os.replace(temp,path)
def log(message):
    line=now()+' '+message; print(line,flush=True)
    LOG.mkdir(parents=True,exist_ok=True)
    with (LOG/'queue.log').open('a',encoding='utf-8') as f:f.write(line+'\n')
def digest(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as f:
        for block in iter(lambda:f.read(1048576),b''):h.update(block)
    return h.hexdigest()

class Lock:
    def __enter__(self):
        import msvcrt
        LOG.mkdir(parents=True,exist_ok=True)
        fd=os.open(LOG/'writer.lock',os.O_RDWR|os.O_CREAT)
        self.file=os.fdopen(fd,'r+b',buffering=0)
        if os.fstat(fd).st_size==0:self.file.write(b'0')
        self.file.seek(0)
        try: msvcrt.locking(fd,msvcrt.LK_NBLCK,1)
        except OSError:self.file.close();raise RuntimeError('Another queue coordinator owns the writer lock.')
        return self
    def __exit__(self,*args):
        import msvcrt
        self.file.seek(0);msvcrt.locking(self.file.fileno(),msvcrt.LK_UNLCK,1);self.file.close()

def initialize():
    if QUEUE.exists(): return read(QUEUE)
    entries=read(ROOT/'asset_registry.json');jobs=[]
    for a in entries:
        protected=a['slug'] in CONFIG['ProtectedSlugs']
        target=CONFIG['ExistingBindings'].get(a['slug'],a['unity'])
        inspected=protected and safe(target).exists()
        job={
          'id':a['id'],'slug':a['slug'],'category':a['category'],'name':a['name'],
          'pipeline':a['pipeline'],'priority':a['priority'],'dependencies':CONFIG['Dependencies'].get(a['slug'],[]),
          'status':'Needs Review' if protected else 'Not Started','protected_existing':protected,
          'existing_binding':target if inspected else '', 'dependency_available':inspected,
          'source_path':a['source'],'generator_path':a['builder'],'output_path':'',
          'reference_path':a['ref'],'requirements':a['brief'],'attempt_count':0,'last_error':'',
          'screenshot_path':'','report_path':'','validated':False,'human_approved':False,
          'last_updated':now(),'history':[],'test_runs':[],
          'notes':'Existing work protected; inspected inventory, no new visual approval.' if protected else 'Production work pending; starters are not finished assets.'}
        jobs.append(job)
    if len(jobs)!=42 or len({j['slug'] for j in jobs})!=42:raise ValueError('Expected 42 distinct tracker groups')
    known={j['slug'] for j in jobs}
    for j in jobs:
        if any(d not in known for d in j['dependencies']):raise ValueError('Unknown dependency')
    state={'schema_version':1,'created_at':now(),'updated_at':now(),'run_state':'Stopped',
           'max_assets_per_run':CONFIG['MaxAssetsPerRun'],'active_worker':None,'last_error':'','assets':jobs}
    atomic(QUEUE,state);sync(state);log('Initialized all 42 tracker groups; protected existing work excluded from rebuilding.')
    return state

def sync(state):
    """Reconcile all 42 rows, retaining workbook sheets, formulas and formatting."""
    spec=importlib.util.spec_from_file_location('tracker_helpers',ROOT/'Tools/update-asset-tracker.py')
    mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
    q,put,get,value=mod.q,mod.put,mod.get,mod.value
    registry=read(ROOT/'asset_registry.json');by_id={x['id']:x for x in state['assets']}
    with zipfile.ZipFile(ROOT/'Asset_Tracker.xlsx') as z: originals={i.filename:(i,z.read(i.filename)) for i in z.infolist()}
    sheet=ET.fromstring(originals['xl/worksheets/sheet2.xml'][1]);rows=sheet.findall(q('sheetData')+'/'+q('row'))
    for a in registry:
        j=by_id[a['id']];a['progress_status']=j['status'];a['queue_status']=j['status']
        a['owner']='Codex queue';a['last_reviewed']=j['last_updated'][:10]
        a['dependencies']='; '.join(j['dependencies']);a['attempt_count']=j['attempt_count']
        a['last_error']=j['last_error'];a['output_path']=j['output_path'];a['screenshot_path']=j['screenshot_path']
        a['notes']=j['notes'];a['evidence']=j['report_path'] or (j['test_runs'][-1]['job_file'] if j['test_runs'] else a.get('evidence',''))
        row=next(r for r in rows if value(get(r,'A'))==a['id'])
        mapping={'H':'progress_status','I':'owner','J':'builder','K':'blend','L':'unity','M':'ref','N':'last_reviewed',
                 'O':'check','P':'notes','Q':'evidence','R':'dependencies','S':'attempt_count','T':'last_error','U':'output_path','V':'screenshot_path'}
        for col,key in mapping.items():put(row,col,a.get(key,''),'0')
    header=next(r for r in rows if r.get('r')=='5');style=get(header,'N').get('s','0')
    for col,title in {'R':'Dependencies','S':'Attempts','T':'Last error','U':'Output directory','V':'Screenshot'}.items():put(header,col,title,style)
    for validation in sheet.findall(q('dataValidations')+'/'+q('dataValidation')):
        if validation.get('sqref')=='H6:H47':validation.find(q('formula1')).text='"'+','.join(STATUSES)+'"'
    filt=sheet.find(q('autoFilter'))
    if filt is not None:filt.set('ref','A5:V47')
    dim=sheet.find(q('dimension'))
    if dim is not None:dim.set('ref','A1:V47')
    queue=ET.fromstring(originals['xl/worksheets/sheet3.xml'][1])
    for row in queue.findall(q('sheetData')+'/'+q('row')):
        asset=by_id.get(value(get(row,'B')))
        if asset:put(row,'G',asset['status'])
    book=ET.fromstring(originals['xl/workbook.xml'][1]);calc=book.find(q('calcPr'))
    if calc is None:calc=ET.SubElement(book,q('calcPr'))
    calc.set('fullCalcOnLoad','1');calc.set('forceFullCalc','1')
    updates={'xl/worksheets/sheet2.xml':sheet,'xl/worksheets/sheet3.xml':queue,'xl/workbook.xml':book}
    temporary=LOG/'tracker-sync.xlsx';LOG.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(temporary,'w') as out:
        for name,(info,data) in originals.items():out.writestr(info,ET.tostring(updates[name],encoding='utf-8',xml_declaration=True) if name in updates else data)
    fields=list(dict.fromkeys(k for a in registry for k in a));buf=io.StringIO(newline='')
    writer=csv.DictWriter(buf,fieldnames=fields);writer.writeheader();writer.writerows(registry)
    # Workbook replacement fails safely if Excel holds an exclusive lock. Queue remains authoritative.
    os.replace(temporary,ROOT/'Asset_Tracker.xlsx')
    atomic(ROOT/'asset_registry.json',registry)
    csvtemp=LOG/'tracker-sync.csv';csvtemp.write_text(buf.getvalue(),encoding='utf-8');os.replace(csvtemp,ROOT/'assets.csv')

def save(state):
    state['updated_at']=now();atomic(QUEUE,state);sync(state)
def eligible(state):
    by_slug={j['slug']:j for j in state['assets']}
    def available(slug):
        d=by_slug[slug]
        return d['dependency_available'] or (d['validated'] and d['status'] in ['Validated','Needs Review','Complete'])
    return sorted((j for j in state['assets'] if not j['protected_existing'] and
        j['status'] in ['Not Started','In Progress','Generated','Imported'] and
        all(available(d) for d in j['dependencies'])),key=lambda j:(j['priority'],j['id']))
def plan(state,count):
    hypothetical=json.loads(json.dumps(state));out=[]
    while len(out)<count:
        candidates=eligible(hypothetical)
        if not candidates:break
        j=candidates[0];out.append(j);j['status']='Validated';j['validated']=True
    return out

def process_alive(pid):
    if not pid:return False
    import ctypes
    api=ctypes.windll.kernel32
    api.OpenProcess.restype=ctypes.c_void_p
    api.OpenProcess.argtypes=[ctypes.c_ulong,ctypes.c_int,ctypes.c_ulong]
    api.GetExitCodeProcess.argtypes=[ctypes.c_void_p,ctypes.POINTER(ctypes.c_ulong)]
    api.CloseHandle.argtypes=[ctypes.c_void_p]
    handle=api.OpenProcess(0x1000,False,int(pid))
    if not handle:return False
    code=ctypes.c_ulong();api.GetExitCodeProcess(handle,ctypes.byref(code));api.CloseHandle(handle)
    return code.value==259

def orphan_pid(run_id):
    # Covers the tiny crash window between child creation and persisted child PID.
    if not run_id or any(c not in '0123456789_abcdef' for c in run_id):return None
    ps="$ErrorActionPreference='Stop'; Get-CimInstance Win32_Process -Filter \"name='codex.exe'\" | Where-Object { $_.CommandLine -like '*"+run_id+"*' } | Select-Object -ExpandProperty ProcessId"
    result=subprocess.run(['powershell','-NoProfile','-Command',ps],capture_output=True,text=True,timeout=30)
    if result.returncode:raise RuntimeError('Cannot rule out a prior worker process; stop rather than risk two writers.')
    ids=[int(s) for s in result.stdout.split() if s.isdigit()]
    return ids[0] if ids else None

def preflight():
    if shutil.disk_usage(ROOT).free < CONFIG['MinimumFreeDiskGB']*1024**3:raise RuntimeError('Project-wide disk space below minimum.')
    blender=Path(CONFIG['BlenderExecutable'])
    if not blender.is_file():raise RuntimeError('Project-wide Blender executable missing.')
    r=subprocess.run([str(blender),'--version'],capture_output=True,text=True,timeout=30)
    if r.returncode:raise RuntimeError('Project-wide Blender installation failure: '+r.stderr)
    codex=shutil.which(CONFIG['CodexExecutable'])
    if not codex:raise RuntimeError('Codex CLI not installed on PATH.')
    help_text=subprocess.run([codex,'exec','--help'],capture_output=True,text=True,timeout=30).stdout
    if '--sandbox' not in help_text:raise RuntimeError('CLI workspace-write not supported; refusing unsafe fallback.')
    auth=subprocess.run([codex,'login','status'],capture_output=True,text=True,timeout=30)
    if auth.returncode:raise RuntimeError('Project-wide unavailable Codex authentication: '+auth.stderr)
    # Exclude idle interactive Blender windows; stop on any existing background build.
    ps="$ErrorActionPreference='Stop'; @(Get-CimInstance Win32_Process -Filter \"name='blender.exe'\" | Where-Object { $_.CommandLine -match '(--background| -b )' }).Count"
    bg=subprocess.run(['powershell','-NoProfile','-Command',ps],capture_output=True,text=True,timeout=30)
    if bg.returncode or bg.stdout.strip()!='0':raise RuntimeError('Another background Blender writer is active or cannot be checked.')
    return codex,help_text

def service_guard(folder,run_id):
    request=folder/'guard-request.json'
    if not request.exists():return
    data=read(request);response=folder/'guard-response.json'
    if response.exists() and read(response).get('nonce')==data.get('nonce'):return
    if data.get('run_id')!=run_id or data.get('stage') not in ['before-blender','before-validation','before-import']:
        raise RuntimeError('Project-wide invalid writer guard request')
    ps="$ErrorActionPreference='Stop'; @(Get-CimInstance Win32_Process -Filter \"name='blender.exe'\" | Where-Object { $_.CommandLine -match '(--background| -b )' }).Count"
    check=subprocess.run(['powershell','-NoProfile','-Command',ps],capture_output=True,text=True,timeout=30)
    passed=check.returncode==0 and check.stdout.strip()=='0'
    atomic(response,{'nonce':data['nonce'],'stage':data['stage'],'status':'PASS' if passed else 'STOP',
                     'error':'' if passed else 'Background Blender active or process query unavailable.','at':now()})
    log('Writer boundary '+data['stage']+': '+('PASS' if passed else 'STOP'))

def checkpoint(folder):
    manifest={}
    with zipfile.ZipFile(folder/'checkpoint.zip','w',compression=zipfile.ZIP_DEFLATED,compresslevel=1) as z:
        for base in ['Assets','Packages','ProjectSettings','Tools']:
            for p in (ROOT/base).rglob('*'):
                if p.is_file() and '__pycache__' not in p.parts:
                    rel=relative(p);manifest[rel]=digest(p);z.write(p,rel)
        for name in ['AGENTS.md','Asset_Tracker.xlsx','asset_registry.json','assets.csv']:
            z.write(ROOT/name,name)
    atomic(folder/'checkpoint-hashes.json',manifest)
    return manifest

def verify_preservation(manifest,job):
    allowed=[job['source_path'].rstrip('/')+'/',job['output_path'].rstrip('/')+'/', 'Assets/DetectiveDesk/AssetReview/']
    if job['smoke_test']:allowed=allowed[1:]
    changed=[]
    for rel,sha in manifest.items():
        if any(rel.startswith(a) for a in allowed):continue
        p=safe(rel)
        if not p.is_file() or digest(p)!=sha:changed.append(rel)
    # New files outside the assigned source/output/review directories are also forbidden.
    parent_meta=set()
    for prefix in allowed:
        parent=safe(prefix)
        while parent.is_relative_to(ROOT/'Assets') and parent!=ROOT/'Assets':
            parent_meta.add(relative(parent)+'.meta');parent=parent.parent
    for base in ['Assets','Packages','ProjectSettings','Tools']:
        for path in (ROOT/base).rglob('*'):
            if not path.is_file() or '__pycache__' in path.parts:continue
            rel=relative(path)
            # Unity may create a delayed sidecar for a previous worker's newly saved report/image.
            # Accept only NEW sidecars whose pre-checkpoint source still matches exactly.
            if rel.endswith('.meta') and rel[:-5] in manifest:
                source=safe(rel[:-5])
                if source.is_file() and digest(source)==manifest[rel[:-5]]:continue
            if rel not in manifest and rel not in parent_meta and not any(rel.startswith(a) for a in allowed):changed.append('NEW '+rel)
    if changed:raise RuntimeError('PROJECT-WIDE protection checkpoint differs: '+', '.join(changed[:12])+'. Backup retained; no automatic rollback of possible user edits.')

def verify_evidence(job):
    out=safe(job['output_path']);u=read(out/'unity-validation.json')
    if u.get('status')!='PASS' or not u.get('protected_scene_unchanged') or u.get('console_errors',1)!=0:raise ValueError('Unity review validation failed')
    for key in ['prefab_path','screenshot_path']:
        if not safe(u[key]).is_file():raise ValueError('Missing Unity evidence '+key)
    if digest(safe(u['screenshot_path']))!=u['screenshot_sha256']:raise ValueError('Screenshot hash differs')
    if job['pipeline']=='Blender':
        b=read(out/'blender-validation.json')
        if b.get('status')!='PASS' or not b.get('embedded_source_exact') or b.get('mesh_count',0)<1:raise ValueError('Blender validation missing or failed')
        for k in ['blend','fbx','generator']:
            if digest(safe(b[k]))!=b[k+'_sha256']:raise ValueError('Blender evidence changed: '+k)
        if u.get('source_sha256')!=b['fbx_sha256']:raise ValueError('Unity imported a different source than Blender validated')
    report=out/'asset-report.md'
    if not report.is_file() or report.stat().st_size<80:raise ValueError('Missing meaningful asset report')
    if (safe(u['screenshot_path']).read_bytes()[:8])!=b'\x89PNG\r\n\x1a\n':raise ValueError('Invalid PNG screenshot')
    return u

def run_worker(state,asset,codex,help_text,smoke=False):
    run_id=dt.datetime.now().strftime('%Y%m%d_%H%M%S')+'_'+uuid.uuid4().hex[:8]
    folder=LOG/'runs'/run_id;folder.mkdir(parents=True)
    job=dict(asset);job.update({'smoke_test':smoke,'run_id':run_id,'blender_executable':CONFIG['BlenderExecutable'],
        'output_path':CONFIG['OutputRoot']+('/_Tests/' if smoke else '/')+asset['slug']+'/'+run_id,
        'build_output_path':CONFIG['LogRoot']+'/BuildStaging/'+run_id+'/'+asset['slug']})
    job_file=folder/'job.json';job['job_file']=relative(job_file);atomic(job_file,job)
    manifest=checkpoint(folder)
    if not smoke:
        asset['attempt_count']+=1;asset['status']='In Progress';asset['output_path']=job['output_path'];asset['last_updated']=now();asset['last_job_file']=relative(job_file)
    state['active_worker']={'pid':None,'run_id':run_id,'slug':asset['slug'],'smoke_test':smoke,'job_file':relative(job_file)};save(state)
    prompt=(Path(__file__).with_name('worker-prompt.md')).read_text().replace('JOB_FILE',relative(job_file)).replace('JOB_BUILD_OUTPUT',job['build_output_path']).replace('JOB_OUTPUT',job['output_path'])
    prompt+='\n'+json.dumps(job,indent=2)
    (folder/'prompt.md').write_text(prompt,encoding='utf-8')
    args=[codex,'exec','--skip-git-repo-check','--json','--color','never','-C',str(ROOT),
          '-c','mcp_servers.unitymcp.enabled=false','--output-schema',str(Path(__file__).with_name('worker-result.schema.json')),
          '--output-last-message',str(folder/'result.json')]
    if '--approve-for-me' in help_text:args.append('--approve-for-me')
    else:args+=['--sandbox','workspace-write','-c','approval_policy="never"']
    args.append('-');atomic(folder/'invocation.json',{'executable':codex,'args':args[1:],'permissions':'workspace-write','automatic_review':'--approve-for-me' in args})
    log('Starting sole '+('TEST ' if smoke else '')+'worker for '+asset['slug']+'; '+run_id)
    with (folder/'events.jsonl').open('wb') as stdout,(folder/'stderr.log').open('wb') as stderr:
        proc=subprocess.Popen(args,cwd=ROOT,stdin=subprocess.PIPE,stdout=stdout,stderr=stderr)
        state['active_worker']['pid']=proc.pid;atomic(QUEUE,state)
        try:proc.stdin.write(prompt.encode('utf-8'));proc.stdin.close()
        except BrokenPipeError:
            proc.wait(timeout=30)
            raise RuntimeError('Project-wide CLI startup failed; inspect '+relative(folder/'stderr.log'))
        started=time.monotonic()
        while proc.poll() is None:
            service_guard(folder,run_id)
            if time.monotonic()-started>CONFIG['WorkerTimeoutMinutes']*60:
                # Do not kill and orphan an in-flight Blender/Unity writer. Leave PID for safe recovery.
                raise RuntimeError('Project-wide worker timeout; worker may still run. No replacement worker permitted until it exits.')
            time.sleep(1)
    verify_preservation(manifest,job)
    text=(folder/'stderr.log').read_text(encoding='utf-8',errors='replace')
    text+=(folder/'events.jsonl').read_text(encoding='utf-8',errors='replace')
    lower=text.lower()
    if (proc.returncode or not (folder/'result.json').exists()) and any(x in lower for x in ['usage limit','rate limit exceeded','insufficient_quota','quota exceeded','not authenticated','permission denied','auto-review rejected']):
        raise RuntimeError('Project-wide usage/auth/permission failure; see '+relative(folder))
    result=read(folder/'result.json') if (folder/'result.json').exists() else {'outcome':'asset_error','error':'CLI returned no structured result; exit '+str(proc.returncode)}
    if result['outcome'] in ['project_error','permission_blocked','usage_limit']:raise RuntimeError('Project-wide '+result['outcome']+': '+result['error'])
    if proc.returncode or result['outcome']!='ok':raise ValueError(result.get('error') or 'Worker exit '+str(proc.returncode))
    evidence=verify_evidence(job)
    for status in ['Generated','Imported','Validated','Needs Review']:
        if smoke:continue
        asset['status']=status;asset['history'].append({'at':now(),'status':status,'run_id':run_id,'evidence':job['output_path']})
        asset['last_updated']=now();save(state)
    asset['last_error']=''
    if smoke:
        asset['test_runs'].append({'at':now(),'status':'Needs Review','validated':True,'job_file':relative(job_file),
            'output_path':job['output_path'],'screenshot_path':evidence['screenshot_path']})
        asset['notes']='Isolated queue smoke test validated; production status unchanged. Human visual review still required.'
    else:
        asset['validated']=True;asset['screenshot_path']=evidence['screenshot_path'];asset['report_path']=relative(safe(job['output_path'])/'asset-report.md')
        asset['notes']='Automated evidence validated; human visual approval pending. No main-scene placement performed.'
    state['active_worker']=None;save(state);log('Worker verified; '+asset['slug']+' awaits human visual review.')

def main():
    parser=argparse.ArgumentParser();parser.add_argument('action',choices=['initialize','dryrun','run','resume','pause','stop','status','test'])
    parser.add_argument('--max-assets',type=int,default=0);parser.add_argument('--plan-count',type=int,default=10);parser.add_argument('--test-slug',default='WallMolding')
    args=parser.parse_args()
    args.max_assets=args.max_assets or CONFIG['MaxAssetsPerRun']
    if not 1<=args.max_assets<=42:raise ValueError('MaxAssetsPerRun must be from 1 to 42')
    if args.action=='dryrun':
        if not QUEUE.exists():raise RuntimeError('Initialize once before dry-run; dry-run never writes files.')
        for i,j in enumerate(plan(read(QUEUE),args.plan_count),1):print(f"{i:2}. {j['id']} {j['priority']} {j['slug']} [{j['pipeline']}] dependencies={','.join(j['dependencies']) or 'none'}")
        print('Conditional plan: later entries assume dependency validation succeeds; no assets or tracker files changed.');return
    if args.action=='status':
        print(json.dumps(read(QUEUE) if QUEUE.exists() else {'run_state':'Not initialized'},indent=2));return
    if args.action in ['pause','stop']:
        atomic(CONTROL,{'requested':args.action,'at':now()});print(args.action+' requested; current worker finishes to a safe boundary before scheduling stops.');return
    with Lock():
        state=initialize()
        if args.action=='initialize':return
        previous=state.get('active_worker')
        if previous is None:
            # A completed CLI can leave valid evidence when the coordinator stops at a boundary.
            # Recover it before considering another generation, rather than wasting its attempt.
            unfinished=[j for j in state['assets'] if j['status'] in ['In Progress','Generated','Imported'] and j['output_path']]
            for pending in unfinished:
                matches=[]
                for jobfile in (LOG/'runs').glob('*/job.json'):
                    saved=read(jobfile)
                    if saved.get('slug')==pending['slug'] and saved.get('output_path')==pending['output_path'] and not saved.get('smoke_test'):
                        matches.append((jobfile,saved))
                if matches:
                    jobfile,saved=sorted(matches,key=lambda pair:pair[0].as_posix())[-1]
                    previous={'pid':None,'run_id':saved['run_id'],'slug':pending['slug'],'smoke_test':False,'job_file':relative(jobfile)}
                    state['active_worker']=previous
                    break
        if previous and (process_alive(previous.get('pid')) or orphan_pid(previous.get('run_id'))):raise RuntimeError('Previous worker is still active; refusing a second writer. Wait for it to exit, then Resume.')
        if previous:
            log('Recovering interrupted worker '+previous['run_id']+' from persisted job and evidence.')
            j=next(a for a in state['assets'] if a['slug']==previous['slug'])
            old_job=read(safe(previous['job_file']))
            try:
                verify_preservation(read(safe(previous['job_file']).parent/'checkpoint-hashes.json'),old_job)
                ev=verify_evidence(old_job)
                if previous['smoke_test']:j['test_runs'].append({'at':now(),'status':'Needs Review','validated':True,'job_file':previous['job_file'],'screenshot_path':ev['screenshot_path'],'recovered':True})
                else:j.update({'status':'Needs Review','validated':True,'screenshot_path':ev['screenshot_path'],
                               'report_path':relative(safe(old_job['output_path'])/'asset-report.md'),'last_error':'',
                               'notes':'Recovered verified outputs; human visual review pending.'})
            except (OSError,ValueError,KeyError):
                j['last_error']='Interrupted attempt; checkpoint and logs retained.'
                if not previous['smoke_test']:j['status']='Blocked' if j['attempt_count']>=1+CONFIG['RetriesPerAsset'] else 'In Progress'
            state['active_worker']=None;save(state)
        try:codex,help_text=preflight()
        except Exception as error:
            state['run_state']='Stopped';state['last_error']=str(error);save(state);log('QUEUE STOPPED during preflight: '+str(error));raise
        atomic(CONTROL,{'requested':'run','at':now()})
        state['run_state']='Running';state['last_error']='';state['max_assets_per_run']=args.max_assets;save(state)
        count=0
        try:
            while count<args.max_assets:
                request=read(CONTROL).get('requested')
                if request in ['pause','stop']:
                    state['run_state']='Paused' if request=='pause' else 'Stopped';save(state);log(state['run_state']+' at worker boundary.');return
                if args.action=='test':candidates=[j for j in state['assets'] if j['slug']==args.test_slug and not j['protected_existing']]
                else:candidates=eligible(state)
                if not candidates:break
                asset=candidates[0];count+=1
                attempts=0
                while attempts<=CONFIG['RetriesPerAsset']:
                    if not args.action=='test' and asset['attempt_count']>=1+CONFIG['RetriesPerAsset']:
                        asset['status']='Blocked';save(state);break
                    attempts+=1
                    try:
                        preflight();run_worker(state,asset,codex,help_text,args.action=='test');break
                    except (ValueError,KeyError,FileNotFoundError) as error:
                        asset['last_error']=str(error);asset['last_updated']=now();state['active_worker']=None
                        log('Asset attempt failed: '+asset['slug']+': '+str(error))
                        if attempts>CONFIG['RetriesPerAsset'] or asset['attempt_count']>=1+CONFIG['RetriesPerAsset']:
                            if args.action!='test':asset['status']='Blocked'
                            else:asset['test_runs'].append({'at':now(),'status':'Blocked','error':str(error)})
                        save(state)
                if args.action=='test':break
            state['run_state']='Stopped';save(state);log('Bounded run ended; processed '+str(count)+' asset(s). No overnight run is active.')
        except BaseException as error:
            state['run_state']='Stopped';state['last_error']=str(error)
            if state.get('active_worker'):
                failed=next(j for j in state['assets'] if j['slug']==state['active_worker']['slug'])
                failed['last_error']=str(error);failed['last_updated']=now()
                if state['active_worker']['smoke_test']:
                    failed['test_runs'].append({'at':now(),'status':'Blocked','error':str(error),'job_file':state['active_worker']['job_file']})
            # Keep a live worker PID on timeout/interruption so Resume cannot launch another.
            if state.get('active_worker') and not process_alive(state['active_worker'].get('pid')):state['active_worker']=None
            atomic(QUEUE,state)
            try:sync(state)
            except Exception:pass
            log('QUEUE STOPPED: '+str(error));raise
if __name__=='__main__':
    try:main()
    except Exception as error:print('ERROR: '+str(error),file=sys.stderr);sys.exit(1)
