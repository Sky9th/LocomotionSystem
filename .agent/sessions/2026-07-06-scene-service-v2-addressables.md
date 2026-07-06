# 2026-07-06 — SceneService v2 + Addressables Loading Pipeline

## Background

Build 后 PropertyDefSO 查找失败——`PropertyDefinitionRegistry` 依赖 `AssetDatabase.FindAssets`
（`#if UNITY_EDITOR`），Build 中字典始终为空。之前的临时修复（`PropertyTreeSO._referencedDefs`
bake + `PropertyTreeBuildPreprocessor`）违反正统设计，已回滚。

同时 SceneService 需从"单场景切换工具"升级为覆盖 Boot/Transition/Streaming/Background
四种模式的"加载管理中心"，为后期内容密集型大地图 + 流式地块 + 后台预加载预留架构。

## Changes

### SceneService 重构（L2_SceneService/）
- SceneService.cs — 重写为入口，持有 BootPipeline/SceneLoader/TransitionGate/LoadProgress
- Boot/BootPipeline.cs — IBootTask 注册模式，顺序执行 → _gate.Begin(firstScene)
- Boot/IBootTask.cs — string Description + IEnumerator Execute()
- Boot/Tasks/PropertyDefBootTask.cs — Addressables label "boot" → PropertyDefinitionRegistry.Initialize
- Transition/SceneLoader.cs — LoadSceneAsync / UnloadSceneAsync / LoadLabelAsync / ReleaseLabels
- Transition/TransitionGate.cs — 门控+守卫+最小显示时间+进度条+事件+资产生命周期
- Progress/LoadProgress.cs — SceneProgressEvent 发布 + 加权复合进度
- Config/SceneLoadConfigSO.cs — SceneId 枚举 + SceneAssetLabel [Flags] 枚举
- Structs/ — SSceneRequest / SSceneTransition / SLoadingProgress
- Streaming/ 和 Background/ — 空目录预留

### Addressables 管线（L2_AddressablesService/）
- AddressablesService.cs — InitializeAsync / LoadByLabel<T> / Release / ReleaseAll + Handle 缓存
- InitializeAsync 添加 `if (IsInitialized) yield break` 防重复初始化
- LoadByLabel: `new List<string> { label }` 修复 string→IEnumerable<char> 重载 Bug（ADDR-3237）

### 事件合并 6→3
- SceneRequestEvent — Load + Reload + Unload 合并（SSceneRequest + SceneRequestType enum）
- SceneTransitionEvent — Start + Complete 合并（SSceneTransition + SceneTransitionPhase enum）
- SceneProgressEvent — 新增，替代 LoadingProgressEvent，归入 Events/Scene/
- EventHub 删除 loadingEvents 数组（SceneProgressEvent 入 sceneEvents）

### L1_Core / L2 适配
- GameService.cs — BeginPreload 调用 SceneService
- UIService.cs — HandleSceneLoadStart+Complete → HandleSceneTransition
- PlayerService.cs — 适配 SceneTransitionEvent
- TimeService.cs — HandleSceneLoadStart+Complete → HandleSceneTransition
- LoadingOverlay.cs — 进度条 + OnInitialize 订阅 SceneProgressEvent

### PropertyDefinitionRegistry 修复
- 删除 #if UNITY_EDITOR AssetDatabase.FindAssets
- 新增 `public static void Initialize(List<PropertyDefSO>)` — 幂等清空+重建

### 结构清理
- 删除 LoadingOrchestrator.cs（逻辑并入 BootPipeline/TransitionGate）
- 删除 PropertyTreeBuildPreprocessor.cs
- 删除 L3_Properties/PropertyBootTask.cs
- 删除 L1_Core/IBootTask.cs
- 旧 S*.cs（6 个）→ 删除，新 struct（3 个）→ Structs/
- LoadingProgressEvent → SceneProgressEvent，归入 Events/Scene/

### 文档
- scene-service.md 标为 DEPRECATED + 新文档 scene-service-loading.md (v2)
- tech/README.md 索引更新

## Decisions

| 决策 | 选择 | 被拒绝方案 | 理由 |
|------|------|-----------|------|
| PropertyDefSO 加载 | Addressables Label "boot" | Resources.Load, AssetDatabase bake | 正统方案，不依赖编辑器专用 API |
| IBootTask 位置 | L2_SceneService/Boot/ | L1_Core | 只有 Loading 系统消费，不应放 L1 |
| Scene/Labels 配置 | 枚举 (SceneId + SceneAssetLabel) | 字符串、自定义 PropertyDrawer | 枚举最简单，inspector 下拉/多选 |
| 事件粒度 | 合并 6→3 | 6 个细粒度事件 | Request 类只有一个消费者，Transition 类消费者只关心状态 |
| SceneLoadConfigSO 加载 | Inspector 序列化 List | Addressables Label "scene-configs" | 几个 Config 不值得走 Addressables |
| LoadingOrchestrator | 删除，并入 BootPipeline | 保留为 L2 | SceneService+Orchestrator 关系混乱 |
| previousConfig 传递 | 显式参数 | TransitionGate _currentConfig 字段 | 协程间 UnityEngine.Object 字段不稳定 |

## Known Issues

- [ ] MainMenu 场景卸载失败 — _firstSceneConfig Inspector 引用失效（MissingReference，SO 删除重建后 GUID 变），需重新拖入
- [ ] TransitionGate Begin 参数：_currentConfig 有值但前场景不卸载 → 改用 RuntimeSceneState struct 传递值类型
- [x] Addressables LoadByLabel string→IEnumerable<char> 重载 Bug — 已修复（List<string> 包装）
- [x] PropertyDefSO 子类类型不匹配 — 已修复（正确重载 + 确认 211 个 def 加载成功）

## Cross-References

### Related Plans
- [../plans/modular-crafting-rabin.md](../plans/modular-crafting-rabin.md) — 完整实施计划

### Related Tech Docs
- [../tech/L2-services/L2-scene-service/scene-service-loading.md](../tech/L2-services/L2-scene-service/scene-service-loading.md) — v2 技术文档
- [../tech/L2-services/L2-scene-service/scene-service.md](../tech/L2-services/L2-scene-service/scene-service.md) — v1 (DEPRECATED)

### Flag for Design Doc Creation
- [x] No design doc needed — internal architecture refactor, no player-facing behavior change.
