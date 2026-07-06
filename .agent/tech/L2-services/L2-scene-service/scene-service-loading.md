# SceneService · 加载管理中心

> `L2_SceneService/SceneService.cs` — ModuleChildMono，统一管理全部加载模式。
>
> **v2 — 2026-07-06。** 旧版文档：[scene-service.md](scene-service.md)（⛔ DEPRECATED）。

## 状态标记

| 标记 | 含义 |
|------|------|
| ✅ | 本次实现 |
| ⏳ | 延后（架构已预留，代码待实现） |
| 📁 | 仅预留目录 |

---

## 架构概览 ✅

```
Core.unity                 ← 首场景、常驻、永不卸载
  └─ LoadingOverlay        ← Core 首帧立即显示
  └─ GameService (DontDestroyOnLoad)

All other scenes are Additive, loaded/unloaded on top of Core.
```

Core 持有所有 L2 Service 和进度条 UI。其余场景全部 Additive 叠加。`SceneManager.SetActiveScene()` 在加载后调用以激活灯光设置。

## 模块结构

```
L2_SceneService/
├── SceneService.cs               # ✅ L2 入口，持有子模块
├── Boot/
│   ├── IBootTask.cs              # ✅ 接口
│   ├── BootPipeline.cs           # ✅ 收集 IBootTask → 顺序执行 → BootTasksComplete
│   └── Tasks/
│       └── PropertyDefBootTask.cs # ✅ Label "boot" → PropertyDefinitionRegistry.Initialize()
├── Config/
│   └── SceneLoadConfigSO.cs      # ✅ SO：ScenePath, AssetLabels, Mode, MinDisplayTime
├── Transition/
│   ├── SceneLoader.cs            # ✅ LoadSceneAsync / Unload / LoadLabelAsync
│   └── TransitionGate.cs         # ✅ 门控 + 守卫 + 进度条 + 事件 + 资产生命周期
├── Streaming/                    # 📁 留空
├── Background/                   # 📁 留空
├── Progress/
│   └── LoadProgress.cs           # ✅ 发布 LoadingProgressEvent + 加权复合进度
└── Structs/                      # ✅ 全部 struct
    ├── SLoadSceneRequest.cs
    ├── SReloadSceneRequest.cs
    ├── SSceneLoadComplete.cs
    ├── SSceneLoadStart.cs
    ├── SSceneTransition.cs
    ├── SUnloadSceneRequest.cs
    └── SLoadingProgress.cs       # ✅ 从 LoadingProgressEvent.cs 提取
```

## 调用链 ✅

```
被谁调:
  GameService.Start()                           → BeginPreload(mainMenuConfig)
  EventHub (SceneLoadRequestEvent)              → HandleSceneLoadRequest
  EventHub (SceneReloadRequestEvent)            → HandleSceneReloadRequest
  EventHub (SceneUnloadRequestEvent)            → HandleSceneUnloadRequest

调谁:
  GameContext                                   → RegisterService(this)
  AddressablesService                           → InitializeAsync, LoadByLabel, Release
  EventHub                                      → SceneLoadStartEvent, SceneLoadCompleteEvent, LoadingProgressEvent
  SceneManager                                  → LoadSceneAsync, UnloadSceneAsync, SetActiveScene
  PropertyDefinitionRegistry                    → Initialize(List<PropertyDefSO>)
```

## 耦合模块 ✅

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | AddressablesService | L2 独立基础设施 |
| 依赖 | EventHub | 接收加载请求、发布加载事件 |
| 依赖 | GameContext | 注册 + 快照发布 |
| 被依赖 | GameService | Start() 中调用 BeginPreload |
| 被依赖 | PlayerService | 加载完成后 Spawn Player |
| 被依赖 | UIService | 进度条显隐 |

## 公开 API

### SceneService ✅

```csharp
void BeginPreload(SceneLoadConfigSO mainMenuConfig)
void RegisterBootTask(IBootTask task)
```

### BootPipeline ✅

