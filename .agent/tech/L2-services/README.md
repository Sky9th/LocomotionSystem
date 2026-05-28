# Services · Service 层

> 每个 Service 继承 BaseService，由 L1 GameService 管理生命周期。Service 使用 L3 Module 完成具体功能。

## 层级定位

L2 是 L1 和 L3 之间的桥梁。Service 不包含领域业务逻辑（业务逻辑在 L3 Module 中），只做协调、生命周期管理和外部系统对接。

- **被 L1 管理**：GameService.Bootstrap() 发现、注册、初始化。
- **使用 L3 Module**：PlayerService -> Character，CameraService -> Cinemachine 等。
- **Service 间不直接引用**：通过 GameContext.TryResolveService<>() 或 EventDispatcher 通信。

## 分层

| 层 | Service | 生命周期 |
|----|---------|---------|
| 基础设施 | EventDispatcherService, SceneService, GameStateService, TimeService | 始终存活 |
| 会话 | PlayerService, CameraService | Playing 创建，回 MainMenu 销毁 |

## 调用链

```
L2 启动 (L1 驱动):
  GameService.Bootstrap()
    -> 发现所有 BaseService
    -> 逐个 Register -> OnRegister -> ctx.RegisterService(this)
    -> 逐个 AttachDispatcher
    -> 逐个 ActivateSubscriptions
    -> 逐个 OnServicesReady

L2 运行时:
  SceneService <- SLoadSceneRequest (来自 UI/MainMenu)
    -> LoadSceneAsync(Additive) -> SSceneLoadComplete
      -> PlayerService.CreatePlayer() -> SPlayerSpawnedEvent
        -> CameraService 开始跟随

  GameStateService <- SIActionUIEscape (来自 L2_Input)
    -> PublishState(SGameState)
      -> TimeService -> Time.timeScale = 0 (暂停)
      -> GameService -> TeardownSession (回主菜单)

L2 Teardown (Playing -> MainMenu):
  GameService.TeardownSession()
    -> PlayerService.OnGameplaySessionEnd() -> Destroy(player)
    -> CameraService.OnGameplaySessionEnd() -> DestroyPivot
    -> GameContext.ClearSnapshots()
```

## 耦合模块

| Service | 使用的 L3 Module | 被谁使用 |
|---------|-----------------|---------|
| EventDispatcherService | -- | 所有 L2/L3（事件通道） |
| SceneService | -- | UI (发送加载请求) |
| TimeService | -- | 全局 (Time.timeScale) |
| GameStateService | L2-input (接收 ESC) | GameService, TimeService, UI |
| PlayerService | L2-modules/L3-character (CharacterActor) | CameraService |
| CameraService | Cinemachine (Unity) | L2-modules/L3-character (朝向) |

## 设计决策

| 决策 | 原因 |
|------|------|
| Service 间禁止直接引用 | 解耦 -- 通过 GameContext 查找或 EventDispatcher 通信 |
| GameService 抢先订阅 SGameState | Teardown 必须在其他订阅者之前执行 |
| Core.unity 常驻 + Additive Loading | Service 不随场景切换销毁 |
| 会话层 Service 实现 IGameplaySessionHandler | 统一的会话结束清理入口 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 一次性订阅 (OneShotListener) -- 收到一次后自动取消 | 待做 | event-dispatcher.md |
| 多线程支持 -- 外围队列将 Publish 推入主线程 | 远期 | event-dispatcher.md |
| 优先级支持 -- Subscribe 时指定回调顺序 | 远期 | event-dispatcher.md |
| 门过渡 -- 同会话内切换内容场景时不销毁 Player | 待做 | scene-service.md |
| 加载进度回调 -- SetPhase/SetProgress 接口 | 待做 | scene-service.md |
| 慢放/快进时音效 Pitch 联动 | 待做 | time-service.md |
| 时间倍率预设 (子弹时间 0.2x / 快进 2x) | 待做 | time-service.md |
| Modal 状态支持 -- 弹窗时 Playing 状态不切 Paused | 待做 | game-state-service.md |
| Loading 状态独立 -- 从 Playing/Paused 分离为独立状态 | 待做 | game-state-service.md |
| Player Stats 正式 push/pull 接口 | 待做 | player-service.md (代码 TODO) |
| 多人支持 -- 管理多个 Player 实例 | 远期 | player-service.md |
| PlayerStart 查找改为配置引用 | 待做 | player-service.md |
| GameProfile.cameraLookRotationSpeed 实际接入 | 待做 | camera-service.md |
| 摄像机碰撞检测 (Cinemachine Collider) | 待做 | camera-service.md |

## 子文档索引

### 基础 Service

| 文件 | 内容 |
|------|------|
| [event-dispatcher.md](L2-event-dispatcher/event-dispatcher.md) | EventDispatcherService -- Subscribe/Publish/Unsubscribe |
| [scene-service.md](L2-scene-service/scene-service.md) | SceneService -- Additive Loading 流程 |
| [time-service.md](L2-time-service/time-service.md) | TimeService -- Gameplay/UI 时间分离 |
| [game-state-service.md](L2-game-state-service/game-state-service.md) | GameStateService -- 状态机 + ESC + Cursor |
| [player-service.md](L2-player-service/player-service.md) | PlayerService -- Spawn/Despawn/位置追踪 |
| [camera-service.md](L2-camera-service/camera-service.md) | CameraService -- Cinemachine + 鼠标地面坐标 |

### 复合 Service 及 L3 模块

| 模块 | 内容 |
|------|------|
| [L2-input](L2-input/README.md) | 复合 Service -- Input actions, structs, 按键映射 |
| [L2-ui](L2-ui/README.md) | 复合 Service -- UI 面板、组件、主题系统 |
| [L2-audio](L2-audio/README.md) | 复合 Service -- 音频管理、数据通道 |
| L2-modules | 占位容器 -- L3 独立模块 ([Character](L2-modules/L3-character/README.md), [Stats](L2-modules/L3-stats/README.md), [Pathfinding](L2-modules/L3-pathfinding/README.md)) |
