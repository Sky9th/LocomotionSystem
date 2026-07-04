# 2026-07-04 — ImportExport Unification + Runtime Fixes Batch

## Background

The ImportExport system across 10+ modules had accumulated a subtle semantic issue: when an import
overwrites existing assets, they were counted as `skipped`, making it impossible to distinguish
between truly skipped entries and updated ones. Separately, several runtime bugs had been reported
during Pipeline animation testing — weapon physics jitter, pathfinding agent stalls, and windup
state ordering issues. This session addressed both the ImportExport API gap and the accumulated
runtime fixes in a single batch.

## Changes

### ImportExport Unification (Shared + 9 Modules)
- All `ImportFromJson()` signatures: `(created, skipped, errors)` → `(created, updated, skipped, errors)`
- Existing asset overwrite now increments `updated` instead of `skipped`
- Shared `ImportExport.cs` — Display, button, and result section all adapted to new tuple
- Window `onImport` callbacks simplified — no longer deconstruct/reconstruct the tuple
- Modules: Tag, Ability, AbilityTree, Activation, Effect, Noise, Search, Animation, Property

### Editor Fixes
- **EffectEditorWindow**: Added `description` TextArea field in detail form (was missing from editor)
- **EditorLabel**: `EditorGUILayout.LabelField` → `GUILayout.Label` with `EditorStyles.label` base style — fixes tooltip hover not responding on fixed-width labels
- **TagPicker**: `rootFilter` now supports multi-segment paths (e.g. `Ability.Effect`) via `_model.Find()` lookup

### Runtime Fixes
- **WindupState**: Reordered check — `_windupDuration <= 0` evaluates BEFORE animation fire marker, so windup-less abilities skip animation logic entirely
- **CharacterEquipment**: New `DisableViewPhysics()` method — disables Collider + sets Rigidbody kinematic on spawned weapon views, preventing physics push/jitter with enemy bodies
- **PathfindingAgent**: `SetDestination()` now sets `ai.isStopped = false`, resuming agents that were paused
- **AssetRefListPropertyDefSO**: Refactored GUID list parsing with `ParseGuidList()` using `JsonUtility` + `GuidListWrap` wrapper (matching `RdTagList` pattern)

### Data Updates
- **effects_all.json**: Full re-export from Unity Editor (expanded from ~60 lines to ~1200, covering entire field set per effect)
- **Melee combo links**: HeavyChop→SlashB, LightCut→2HitComboA, SlashB→3HitComboA, Kick→Grab, Punch→Kick — wired up previously-empty `NextSkill` references
- **Effect tuning**: Bleed baseValue 0→5 / maxStacks 1→5, Slow duration 2s→3s, description text corrections

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Add `updated` to existing tuple rather than separate bool/enum | A: Return a result struct with flags → over-engineering for callers. B: Add `updated` as separate out param → breaks the functional pattern. | Extending the tuple is the minimal change — all callers already destructure, adding one field is trivial. |
| WindupState: check duration before animation, not after | A: Keep original order but skip when `_windupDuration <= 0` inside the animation block → redundant nesting. | The fix also handles the case where no animation is assigned — avoids reading animation state unnecessarily. |
| Disable physics on weapon views rather than layer-based collision ignore | A: Use a dedicated physics layer + collision matrix → requires layer management, affects all weapon prefabs. B: Remove Collider components entirely → loses editor reference data. | Disabling at spawn is runtime-only, preserves prefab data, and handles Rigidbody (kinematic) which layer-based approach doesn't. |
| GUILayout.Label over EditorGUILayout.LabelField for tooltip fix | A: Add a custom EditorWindow.OnGUI wrapper → overkill for a label component. | EditorGUILayout.LabelField uses a cached GUIContent internally that prevents tooltip from updating. GUILayout.Label + new GUIContent each frame fixes this directly. |

## Known Issues

- [ ] Weapon view physics disabled globally — if a weapon prefab needs collision for non-combat purposes (e.g. environmental interaction), `DisableViewPhysics` will need a whitelist (P2 — no known use case yet)
- [ ] Effects data re-exported as v1.0 format — the old v2.0 format had hand-curated fields; verify no fields were lost in the Unity export round-trip (P1 — spot-check top 5 effects)
- [x] All ImportExport modules compile and pass static analysis

## Cross-References

### Related Sessions
- [2026-07-04-ability-pipeline-animation.md](2026-07-04-ability-pipeline-animation.md) — Pipeline testing uncovered the WindupState and Pathfinding bugs fixed here
- [2026-07-04-weapon-bar-cooldown-inventory-fixes.md](2026-07-04-weapon-bar-cooldown-inventory-fixes.md) — same session batch, different subsystem

### Related Tech Docs
- [tech/editor/README.md](../tech/editor/README.md) — Editor components (ImportExport, EditorLabel) documented
- [tech/L2-services/L2-modules/L3-ability/README.md](../tech/L2-services/L2-modules/L3-ability/README.md) — Ability editor tools + WindupState
- [tech/L2-services/L2-modules/L3-character/L4-equipment/README.md](../tech/L2-services/L2-modules/L3-character/L4-equipment/README.md) — CharacterEquipment
- [tech/L2-services/L2-modules/L3-properties/README.md](../tech/L2-services/L2-modules/L3-properties/README.md) — AssetRefListPropertyDefSO

### Flag for Design Doc Creation
- [x] No design doc needed — ImportExport unification is internal refactoring; runtime fixes restore intended behavior; data tuning (combo links, effect params) are implementation of existing design.

v0.36.6
