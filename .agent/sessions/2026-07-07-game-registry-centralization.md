# 2026-07-07-game-registry-centralization

## Background

场景切换时，`TransitionGate.Begin()` 在卸载旧场景后调用 `SceneLoader.ReleaseLabels()`，传入的 label 包含 `"boot"`。`AddressablesService.Release("boot")` 通过 `UnityAddressables.Release(handle)` 释放了所有 boot 资产的 native backing，导致 8 个 static Registry 字典中 key 仍存在但 value 全部变为 Unity fake-null。

同时，这 8 个 Registry 是 standalone static class（CharacterRegistry、ItemRegistry、AbilityTreeRegistry、PropertyDefinitionRegistry、PropertyTreeRegistry、AnimationProfileRegistry、GroundSystemConfigRegistry、AudioConfigRegistry），分散在 4 个模块目录下，缺乏集中生命周期管理。将 SO 引用存在 static 字段中本身不保证 Addressables 不释放 native 对象。

## Changes

### L2_EntityService（新建）
- **GameRegistry.cs**：一个实例类持有全部 8 个 Registry 的 `Dictionary<string, SO>`，提供 `Init*()` / `Find*()` / `Contains*()` / `ResolveAbilityTrees()` 方法。`ResolveAbilityTrees(string[])` 是所有 "ID 数组 → SO 数组" 的唯一入口
- 删除 CharacterRegistry.cs + ItemRegistry.cs（数据已迁移至 GameRegistry）

### L1_Core
- **GameService.cs**：新增 `AssetRegistry` 属性（GameRegistry 实例），`Awake` 中初始化；`BootPipeline` 通过 `SetCatalog()` 注入 `BootAssetCatalog` 作为二级强引用根

### L2_AddressablesService — 数据丢失修复
- 新增 `PinnedLabels` 静态集合（`{ "boot" }`），`Release(label)` 检查到 pinned label 时直接 `return`，输出 info 日志

### L2_SceneService / Boot Pipeline
- 7 个 BootTask 全部改为 `GameService.Instance.AssetRegistry.Init*()`
- **BootPipeline.cs**：catalog 构建后调用 `GameService.Instance.AssetRegistry.SetCatalog(catalog)`；删除 `catalog.BuildTypeIndex()` 调用
- **BootAssetCatalog.cs**：删除死索引 `_byType` + `BuildTypeIndex()` 方法
- 删除 PrototypeArtTask.cs（孤儿类，无接口、无调用者）
- `PropertyBootTask` / `PropertyTreeBootTask` XML 注释更新

### L3_Ability — 技能树获取重构
- **AbilityForest.cs**：构造函数简化为 `AbilityForest(string[] innateTreeIds)`，内部通过 `GameRegistry.ResolveAbilityTrees()` 批量解析；新增 `SetInnateTrees()` 用于 Start 阶段延迟注入
- 删除 AbilityTreeRegistry.cs

### L3_Character
- **CharacterActor.cs**：`innateTreeIds: string[]` 序列化字段，Awake 直传 AbilityForest；4 处 `GameService.Instance.` → `?.` 空守卫
- **CharacterActor.Debug.cs**：`GameService.Instance.` → `?.` 空守卫
- **CharacterDefSO.cs**：清理无用 import
- 删除 AnimationProfileRegistry.cs、GroundSystemConfigRegistry.cs、AudioConfigRegistry.cs

### L3_Properties
- **PropertyTable.cs**：`PropertyTreeRegistry.Find` → `GameService.Instance.AssetRegistry.FindPropertyTree`
- **PropertyTreeSO.cs**：`PropertyDefinitionRegistry.FindById` → `GameService.Instance?.AssetRegistry.FindPropertyDef`
- 删除 `GameRegistry.InvalidatePropertyDefs()` 方法 + 编辑器所有 `.InvalidatePropertyDefs()` 调用
- 删除 PropertyDefinitionRegistry.cs、PropertyTreeRegistry.cs

## Decisions

| 决策 | 选择 | 被拒绝方案 | 理由 |
|------|------|-----------|------|
| Registry 集中化方式 | GameService 持有单个 GameRegistry 实例 | 保留 static 类 + thin wrapper | 实例跟随 DontDestroyOnLoad 生命周期，配合 PinnedLabels 形成双重保护 |
| 数据丢失修复 | AddressablesService.PinnedLabels | TransitionGate 过滤 boot label | 防御在释放点而非调用方，防止未来其他调用路径触发 |
| 技能树获取 | 序列化 `string[] innateTreeIds`，Awake 直传 | CharacterDefSO 读取 / 延迟 Func 委托 | 用户要求直接传 ID，不绕弯 |
| 死代码处理 | 直接删除 | 保留 + TODO | 无编译/运行时引用，保留增加维护负担 |
| Editor InvalidatePropertyDefs | 删除全部调用 + 方法 | 保留 `?.` 空操作 | Editor 永远不在 Play Mode，调用毫无意义 |
| BootAssetCatalog 类型索引 | 删除 `_byType` + `BuildTypeIndex()` | 修复 Get<T> 使用索引 | 修复成本高于收益（subclass 问题），直接 O(n) 扫描够用 |

## Known Issues

- [x] Scene 切换后 Registry SO 引用变 null — fixed (PinnedLabels)
- [x] 8 个 static Registry 分散无集中管理 — fixed (GameRegistry)
- [ ] `CharacterActor` 属性 getter（CharacterAnimationProfile 等）在 `GameService.Instance` 为 null 时返回 null，调用方需容忍 (P2)
- [ ] `innateTreeIds` 字段在 session 中被删除又加回 — Prefab 上之前配置的值已丢失，需手动重配 (P2)
- [ ] `PropertyTable.FromPreset()` / `PropertyTreeSO.ResolveDef()` 现在依赖 GameService 单例，脱离场景无法单测 (P2)

## Cross-References

### Related Sessions
- [2026-07-06-scene-service-v2-addressables.md](2026-07-06-scene-service-v2-addressables.md) — BootPipeline v2 Addressables 加载架构
- [2026-07-06-addressable-pipeline-restructure.md](2026-07-06-addressable-pipeline-restructure.md) — 加载管道重构

### Related Tech Docs
- tech/L2-services/L2-entity-service/ — GameRegistry 新建、EntityService 更新
- tech/L2-services/L2-modules/L3-ability/ — AbilityForest 更新
- tech/L2-services/L2-modules/L3-character/ — CharacterActor 更新
- tech/L2-services/L2-modules/L3-properties/ — PropertyTable/PropertyTreeSO 更新

### Flag for Design Doc Creation
- [x] No design doc needed — all changes are internal refactoring, bug fix, and dead code removal. No design-facing changes.
