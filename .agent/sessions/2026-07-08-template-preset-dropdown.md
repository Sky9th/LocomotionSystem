# 2026-07-08 — Template Preset Dropdown

## Background

Previously the Template field in EntityEditorWindow was a raw ObjectField — users had to drag-and-drop PropertyTreeSO assets. This was slow and error-prone. The user wanted a dropdown with hardcoded presets per category, filtered by the entity's concrete SO type.

## Changes

### EntityEditorWindow base
- Added `GetTemplatePresets(Type selectedType)` virtual method (returns null = fallback to ObjectField)
- Added `DrawTemplateField()` — renders dropdown via GenericMenu when presets exist, ObjectField otherwise
- Added `ResolvePresetSOs()` — caches PropertyTreeSO asset lookup by name

### Category editors
- WeaponEditorWindow: MeleeWeaponSO → melee-only presets; RangedWeaponSO → ranged-only presets
- PropEditorWindow: 6 sub-types each get filtered presets (Armor → armor trees, Ammo → ammo trees, etc.)
- CharacterEditorWindow, BuildingEditorWindow, SceneItemEditorWindow: full preset lists (single-type)

## Decisions

| Decision | Alternatives | Reason |
|----------|-------------|--------|
| `GetTemplatePresets(Type selectedType)` with type parameter | A: Separate method per sub-type → explosion of override methods. B: Dictionary<Type, string[]> config → less flexible. | Single virtual method, switch expression in subclass, clean. |
| GenericMenu dropdown (not EditorGUILayout.Popup) | Popup requires int index mapping → fragile when presets change. | GenericMenu with label matching is resilient to missing/moved assets. |
| Resolve by asset name via AssetDatabase | A: Direct SO references → can't hardcode in code-only subclass. B: GUID-based → unreadable. | Asset names are human-readable and stable. Missing assets show as disabled items. |

## Known Issues

- [ ] `ResolvePresetSOs` scans all PropertyTreeSO assets each frame (called in OnGUI) — should cache with dirty check (P2 — perf acceptable for editor tool with ~35 trees)

## Cross-References

### Related Sessions
- [2026-07-08-entity-editor-architecture-refactor.md](2026-07-08-entity-editor-architecture-refactor.md) — main EntityEditor refactoring session

### Related Tech Docs
- `tech/editor/tools/L2_EntityService/EntityEditorWindow.md` — updated with GetTemplatePresets method

### Flag for Design Doc Creation
- [x] No design doc needed — editor UI enhancement, no gameplay change.