```csharp
bool        BootTasksComplete               // tasks 执行完毕（TransitionGate 门控此标记）
bool        IsReady                         // 首个场景加载完毕
void        Register(IBootTask task)        // 收集启动任务
IEnumerator Run(SceneLoadConfigSO firstScene)  // tasks → configRegistry → BootTasksComplete → _gate.Begin
IEnumerator WaitUntilTasksComplete()        // while (!BootTasksComplete) yield return null
```

### SceneLoader ✅

```csharp
IEnumerator LoadSceneAsync(string path, LoadSceneMode mode)
IEnumerator UnloadSceneAsync(string name)
IEnumerator LoadLabelAsync(List<string> labels, Action<float> onProgress)
void        ReleaseLabels(List<string> labels)      // → Addressables.Release(label)
void        UnloadAll()                             // Session 结束
```

### TransitionGate ✅

```csharp
IEnumerator Begin(SceneLoadConfigSO config)
    //   if (_isTransitioning) yield break            ← 守卫
    //   _isTransitioning = true
    //   yield return _boot.WaitUntilTasksComplete()
    //   Raise SceneLoadStartEvent
    //   _progress.Publish(config.ScenePath, 0.0)
    //   并行（加权 70/30）:
    //     _loader.LoadSceneAsync(config.ScenePath, Additive)
    //     _loader.LoadLabelAsync(config.AssetLabels, onProgress)
    //   SetActiveScene(loadedScene)
    //   若前场景 → _loader.UnloadSceneAsync + _loader.ReleaseLabels
    //   _activeLabels = config.AssetLabels
    //   WaitMinDisplay(config.MinDisplayTime)
    //   Raise SceneLoadCompleteEvent
    //   _isTransitioning = false
```

### LoadProgress ✅

```csharp
void Publish(string phase, float progress)
void Clear()
void BeginComposite(int trackCount)
void UpdateTrack(int track, float value)
float TotalProgress                                // SUM / N
```

### IBootTask ✅

```csharp
interface IBootTask
    string       Description { get; }
    IEnumerator  Execute()
```

### SceneLoadConfigSO ✅

```csharp
class SceneLoadConfigSO : ScriptableObject
    string          ScenePath         // "Assets/Scenes/OpenWorld.unity"
    List<string>    AssetLabels       // ["scene-openworld", "shared-characters"]
    LoadMode        Mode              // FullTransition | AdditiveWithFade | Streaming
    float           MinDisplayTime
    CurtainType     Curtain           // LoadingScreen | BriefFade | None
```

资产目录：`Assets/Data/SceneConfigs/*.asset` ✅

## Label 体系

### 维度 1：地貌（区块资产）⏳

> 流式地块加载实现后才启用。

| Label | 内容 |
|-------|------|
| `chunk-forest` | 雨林植被、湿润音效 |
| `chunk-wetland` | 沼泽、水面 |
| `chunk-city` | 建筑残骸、沥青路面、城市丧尸 |
| `chunk-ruins` | 废墟、瓦砾 |
| `chunk-snow` | 雪地、冻土 |
| `chunk-desert` | 沙漠、仙人掌 |

### 维度 2：场景（关卡资产）✅

> MainMenu、OpenWorld 的 SceneLoadConfigSO 使用。

| Label | 内容 |
|-------|------|
| `scene-openworld` | 大地图共享资产 |
| `scene-boss-act2` | Boss 关专属资产 |
| `scene-underground` | 地下设施 |
| `scene-shelter` | 避难所 |
| `scene-story-*` | 剧情关 |

### 维度 3：加载时机

| Label | 加载时机 | 生命周期 | 状态 |
|-------|---------|---------|------|
| `boot` | BootPipeline 一次性 | **永不释放** | ✅ |
| `scene-configs` | BootPipeline（构建 configRegistry） | 永不释放 | ✅ |
| `scene-*` | TransitionGate（场景切换时） | 场景卸载时释放 | ✅ |
| `chunk-*` | ChunkLoader（流式） | 地块卸载时释放 | ⏳ |
| `bg-*` | BackgroundPreloader（空闲帧） | 切换场景时取消 | ⏳ |

## 调用链详解

### App 冷启动 ✅

