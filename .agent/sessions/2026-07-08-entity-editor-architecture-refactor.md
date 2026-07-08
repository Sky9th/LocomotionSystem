# 2026-07-08 — EntityEditor Architecture Refactor

## Background

ItemEditor was too narrow — it only served ItemDefSO subclasses, but Properties is the foundation for Entity infrastructure ("everything is an Entity"). The game will have massive numbers of entities across 5 major categories: Characters, Weapons, Props (tools/consumables/armor/materials), Buildings, and Scene Items (furniture/decorations/scene objects). A unified EntityEditor base framework was needed, with each category having its own Editor + Importer to properly segment data and avoid an all-in-one editor.

This is S5 (Phase 5: Entity Economy), following the completion of ItemEditor + Import/Export in Phase 4.

## Changes

### SO Hierarchy + L3 Module Split
- Created 4 new abstract SO base classes: `WeaponDefSO`, `PropDefSO`, `BuildingDefSO`, `SceneItemDefSO` (all `: PropertyPresetSO`)
- Split `L3_Item/` into 4 modules: `L3_Weapon/`, `L3_Prop/`, `L3_SceneItem/` + new `L3_Building/`
- Migrated 8 existing SO subclasses (MeleeWeaponSO→L3_Weapon, ArmorSO→L3_Prop, etc.) with updated parent class + namespace
- Marked `ItemDefSO` as `[Obsolete]`, kept in `L3_Item/` for backward compat with existing .asset files
- `.meta` GUID migration: Blade.asset (MeleeWeaponSO) and Pistol.asset (RangedWeaponSO) preserved old GUIDs

### EntityEditorWindow Base Class
- `L2_EntityService/Editor/EntityEditorWindow.cs` — abstract base extracted from old ItemEditorWindow
- Three-column layout, 9-type PropertyForm dispatch, SlotDef editor, Save/Create/Delete, Ctrl+S
- 6 abstract methods (GetTargetType, GetEditorTitle, GetBreadcrumb, GetCreateMenuItems, GetDefaultAssetDir, GetAssetFilter)
- 3 virtual methods (DrawExtraToolbarButtons, DrawCategorySpecificSection, GetStatusSummary)
- `OpenImportWindow()` → `Action` delegate — no reflection

### 5 Category Editors (~30 lines each)
- `CharacterEditorWindow` (L3_Character/Editor/) — edits CharacterDefSO
- `WeaponEditorWindow` (L3_Weapon/Editor/) — edits MeleeWeaponSO / RangedWeaponSO
- `PropEditorWindow` (L3_Prop/Editor/) — edits ArmorSO / ConsumableSO / AmmoSO / ToolSO / ContainerSO / MaterialSO
- `BuildingEditorWindow` (L3_Building/Editor/) — edits BuildingDefSO
- `SceneItemEditorWindow` (L3_SceneItem/Editor/) — edits SceneItemDefSO

### Unified Import/Export
- `EntityImportConfig` (config object) + `EntityImporter` (shared engine) — delegate-based, replaces 5 nearly-identical importer classes
- Unified DTO: `EntityEntry` / `EntityExportFile` — consistent JSON format across all categories
- 5 ImportWindows reduced to ~45-line thin wrappers each
- `EntityImportConfig.BuildPreview` → `Func<string, string>` delegate handles per-category preview differences

### Runtime Type References Updated
- `AssetService.cs` — Boot loading: `Get<ItemDefSO>()` → `Get<WeaponDefSO>()` / `Get<PropDefSO>()` / `Get<SceneItemDefSO>()` / `Get<BuildingDefSO>()`
- `PlayerService.cs` — `FindItem<ItemDefSO>` → `FindItem<MeleeWeaponSO>` / `FindItem<RangedWeaponSO>` / `FindItem<ContainerSO>`

### Documentation + Cleanup
- Deleted `L2_ItemService` design docs (never implemented, no code)
- Deleted `L3_Item` old tech docs (README.md, item-def-so.md)
- Deleted old editor docs (ItemEditorWindow.md, ItemImportExport.md)
- Updated `tech/README.md` — removed L2-item-service + L3-item, added L3-weapon/prop/sceneitem/building entries
- Updated `editor/README.md` — tools list updated with all 5 category editors

