# DetectiveDesk sequential asset queue

The queue is installed and **Stopped**. No overnight production run has been started. Default `MaxAssetsPerRun` is **3**. Only one isolated WallMolding starter was generated during setup, through an actual non-interactive Codex CLI worker.

## Start from the Unity project root

```powershell
Set-Location 'C:\Users\decra\source\Antigravity\SpinTheGlobe\SpinTheGlobe'
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Run-AssetQueue.ps1 -Action DryRun -PlanCount 10
# Starts up to three eligible production assets, only when you choose to run it:
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Run-AssetQueue.ps1 -Action Resume -MaxAssetsPerRun 3
```

The execution-policy argument applies only to that PowerShell process. No system or user policy is changed. Keep Unity open with MCP connected and leave the project files alone during unattended production. An idle interactive Blender window can remain open; another background Blender build blocks scheduling. Do not run a second asset generator, queue or Unity-writing agent concurrently.

`Run` and `Resume` both recover persisted work before scheduling. `Resume` accepts completed, hash-verified evidence after a coordinator crash, or retries an unfinished attempt in a fresh versioned directory without deleting previous outputs. It refuses to start a second worker while the recorded PID or a matching orphan CLI process is alive. Fix the recorded project-wide blocker before resuming. It does not log in, install software or expand permissions automatically.

## Controls

From another terminal in the same project:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Run-AssetQueue.ps1 -Action Pause
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Run-AssetQueue.ps1 -Action Stop
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Run-AssetQueue.ps1 -Action Status
```

Pause and Stop are graceful: the current worker finishes to a safe boundary; no next worker is launched. They do not terminate Blender in the middle of saving a source. Resume launches only after the previous worker has exited. A coordinator timeout also retains the worker PID; it never kills it and immediately starts a replacement. If the coordinator has crashed while a worker is still running, wait for that worker to exit before Resume. Writer-guard requests fail closed if the coordinator is unavailable.

Edit `Tools/AssetQueue/config.json` to configure MaxAssetsPerRun, retries, minimum free disk space, timeout, dependencies and protected bindings. An explicit PowerShell `-MaxAssetsPerRun` overrides the configured default. Larger runs require a separate user instruction; setup did not execute one. DryRun reads queue state only, never invokes workers or updates assets/trackers, and displays a conditional dependency-aware plan. It requires the already initialized queue.

## Source of truth and statuses

`asset_queue.json` covers all 42 tracker groups, including non-3D work. Each entry records priority, dependencies, source/generator/output paths, attempts, last error, screenshots, reports, history, protected-existing status and test evidence. Queue writes use temporary files plus atomic replacement. An OS-held writer lock prevents two coordinators; stale lock files do not require deletion after a crash.

Statuses are `Not Started`, `In Progress`, `Generated`, `Imported`, `Validated`, `Needs Review`, `Blocked`, and `Complete`. The coordinator advances through generated/imported/validated states only after checking actual output/evidence files and hashes. Workers never mark Complete. Validated output ends at Needs Review because technical import does not prove aesthetic approval. Smoke tests are recorded under `test_runs` and do not advance production status or satisfy dependencies.

Excel, JSON registry and CSV are synchronized for all 42 rows after state changes; spreadsheet formatting, sheets, formulas and source-maturity fields are retained. Excel's status validation list includes all eight states. The queue is authoritative if a tracker write is interrupted; Resume synchronizes it again before workers run. Close Excel while running the queue: if it locks the workbook, the coordinator stops rather than losing progress. The original workbook and trackers are preserved under `ProductionLogs/AssetQueueSetup/Before/`, with additional per-worker checkpoints. The old tracker-only updater now refuses independent writes to prevent divergent status sources.

Human decisions can be recorded after the runner stops:

```powershell
# After inspecting a technically validated production asset:
python .\Tools\AssetQueue\set_status.py --slug DetectiveDesk --status Complete --human-approve --notes 'Reviewed preview and dimensions; approved for later integration'
# After fixing an asset-specific blocker, explicitly reset its retry budget:
python .\Tools\AssetQueue\set_status.py --slug DetectiveDesk --status 'Not Started' --retry-blocked --notes 'Blocker corrected; retry authorized'
```

Complete requires verified production evidence AND explicit human approval. An isolated smoke test is insufficient. Protected existing assets cannot be scheduled through the retry command; use a separately requested asset-specific migration or integration. Do not manually use generated/validated states to assert unexecuted work.

## One worker and one writer at a time

The installed CLI is `codex-cli 0.164.0-alpha.11.1`. Its captured help is in `ProductionLogs/AssetQueueSetup/CodexExecHelp.txt`. On this version, `--approve-for-me` selects workspace-write plus automatic review and **conflicts** with an additional `--sandbox` flag. The runner therefore invokes:

```text
codex exec --approve-for-me --skip-git-repo-check --json --color never -C <UnityRoot>
  -c mcp_servers.unitymcp.enabled=false
  --output-schema <worker-result.schema.json> --output-last-message <run/result.json> -
