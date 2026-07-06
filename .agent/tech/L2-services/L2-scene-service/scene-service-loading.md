# SceneService · 加载管理中心

> `L2_SceneService/SceneService.cs` — ModuleChildMono，统一管理全部加载模式。
>
> **v3 — 2026-07-06。** 旧版文档：[scene-service.md](scene-service.md)（⛔ DEPRECATED），[scene-service-loading.md](scene-service-loading.md)（v2）。
>
> **Last Verified**: 2026-07-06 | **Verification**: All referenced files exist, signatures match code. Pipeline/ 目录 + BootTaskComposer 模式落地。

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
├── SceneService.cs                    # ✅ L2 入口
├── Pipeline/
│   ├── Boot/
│   │   ├── BootPipeline.cs            # ✅ IBootTask 收集 → 顺序执行
│   │   ├── IBootTask.cs               # ✅ 接口
│   │   ├── BootTaskComposer.cs        # ✅ 单文件定义所有 Boot Task 列表和顺序
│   │   └── Tasks/
│   │       ├── PropertyBootTask.cs     # ✅ PropertyDefSO + PropertyTreeSO → Registry
│   │       ├── TagBootTask.cs          # ✅ RdTagDefSO → BFS 重建 FullTag
│   │       ├── AbilityBootTask.cs      # ✅ 7 种能力 SO（Active/Passive/Tree/Activation/Search/Effect/Noise）
│   │       ├── ItemBootTask.cs         # ✅ 3 种物品 SO（ItemDef/MeleeWeapon/RangedWeapon）
│   │       └── CharacterBootTask.cs    # ✅ CharacterDefSO
│   ├── Scene/
│   │   └── Tasks/
│   │       └── PrototypeArtTask.cs     # ✅ 示例 — PolygonPrototype 美术资产
│   └── Streaming/                      # ⏳ 预留
├── Config/
│   └── SceneLoadConfigSO.cs           # ✅ SceneAssetLabel (Boot + PrototypeArt)
├── Transition/
│   ├── SceneLoader.cs                 # ✅ LoadSceneAsync / Unload / LoadLabelAsync
│   └── TransitionGate.cs              # ✅ 门控 + 守卫 + 进度条 + 事件 + 资产生命周期
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

## 调用链 ✅

```
被谁调:
  GameService.Start()                           → BeginPreload(mainMenuConfig)
  EventHub (SceneRequestEvent)                  → HandleSceneRequest

调谁:
  GameContext                                   → RegisterService(this)
  AddressablesService                           → LoadByLabel (via BootTasks)
  EventHub                                      → SceneTransitionEvent, SceneProgressEvent
  SceneManager                                  → LoadSceneAsync, UnloadSceneAsync, SetActiveScene
  PropertyDefinitionRegistry                    → Initialize (via PropertyBootTask)
```

## 耦合模块 ✅

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | AddressablesService | L2 基础设施 |
| 依赖 | EventHub | 接收加载请求、发布加载事件 |
| 依赖 | GameContext | 注册 |
| 被依赖 | GameService | Start() 中调用 BeginPreload |
| 被依赖 | 各 IBootTask | 通过 BootTaskComposer 注入地址 |

## 公开 API

### SceneService ✅

```csharp
void BeginPreload(string preferredSceneName = null)
void RegisterBootTask(IBootTask task)  // 兼容旧接口，内部调 RegisterAll
```

### BootPipeline ✅

```csharp
bool        BootTasksComplete
bool        IsReady
void        RegisterAll(List<IBootTask> tasks)  // 批量注册
void        Register(IBootTask task)            // 单个注册（兼容旧接口）
IEnumerator Run(SceneLoadConfigSO firstScene)
IEnumerator WaitUntilTasksComplete()
```

### BootTaskComposer ✅

```csharp
static List<IBootTask> CreateAll(AddressablesService addressables)
// 返回：[TagBootTask, PropertyBootTask, AbilityBootTask, ItemBootTask, CharacterBootTask]
// 新增数据领域只改这一个文件
```

### IBootTask ✅

```csharp
interface IBootTask
    string       Description { get; }
    IEnumerator  Execute()
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
| `boot` | BootPipeline | 永不释放 | ✅ |
| `prototype-art` | 原型场景 TransitionGate | 场景卸载时释放 | ✅ |

## App 冷启动 ✅

```
GameService.Start()
  → _sceneService.BeginPreload()
    → _boot.Run(firstSceneConfig):
        _addressables.InitializeAsync()

        foreach task in BootTaskComposer.CreateAll():
            yield return task.Execute()
            ┌─ TagBootTask:         RdTagDefSO → BFS RefreshCache
            ├─ PropertyBootTask:    PropertyDefSO → Registry.Initialize
            ├─ AbilityBootTask:     7 种能力 SO → handle 缓存
            ├─ ItemBootTask:        3 种物品 SO → handle 缓存
            └─ CharacterBootTask:   CharacterDefSO → handle 缓存

        BootTasksComplete = true
        _gate.Begin(firstSceneConfig):
            并行加载: Scene + config.AssetLabels
            SetActiveScene(loadedScene)
        IsReady = true
```

## 场景切换（MainMenu → NewGame）✅

```
SceneRequestEvent("NewGame")
  → _gate.Begin(newGameConfig, previousSceneName, previousScenePath, previousAssetLabels)
      并行加载: Scene + Labels
      卸载前场景 → ReleaseLabels(previousAssetLabels)
```

## 关键设计点 ✅

- **BootTaskComposer**：新增数据领域只需加 Task 文件 + Composer 里加一行。SceneService 不感知。
- **按领域拆分**：不是按 SO 类型（会 15+ Task），也不是一个大 Task（初始化杂糅），而是 5 个领域级 Task
- **RdTagDefSO 修复**：TagBootTask 在 boot 阶段 BFS 根→叶调用 RefreshCache()，解决 Build 下 OnEnable 顺序问题
- **SceneAssetLabel 精简**：只保留正在用的 Boot + PrototypeArt，不加时只加 enum entry
- **Editor 标签工具**：两个菜单项扫文件夹，不按类型拆分

## 失败处理 ✅

| 故障 | 处理 |
|------|------|
| ScenePath 无效 | Error → 中止 |
| LoadByLabel 返回 0 | Warning（非致命） |
| Boot Label 返回 0 PropertyDefSO | Registry.Initialize(empty) |
| Addressables 初始化失败 | Error → 继续（降级） |
| 重复 SceneRequest | _isTransitioning 守卫 → yield break |

## v2 → v3 变更

| v2 | v3 | 原因 |
|----|----|------|
| `PropertyDefBootTask` 单个 Task | 5 个领域 Task + BootTaskComposer | 按领域聚合，不再逐个 SO 类型拆分 |
| `Boot/` 目录平铺 | `Pipeline/Boot/` | 三层管道收进 Pipeline/ 父目录 |
| `LoadingOrchestrator` | 删除 | 已被 BootPipeline 完全替代 |
| SceneAssetLabel 16 个 entry | Boot + PrototypeArt | 未使用的延后标签一律不预定义 |
| AddressableAssetsData 在 Assets/ | Assets/Settings/ | 配置归 Settings |
| `Background/`、`Streaming/` 空目录 | 删除 | 用的时候再加 |
