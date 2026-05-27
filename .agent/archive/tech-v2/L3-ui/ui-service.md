# UIService · UI 总管

> `Assets/Scripts/UI/UIService.cs` — 继承 BaseService + IGameplaySessionHandler。Screen/Overlay/Modal 生命周期管理、场景过渡、导航路由。

## 调用链

```
OnRegister → context.RegisterService(this) + panelConfig.BuildLookup()
OnSubscriptionsActivated → Dispatcher.Subscribe<SGameState, SSceneLoadStart, SSceneLoadComplete>
OnServicesReady → 读 SGameState → HandleGameState

外部调用:
  ├── MainMenuScreen.HandleNewGame() → uiService.RequestNewGame()
  │   └── StartSceneTransition("NewGame", Playing)
  │       └── 淡出 Screen → Destroy → Dispatcher.Publish(SLoadSceneRequest)
  │
  ├── MainMenuScreen.HandleQuit() → uiService.RequestQuit()
  │   └── Application.Quit()
  │
  ├── PauseMenuScreen.HandleContinue() → uiService.RequestResume()
  │   └── GameStateService.RequestState(Playing)
  │
  ├── PauseMenuScreen.HandleMainMenu() → uiService.RequestMainMenu()
  │   └── 淡出 Screen → Destroy → Dispatcher.Publish(SUnloadSceneRequest)
  │
  └── VitalsOverlay.Update() → uiService.TryGetPlayerStats()

事件回调:
  ├── HandleGameState → ShowScreen/HideScreen/ShowOverlay
  ├── HandleSceneLoadStart → loadingCanvasGroup.alpha = 1
  └── HandleSceneLoadComplete → loadingCanvasGroup.alpha = 0 → GameStateService
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | 01-core (EventDispatcherService) | 订阅 SGameState / SSceneLoadStart / SSceneLoadComplete |
| 依赖 | 01-core (PlayerService) | TryGetPlayerStats |
| 依赖 | 01-core (GameStateService) | RequestState |
| 依赖 | UIPanelConfigSO | id→Prefab 查找 |
| 持有 | UIScreen / UIOverlay | 创建、管理、销毁 |
| 被调用 | MainMenuScreen / PauseMenuScreen | 按钮事件回调 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `IsInputBlocked` | bool | 场景过渡期间是否锁定输入 |

## 公开方法

### ShowScreen()
```csharp
public void ShowScreen(UIScreenId id, object args = null)
```
- **用途**: 显示指定 Screen，自动销毁当前 Screen
- **参数**: `id` — Screen 枚举；`args` — 可选参数
- **调用者**: HandleGameState / 外部导航
- **备注**: 如果已有 currentScreen，等待 ExitSequence 完成后再创建新 Screen

### HideScreen()
```csharp
public void HideScreen(UIScreenId id)
```
- **用途**: 隐藏指定 Screen
- **参数**: `id` — Screen 枚举
- **调用者**: HandleGameState (Playing 分支)

### ShowOverlay()
```csharp
public void ShowOverlay(UIOverlayId id, object args = null)
```
- **用途**: 显示 Overlay，如已存在则跳过
- **参数**: `id` — Overlay 枚举；`args` — 可选参数
- **调用者**: HandleGameState (Playing 分支)

### HideOverlay()
```csharp
public void HideOverlay(UIOverlayId id)
```
- **用途**: 隐藏 Overlay
- **参数**: `id` — Overlay 枚举
- **调用者**: 外部调用

### TryGetSnapshot()
```csharp
public bool TryGetSnapshot<T>(out T snapshot) where T : struct
```
- **用途**: 委托 GameContext.TryGetSnapshot
- **调用者**: 各 Overlay/Screen 的数据查询
- **备注**: 方便 UI 层读取游戏状态

### TryGetPlayerStats()
```csharp
public bool TryGetPlayerStats(out Dictionary<string, (float current, float max)> stats)
```
- **用途**: 查询玩家角色数值
- **返回**: 是否成功获取
- **调用者**: VitalsOverlay.Update()
- **备注**: 委托 PlayerService.TryGetPlayerStats

### RequestNewGame()
```csharp
public void RequestNewGame()
```
- **用途**: 开始新游戏，触发场景加载 → Playing
- **调用者**: MainMenuScreen.HandleNewGame()

### RequestMainMenu()
```csharp
public void RequestMainMenu()
```
- **用途**: 返回主菜单，触发场景卸载
- **调用者**: PauseMenuScreen.HandleMainMenu()
- **备注**: IsInputBlocked 防止重复调用

### RequestResume()
```csharp
public void RequestResume()
```
- **用途**: 恢复游戏 (取消暂停)
- **调用者**: PauseMenuScreen.HandleContinue()

### RequestQuit()
```csharp
public void RequestQuit()
```
- **用途**: 退出游戏
- **调用者**: MainMenuScreen.HandleQuit()

### OnGameplaySessionEnd()
```csharp
public void OnGameplaySessionEnd()
```
- **用途**: 会话结束时清除所有 Overlay
- **调用者**: GameService.TeardownSession()
- **备注**: IGameplaySessionHandler 实现

## 内部方法

### ActivateScreen()
```csharp
private void ActivateScreen(UIScreen screen, UIScreenId id, object args)
```
- **用途**: 设置 currentScreen，激活 GameObject，播放进入动画
- **调用者**: ShowScreen

### StartSceneTransition()
```csharp
private void StartSceneTransition(string sceneName, EGameState targetState)
```
- **用途**: 场景切换流程 — 淡出当前 Screen → 发布场景加载请求
- **参数**: `sceneName` — 目标场景名；`targetState` — 加载完成后的目标游戏状态
- **调用者**: RequestNewGame

### HandleSceneLoadStart()
```csharp
private void HandleSceneLoadStart(SSceneLoadStart _, MetaStruct __)
```
- **用途**: 场景加载开始时显示 loading 遮罩
- **调用者**: EventDispatcherService

### HandleSceneLoadComplete()
```csharp
private void HandleSceneLoadComplete(SSceneLoadComplete evt, MetaStruct __)
```
- **用途**: 场景加载完成时隐藏 loading 遮罩，切换到目标游戏状态
- **调用者**: EventDispatcherService

### TryGetScreen()
```csharp
private bool TryGetScreen(UIScreenId id, out UIScreen screen)
```
- **用途**: 查找/实例化 Screen Prefab
- **调用者**: ShowScreen
- **备注**: 先查已有实例，没有再 Instantiate

### TryGetOverlay()
```csharp
private bool TryGetOverlay(UIOverlayId id, out UIOverlay overlay)
```
- **用途**: 查找/实例化 Overlay Prefab
- **调用者**: ShowOverlay / HideOverlay

### HandleGameState()
```csharp
private void HandleGameState(SGameState state, MetaStruct meta = default)
```
- **用途**: 游戏状态切换 → 显示/隐藏对应 UI
- **调用者**: EventDispatcherService + OnServicesReady

### HideAllOverlays()
```csharp
private void HideAllOverlays()
```
- **用途**: 销毁所有活跃 Overlay
- **调用者**: OnGameplaySessionEnd

## 内部机制

- **PanelState**: 缓存已实例化的 MonoBehaviour 实例，避免重复 Instantiate
- **Screen 互斥**: 通过 currentScreen 字段保证同时只有一个 Screen
- **Overlay 并存**: 通过 activeOverlays List 维护，可多个同时显示
- **输入锁定**: IsInputBlocked 防止场景过渡期间的重复点击

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Modal 弹窗系统 | 待做 | 旧 ui-system.md |
| 设置面板导航 | 待做 | 旧 ui-system.md |
| 存档面板导航 | 待做 | 旧 ui-system.md |
