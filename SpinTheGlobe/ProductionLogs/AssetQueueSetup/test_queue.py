"""Safety/state tests in project-local disposable fixtures; never launches a real asset worker."""
import copy, importlib.util, json, os, pathlib, shutil, sys, unittest, uuid
from unittest.mock import patch
PROJECT=pathlib.Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('asset_queue_under_test',PROJECT/'Tools/AssetQueue/queue.py')
q=importlib.util.module_from_spec(spec);spec.loader.exec_module(q)

class QueueTests(unittest.TestCase):
    def setUp(self):
        self.root=PROJECT/'ProductionLogs/AssetQueueSetup/TestFixtures'/uuid.uuid4().hex
        assert self.root.resolve().is_relative_to(PROJECT/'ProductionLogs/AssetQueueSetup')
        self.root.mkdir(parents=True)
        self.patches=[patch.object(q,'ROOT',self.root),patch.object(q,'QUEUE',self.root/'asset_queue.json'),
                      patch.object(q,'CONTROL',self.root/'control.json'),patch.object(q,'LOG',self.root/'Logs')]
        for p in self.patches:p.start()
        self.addCleanup(lambda:[p.stop() for p in reversed(self.patches)])
    def state(self,n=3):
        return {'run_state':'Stopped','active_worker':None,'last_error':'','assets':[
          {'id':f'DD-{i:02}','slug':f'Asset{i}','priority':'P1','pipeline':'Blender','dependencies':[],'protected_existing':False,
           'status':'Not Started','validated':False,'dependency_available':False,'attempt_count':0,'last_error':'',
           'last_updated':q.now(),'test_runs':[],'history':[],'notes':''} for i in range(1,n+1)]}
    def invoke(self,state,worker,*extra):
        with (patch.object(q,'initialize',return_value=state),patch.object(q,'sync'),patch.object(q,'preflight',return_value=('fake','--approve-for-me')),
             patch.object(q,'run_worker',side_effect=worker) as spy,patch.object(sys,'argv',['queue.py','run','--max-assets','3',*extra])):
            q.main()
            return spy.call_count
    def test_dependency_order_and_protected_exclusion(self):
        s=self.state();s['assets'][0]['dependencies']=['Asset2'];s['assets'][2]['protected_existing']=True
        self.assertEqual([j['slug'] for j in q.plan(s,10)],['Asset2','Asset1'])
        self.assertTrue(all(j['status']=='Not Started' for j in s['assets']))
    def test_two_retries_then_block_and_continue(self):
        s=self.state()
        def fail(state,asset,*args):asset['attempt_count']+=1;raise ValueError('Asset-specific test failure')
        self.assertEqual(self.invoke(s,fail),9)
        self.assertTrue(all(j['status']=='Blocked' and j['attempt_count']==3 for j in s['assets']))
    def test_project_failure_stops_independent_assets(self):
        s=self.state();calls=[]
        def fail(*args):calls.append(1);raise RuntimeError('Project-wide authentication unavailable')
        with self.assertRaisesRegex(RuntimeError,'authentication'):self.invoke(s,fail)
        self.assertEqual(len(calls),1);self.assertEqual(s['run_state'],'Stopped')
    def test_pause_finishes_one_worker(self):
        s=self.state()
        def done(state,asset,*args):asset.update(status='Needs Review',validated=True);q.atomic(q.CONTROL,{'requested':'pause'})
        self.assertEqual(self.invoke(s,done),1);self.assertEqual(s['run_state'],'Paused')
    def test_stop_finishes_one_worker(self):
        s=self.state()
        def done(state,asset,*args):asset.update(status='Needs Review',validated=True);q.atomic(q.CONTROL,{'requested':'stop'})
        self.assertEqual(self.invoke(s,done),1);self.assertEqual(s['run_state'],'Stopped')
    def test_live_orphan_cannot_launch_second_worker(self):
        s=self.state();s['active_worker']={'pid':os.getpid()}
        with self.assertRaisesRegex(RuntimeError,'still active'):self.invoke(s,lambda *a:self.fail('Worker launched'))
    def recovery_fixture(self):
        s=self.state(1);folder=self.root/'Logs/runs/recover';folder.mkdir(parents=True)
        q.atomic(folder/'job.json',{'slug':'Asset1','pipeline':'Blender','source_path':'Assets/Source','output_path':'Assets/Output','smoke_test':False})
        q.atomic(folder/'checkpoint-hashes.json',{})
        s['active_worker']={'pid':-1,'run_id':'abc_123','slug':'Asset1','smoke_test':False,'job_file':'Logs/runs/recover/job.json'}
        return s
    def test_crash_recovery_accepts_only_verified_evidence_without_rebuild(self):
        s=self.recovery_fixture()
        with patch.object(q,'process_alive',return_value=False),patch.object(q,'orphan_pid',return_value=None),patch.object(q,'verify_preservation'),patch.object(q,'verify_evidence',return_value={'screenshot_path':'Assets/Output/preview.png'}):
            self.assertEqual(self.invoke(s,lambda *a:self.fail('Verified asset rebuilt')),0)
        self.assertEqual(s['assets'][0]['status'],'Needs Review');self.assertTrue(s['assets'][0]['validated'])
    def test_interrupted_exhausted_attempt_is_blocked(self):
        s=self.recovery_fixture();s['assets'][0]['attempt_count']=3
        with patch.object(q,'process_alive',return_value=False),patch.object(q,'orphan_pid',return_value=None),patch.object(q,'verify_preservation'),patch.object(q,'verify_evidence',side_effect=FileNotFoundError('Unfinished evidence')):
            self.assertEqual(self.invoke(s,lambda *a:self.fail('Exhausted asset retried')),0)
        self.assertEqual(s['assets'][0]['status'],'Blocked')
    def test_lock_rejects_second_coordinator_and_releases(self):
        with q.Lock():
            with self.assertRaisesRegex(RuntimeError,'writer lock'):
                with q.Lock():pass
        with q.Lock():pass
    def test_dryrun_does_not_write(self):
        q.atomic(q.QUEUE,self.state());before=q.QUEUE.read_bytes()
        with patch.object(sys,'argv',['queue.py','dryrun']):q.main()
        self.assertEqual(before,q.QUEUE.read_bytes());self.assertFalse(q.CONTROL.exists())
    def test_preservation_detects_changed_gameplay(self):
        path=self.root/'Assets/Game.unity';path.parent.mkdir();path.write_text('original')
        baseline={'Assets/Game.unity':q.digest(path)};path.write_text('changed')
        with self.assertRaisesRegex(RuntimeError,'protection checkpoint'):
            q.verify_preservation(baseline,{'source_path':'Assets/Source','output_path':'Assets/Output','smoke_test':False})
        self.assertEqual(path.read_text(),'changed')  # Never silently overwrite possible user edits.
    def test_delayed_meta_allowed_only_for_unchanged_checkpoint_source(self):
        path=self.root/'Assets/Previous/preview.png';path.parent.mkdir(parents=True);path.write_bytes(b'original image')
        baseline={'Assets/Previous/preview.png':q.digest(path)}
        path.with_name('preview.png.meta').write_text('fileFormatVersion: 2\nguid: test')
        job={'source_path':'Assets/NewSource','output_path':'Assets/NewOutput','smoke_test':False}
        q.verify_preservation(baseline,job)
        path.write_bytes(b'changed image')
        with self.assertRaisesRegex(RuntimeError,'protection checkpoint'):q.verify_preservation(baseline,job)
    def test_all_42_tracker_statuses_synchronize_without_losing_formulas(self):
        before=PROJECT/'ProductionLogs/AssetQueueSetup/Before'
        for name in ['asset_registry.json','assets.csv','Asset_Tracker.xlsx','Tools/update-asset-tracker.py']:
            dest=self.root/name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(before/name,dest)
        state=q.initialize();self.assertEqual(len(state['assets']),42)
        for i,j in enumerate(state['assets']):j['status']=q.STATUSES[i%len(q.STATUSES)]
        q.sync(state)
        import zipfile,xml.etree.ElementTree as E
        with zipfile.ZipFile(self.root/'Asset_Tracker.xlsx') as z:
            self.assertIsNone(z.testzip());ns={'m':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
            sheet=E.fromstring(z.read('xl/worksheets/sheet2.xml'))
            vals=[v.text for v in sheet.findall('m:sheetData/m:row/m:c/m:v',ns)]
            for status in q.STATUSES:self.assertIn(status,vals)
            self.assertIn(b'COUNTA',z.read('xl/worksheets/sheet1.xml'))

if __name__=='__main__':unittest.main(verbosity=2)
