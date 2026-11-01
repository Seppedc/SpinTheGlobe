"""Update registry, CSV and the formatted Excel tracker together using Python stdlib.
Example: python Tools/update-asset-tracker.py --slug WallMolding --status Review --check 'Executed validation details'
Existing tracker files are backed up under ProductionLogs/TrackerBackups before writing.
The pack's `status` field describes source maturity; `progress_status` matches Excel's Status column.
"""
import argparse
import csv
import datetime
import io
import json
import os
from pathlib import Path
import shutil
import uuid
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]
NS = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'
ET.register_namespace('x', NS)
ET.register_namespace('r', 'http://schemas.openxmlformats.org/officeDocument/2006/relationships')
def q(name): return '{'+NS+'}'+name
def value(cell):
    if cell is None: return ''
    return cell.findtext(q('v')) or cell.findtext(q('is')+'/'+q('t')) or ''
def get(row, column):
    return next((c for c in row.findall(q('c')) if c.get('r') == column+row.get('r')), None)
def put(row, column, text, style=None):
    cell = get(row, column)
    if cell is None:
        cell=ET.SubElement(row,q('c'),{'r':column+row.get('r')})
        if style is not None: cell.set('s', style)
    cell.set('t','str')
    for child in list(cell): cell.remove(child)
    ET.SubElement(cell,q('v')).text=str(text)

def main():
    if (ROOT/'asset_queue.json').exists():
        raise SystemExit('asset_queue.json is now the source of truth. Use Tools/Run-AssetQueue.ps1 and Tools/AssetQueue/set_status.py; direct tracker-only edits are disabled to prevent divergence.')
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--slug',required=True)
    parser.add_argument('--status',choices=['Not Started','In Progress','Review','Complete','Blocked'])
    for field in ('maturity','source','builder','blend','unity','ref','check','notes','evidence','fbx'):
        parser.add_argument('--'+field)
    parser.add_argument('--owner',default='Codex')
    args=parser.parse_args()
    registry=ROOT/'asset_registry.json'; workbook=ROOT/'Asset_Tracker.xlsx'; csvfile=ROOT/'assets.csv'
    records=json.loads(registry.read_text(encoding='utf-8-sig'))
    matches=[a for a in records if a['slug']==args.slug]
    if len(matches)!=1: raise ValueError('Expected one registry entry for '+args.slug)
    entry=matches[0]
    with zipfile.ZipFile(workbook) as z:
        original={info.filename:(info,z.read(info.filename)) for info in z.infolist()}
    sheet=ET.fromstring(original['xl/worksheets/sheet2.xml'][1])
    rows=sheet.find(q('sheetData')).findall(q('row'))
    row=next(r for r in rows if value(get(r,'A'))==entry['id'])
    entry['progress_status']=args.status or value(get(row,'H'))
    for key in ('source','builder','blend','unity','ref','check','notes','evidence','fbx'):
        if getattr(args,key) is not None: entry[key]=getattr(args,key)
    if args.maturity is not None: entry['status']=args.maturity
    entry['owner']=args.owner
    entry['last_reviewed']=datetime.date.today().isoformat()
    fields={'G':'status','H':'progress_status','I':'owner','J':'builder','K':'blend','L':'unity',
            'M':'ref','N':'last_reviewed','O':'check','P':'notes','Q':'evidence'}
    for col,key in fields.items(): put(row,col,entry.get(key,''),style='0')
    header=next(r for r in rows if r.get('r')=='5')
    header_style=get(header,'N').get('s','0')
    for col,title in [('O','Executed validation'),('P','Progress notes'),('Q','Evidence / test output')]:
        put(header,col,title,header_style)
    columns=sheet.find(q('cols'))
    for number in (15,16,17):
        if columns is not None and not any(c.get('min')==str(number) for c in columns):
            ET.SubElement(columns,q('col'),{'min':str(number),'max':str(number),'width':'65','customWidth':'1'})
    dim=sheet.find(q('dimension'))
    if dim is not None: dim.set('ref','A1:Q47')
    filt=sheet.find(q('autoFilter'))
    if filt is not None: filt.set('ref','A5:Q47')
    changed={'xl/worksheets/sheet2.xml':ET.tostring(sheet,encoding='utf-8',xml_declaration=True)}
    queue=ET.fromstring(original['xl/worksheets/sheet3.xml'][1])
    for queued in queue.findall(q('sheetData')+'/'+q('row')):
        if value(get(queued,'B'))==entry['id']: put(queued,'G',entry['progress_status'])
    changed['xl/worksheets/sheet3.xml']=ET.tostring(queue,encoding='utf-8',xml_declaration=True)
    book=ET.fromstring(original['xl/workbook.xml'][1])
    calc=book.find(q('calcPr'))
    if calc is None: calc=ET.SubElement(book,q('calcPr'))
    calc.set('fullCalcOnLoad','1');calc.set('forceFullCalc','1')
    changed['xl/workbook.xml']=ET.tostring(book,encoding='utf-8',xml_declaration=True)
    old_csv=list(csv.DictReader(io.StringIO(csvfile.read_text(encoding='utf-8-sig'))))
    csv_fields=list(old_csv[0])
    for record in records:
        for field in record:
            if field not in csv_fields: csv_fields.append(field)
    buffer=io.StringIO(newline='')
    writer=csv.DictWriter(buffer,fieldnames=csv_fields);writer.writeheader();writer.writerows(records)
    backup=ROOT/'ProductionLogs/TrackerBackups'/(datetime.datetime.now().strftime('%Y%m%d_%H%M%S')+'_'+uuid.uuid4().hex[:8])
    backup.mkdir(parents=True)
    for path in (registry,csvfile,workbook): shutil.copy2(path,backup/path.name)
    tempbook=backup/'updated.xlsx'
    with zipfile.ZipFile(tempbook,'w') as out:
        for name,(info,data) in original.items(): out.writestr(info,changed.get(name,data))
    with zipfile.ZipFile(tempbook) as test:
        assert test.testzip() is None
        for name in changed: ET.fromstring(test.read(name))
    try:
        registry.write_text(json.dumps(records,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
        csvfile.write_text(buffer.getvalue(),encoding='utf-8')
        os.replace(tempbook,workbook)
    except Exception:
        for path in (registry,csvfile,workbook): shutil.copy2(backup/path.name,path)
        raise
    print('Updated '+args.slug+' in JSON, CSV and formatted XLSX; backup: '+str(backup))
if __name__=='__main__': main()
