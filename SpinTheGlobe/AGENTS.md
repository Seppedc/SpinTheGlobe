# DetectiveDesk — persistent rules for Codex

## Blender-first asset pipeline (mandatory)

When creating or modifying a 3D asset, ALWAYS follow `.agents/skills/blender-unity-assets/SKILL.md`.

1. Create or update one deterministic Blender Python generator for the asset.
2. Execute that generator with local Blender; never claim success if it has not run.
3. Save the editable `.blend` inside `Assets/DetectiveDesk/ArtSource/<AssetName>/` with generator and textures next to it.
4. Embed a copy of the generator in the `.blend` Text Editor as `GENERATE_<AssetName>.py` (and pack external image textures when possible).
5. Prefer placing the `.blend` directly in the Unity project and letting Unity import it. If incompatible, produce a `.fbx` *from the same Blender source* and use the FBX instead; never maintain an unrelated replacement mesh.
6. Use Unity MCP to inspect and replace *only visual children*, preserving prefabs, collider, scripts, animations, gameplay, transform and scene camera. Backup first.
7. Re-run the generator to rebuild from updated parameters rather than starting from scratch. Preserve stable object names and pivots.
8. Verify expected output files exist, nonempty mesh, material assignments, and no Unity import/Console errors. Take Game camera screenshot for visual comparison.
9. Report file paths, changed objects, successful validations, and limitations.

Do not destructively wipe Blender scenes unrelated to the target asset. Do not regenerate game visuals without updating the Blender source script and `.blend` together. The source `.blend` is the canonical editable model.

## Existing project protection

Inspect and merge existing files; never overwrite unrelated assets or scripts. Preserve gameplay roots, prefab references, colliders, animations, transforms and material overrides. For workflow setup or standalone import tests, do not instantiate assets in, save, or otherwise modify the open gameplay scene. Prefab replacement and scene saves apply only when requested for that asset. Keep existing unsaved user edits intact.

Blender is configured locally in `Tools/blender-path.json`; use `Tools/build-blender-asset.ps1 -AssetName <Name>`. The wrapper starts a separate empty background Blender process, preserving interactive Blender sessions. Use process-scoped `powershell -ExecutionPolicy Bypass -File ...` when needed; never change system execution policy. Review an existing output before an explicitly requested rebuild.

## Existing production asset bindings

For pin coffer changes, read `Assets/DetectiveDesk/ArtSource/PinCoffer/README.md` first. Update its original-derived `build_pincoffer.py`, rebuild with `-AssetName PinCoffer -ExportFbx`, then use the scoped **Tools > Detective Desk > Blender Assets > Refresh Pin Coffer From Blender** editor command. Preserve the existing prefab and normalized Mesh asset identities, insert lift, all twelve gameplay slot positions, colliders and existing material references. The old `Assets/Objects/Pinbox.blend` must remain because the current URP materials reference its sub-assets; do not treat it or ExampleCoffer as the production generator.

## Asset production pack tracking (merged from AGENTS_ASSET_ADDENDUM.md)

- Before asset work, read `Asset_Tracker.xlsx`, `asset_registry.json` and `BLENDER_ASSET_STANDARD.md`. Update the registry, CSV and spreadsheet together after each actual change, recording source paths, owner, review date, executed validation and limitations. Do not mark starter geometry or unexecuted tests complete.
- Use `python Tools/update-asset-tracker.py --slug <Slug> --status <Status> --check <ExecutedValidation> --notes <Notes>` to keep all three tracker formats synchronized with automatic backups. The pack's registry `status` describes source maturity; `progress_status` matches Excel's Status column. Preserve existing workbook formatting and unrelated progress.
- New assets use `Assets/DetectiveDesk/ArtSource/<Category>/<Slug>/`, with references in `References/` and maps in `textures/`. Existing established source paths take precedence: the completed coffer stays at `ArtSource/PinCoffer/`; the collectible pin retains `Assets/Objects/Pin.blend` and its verified detailed generator at `ArtSource/Gameplay/CollectiblePin/build_collectible_pin.py`. Inspect registry bindings before generating. Do not relocate completed assets or change their pivots, axis settings or material references just to match a starter template.
- Preserve and update detailed generators for PinCoffer and CollectiblePin. Their categorized `build_asset.py` files are generic reference starters, never production replacements. `Tools/build-one.ps1` blocks these two slugs; the existing coffer uses its established build and visual refresh commands. The unadapted detailed pin script must be inspected and safely adapted before a requested rebuild, including output paths and embedded source.
- Apply the existing Blender-first sequence: update source, execute isolated Blender, save editable `.blend` with its generator and helper dependencies embedded, export same-source FBX where needed, inspect Unity import, update only authorized visual children, verify camera/materials/Console, then record evidence in the tracker. Preserve existing gameplay and unrelated objects under the rules above.
- Pack generators are structured starters, not finished hero assets. UI, materials, textures, animation and VFX groups use their `IMPLEMENTATION_BRIEF.md` and the appropriate Unity tools rather than fabricated meshes. Reference images are art direction only; add new asset references when approved.

