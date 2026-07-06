# ⛔ DEPRECATED — SceneService v1

> 本文档已废弃，由 [scene-service-loading.md](scene-service-loading.md) 取代。
> 保留供历史参考。2026-07-06。

# SceneService · 场景管理

> `Core/Scene/SceneService.cs` — Additive Loading 场景加载/卸载，继承 BaseService

## 调用链

```
被谁调:
  GameService.Bootstrap()                    → Register()
  EventDispatcher                            → HandleLoadSceneRequest / HandleUnloadSceneRequest (订阅)
  GameService.Bootstrap() (Editor only)       → SetCurrentContentScene()

调谁:
  GameContext                                → RegisterService(), PublishState()
  Dispatcher                                 → Publish(SSceneLoadStart), Publish(SSceneLoadComplete)
  SceneManager                               → LoadSceneAsync(), UnloadSceneAsync(), GetSceneByName()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GameContext | 注册 + 发布过渡状态 |
| 依赖 | EventDispatcher | 接收加载/卸载请求、发布加载完成 |
| 被依赖 | PlayerService | 加载完成后 Spawn Player |
| 被依赖 | TimeService | 加载中暂停游戏时间 |
| 被依赖 | UI | 发送加载请求 (SLoadSceneRequest) |

## 公开属性

```csharp
public string CurrentContentScene { get; }   // 当前加载的内容场景名
```

## 方法

### OnRegister()
```csharp
protected override bool OnRegister(GameContext context)
```
- **用途**: 调用 `context.RegisterService(this)`

### OnSubscriptionsActivated()
```csharp
protected override void OnSubscriptionsActivated()
```
- **用途**: 订阅 `SLoadSceneRequest` 和 `SUnloadSceneRequest`

### SetCurrentContentScene()
```csharp
public void SetCurrentContentScene(string sceneName)
```
- **用途**: 设置当前场景名（不触发加载）
- **调用者**: `GameService.Bootstrap()` — Editor 下非 Core 场景启动时记录当前场景

### HandleLoadSceneRequest()
```csharp
private void HandleLoadSceneRequest(SLoadSceneRequest request, MetaStruct meta)
```
- **用途**: 启动协程 `LoadContentScene(sceneName)`
- **调用者**: EventDispatcher 回调

### LoadContentScene()
```csharp
private IEnumerator LoadContentScene(string sceneName)
```
- **用途**: 异步加载内容场景流程
- **流程**:
  1. 发布 `SSceneLoadStart`
  2. `PublishState(SSceneTransition{IsLoading=true})` — UI 可响应此状态显示 Loading
  3. `SceneManager.LoadSceneAsync(sceneName, Additive)`
  4. 如果存在旧场景 → `UnloadSceneAsync(old)`
  5. 设置 `currentContentScene`
  6. 等待 `minLoadingDisplayTime`（默认 0.5s）— 防止 Loading 闪烁
  7. `PublishState(SSceneTransition{IsLoading=false})`
  8. 发布 `SSceneLoadComplete`

### HandleUnloadSceneRequest()
```csharp
private void HandleUnloadSceneRequest(SUnloadSceneRequest request, MetaStruct meta)
```
- **用途**: 启动协程 `UnloadContentScene(sceneName)`
- **备注**: 如果请求未指定场景名，卸载 `currentContentScene`

### UnloadContentScene()
```csharp
private IEnumerator UnloadContentScene(string sceneName)
```
- **用途**: 异步卸载内容场景流程
- **流程**: 与加载对称 — 发布事件 → 卸载 → 等待最小显示时间 → 发布完成

### OnDestroy()
```csharp
private void OnDestroy()
```
- **用途**: 取消 `SLoadSceneRequest` 和 `SUnloadSceneRequest` 订阅

## 场景加载架构

```
Core.unity (常驻, DontDestroyOnLoad)
  ├── GameService (root)
  ├── GameContext
  ├── EventDispatcherService
  ├── CameraService
  ├── UIService
  └── ...所有 Service

+ Gameplay.unity (Additive, 同时只有一个)
  ├── 关卡地形/建筑/道具
  ├── Player Spawn 点 (PlayerStart)
  └── 敌人/NPC
```

**规则**: Core 永不卸载，内容场景 Additive 加载。切换到新场景时先加载后卸载旧场景（无缝过渡）。

## 关键设计点

- **MainMenuScreen 内嵌于 Core Canvas** — 不需要单独的 MainMenu 场景，减少加载步骤
- **Loading 不通过 Instantiate/Destroy** — 通过 `loadingCanvasGroup.alpha` 切换，避免 GC
- **时间管理由 TimeService 自主决定** — SceneService 不管时间，只发布事件；TimeService 监听后自行冻结/恢复
- **`SUnloadSceneRequest(null)` 表示卸载当前内容场景** — 调用方不需要知道当前场景名
- **返回主菜单时 TeardownSession 显式销毁会话层对象** — 不依赖场景卸载的自动销毁

## 场景 Structs

- `SLoadSceneRequest` — `{ string SceneName }`
- `SSceneLoadStart` — `{ string SceneName }`
- `SSceneLoadComplete` — `{ string SceneName, string PreviousSceneName }`
- `SSceneTransition` — `{ string SceneName, string PreviousSceneName, bool IsLoading }`
- `SUnloadSceneRequest` — `{ string SceneName }`

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 门过渡 — 同会话内切换内容场景时不销毁 Player | 待做 | 旧 service-architecture.md — Player 在会话层，场景切换不受影响 |
| 加载进度回调 — `SetPhase/SetProgress` 接口 | 待做 | 当前 `minLoadingDisplayTime` 是固定等待，无真实进度 |
