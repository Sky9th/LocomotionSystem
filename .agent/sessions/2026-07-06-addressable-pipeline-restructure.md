# 2026-07-06 — Addressables 加载管道重构

## Background

项目之前只有 `PropertyDefSO` 走 Addressables `boot` 加载，其余数据 SO 靠 Prefab `[SerializeField]` 引用链被动打包。Build 下 `RdTagDefSO.FullTag` 因 `OnEnable` 顺序不确定而断裂，暴露了核心问题：**加载策略没有明确规则**——哪些 SO 该走 Addressables、哪些靠直引，完全按"出了 Build bug 再补"的节奏推进。

本次会话的核心目标：建立清晰的加载分层规则 + 目录结构，让新增资产类型时不再猜。

## Changes

### 加载管道目录（Pipeline/）
- `Pipeline/Boot/` — 5 个 BootTask 按领域拆分：
  - `PropertyBootTask` — PropertyDefSO（10 子类），初始化 PropertyDefinitionRegistry
  - `TagBootTask` — RdTagDefSO，加载后 BFS 重建 FullTag 缓存
  - `AbilityBootTask` — 7 种能力 SO（Active/Passive/Tree/Activation/Search/Effect/Noise）
  - `ItemBootTask` — 3 种物品 SO（ItemDef/MeleeWeapon/RangedWeapon）
  - `CharacterBootTask` — CharacterDefSO
- `BootTaskComposer` — 统一 Task 列表和顺序，SceneService 只调一行 `RegisterAll`
- `BootPipeline` — 新增 `RegisterAll(List<IBootTask>)`
- `Pipeline/Scene/Tasks/PrototypeArtTask` — Scene 层示例，加载 `prototype-art` 标签资产
- 删除 `LoadingOrchestrator`（v1 编排器，已完全被 BootPipeline 替代）

### SceneAssetLabel 精简
- 移除 14 个未使用的 label（SceneOpenWorld ~ SharedAudioBoss）
- 保留 `Boot` + 新增 `PrototypeArt`
- 对应 Addressables label table 同步精简

### RdTagDefSO
- 去掉懒加载 getter，恢复简洁的 `cachedFullTag` + `OnEnable` 计算
- `RefreshCache()` 改为 public，由 TagBootTask 在 boot 阶段 BFS 根→叶调用
- Build 下 OnEnable 顺序问题由 Task 层解决，SO 自身不防御

### Editor 工具
- `DataLabelTools.cs` — 两个菜单项：
  - `Tag All Data as 'boot'` — 扫描 `Assets/Data/` 下所有资产
  - `Tag Prototype Art as 'prototype-art'` — 扫描 `Assets/Art/PolygonPrototype/` 下所有资产
- 删除旧的 `PropertyDefLabelTool`

### 目录调整
- `AddressableAssetsData/` 从 `Assets/` 移至 `Assets/Settings/`
- 删除空目录 `Background/`、`Streaming/`

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Task 按领域聚合，不按 SO 类型拆分 | A: 每种 SO 一个 Task → 15+ 个文件平铺。B: 一个 DataCatalogBootTask 加载所有 SO → 初始化逻辑杂糅 | 领域粒度折中——5 个 Task，每个 Task 内按类型调多次 LoadByLabel |
| BootTaskComposer 集中编排 | A: SceneService 逐行 Register → 新增领域必改 SceneService。B: 配置 SO 驱动 → 过度设计 | Composer 是单文件纯代码列表，新增领域只改这一个文件 |
| RdTagDefSO 不用懒加载，Task 统一修 | A: 懒 getter 自动处理 → SO 自身防御，但概念上加载顺序是 Task 的职责 | Task 控制加载时机，SO 保持简单 |
| Pipeline/ 父目录收拢三层 | A: Boot/ Scene/ Streaming/ 平铺在根 → 和 Config/ Transition/ 混在一起 | 三层是同一概念域（加载管道），归入 Pipeline/ 语义清晰 |

## Known Issues

- [ ] `AbilityBootTask` 等 4 个新 Task 的 Editor 标签工具只处理了 SO 类型，未包含 Configuration/Animation 等特殊子类（P2 — 后续按需补）
- [ ] Scene 层 `SceneTaskComposer` 尚未创建——TransitionGate 暂时无法自动调用 Scene Task（P2 — 需要时再加）
- [ ] `BootTaskComposer` 只覆盖 boot 层，Scene 层和 Streaming 层的 Composer 暂未实现
- [x] Build 下 FullTag 断裂问题已通过 TagBootTask BFS 修复

## Cross-References

### Related Sessions
- [2026-07-06-gameManager-duplicate-fix.md](2026-07-06-gameManager-duplicate-fix.md) — 同一会话中 GameManager 重复修复 + SceneService 覆写问题

### Related Plans
- [../plans/majestic-napping-pike.md](../plans/majestic-napping-pike.md) — 加载架构重组实施计划

### Related Tech Docs
- L2_SceneService — 待 rd-tech-doc 创建/更新 Pipeline 目录文档
- L1_Core/RdTag — 待 rd-tech-doc 更新 RdTagDefSO 文档

### Flag for Design Doc Creation
### BootTask 日志完善（追加）
- 5 个 BootTask 改为单条 Debug.Log（StringBuilder 拼接）
- 每类资产逐行打印：Tag 的 FullTag/Depth/Parent、Property 的 Type/Id、Active 的引用链（activation→search→noise→effects）、Tree 的节点和兼容标签、Item/Character 的 prefab/overrides
- 所有 Unity Object 空引用使用显式判空，避免 Build 下 UnassignedReferenceException
- AddressableAssetsData 残留文件清理

### Flag for Design Doc Creation
- [x] No design doc needed — 纯加载架构重构 + 日志完善，无设计面变更。
