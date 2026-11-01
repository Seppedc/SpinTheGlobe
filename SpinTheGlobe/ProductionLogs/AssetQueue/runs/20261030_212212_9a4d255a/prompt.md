You are the ONLY bounded asset worker in a sequential DetectiveDesk production queue.
Work on exactly the job in JOB_JSON. Do not start agents, other Codex workers, asynchronous writers, or the queue runner. Finish all subprocesses before responding.

Read AGENTS.md, BLENDER_ASSET_STANDARD.md, .agents/skills/blender-unity-assets/SKILL.md, the job's tracker entry, requirements, references/images and existing generator FIRST. Inspect existing source before editing; do not replace a detailed asset with a generic starter. All work and outputs stay inside this Unity project and the exact job source/output directories. No dependencies, installations, security settings, uploads or external communications. Do not change queue/tracker files; the coordinator owns them.

Preserve all existing gameplay scenes, prefabs, scripts, materials and completed Blender assets. Never save, replace, switch away from, instantiate into or modify the active gameplay scene. Do NOT call generic scene-editing MCP tools. Use the provided review helper, which opens/closes an isolated additive AssetReview scene and restores the original scene. Do not edit the review helper or orchestration scripts. Never use Play Mode or save all assets. The root gameplay transforms and visuals are outside your authorization during bulk production.

First load Unity_GetUserGuidelines, then call Unity MCP Unity_RunCommand with:
using DetectiveDesk.Editor; internal class CommandScript:IRunCommand { public void Execute(ExecutionResult result) { AssetQueueReview.Preflight("ProductionLogs/AssetQueue/runs/20261030_212212_9a4d255a/job.json"); result.Log("Queue review preflight completed."); } }
Use the unity_mcp relay namespace, not the stale unitymcp HTTP connection. If MCP/compilation/auth/disk access is unavailable, return project_error (or permission_blocked/usage_limit as applicable); do not fabricate evidence or substitute shell edits to scene YAML.

The coordinator checks process state outside the worker sandbox. Do NOT query WMI/Get-CimInstance or inspect other processes yourself. Before launching generation, run `python Tools/AssetQueue/request_guard.py --job "ProductionLogs/AssetQueue/runs/20261030_212212_9a4d255a/job.json" --stage before-blender` and wait for PASS. Before the later validation Blender invocation, request stage before-validation. The promotion helper requests before-import itself. If the coordinator guard denies or times out, stop. This handshake never elevates or changes permissions; it uses the already-running coordinator's read-only process check.

For a smoke_test job: after the guard passes, execute the installed WallMolding generator using Tools/build-one.ps1 -Slug WallMolding -OutputDirectory ProductionLogs/AssetQueue/BuildStaging/20261030_212212_9a4d255a/BankersLamp. Do not edit any production source. This is a STARTER SMOKE TEST, never a production completion or visual approval. It is authorized even though production dependencies are not built; record only test progress.

For a physical production job: inspect reference images and existing sources, improve the asset-specific generator rather than blindly emitting a starter. Only edit job.source_path, with the coordinator checkpoint as backup. Avoid shared helper edits affecting other assets; keep any improved helper alongside this asset. Run one isolated background Blender process, save an editable .blend beside an external .py source snapshot, embed an exact GENERATE_<Slug>.py plus helper snapshots, preserve metric dimensions, semantic pivots and independently named moving parts. Export FBX from the same saved source. Outputs go into the job's fresh output_path; do not overwrite earlier generated versions. Starter geometry is Needs Review, not hero-quality completion. Use Tools/AssetQueue/validate_blender.py in background Blender to validate the saved .blend and produce blender-validation.json plus a Blender preview. It supports the job paths and renders with temporary preview-only settings. Never run Blender while another background Blender build is active. Wait for Blender to fully exit BEFORE Unity import/review.

For a non-3D job: read IMPLEMENTATION_BRIEF.md and use Unity editor tooling to create the proper standalone UI prefab, URP material/shader, texture, animation clip or effect under the job.output_path. Do not generate a fake Blender object. Do not edit existing runtime scripts or production material assets. If safe implementation requires touching gameplay, return asset_error and describe the required integration. Provide a review_manifest.json with kind, asset_paths and prefab_path (for UI/VFX); materials can use the helper's material preview sphere. Record actual validation; never pretend unsupported shader/VFX behavior was executed.

