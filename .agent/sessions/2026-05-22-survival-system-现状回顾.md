# Survival System 分支现状回顾

> 日期: 2026-05-22
> 分支: feature/survival-system
> 目的: 对照短期/长期计划，梳理已完成、进行中、待办

---

## 一、短期计划执行状态

| Phase | 内容 | 计划状态 | 实际状态 |
|-------|------|---------|---------|
| 1 | LocomotionSystem 完结 | ✅ | ✅ 已完成 |
| 1.5 | 音效系统骨架 | ✅ | ✅ 已完成 |
| 2 | 通用数值系统 | ✅ | ✅ 已完成 |
| 2.5 | Character Stats 管理 | ✅ | ✅ 已完成 |
| 3 | 基本 HUD UI | ✅ | ⚠️ 功能完成，但正在重构（见下） |
| 3.5 | PauseMenu + Loading | ✅ | ⚠️ 同上 |
| 4 | 战斗基础 | 后续 | 未开始 |
| 5 | 角色动画增强 | 后续 | 未开始 |

---

## 二、未提交的重构：Manager → Service 架构迁移

当前工作树有大量未提交变更（-6164 行 / +402 行），核心是 **从 Manager 单体迁移到 BaseService 服务架构**。

### 已删除的旧 Manager

| 旧文件 | 替代 |
|--------|------|
| Core/GameManager.cs | Core/GameService.cs (BaseService 编排器) |
| Core/GameState.cs | Core/GameStateService.cs (状态机 + EGameState) |
| Core/PlayerManager.cs | Core/PlayerService.cs (玩家创建，CreatePlayer 注释掉) |
| Core/CameraManager.cs | Core/CameraService.cs (类名仍为 CameraManager) |
| Core/EventDispatcher.cs | Core/EventDispatcherService.cs |
| Core/TimeScaleManager.cs | Core/Time/TimeService.cs |
| Inputs/InputManager.cs | Inputs/InputService.cs |
| UI/UIManager.cs | UI/UIService.cs |

### 新增的 Service 基础设施

| 文件 | 职责 |
|------|------|
| Core/BaseService.cs | 抽象基类：Register → AttachDispatcher → ActivateSubscriptions → OnServicesReady |
| Core/GameService.cs | Singleton 编排器：Awake 引导 bootstrap，自动发现子 BaseService |
| Core/GameContext.cs | 服务注册表 + 快照仓库（已存在，未删除） |

### 新增的子模块

| 目录 | 文件 |
|------|------|
| Core/Scene/ | SceneService, SLoadSceneRequest, SSceneLoadStart, SSceneLoadComplete, SSceneTransition |
| Core/Time/ | TimeService, STimeFreeze, STimeResume, STimeScaleIAction |

### BaseService 生命周期

```
GameService.Awake
  → GameContext.Initialize()
  → RegisterService (自动发现 GetComponentsInChildren<BaseService>)
    → 每个 service.Register(gameContext)
      → service.OnRegister(context)  // 子类实现：注册自己到 GameContext
  → AttachDispatcherToServices
    → service.AttachDispatcher(eventDispatcher)
      → service.OnDispatcherAttached()  // 子类实现
  → ActivateServiceSubscriptions
    → service.ActivateSubscriptions()
      → service.OnSubscriptionsActivated()  // 子类实现：Subscribe 事件
  → InitializeServices
    → service.NotifyInitialized()
      → service.OnServicesReady()  // 子类实现：依赖其他服务的初始化
```

---

## 三、场景架构重构：Core Scene + Additive Loading

### 旧架构
- MainMenu.unity / NewGame.unity 各自独立，LoadSceneMode.Single
- LoadingOverlay 通过 Instantiate/Destroy 切换

### 新架构（未提交）
- **Core.unity** (Scene 0, 永不卸载) — GameService + EventSystem + UIService + Canvas
- **MainMenu.unity / NewGame.unity** — 内容场景，Additive 加载/卸载
- 内容场景保留 GameManager 副本，singleton 自毁，确保开发时可独立 Play

### 加载流程

```
UIService.StartSceneTransition:
  1. loadingCanvasGroup.alpha = 1
  2. currentScreen.PlayExitSequence
  3. Dispatcher.Publish(SLoadSceneRequest)
     → SceneService.LoadContentScene()
       a. Dispatcher.Publish(STimeFreeze) → TimeService: timeScale=0
       b. LoadSceneAsync(scene, Additive)
       c. UnloadSceneAsync(旧场景)
       d. minLoadingDisplayTime (unscaledDeltaTime)
  4. SceneService 完成 → SSceneLoadComplete
     → UIService: loadingCanvasGroup.alpha = 0
     → Dispatcher.Publish(STimeResume) → TimeService: timeScale=1
     → GameStateService.RequestState(targetState)
```

---

## 四、UI Prefab 重组

旧路径（已删除）:
```
Assets/Prefabs/UI/Button.prefab
Assets/Prefabs/UI/Label.prefab
Assets/Prefabs/UI/Panel.prefab
Assets/Prefabs/UI/StatBar.prefab
Assets/Prefabs/UI/MainMenu.prefab
Assets/Prefabs/UI/VitalsOverlay.prefab
Assets/Prefabs/UI/LoadingOverlay.prefab
Assets/Prefabs/UI/PauseMenuScreen.prefab
```

新路径（未跟踪）:
```
Assets/Prefabs/UI/Components/Button.prefab
Assets/Prefabs/UI/Components/Label.prefab
Assets/Prefabs/UI/Components/Panel.prefab
Assets/Prefabs/UI/Components/StatBar.prefab
Assets/Prefabs/UI/Screens/MainMenuScreen.prefab
Assets/Prefabs/UI/Screens/PauseMenuScreen.prefab
Assets/Prefabs/UI/Overlays/VitalsOverlay.prefab
Assets/Prefabs/UI/Overlays/LoadingOverlay.prefab
```

UI 脚本代码未删除，但 UIManager.cs 已替换为 UIService.cs。

---

## 五、已知问题 & 待办

### 架构层面

1. **CameraService 类名仍为 CameraManager** — 文件名 CameraService.cs 但 `class CameraManager : BaseService`，命名不一致
2. **PlayerService.CreatePlayer() 被注释掉** — 玩家创建逻辑暂未接入
3. **UIService 引用 UIManager 残留** — 内部字段名/注释可能仍有 UIManager 旧名
4. **UIOverlay/UIScreen 的 Initialize 签名** — 仍接受 `UIManager` 类型参数，需改为 `UIService`

### 长期计划未更新

long-term.md 中 HUD UI 完成度仍标为 0%，实际 Phase 3+3.5 已完成功能开发，正在做架构重构。

### 短期计划 Phase 3 待办

- StatusOverlay（骨架）
- ClockOverlay
- MainMenu 加载存档/设置子面板

---

## 六、P0 总体完成度

| 子项 | 完成度 |
|------|--------|
| LocomotionSystem | 100% ✅ |
| 音效系统骨架 | 100% ✅ |
| 通用数值系统 | 100% ✅ |
| Stats 业务管理 | 100% ✅ |
| HUD UI | ~90%（功能完成，架构重构中） |
| Service 架构迁移 | ~80%（代码写好，未提交，待验证） |
| Core Scene + Additive | ~80%（代码写好，未提交，待验证） |
| 资源系统 | 0% |
| 建造原型 | 0% |
| 丧尸AI | 0% |
| 玩家交互 | 0% |
| 地图原型 | 0% |
