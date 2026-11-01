import json
from pathlib import Path
p=Path("ProductionLogs/PinLauncherRework")
before=json.loads((p/"scene-components-before.json").read_text(encoding="utf-8-sig"))["objects"]
after=json.loads((p/"preservation-after.json").read_text(encoding="utf-8-sig"))
rows={r["id"]:r for r in after["rows"]}
objects={o["id"]:o for o in after["objects"]}
root="DetectiveOffice/GameplayObjects/ToyPinLauncher - aimed up at map"
errors=[]; allowed=[]; count=0; slots=[]
for o in before:
 a=objects.get(o["identity"])
 if a is None: errors.append({"missing_object":o["path"]}); continue
 for key in ["path","activeSelf","layer","tag","siblingIndex","prefab"]:
  if a[key]!=o[key]:errors.append({"object":o["path"],"field":key,"before":o[key],"after":a[key]})
 if "slot" in o["path"].lower():slots.append(o["path"])
 for c in o["components"]:
  count+=1;a=rows.get(c["identity"])
  if a is None:errors.append({"missing_component":c["identity"]});continue
  old=json.loads(c["serializedValues"]);new=json.loads(a["json"])
  if old!=new:
   if o["path"].startswith(root+"/") and c["type"].endswith("MeshRenderer"):
    normalized=json.loads(a["json"])
    for v in normalized.values():v["m_Enabled"]=next(iter(old.values()))["m_Enabled"]
    if normalized==old:allowed.append({"renderer":o["path"],"change":"enabled only"})
    else:errors.append({"component":o["path"],"type":c["type"],"before":old,"after":new})
   else:errors.append({"component":o["path"],"type":c["type"],"before":old,"after":new})
  refs=[r["property"]+"="+r["identity"] for r in c["references"]]
  extra=set(a["refs"])-set(refs);missing=set(refs)-set(a["refs"])
  if extra or missing:
   if o["path"]==root and c["type"]=="UnityEngine.Transform" and not missing and len(extra)==1 and next(iter(extra)).startswith("m_Children.Array.data["):
    allowed.append({"parent_child_addition":list(extra)})
   else:errors.append({"references":o["path"],"type":c["type"],"extra":list(extra),"missing":list(missing)})
report={"protected_scene_unchanged":not errors,"original_objects_checked":len(before),"original_components_checked":count,"allowed_changes":allowed,"slot_paths":slots,"errors":errors}
(p/"preservation-comparison.json").write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2) if errors else json.dumps({k:v for k,v in report.items() if k not in ["allowed_changes","slot_paths"]},indent=2))
print("allowed changes",len(allowed),"slot matches",len(slots))