```
GameService.Start()                                                        [L1]
  → _sceneService.BeginPreload(mainMenuConfig)                             [L2]
    → _bootPipeline.Run(mainMenuConfig):                                   [BootPipeline]
        _progress.Publish("Initializing...", 0.0)
        yield return _addressables.InitializeAsync()

        foreach task in _tasks:
          _progress.Publish(task.Description, p)
          yield return task.Execute()                 ← PropertyDefBootTask → Registry 填充

        yield return _loader.LoadLabelAsync(["scene-configs"], ...)
        构建 _configRegistry : Dictionary<string, SceneLoadConfigSO>

        BootTasksComplete = true                      ← TransitionGate 门控此标记

        _gate.Begin(mainMenuConfig):                                        [TransitionGate]
          yield return _boot.WaitUntilTasksComplete()  ← 刚设为 true，秒过
          Raise SceneLoadStartEvent("MainMenu")
          _progress.Publish("Entering MainMenu...", 0.0)

          并行 → 加权聚合 70/30:
            _loader.LoadSceneAsync(config.ScenePath, Additive)
            _loader.LoadLabelAsync(config.AssetLabels)

          SetActiveScene(loadedScene)
          WaitMinDisplay(config.MinDisplayTime)
          _activeLabels = config.AssetLabels
          Raise SceneLoadCompleteEvent("MainMenu")
          _progress.Clear()

        IsReady = true
```

### 场景切换（MainMenu → OpenWorld）✅

```
  SceneLoadRequestEvent("OpenWorld")
    → 查 _configRegistry["OpenWorld"] → SceneLoadConfigSO
    → _gate.Begin(config):
        if (_isTransitioning) yield break
        _isTransitioning = true

        yield return _boot.WaitUntilTasksComplete()     ← 已 complete

        Raise SceneLoadStartEvent
        _progress.Publish("Loading OpenWorld...", 0.0)

        并行 → 加权 70/30:
          _loader.LoadSceneAsync(config.ScenePath, Additive)
          _loader.LoadLabelAsync(config.AssetLabels)

        SetActiveScene(loadedScene)
        _loader.UnloadSceneAsync("MainMenu")
        _loader.ReleaseLabels(previousConfig.AssetLabels)

        WaitMinDisplay(config.MinDisplayTime)
        Raise SceneLoadCompleteEvent
        _progress.Clear()
        _isTransitioning = false

        → PlayerService.CreatePlayer → PropertyTable.FromPreset ✅
```

### Reload（玩家死亡）⏳

> 依赖玩家死亡/重生系统。TransitionGate 已预留 reload 路径（不释放 Label，不重复 LoadLabelAsync）。

```
  SceneReloadRequestEvent(currentScene)
    → 查 _configRegistry[currentScene] → config
    → _gate.Begin(config):
        // 不释放 Label — 同一场景资产复用
        LoadSceneAsync(config.ScenePath, Additive)
        UnloadSceneAsync(oldInstance)
```

### 回主菜单 → 再开新游戏 ✅

```
  SceneLoadRequestEvent("MainMenu")
    → 正常切换流程
    → 释放前场景 AssetLabels
    → WaitUntilTasksComplete() 仍为 true（Boot 资产常驻）
    → PropertyDefinitionRegistry 字典仍存在
    → 无需重新预加载
```

### 大地图行走 — 流式地块 ⏳

> 依赖 Streaming/ChunkLoader 实现。

```
  ChunkLoader.Update → 玩家靠近 chunk_forest_07_03
    → LoadSceneAsync("chunk_forest_07_03", Additive)   // 无屏
    → LoadLabelAsync("chunk-forest")
    → 释放最远地块
```

### 进入建筑内部 ⏳

> 依赖 TransitionGate 的 AdditiveWithFade 模式实现。

```
  Trigger → _gate.Begin(hospitalConfig):
    Mode = AdditiveWithFade, Curtain = BriefFade
    // 不卸载 OpenWorld
```

## 关键设计点 ✅

