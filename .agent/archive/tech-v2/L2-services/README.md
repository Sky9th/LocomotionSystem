# L2-services · Service 层

> 每个 Service 继承 BaseService，由 L1 GameService 管理生命周期。Service 使用 L3 Module 完成具体功能。

## 定位

L2 是 L1 和 L3 之间的桥梁。Service 不包含领域业务逻辑（业务逻辑在 L3 Module 中），只做协调、生命周期管理和外部系统对接。

**被 L1 管理**：GameService.Bootstrap() 发现、注册、初始化。
**使用 L3 Module**：PlayerService → Character，CameraService → Cinemachine 等。
**Service 间不直接引用**：通过 GameContext.TryResolveService<>() 或 EventDispatcher 通信。

## 分层

| 层 | Service | 生命周期 |
|----|---------|---------|
| 基础设施 | EventDispatcherService, SceneService, GameStateService, TimeService | 始终存活 |
| 会话 | PlayerService, CameraService | Playing 创建，回 MainMenu 销毁 |

## 调用链

```
L2 启动 (L1 驱动):
  GameService.Bootstrap()
    → 发现所有 BaseService
    → 逐个 Register → OnRegister → ctx.RegisterService(this)
    → 逐个 AttachDispatcher
    → 逐个 ActivateSubscriptions
    → 逐个 OnServicesReady

L2 运行时:
  SceneService ← SLoadSceneRequest (来自 UI/MainMenu)
    → LoadSceneAsync(Additive) → SSceneLoadComplete
      → PlayerService.CreatePlayer() → SPlayerSpawnedEvent
        → CameraService 开始跟随

  GameStateService ← SIActionUIEscape (来自 L3-input)
    → PublishState(SGameState)
      → TimeService → Time.timeScale = 0 (暂停)
      → GameService → TeardownSession (回主菜单)

L2 Teardown (Playing→MainMenu):
  GameService.TeardownSession()
    → PlayerService.OnGameplaySessionEnd() → Destroy(player)
    → CameraService.OnGameplaySessionEnd() → DestroyPivot
    → GameContext.ClearSnapshots()
```

## 耦合模块

| Service | 使用的 L3 Module | 被谁使用 |
|---------|-----------------|---------|
| EventDispatcherService | — | 所有 L2/L3（事件通道） |
| SceneService | — | UI (发送加载请求) |
| TimeService | — | 全局 (Time.timeScale) |
| GameStateService | L3-input (接收 ESC) | GameService, TimeService, UI |
| PlayerService | L3-character (CharacterActor) | CameraService |
| CameraService | Cinemachine (Unity) | L3-character (朝向) |

## 设计决策

| 决策 | 原因 |
|------|------|
| Service 间禁止直接引用 | 解耦 — 通过 GameContext 查找或 EventDispatcher 通信 |
| GameService 抢先订阅 SGameState | Teardown 必须在其他订阅者之前执行 |
| Core.unity 常驻 + Additive Loading | Service 不随场景切换销毁 |
| 会话层 Service 实现 IGameplaySessionHandler | 统一的会话结束清理入口 |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [event-dispatcher.md](event-dispatcher.md) | EventDispatcherService — Subscribe/Publish/Unsubscribe |
| [scene-service.md](scene-service.md) | SceneService — Additive Loading 流程 |
| [time-service.md](time-service.md) | TimeService — Gameplay/UI 时间分离 |
| [game-state-service.md](game-state-service.md) | GameStateService — 状态机 + ESC + Cursor |
| [player-service.md](player-service.md) | PlayerService — Spawn/Despawn/位置追踪 |
| [camera-service.md](camera-service.md) | CameraService — Cinemachine + 鼠标地面坐标 |