## Sequential unattended asset queue

`asset_queue.json` is now the automation source of truth for all 42 groups. `Tools/Run-AssetQueue.ps1` schedules exactly one bounded `codex exec` worker at a time, synchronizes Excel/registry/CSV, checkpoints existing files and enforces protected source bindings. Its default MaxAssetsPerRun is 3 in `Tools/AssetQueue/config.json`. Do not run production until explicitly instructed; a dry-run is read-only. Direct use of the older tracker-only updater is disabled while the queue exists. Human status decisions use `Tools/AssetQueue/set_status.py` after the runner stops, with evidence and explicit approval required for Complete.

Bulk workers write only assigned source/output directories and use `AssetQueueReview` for an isolated additive AssetReview scene. They never change active gameplay scenes, scene placements, existing gameplay scripts or completed assets. Physical models are built and validated in project-local ProductionLogs staging outside Assets; only after Blender exits are outputs promoted for Unity import. Request the coordinator's writer guard before each Blender/import phase; workers must not query WMI or broaden sandbox permissions. Never overlap queue workers, Blender builds or Unity review writers. Never invoke parallel agents within an asset worker.

Use only supported CLI permissions: `--approve-for-me` selects workspace-write and automatic review on this installation and conflicts with a separate `--sandbox` flag. If unavailable, use explicit workspace-write with approval_policy=never and stop on required permissions. Do not use deprecated full-auto or unrestricted bypass flags. Permission/auth/usage/disk/project-wide compilation failures stop the queue. Failed asset attempts get two retries; persistent failures become Blocked while independent work can proceed. Imported/validated assets remain Needs Review until human visual approval; smoke tests do not advance production statuses or satisfy dependencies.

## Gameplay bindings

The implemented game loop lives in `Assets/DetectiveDesk/Scripts/Gameplay/`; see `Assets/DetectiveDesk/Gameplay/README.md` and `Specification.md` before gameplay changes. One `GameManager` and one `GlobeController` own momentum and crossing-based payouts; the existing `DetectiveGame` remains the HUD/case adapter. Do not reintroduce its prototype passive income or a second sphere/needle rotation owner. The original RPM needle pivot is driven directly; its visual child keeps its identity pose. Forward geographic rotation uses negative Unity Y, with a monotonic positive phase for rewards and per-pin 360-degree rearming.

Preserve the current PhysX backend setting; the prior None fallback disabled collider queries and rigidbodies. Keep eight country assets and their Natural Earth provenance, existing coffer socket/skin bindings, original art references and current scene layout. The third figurine is intentional. Restoration art currently reuses disassembled authored parts, with a hook for four final authored tiers. Balance and cosmetic skin multipliers remain configurable. Gameplay installation helpers are initial setup only and must not be rerun over the configured scene. Validation is opt-in through `GameplayMathValidation` and `GameplayRuntimeValidation`; never save their temporary Play Mode money, configuration clones or upgrade state. Executed evidence is in `ProductionLogs/Gameplay/`.

Upgrades now use `Assets/DetectiveDesk/Gameplay/SkillTree/DefaultSkillTree.asset` and the reusable `SkillTreePanel.prefab`. The 39-node tree derives ownership from the existing manager levels/figurine tiers; prerequisite gates live in `GameManager.BuyUpgrade` and `RestoreFigurine`, not only the UI. Preserve existing cash costs, grandfathered ownership and the legacy panel retained inactive for rollback. The visible `GameUI/UpgradesButton` opens the tree beside Archive; figurine clicks focus their restoration path. Read the skill-tree README before editing graph IDs/requirements or UI. Initial `SkillTreeIntegration.Build` and `SkillTreeEntryIntegration.Install` refuse duplicate installation. Validation and preservation evidence is in `ProductionLogs/SkillTree/`; physical mouse/keyboard events were not synthesized.