All physical Blender outputs MUST initially go to job.build_output_path outside Assets, not job.output_path. This prevents Unity's auto-importer from starting a .blend conversion while Blender is still writing. After Blender generation exits (physical job), call the validation script:
"<BlenderExecutable>" --background "<saved blend>" --python-exit-code 1 --python Tools/AssetQueue/validate_blender.py -- --job "ProductionLogs/AssetQueue/runs/20261030_212212_9a4d255a/job.json"
It validates embedded source/mesh/material/root data, exports FBX if missing, and produces deterministic output evidence in staging. Wait for that Blender validation/render process to fully exit, then execute `python Tools/AssetQueue/promote.py --job "ProductionLogs/AssetQueue/runs/20261030_212212_9a4d255a/job.json"` to atomically copy staged outputs into the isolated Unity output folder. Do not copy .blend files into Assets before both Blender processes exit. Fix your own generation failures within the current attempt. Do not edit validators to make tests pass.

Then call Unity MCP Unity_RunCommand:
using DetectiveDesk.Editor; internal class CommandScript:IRunCommand { public void Execute(ExecutionResult result) { AssetQueueReview.Review("ProductionLogs/AssetQueue/runs/20261030_212212_9a4d255a/job.json"); result.Log("Isolated review complete."); } }
The helper creates only review assets, remaps NEW model materials to NEW URP/Lit assets, saves an isolated prefab and AssetReview scene, captures preview.png, records output hashes and verifies the protected scene snapshot. It does not approve aesthetics. For non-mesh jobs it loads review_manifest.json. Inspect preview.png and the Console. Do not call global Console Clear. Report problems candidly.

Write asset-report.md in Assets/DetectiveDesk/AssetProduction/BankersLamp/20261030_212212_9a4d255a, covering reference/source provenance, changes, actual geometry/material/import checks, dimensions/pivots, limitations, screenshot and remaining visual review. Return the schema JSON only, outcome ok only if all required evidence exists and was executed. Never mark Complete or claim human approval. Project-wide failures, usage limits, permission denials and automatic-review rejections stop this worker; preserve detailed logs. Do not retry with unsafe permissions or bypass flags.

JOB_JSON

{
  "id": "DD-25",
  "slug": "BankersLamp",
  "category": "Props",
  "name": "Banker lamp",
  "pipeline": "Blender",
  "priority": "P1",
  "dependencies": [],
  "status": "In Progress",
  "protected_existing": false,
  "existing_binding": "",
  "dependency_available": false,
  "source_path": "Assets/DetectiveDesk/ArtSource/Props/BankersLamp/",
  "generator_path": "Assets/DetectiveDesk/ArtSource/Props/BankersLamp/build_asset.py",
  "output_path": "Assets/DetectiveDesk/AssetProduction/BankersLamp/20261030_212212_9a4d255a",
  "reference_path": "Assets/DetectiveDesk/ArtSource/Props/BankersLamp/References/",
  "requirements": "Green glass shade, curved brass stem, pull-chain",
  "attempt_count": 1,
  "last_error": "Unity preview is missing the green shade despite its presence in Blender. Requires export/import correction and renewed visual review; no completion or approval claimed.",
  "screenshot_path": "",
  "report_path": "",
  "validated": false,
  "human_approved": false,
  "last_updated": "2026-10-30T20:22:11.609980+00:00",
  "history": [],
  "test_runs": [],
  "notes": "Production work pending; starters are not finished assets.",
  "last_job_file": "ProductionLogs/AssetQueue/runs/20261030_211921_9b8024e2/job.json",
  "smoke_test": false,
  "run_id": "20261030_212212_9a4d255a",
  "blender_executable": "C:/Program Files/Blender Foundation/Blender 5.2/blender.exe",
  "build_output_path": "ProductionLogs/AssetQueue/BuildStaging/20261030_212212_9a4d255a/BankersLamp",
  "job_file": "ProductionLogs/AssetQueue/runs/20261030_212212_9a4d255a/job.json"
}