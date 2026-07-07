# 2026-07-07 — AssetService 新建 + Boot 管线消除 + SceneService 瘦身

## Background

SceneService v3 重构后，`Pipeline/Boot/` 下 12 个文件的职责是游戏数据初始化（往 GameRegistry 注册 SO），与场景加载无关，却全部塞在 L2_SceneService 里。同时缺少 DontDestroyOnLoad 强引用锚点，Addressables 加载的资产在场景切换时被原生侧释放，C# 字典 key 还在但 value 没了。

本 session 同时解决两个问题：新建 L2 AssetService 作为唯一资产入口和强引用根；Boot 管线消除——12 文件收敛为 AssetService 内部一个方法；SceneService 回归纯粹的场景切换。

这是 feature/ability-pipeline 分支上的架构级重构。

## Changes

### L2_AssetService (new)
- `AssetService.cs` — Addressables init + handle 缓存 + label 加载/释放 + Boot 填充 + `IGameplaySessionHandler`
- `AssetCatalog.cs` — 8 字典 + Init + Find，rename from GameRegistry，删 IsInitialized/_xxxReady bools/死 API/LogNotReady
- 消费者 API: `GameService.Instance.Assets.FindCharacter("Player")`

### Boot 管线消除
- 删除 12 文件: `IBootTask`, `BootPipeline`, `BootTaskComposer`, `BootAssetCatalog`, 8 BootTask
- 收敛为 `AssetService.RunBootInit()` — 同步 idempotent，由 `SceneService.EnsureBootReady()` 在首场景加载前调用
- `RdTagDefSO.RebuildAllCaches` → `AssetService` 私有静态方法

### SceneService 瘦身
- 公开 API: `Load()` / `Load(SceneId)` (IEnumerator)，`EnsureBootReady()` 内联
- 删除: `BeginPreload`, `RegisterBootTask`, `HandleSceneRequest`, `TransitionTo`
- `TransitionGate` → `SceneTransition` (更名, 去 Boot 感知, 内联进度计算, 删 SceneLoader/LoadProgress 依赖)
- 删除 `SRuntimeSceneState` — config SO 现在有强引用锚，直接用 `SceneLoadConfigSO?`

### L2_AddressablesService 删除
- 功能吸收进 AssetService

### 事件清理
- 删除 `SceneRequestEvent`, `SSceneRequest` — 场景切换不再走事件
- `UIService` 4 处 `SceneRequestEvent.Raise` → `_sceneService.Load(SceneId)` + `ParseSceneId` helper

### 命名修正
- `GameRegistry` → `AssetCatalog`
- `GameService.AssetRegistry` → `GameService.Assets`
- `BootAssetsLoaded` → `BootComplete`
- `TransitionGate` → `SceneTransition`
- `Begin` → `Transition`

### 其他清理
- 删除 `SceneLoader.cs`, `LoadProgress.cs` — 薄包装不值得独立文件
- `SceneService` 公开 `GetInitialConfig` 删除 — 改用 `Load(SceneId)` 直接指定
- `GameService` `ConsumeEditorScenePreference` 迁入 `SceneService`

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Boot 任务集中在 AssetService，不放散到 L3 | A: 每 L3 模块自己暴露 BootTask → 碎片化; B: 新建 L2 BootService → 过度 | 数据从哪加载是系统层的事，L3 只暴露 `InitXxx()` 接收数据 |
| AssetHandleCache/BootInitRunner 不独立文件 | A: 保持独立文件 → 各 80-150 行纯委托 | 都是 AssetService 内部实现细节，独立文件增加导航成本无收益 |
| 删除 LoadProgress 类 | A: 保留双轨加权复合进度 → 15 行只为算一个 float | 内联 4 行等效，LoadingOverlay 只看一个数字不关心来源 |
| SceneRequestEvent 删除 | A: 保留事件 → 解耦 UI 和 Scene | UI 直接调 `Load(SceneId)` 更直接，事件增加缩进无实际解耦收益 |
| AssetCatalog 保留为独立文件 | A: 合并进 AssetService → ~600 行过大; B: 打散到各 L3 → 碎片化 | 330 行字典+查询，职责清晰(纯数据容器)，独立文件便于消费者发现 API |

## Known Issues

- [ ] `GameManager.prefab` 需在 Unity Editor 中手动更新 — 删 AddressablesService 子对象，加 AssetService 子对象 (P0)
- [ ] Addressables 初始化失败后降级路径未覆盖 — `EnsureInitialized` 失败后 `IsInitialized=false`，`RunBootInit` 仍会尝试从空 `_loadedAssets` 构建 (P2)

## Cross-References

### Related Plans
- [../plans/toasty-wiggling-llama.md](../plans/toasty-wiggling-llama.md) — 最终审批通过的 plan

### Related Tech Docs
- [../tech/L2-services/L2-asset-service/asset-service.md](../tech/L2-services/L2-asset-service/asset-service.md) — new
- [../tech/L2-services/L2-scene-service/scene-service-loading.md](../tech/L2-services/L2-scene-service/scene-service-loading.md) — updated v4

### Flag for Design Doc Creation
- [x] No design doc needed — pure internal refactoring, no design-facing changes.