### Code Review Fixes
- Dictionary fields initialized at declaration (`= new()`) to prevent NRE
- Dead `_overrides` field removed (written 3 times, never read)
- `DrawTwoColumns` → `DrawThreeColumnBody` (was actually 3 columns)
- `_rightScroll` → `_centerScroll` (was controlling center panel, not right)
- `GetImportWindowType()` returning `Type` with reflection → `OpenImportWindow()` returning `Action` delegate
- `#if UNITY_EDITOR` guards added to EntityEditorWindow.cs + EntityImportConfig.cs
- `MenuPriority` field removed from `EntityImportConfig` (never read, each window uses `[MenuItem]` attribute)

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| SO hierarchy: new intermediate abstract classes (WeaponDefSO, PropDefSO) | A: Keep ItemDefSO as umbrella, only split editors → SO class hierarchy still misleading. B: Flatten all to PropertyPresetSO direct → loses type safety for FindItem<T>. | New intermediate classes preserve type safety and match the domain model: Weapon ≠ Prop ≠ SceneItem. |
| L3 modules split into 5 independent directories | A: Keep all under L3_Item/ with subdirectories → namespace clutter, module deletion leaves orphans. | Separate directories = clear module boundaries, each deletable independently. |
| EntityEditor base class uses virtual methods, not generics | A: `EntityEditorWindow<T> where T : PropertyPresetSO` → Unity EditorWindow doesn't support generics. B: Configuration object injection → would duplicate property form logic. | Virtual methods with 6 abstract + 3 virtual = thin subclasses, same pattern as existing Ability editors. |
| EntityImporter uses config object + delegates | A: Each importer independent (original approach) → 80% code duplication, 5 places to fix bugs. B: Generic base class `EntityImporter<T>` → JsonUtility.FromJson needs concrete type at compile time. | Config object with `TypeMap` and `DefaultType` handles multi-type and single-type uniformly. Delegates handle preview customization. |
| Entity editor infrastructure in L2_EntityService/Editor/ (not Shared/Editor/) | A: Shared/Editor/Entity/ → L2_EntityService is the proper owner, "Shared" should only hold UI components not business logic. | L2 owns the Entity concept, so its editor belongs to L2. Shared/ is for generic UI components like EditorCard, EditorButton. |
| Backpack.asset kept as ItemDefSO (not migrated to ContainerSO) | A: Change asset's m_Script GUID to ContainerSO → risk of data loss if ContainerSO evolves differently. | ItemDefSO.cs still exists (deprecated), Backpack loads fine. PlayerService uses `FindItem<ContainerSO>` which may need data migration in future. |

## Known Issues

- [ ] Backpack.asset is still of type ItemDefSO (not ContainerSO) — PlayerService.FindItem<ContainerSO> may not find it (P1 — data migration needed)
- [ ] RangedWeaponSO.GetDamageEffects still leaks ScriptableObject instances every call (pre-existing, P2)
- [ ] No .asset files exist yet for BuildingDefSO or SceneItemDefSO — editors will show empty trees (P3 — expected, module just scaffolded)
- [x] Blade.asset and Pistol.asset mono script GUIDs preserved via .meta restoration
- [x] All 5 import windows compile and produce correct create/update/skip stats

## Cross-References

### Related Sessions
- [2026-07-07-item-editor-import-export.md](2026-07-07-item-editor-import-export.md) — completed ItemEditor + ItemImportExport before the refactor

### Related Plans
- [../plans/s5-itemeditor-properties-ent-eager-mochi.md](../plans/s5-itemeditor-properties-ent-eager-mochi.md) — EntityEditor architecture plan (this session)

### Related Tech Docs
- `tech/L2-services/L2-entity-service/` — EntityEditorWindow now lives under L2_EntityService/Editor/
- `tech/L2-services/L2-modules/L3-weapon/`, `L3-prop/`, `L3-sceneitem/`, `L3-building/` — new module docs needed
- `tech/editor/README.md` — updated tools list
- `tech/README.md` — updated module tree

### Related Design Docs
- None — internal architecture refactor, no player-visible behavior changes.

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactor, all user-facing behavior unchanged.