- **死锁防护**：`BootPipeline` 拆分 `BootTasksComplete` 和 `IsReady`。`BootTasksComplete` 在 tasks 完成后立刻设为 true，TransitionGate 门控此标记，不等待 `IsReady`。
- **重复请求守卫**：`TransitionGate._isTransitioning` — Begin() 返回前拒绝第二个请求。
- **资产生命周期**：切换场景时释放前场景的 Addressables Labels（`boot` 标签永不释放）。
- **场景名→Config 解析**：Boot 阶段加载全部 `SceneLoadConfigSO`（Label `scene-configs`），构建字典。
- **进度条**：Boot 阶段由 BootPipeline 直接控制，后续由 TransitionGate 通过事件控制。`LoadLabelAsync` 进度为二进制（0→1），Label 内无逐资产中间值。
- **灯光**：Additive 加载后调用 `SetActiveScene(loadedScene)`。
- **配置驱动**：新增场景 = 新建 `SceneLoadConfigSO` + 填 Labels，不改代码。
- **IBootTask 注册模式**：新增 Boot 资产只需 `Register(new XxxBootTask(...))`。

## 失败处理矩阵 ✅

| 故障 | 处理 |
|------|------|
| ScenePath 无效/为空 | `OnValidate()` 报警；运行时 → Error、中止 |
| LoadSceneAsync 超时 (5s) | Log Error → 返回 MainMenu |
| LoadByLabel 返回 0 | Log Warning（非致命） |
| Boot Label 返回 0 PropertyDefSO | `Initialize(empty)` — Entity 创建报 Warning |
| Addressables 初始化失败 | `IsInitialized = false` → Error → 继续（降级） |
| 重复 SceneLoadRequest | `_isTransitioning` 守卫 → `yield break` |
| Session 结束时 Transition 进行中 | `OnGameplaySessionEnd` → `_isTransitioning = false` |

## Structs ✅

所有 struct 位于 `L2_SceneService/Structs/`，命名空间 `RedDust.GameScene`。

| Struct | 字段 |
|--------|------|
| `SLoadSceneRequest` | `string SceneName` |
| `SReloadSceneRequest` | `string SceneName` |
| `SSceneLoadStart` | `string SceneName` |
| `SSceneLoadComplete` | `string SceneName, string PreviousScene` |
| `SSceneTransition` | `string CurrentScene, string PreviousScene, bool IsLoading` |
| `SUnloadSceneRequest` | `string SceneName` |
| `SLoadingProgress` | `string PhaseName, float Progress` |

## 延后项

| 项目 | 依赖 | 状态 |
|------|------|------|
| Streaming/ChunkLoader — 流式地块 | 大地图场景就绪 | ⏳ |
| Streaming/ChunkPriorityQueue — 距离排序 | ChunkLoader | ⏳ |
| Background/BackgroundPreloader — 后台预加载 | 多场景体系就绪 | ⏳ |
| 建筑内部 AdditiveWithFade | TransitionGate LoadMode 已预留 | ⏳ |
| `chunk-*` / `bg-*` Labels | 对应加载器实现 | ⏳ |
| Reload 完整流程 | 玩家死亡/重生系统 | ⏳ |
| LoadLabelAsync 逐资产进度 | 无（Addressables API 限制） | ⏳ |

## v1 → v2 变更 ✅

| v1 | v2 | 原因 |
|----|----|------|
| `BaseService` 继承 | `ModuleChildMono` | Module 系统统一生命周期 |
| `EventDispatcher` 订阅 | `EventHub.Get<T>()` | EventDispatcher 已退役 |
| MainMenu 嵌入 Core Canvas | MainMenu 独立 Additive 场景 | 统一加载模型 |
| `SceneService` 只做场景切换 | 加载管理中心 | Boot/Transition/Streaming 全归 Scene |
| `minLoadingDisplayTime` 硬编码 | `SceneLoadConfigSO.MinDisplayTime` | 可配置 |
| 无预加载阶段 | `BootPipeline` + `IBootTask` 注册模式 | PropertyDefSO 必须在首场景前加载 |
| PropertyDefSO 靠 AssetDatabase | Addressables Label `"boot"` 批量加载 | AssetDatabase 编辑器专用 |
| `LoadingOrchestrator` 独立 L2 | 取消，并入 `BootPipeline`/`TransitionGate` | 不应独立 |
| `_referencedDefs` bake hack | 撤销 | 非正统方案 |
| `PropertyTreeBuildPreprocessor` | 撤销 | hack 残留 |
