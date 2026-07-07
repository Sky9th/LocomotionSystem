# 2026-07-07 — Importer Addressables 兼容修复 + Editor UI 修整

## Background

S4 Addressables 加载管道重构后，所有数据 SO 通过 `AssetService.RunBootInit()` 从 `boot` label 加载。
但 9 个现有 Importer 创建新 `.asset` 后未注册到 Addressables——导入的资产在 Build 中不可用。
同时 TagImportExport 因 `RefreshCache` 从 private 改为 public 后反射搜索失败。

此外 Editor UI 有两个累积问题：`EditorButton` 在 Large/Medium 尺寸下文字被裁，`ImportExport` 的 Result 区无滚动条。

## Changes

### Addressables 兼容
- `Shared/Editor/DataLabelTools.cs` — 新增 `EnsureBootLabel(assetPath)` 公共方法，单个资产注册+标记 boot label
- 原 `L2_SceneService/Editor/LabelTools/DataLabelTools.cs` 删除，功能迁至 Shared/Editor，namespace 改为 `RedDust.Shared.EditorUI`

### 9 个 Importer 全部接入
每个文件的 `AssetDatabase.CreateAsset()` 后新增 `DataLabelTools.EnsureBootLabel(assetPath)`：
- `TagImportExport.cs` — +1 调用 + RefreshCache 反射修复（Public|NonPublic|Instance）
- `AbilityImportExport.cs`, `AbilityTreeImportExport.cs` — +1 调用
- `ActivationImportExport.cs`, `SearchImportExport.cs`, `EffectImportExport.cs`, `NoiseImportExport.cs` — +1 调用
- `PropertyImportExport.cs` — +2 调用（PropertyDefSO + PropertyTreeSO）
- `AnimationImportExport.cs` — +5 调用（5 种动画配置 SO）

### Editor UI 修复
- `EditorButton.GetStyle()` — overflow 从 miniButton 继承改为 `new RectOffset()`；按 size 设 `fixedHeight`（Medium=24, Large=28）
- `ImportExport.DrawButtons()` — 按钮 `Large→Medium`；`DrawResultSection()` — errors TextArea 外包 ScrollView（maxHeight=160）

### 清理
- `Human.asset` — 删除孤儿字段 `innateTreeIds`（字段已从 CharacterDefSO 移除，YAML 残留）
- `L2_SceneService/Editor/LabelTools/` — 空目录删除

### 计划
- `short-term.md` — 新增 P5.0 基础设施补完（ItemEditor + ItemImportExport + 兼容验证）
- `long-term.md` — 同步更新

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| `EnsureBootLabel` 放 Shared/Editor，非 L2_SceneService | A: 保留原位置 → 所有 Importer 需引用 SceneService 模块，耦合不合理 | 共享 Editor 工具归 Shared/Editor，与 ImportExport.cs 同级 |
| namespace `RedDust.Shared.EditorUI` | A: 新建 `RedDust.Shared.Editor.Addressables` → 过度拆分 | 与同目录 `ImportExport`、`EditorCard` 一致 |
| `RefreshCache` 反射用 `Public\|NonPublic\|Instance` | A: 改为只搜 `Public` → 未来可能再改回 private | 双标记兼容所有可见性，比精确匹配更健壮 |
| Button `fixedHeight` 按 size 显式设值而非设 0 | A: `fixedHeight=0` 全自适应 → 放弃 miniButton 的尺寸约束 | miniButton 就该小，Medium/Large 给对应的固定高度 |
| 按钮 `Large→Medium` | A: 保留 Large+调大 fixedHeight → Import/Export 不需要那么大的按钮 | Medium 是 Editor 常用尺寸，12px 字体 + 24px 高度刚好 |

## Known Issues

- [ ] 其他 Importer 未逐一测试导入——Tag 已验证通过，其余共享同一组件逻辑无差异（P2）
- [x] `RefreshCache` 反射修复 — 已验证 `Public|NonPublic|Instance` 可搜索到
- [x] `Human.asset` 孤儿字段 — 已删除，Zombie/Blade/Pistol/Backpack 确认无残留

## Cross-References

### Related Sessions
- [2026-07-06-addressable-pipeline-restructure.md](2026-07-06-addressable-pipeline-restructure.md) — S4 Addressables 管道重构，引入 boot label 体系
- [2026-07-06-scene-service-v2-addressables.md](2026-07-06-scene-service-v2-addressables.md) — SceneService v2，Addressables 初始化链路
- [2026-07-04-import-export-unification-and-fixes.md](2026-07-04-import-export-unification-and-fixes.md) — ImportExport 统一 (created,updated,skipped,errors) 四元组

### Related Plans
- [../plans/short-term.md](../plans/short-term.md) — P5.0 基础设施补完
- [../plans/greedy-watching-stream.md](../../../C:/Users/Sky9th/.claude/plans/greedy-watching-stream.md) — 实施计划

### Related Tech Docs
- [tech/editor/README.md](../tech/editor/README.md) — Editor 组件索引
- [tech/L2-services/L2-asset-service/asset-service.md](../tech/L2-services/L2-asset-service/asset-service.md) — AssetService + RunBootInit

### Flag for Design Doc Creation
- [x] No design doc needed — 纯基础设施修复 + UI 修整，无设计面变更。

v0.40.6
