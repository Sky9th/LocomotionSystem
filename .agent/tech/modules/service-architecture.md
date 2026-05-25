# Service 架构与会话生命周期

日期: 2026-05-23

## 分层

DontDestroyOnLoad (GameManager) 分为两层：

**基础设施层**（始终存活）
- EventDispatcherService — 事件总线
- SceneService — 场景加载/卸载
- GameStateService — 全局状态机
- InputService — 输入映射
- TimeService — 时间控制

**会话层**（NewGame 入口 → 返回 MainMenu 销毁）
- PlayerService — 持有 Player GameObject
- CameraService — 持有 cameraPivot, isFollowingPlayer
- UIService — 持有 activeOverlays
- GameContext — 持有 gameplay 快照

内容场景 (Additive) 只放纯关卡内容，卸载时 Unity 自动销毁。

## 会话协调

GameService 是 Boot 和 Teardown 的统一协调者。
Bootstrap 中抢先订阅 SGameState（在 AttachDispatcherToServices 之前），
确保 Teardown 先于其他 SGameState 订阅者执行。

### IGameplaySessionHandler

创建了会话级对象的 Service 实现此接口：

```csharp
public interface IGameplaySessionHandler
{
    void OnGameplaySessionEnd();
}
```

GameService 遍历 registeredServices，检查 `is IGameplaySessionHandler`，统一调用。
interface 不耦合 BaseService。

### sessionWasActive 守卫

GameService 跟踪 `sessionWasActive`。只有经历过 Playing 状态 → MainMenu 才触发 Teardown。
首次启动时的 MainMenu 初始化不误触发。

### Teardown 时序

```
SGameState{MainMenu} 发布（同步）
  ▶ GameService.TeardownSession()           ← 先于 UIService
      PlayerService.OnGameplaySessionEnd()  → Destroy(player)
      CameraService.OnGameplaySessionEnd()  → isFollowingPlayer=false, DestroyCameraPivot
      UIService.OnGameplaySessionEnd()      → HideAllOverlays
      GameContext.ClearSnapshots()
  ▶ UIService.HandleGameState              → ShowScreen(MainMenu)
  ▶ InputService.HandleGameStateChanged    → 禁用输入
```

## Service 间通信规则

- **写**：Service 通过 `PublishState<T>(T snapshot)` 同时写入 GameContext + Dispatcher
- **读**：通过 Dispatcher 订阅（push）或 GameContext 轮询（pull）
- **禁止** Service 持有其他 Service 的直接引用（如 `private PlayerService _playerService`）
- Service 可持有自己创建的 GameObject/内部对象引用

## Player 生命周期

PlayerService 拥有 Player：
- `Instantiate(playerPrefab, transform)` — parent 到自身（DontDestroyOnLoad），场景卸载不销毁
- `OnGameplaySessionEnd()` — 显式 Destroy(playerInstance)
- 门过渡（未来）：Player 在会话层，内容场景切换不影响

## TimeService

TimeService 是时间管理的唯一决策者，不接收外部命令事件。

订阅：
- SSceneLoadStart → 场景加载中冻结
- SSceneLoadComplete → 场景加载结束恢复
- SGameState → Paused 时冻结
- STimeScaleIAction → 输入层的时间倍率调整

`isSceneLoading` 和 `isGamePaused` 分开追踪，ApplyFreeze() 检查两者。
两个原因可能重叠（从 Paused 返回 MainMenu 时场景卸载），保证不提前恢复。

## Editor 直开

EditorCoreLoader.cs（`Assets/Scripts/Editor/`）：
- `[InitializeOnLoad]` 监听 `ExitingEditMode`
- 打开非 Core 场景 → Play → 自动 Additive 加载 Core

GameService Bootstrap 后检测 activeScene ≠ Core：
- SceneService 记录当前内容场景
- 设置 Core 为 activeScene（确保内容场景可被卸载）
- 发布 SSceneLoadComplete → 触发等同正常 NewGame 流程

GameStateService.OnRegister 检测 activeScene ≠ Core → initialState = Playing（跳过 MainMenu）。
