# 2026-07-08 — Design 目录清理 + ItemEditor 功能扩展

## Background

`.agent/design/` 目录长期混杂大量技术文档（架构设计、类层级、运行时流程），与 `.agent/tech/` 中的对应文档重复。21 个文件中仅 10 个是纯策划文档，其余为技术文档或资产清单。需要清理以明确 design/ 和 tech/ 的边界：design/ 只放策划关心的游戏设计，tech/ 放所有技术实现文档。

同时 ItemEditorWindow 在上一个 session 仅支持 3 种物品类型（Item/MeleeWeapon/RangedWeapon），需要扩展到全部 8 种类型并接入 Import/Export 窗口。

## Changes

### Design 目录清理
- 删除 8 个与 tech/ 重复的技术文档：`ability-tag-design.md`, `audio-system.md`, `equipment-system.md`, `event-architecture.md`, `l1-l5-layering.md`, `stats-system.md`, `ui-system.md`, `combat/hit-reaction.md`
- 删除 3 个资产清单（内容有误，不应归档）：`asset-inventory.md`, `icon-inventory.md`, `scene-asset-inventory.md`
- design/ 从 21 个文件精简至 10 个纯策划文档

### L3_Item Editor 扩展
- `ItemEditorWindow.cs` — +Create 菜单扩展为 8 种类型（Item, Melee/Ranged Weapon, Armor, Ammo, Consumable, Container, Material, Tool），按层级分组加 separator
- Import/Export 按钮从占位 Debug.Log 改为调用 `ItemImportWindow.Open()`

### 代码清理
- `DataLabelTools.cs` — 删除废弃的 `TagAllData()` / `TagPrototypeArt()` 菜单方法和 `TagFolder()` 辅助方法（功能已被 BootInitRunner 替代），保留 `EnsureBootLabel()`
- `CreateSkillCardPrefab.cs` + `.meta` — 删除废弃编辑器工具

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 重复技术文档直接删除而非合并 | A: 逐文件对比合并差异 → 工作量大，大部分内容已被 tech/ 对应文档覆盖 | design/ 中的技术文档多为早期草稿，tech/ 已有更完整版本 |
| 资产清单直接删除而非迁移 | A: 迁入 tech/shared/ → 用户指出内容有误，不应归档 | 资产清单是早期随手记录，数据已过期且格式不规范 |
| 删除废弃的 DataLabelTools 菜单方法 | A: 保留并修复 → 批量标记功能已被 BootInitRunner 的自动流程替代 | 死代码应删除，EnsureBootLabel 作为 Importer 调用的公共 API 保留 |

## Known Issues

- [ ] design/ai/, design/character/, design/level/ 三个空目录仍存在 — P3 — 确认是否需要填充或删除

## Cross-References

### Related Sessions
- [2026-07-08-item-editor-window.md](2026-07-08-item-editor-window.md) — ItemEditorWindow 创建，本 session 在其基础上扩展物品类型
- [2026-07-06-skillcard-ui-component.md](2026-07-06-skillcard-ui-component.md) — CreateSkillCardPrefab 的创建上下文，现已删除

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-item/README.md](../tech/L2-services/L2-modules/L3-item/README.md) — ItemDefSO 子类体系

### Flag for Design Doc Creation
- [x] No design doc needed — documentation reorganization + editor extension, no player-facing changes.
