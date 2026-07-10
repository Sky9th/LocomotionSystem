# SceneService · 加载管理中心

> `L2_SceneService/SceneService.cs` — ModuleChildMono，统一管理全部加载模式。
>
> **v4 — 2026-07-07。** Boot 管线迁出至 [AssetService](../L2-asset-service/asset-service.md)，SceneService 回归纯粹的场景切换。
>
> **Last Verified**: 2026-07-10

## 状态标记

| 标记 | 含义 |
|------|------|
| ✅ | 本次实现 |
| ⏳ | 延后（架构已预留，代码待实现）|

---

## 架构概览 ✅

```
Core.unity                 ← 首场景、常驻、永不卸载
  └─ LoadingOverlay        ← Core 首帧立即显示
  └─ GameService (DontDestroyOnLoad)

All other scenes are Additive, loaded/unloaded on top of Core.
```

## 模块结构

```
L2_SceneService/
├── SceneService.cs                    # ✅ L2 入口 — TransitionTo(), GetInitialConfig()
├── Pipeline/
│   ├── Scene/                         # ⏳ 预留 — 场景级加载任务
│   └── Streaming/                     # ⏳ 预留
├── Config/
│   └── SceneLoadConfigSO.cs           # ✅ SceneAssetLabel (Boot + PrototypeArt)
├── Transition/
│   ├── SceneLoader.cs                 # ✅ LoadSceneAsync / UnloadSceneAsync
│   └── TransitionGate.cs              # ✅ 门控 + 并行加载 + Boot 感知 + 进度 + 事件
├── Progress/
│   └── LoadProgress.cs                # ✅ 加权复合进度
├── Editor/
│   └── LabelTools/
│       └── DataLabelTools.cs           # ✅ 2 个菜单：Tag All Data / Tag Prototype Art
└── Structs/
    ├── SSceneRequest.cs
    ├── SSceneTransition.cs
    ├── SLoadingProgress.cs
    └── SRuntimeSceneState.cs
```

> **Boot 管线已迁出**：原 `Pipeline/Boot/` 12 个文件（IBootTask, BootPipeline, BootTaskComposer, BootAssetCatalog, 8 Tasks）已删除。Registry 初始化收敛至 `AssetService.BootInitRunner.RunBootInit()`。

## 调用链 ✅

```
被谁调:
  GameService.Start()                           → TransitionTo(firstConfig)
  EventHub (SceneRequestEvent)                  → HandleSceneRequest → TransitionTo(config)

调谁:
  GameContext                                   → RegisterService(this)
  AssetService                                  → EnsureInitialized, LoadByLabels, ReleaseLabel, RunBootInit
  EventHub                                      → SceneTransitionEvent, SceneProgressEvent
  SceneManager                                  → LoadSceneAsync, UnloadSceneAsync, SetActiveScene
```

## 耦合模块 ✅

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | AssetService | L2 资产服务 — 标签加载 + boot 初始化 |
| 依赖 | EventHub | 接收加载请求、发布加载事件 |
| 依赖 | GameContext | 注册 |
| 被依赖 | GameService | Start() 中调用 TransitionTo(firstConfig) |

## 公开 API

### SceneService ✅

```csharp
SceneLoadConfigSO GetInitialConfig(string preferredSceneName = null)
IEnumerator       TransitionTo(SceneLoadConfigSO config)  // EnsureInitialized → TransitionGate.Begin
```

### SceneAssetLabel ✅

```csharp
[Flags] enum SceneAssetLabel
    None         = 0
    Boot         = 1 << 0   // 所有数据 SO
    PrototypeArt = 1 << 1   // PolygonPrototype 资产
```

### SceneLoadConfigSO ✅

```csharp
class SceneLoadConfigSO : ScriptableObject
    SceneId         Scene
    SceneAssetLabel AssetLabels
    LoadMode        Mode
    float           MinDisplayTime
    CurtainType     Curtain
```

## Label 体系

| Label | 加载时机 | 生命周期 | 状态 |
|-------|---------|---------|------|
| `boot` | 首场景 TransitionGate | 永不释放 (AssetService pinned) | ✅ |
| `prototype-art` | 原型场景 TransitionGate | 场景卸载时释放 | ✅ |

## App 冷启动 ✅

```
GameService.Start()
  → sceneService.TransitionTo(firstConfig)
    → AssetService.EnsureInitialized()          // idempotent
    → TransitionGate.Begin(config):
        labels = ["boot", ...sceneLabels]       // 首次包含 boot
        并行加载: Scene + Labels
        AssetService.RunBootInit()              // sync — 8 Registry Init
        AssetService.LoadAOTMetadata(cb)        // async — HybridCLR 66 AOT metadata assemblies
        SetActiveScene(loadedScene)
```

## 场景切换（MainMenu → NewGame）✅

```
SceneRequestEvent("NewGame")
  → SceneService.HandleSceneRequest
    → TransitionTo(newGameConfig)
      → AssetService.EnsureInitialized()        // no-op
      → TransitionGate.Begin(config, prev):
          labels = ["prototype-art"]             // boot already cached
          并行加载: Scene + Labels
          RunBootInit()                          // no-op (already done)
          卸载前场景 → ReleaseLabel(prevLabels)
```

## 关键设计点 ✅

- **Boot 初始化收敛到 AssetService**：新增数据领域只需在 `BootInitRunner.Run()` 中加一行 Registry.Init。不再有 IBootTask / BootPipeline / BootTaskComposer 三层抽象。
- **SceneService 回归纯粹**：只负责场景切换协调。Boot 资产加载通过 TransitionGate 首次场景加载时自动纳入。
- **AssetService 是唯一资产入口**：其他 L2 不直接接触 Addressables。Handle 缓存 + pinned 机制统一管理资产生命周期。
- **SceneAssetLabel 精简**：只保留正在用的 Boot + PrototypeArt。
- **Editor 标签工具**：两个菜单项扫文件夹，不按类型拆分。

## 失败处理 ✅

| 故障 | 处理 |
|------|------|
| ScenePath 无效 | Error → 中止 |
| LoadByLabel 返回 0 | Warning（非致命） |
| Boot Label 返回 0 PropertyDefSO | Registry.InitPropertyDefs(empty) |
| Addressables 初始化失败 | Error → 继续（降级） |
| 重复 SceneRequest | _isTransitioning 守卫 → yield break |

## v3 → v4 变更

| v3 | v4 | 原因 |
|----|----|------|
| `Pipeline/Boot/` 12 文件 | 删除 — 收敛至 `AssetService.BootInitRunner` | Boot 不属于 Scene |
| `AddressablesService` (L2) | `AssetService` (L2) — 吸收并扩展 | 统一资产入口 + 强引用锚 |
| `BootPipeline` / `IBootTask` / `BootTaskComposer` | 消除 — `RunBootInit()` 一个方法 | 过度抽象 |
| `SceneService._boot` / `BeginPreload` | `TransitionTo()` — 纯场景切换入口 | 职责专一 |
| `TransitionGate` 持有 `BootPipeline` | 持有 `AssetService` — Boot 感知内化 | 依赖方向合理 |
| `SceneLoader.LoadLabelAsync` | 移至 `AssetService.LoadByLabels` | 资产加载 ≠ 场景加载 |
