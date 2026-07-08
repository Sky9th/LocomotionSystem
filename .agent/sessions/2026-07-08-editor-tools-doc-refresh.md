# 2026-07-08 — Editor 工具文档全量刷新

## Background

`.agent/tech/editor/tools/` 目录下有 23 份文档，经过 Entity 分类重构（v0.41.0）和 Editor UI 组件迁移（AbilityTreeNode→EditorTreeNode、GameplayTag→RdTag），大部分文档已过时。2 份描述已删除类的死文档、10 份路径/类名过期、Entity 编辑器系统完全缺档。本次全量审计并刷新。

## Changes

### 删除（2 份死文档）
- `AbilityTreeNode.md` / `AbilityTreeView.md` — 对应 .cs 文件已删除，类已不存在

### 重写（4 份）
- `EntityEditorWindow.md` — 更新至当前架构：6 abstract 方法、9 种 PropertyDefSO 类型分发、Template 预设下拉
- `EditorCoreLoader.md` — 实现从单一 scene load 改为 SessionState 保存/恢复 `playModeStartScene`
- `TagEditorWindow.md` — GameplayTag→RdTag 重命名 + 从占位符展开为完整文档
- `TagImportExport.md` — 同上 + 记录实际类 `RdTagImporter` / `RdTagImportWindow`

### 修复（7 份）
- `EffectEditorWindow.md` / `NoiseEditorWindow.md` / `SearchEditorWindow.md` — AbilityTreeNode→EditorTreeNode、AbilityTreeView→EditorTreeView、GameplayTagDefinitionSO→RdTagDefSO、_foldouts 移除、EditorForm→EditorCard
- `GameContextEditor.md` — 源文件路径 `Assets/Scripts/Editor/` → `Shared/Editor/`
- `SyntyPrototypeBrowser.md` / `SyntyPrototypeMenu.md` — 同上 + MenuItem 修正
- `EditorTree.md` — 添加当前状态标注

### 新建（4 份）
- `EntityImporter.md` — 统一导入引擎 (EntityImportConfig / 5 Phase Import / 6 模块对照)
- `EntityEditorSubclasses.md` — 6 模块配置速查表 (AssetFilter / Template 预设 / Create 菜单)
- `EditorImportExport.md` — 共享 Import/Export UI 面板组件

### 索引
- `tech/README.md` editor/tools 区从占位容器重构为 5 层实际目录树

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Entity 6 子编辑器合并为一份速查表 | 每子类独立 .md | 子类均 ~40 行薄壳，独立文档高度重复，速查表更实用 |
| Tag 文档保留在 `L1_Core/GameplayTag/` 路径 | 跟随源码改名为 `L1_Core/RdTag/` | 文档路径稳定性 > 与源码目录 1:1 对应，避免链接断裂 |
| 死文档直接删除而非标记 STALE | 添加 "⚠️ DELETED" 标注保留文件 | 描述已删除类的文档无保留价值，应删除以保持索引清洁 |

## Known Issues

- [ ] Ability Tree 迁移未完成 — EffectEditorWindow ✅，Noise/Search/Activation/Ability 仍在使用旧的 BuildTree 模式 (P2)
- [ ] AbilityEditorUtility.md 仍为占位符，未展开 (P3)
- [ ] PropertyTreeEditorWindow.md 仍为占位符，未展开 (P3)

## Cross-References

### Related Sessions
- [2026-07-08-entity-data-audit-tag-alignment.md](2026-07-08-entity-data-audit-tag-alignment.md) — 同日 v0.41.1 Entity 数据审核
- [2026-07-08-entity-classification-four-layer-alignment.md](2026-07-08-entity-classification-four-layer-alignment.md) — v0.41.0 重构，本文档刷新的触发源

### Related Tech Docs
- [../tech/editor/tools/L2_EntityService/EntityEditorWindow.md](../tech/editor/tools/L2_EntityService/EntityEditorWindow.md)
- [../tech/editor/tools/L2_EntityService/EntityImporter.md](../tech/editor/tools/L2_EntityService/EntityImporter.md)
- [../tech/editor/tools/L2_EntityService/EntityEditorSubclasses.md](../tech/editor/tools/L2_EntityService/EntityEditorSubclasses.md)

### Flag for Design Doc Creation
- [x] No design doc needed — pure documentation refresh, no design-facing changes.
