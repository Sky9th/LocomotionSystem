# 场景加载架构

> 日期: 2026-05-23
> 状态: 已实现

## 动机

`LoadSceneMode.Single` 场景激活和渲染时序耦合，LoadingOverlay 无法在新场景出现前清理。改用 Persistent Core Scene + Additive Loading。

## 架构

```
Core.unity (Scene 0, 永不卸载)
├── GameService (DontDestroyOnLoad)
│   ├── SceneService    — Additive 加载/卸载/追踪
│   ├── TimeService     — Time.timeScale 集中管理
│   ├── UIService
│   │   ├── MainCanvas (sortOrder=0)
│   │   │   └── MainMenuScreen (内置，非 Additive)
│   │   └── LoadingCanvas (sortOrder=999)
│   ├── PlayerService   — 监听 SSceneLoadComplete 生成玩家
│   └── ...
└── EventSystem

NewGame.unity (Additive 加载)
├── 场景内容
└── PlayerStart anchor
```

## 场景流程

### 加载流程（MainMenu → Playing）

```
UIService.RequestNewGame():
  1. currentScreen.PlayExitSequence
  2. Dispatcher.Publish(SLoadSceneRequest("NewGame"))
     → SceneService.LoadContentScene():
       a. Publish(SSceneLoadStart)
       b. LoadSceneAsync("NewGame", Additive) + 等待
       c. Publish(SSceneLoadComplete)
       d. TimeService 监听到 SSceneLoadStart → 冻结时间
                          SSceneLoadComplete → 恢复时间
  3. SSceneLoadComplete → UIService:
     a. loadingCanvasGroup.alpha = 0
     b. GameStateService.RequestState(Playing)
  4. SGameState{Playing} → GameService: sessionWasActive = true
  5. PlayerService → CreatePlayer()
  6. CameraService → 开始跟随
  7. UIService → ShowOverlay(VitalsOverlay)
```

### 返回主菜单（Playing/Paused → MainMenu）

```
UIService.RequestMainMenu():
  1. currentScreen.PlayExitSequence + Destroy
  2. Dispatcher.Publish(SUnloadSceneRequest(null))
     → SceneService.UnloadContentScene():
       Publish(SSceneLoadStart) → 卸载 → Publish(SSceneLoadComplete)
  3. SSceneLoadComplete → UIService:
     loadingCanvasGroup.alpha = 0
     GameStateService.RequestState(MainMenu)
  4. SGameState{MainMenu} → GameService.TeardownSession():
     PlayerService.OnGameplaySessionEnd() → Destroy(player)
     CameraService.OnGameplaySessionEnd() → 重置状态
     UIService.OnGameplaySessionEnd() → HideAllOverlays()
     GameContext.ClearSnapshots()
  5. UIService.HandleGameState → ShowScreen(MainMenu)
```

## 关键点

- MainMenuScreen 内嵌于 Core 场景 Canvas，无需单独 MainMenu 场景
- Loading 不通过 Instantiate/Destroy，通过 loadingCanvasGroup.alpha 切换
- 时间管理由 TimeService 监听 SSceneLoadStart/Complete 自主决定，SceneService 不管时间
- PlayerService 监听 SSceneLoadComplete 事件驱动生成玩家
- SUnloadSceneRequest(null) 表示卸载当前内容场景
- 返回主菜单时 TeardownSession 显式销毁会话层对象，GameContext 清空快照

## Editor 开发便利

Editor 直开非 Core 场景时，EditorCoreLoader 自动 Additive 加载 Core.unity。
GameService 检测到 activeScene != Core 后自动跳过 MainMenu、设置 Playing 状态、发布 SSceneLoadComplete 触发等同正常 NewGame 流程。

## 事件结构体

| 事件 | 方向 | 说明 |
|------|------|------|
| SLoadSceneRequest | UIService → SceneService | 请求 Additive 加载场景 |
| SUnloadSceneRequest | UIService → SceneService | 请求卸载内容场景（null=当前） |
| SSceneLoadStart | SceneService → 广播 | 场景过渡开始（TimeService 监听到 → 冻结） |
| SSceneLoadComplete | SceneService → 广播 | 场景过渡完成（TimeService 监听到 → 恢复） |