```

The final dash reads the job prompt from stdin. The unused HTTP Unity connector is disabled only for the worker invocation; the existing `unity_mcp` relay remains enabled. No global Codex config is changed. CLI versions without automatic review use explicit `--sandbox workspace-write` and `approval_policy=never`; any required permissions then stop the queue. No deprecated full-auto or unrestricted sandbox-bypass flags are used. CLI behavior was checked locally, with [official non-interactive guidance](https://developers.openai.com/codex/noninteractive) and [security guidance](https://developers.openai.com/codex/security) consulted.

The coordinator takes a complete existing-file checkpoint before each worker and checks pre-existing assets/settings/tools for unauthorized changes afterward. A worker may edit only its assigned unfinished source directory, its new output directory and the isolated review scene. It may not replace completed coffer/pin/globe/stamp/HUD work. Unexpected changed or new files outside those scopes stop the queue; backups are retained, but possible concurrent user edits are never silently rolled back.

Blender generation and validation/rendering happen under `ProductionLogs/AssetQueue/BuildStaging/`, outside Assets. Each process fully exits before the next phase. `request_guard.py` asks the already running coordinator to verify that no background Blender build is active; this avoids unsupported WMI access inside the Codex workspace sandbox without elevating the worker. `promote.py` waits for the before-import guard and atomically copies closed outputs into Assets. Unity can then import them without racing an external Blender writer. Unity's own internal `.blend` conversion is part of the import phase.

`AssetQueueReview` is a scoped editor helper. It verifies the original scene snapshot and Console, opens `Assets/DetectiveDesk/AssetReview/AssetReview.unity` additively, creates/refreshes only review assets, validates references/materials, captures a preview, saves/closes the review scene and restores the original active scene. Dedicated render-layer isolation prevents gameplay objects from entering review screenshots. It refuses a review if that layer is already in use or the review scene is already open. No Play Mode or main-scene save is performed.

## Errors and checkpoints

An asset-specific failure gets at most two retries after its first attempt, then becomes Blocked. Independent eligible work continues. Auth, usage-limit, automatic-review/permission, disk, broken Blender, unavailable MCP, protection-checkpoint or project-wide Unity compilation/Console failures stop the whole queue. It does not bypass a rejected action or assume network/account availability. Dependencies on blocked work remain pending; the queue never calls them complete.

Logs, prompts, invocation flags, worker JSONL, stderr, structured results, job manifests, guard decisions and checkpoint ZIP/hash files are under `ProductionLogs/AssetQueue/`. `queue.log` is the human-readable index. Each output contains external Python source, editable source, FBX where applicable, validation JSON, preview image and `asset-report.md`. Checkpoints are retained; no automatic pruning or deletion was added. Expect disk usage to grow with runs.

UI, texture, material, shader, VFX and animation workers read the implementation brief and create appropriate standalone Unity assets. Texture/material preview fixtures are only review surfaces, not fake production meshes. Non-3D automation is implemented but was not end-to-end executed during this setup. If safe work requires gameplay integration, the worker records a blocker instead of editing gameplay.

## Executed setup checks

- Read existing instructions, engineering standard, skill, registry and master workbook; inspected ArtSource and existing prefabs/materials through MCP. The audit is in `ProductionLogs/AssetQueueSetup/ExistingUnityAssets.txt`. Completed detailed sources were protected, not rebuilt or simplified.
- Blender executable verified at `C:/Program Files/Blender Foundation/Blender 5.2/blender.exe` (5.2.3 LTS). Codex authentication was available. Unity MCP connected.
- Dry-run next ten: DetectiveDesk → DeskStructure → BackWall → WallMolding → WorldMap → WorldMapFrame → RainyWindow → PinLauncher → CaseFiles → StatueSlots. Later entries assume dependency checks pass.
- The first CLI startup exposed the conflicting permission flags and stopped without edits. A subsequent worker passed Unity preflight but stopped on sandbox-denied WMI. No permission escalation occurred. Those logs are retained. After moving the process check to the coordinator handshake, the actual CLI smoke test succeeded.
- Successful run: `20261030_163124_74e8d266`. It generated exactly one isolated WallMolding starter, saved a `.blend` with exact embedded main/helper sources and external copies, exported FBX, validated 8 meshes/1,504 triangles/metric root/bounds, imported through Unity MCP, saved a review prefab and isolated scene, captured preview.png and wrote an asset report. All writer boundary checks passed. The coordinator independently verified hashes and recorded the test in all trackers.
- Test output: `Assets/DetectiveDesk/AssetProduction/_Tests/WallMolding/20261030_163124_74e8d266/`. Production WallMolding remains Not Started, attempts 0; the test is Needs Review. Its simple rails/uprights are not finished crown/baseboard artwork.
- Twelve project-local safety/state tests passed: 42-row/eight-state synchronization and formula preservation, dependency ordering, read-only dry-run, writer lock/release, two retries then Blocked/continue, project-wide stop, graceful Pause/Stop, live-orphan rejection, verified crash recovery, exhausted interrupted-attempt blocking and protected-file detection. These use isolated fixtures; no actual forced crash or 42-asset overnight run was claimed.
- The active gameplay scene's serialized component snapshot, dirty state and pre-existing asset files were checked after setup. See the final preservation files alongside the tests. No original gameplay scene/prefab/model was modified.

## Installed/changed workflow files

- Added `Tools/Run-AssetQueue.ps1`, `Tools/AssetQueue/config.json`, `queue.py`, `worker-prompt.md`, `worker-result.schema.json`, `validate_blender.py`, `request_guard.py`, `promote.py`, and `set_status.py`.
- Added `Assets/DetectiveDesk/Scripts/Editor/AssetQueueReview.cs` and Unity metadata.
- Added `asset_queue.json`, its control file, review scene, isolated test outputs and logs/checkpoints.
- Updated all 42 rows in Excel/registry/CSV to the queue schema, statuses, dependencies and evidence links; originals backed up.
- Appended queue rules to existing AGENTS.md; all previous instructions retained. Added the queue-authority guard to `Tools/update-asset-tracker.py`. Existing detailed generators and other tools remain unchanged.

Nothing is currently running. The start command above is supplied for your later explicit production instruction.

## First authorized production batch

After the user's instruction to start, generated DetectiveDesk (8 meshes / 2,400 triangles), DeskStructure (78 / 36,336), and BackWall (51 / 9,588), sequentially. Each retained its generator snapshot, embedded-source `.blend`, FBX, validation evidence, isolated review prefab, preview and report. All three ended at Needs Review, not Complete. The batch is now Stopped with no active worker; no fourth asset was scheduled.

The run exposed delayed Unity sidecars for the preceding asset's report and images. The guard initially stopped as designed. A regression fix permits only NEW `.meta` files whose checkpointed source still has the exact original hash; changed source or existing metadata remains protected. DeskStructure evidence was recovered after an audited coordinator-only hotfix, without regenerating it; BackWall then ran as the remaining third asset. The original checkpoint and `CoordinatorRecoveryAudit.json` are retained. The safety/state suite now has 13 passing tests, including this sidecar case.

Review limitations: the tabletop's packed grain/wear textures are not yet bound into the new Unity review materials; DeskStructure needs surface refinement and fit review; BackWall's Unity screenshot faces the rear, so use its Blender preview for the inset panel view until front-face review is corrected. None were placed into gameplay. Final Console returned zero errors/warnings, and the complete gameplay scene snapshot still matches its protected baseline, with original unsaved edits intact.

Batch paths and counts: `ProductionLogs/AssetQueue/FirstProductionBatch.json`. Detailed reports and images are in the corresponding `Assets/DetectiveDesk/AssetProduction/<Slug>/<RunId>/` directories. The configured default remains three assets per future run.
