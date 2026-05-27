# InputService
> **源文件**: `Assets/Scripts/Inputs/InputService.cs`

继承 BaseService。持有所有 InputActionHandler 数组，统一管理启停，按游戏状态控制权限。

## 调用链

```
GameService.Bootstrap()
  └── [2] Register → InputService.OnRegister()
  └── [3] AttachDispatcher → OnDispatcherAttached()
      ├── InitializeInputHandlers()
      ├── EnableActions()
      └── SyncInitialGameState()

SGameState 变化
  └── HandleGameStateChanged() → ApplyGameState()
      └── EnforceHandlerStatePermissions()
          ├── handler.SupportsState(currentGameState) ? handler.Enable() : handler.Disable()
          └── 同时检查 IsRegistered && isActiveAndEnabled

OnEnable() → EnableActions()
OnDisable() → DisableActions()
OnDestroy() → foreach handler.Dispose()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | 01-core (EventDispatcherService) | 订阅 SGameState |
| 依赖 | 01-core (GameStateService) | SGameState 触发权限切换 |
| 依赖 | InputActionHandler | 持有数组，调用生命周期 |
| 消费 | 02-character (CharacterEventReceiver) | 间接消费发布的 SIActionXxx |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `AreActionsConfigured` | bool | Handler 是否已初始化 |

## 方法

### OnRegister()
```csharp
protected override bool OnRegister(GameContext context)
```
- **用途**: 注册自身到 GameContext，重置 actionsConfigured 标记
- **参数**: `context` — GameContext 实例
- **返回**: true
- **调用者**: GameService.Bootstrap()，阶段 [2]

### OnDispatcherAttached()
```csharp
protected override void OnDispatcherAttached()
```
- **用途**: 初始化 Handler、启用动作、同步初始游戏状态
- **调用者**: GameService.Bootstrap()，阶段 [3]
- **备注**: 确保 isActiveAndEnabled 时才 EnableActions

### OnSubscriptionsActivated()
```csharp
protected override void OnSubscriptionsActivated()
```
- **用途**: 订阅 SGameState 事件
- **调用者**: GameService.Bootstrap()，阶段 [4]

### InitializeInputHandlers()
```csharp
private void InitializeInputHandlers()
```
- **用途**: 遍历 actionHandlers 数组，逐个调用 InitializeHandler(Dispatcher)
- **调用者**: OnDispatcherAttached()
- **备注**: 幂等，actionsConfigured 后跳过

### OnEnable() / OnDisable()
```csharp
private void OnEnable()
private void OnDisable()
```
- **用途**: Unity 生命周期回调 Enable/Disable 所有 Handler
- **调用者**: Unity Engine
- **备注**: OnEnable 检查 IsRegistered 防止 Awake 时机错乱

### OnDestroy()
```csharp
private void OnDestroy()
```
- **用途**: 取消 SGameState 订阅，Dispose 所有 Handler
- **调用者**: Unity Engine

### EnableActions() / DisableActions()
```csharp
private void EnableActions()
private void DisableActions()
```
- **用途**: 遍历所有 Handler 调用 Enable/Disable
- **调用者**: OnEnable / OnDisable / OnDispatcherAttached
- **备注**: EnableActions 末尾调用 EnforceHandlerStatePermissions 校正状态

### HandleGameStateChanged()
```csharp
private void HandleGameStateChanged(SGameState snapshot, MetaStruct meta)
```
- **用途**: 游戏状态变化回调，转发到 ApplyGameState
- **调用者**: EventDispatcherService
- **参数**: `snapshot` — 游戏状态快照；`meta` — 事件元数据

### SyncInitialGameState()
```csharp
private void SyncInitialGameState()
```
- **用途**: 启动时同步当前游戏状态，确保初始权限正确
- **调用者**: OnDispatcherAttached()

### ApplyGameState()
```csharp
private void ApplyGameState(EGameState nextState, bool force = false)
```
- **用途**: 应用游戏状态，状态无变化时跳过（除非 force）
- **调用者**: HandleGameStateChanged / SyncInitialGameState
- **参数**: `nextState` — 目标状态；`force` — 强制应用
- **备注**: 更新 currentGameState 后调用 EnforceHandlerStatePermissions

### EnforceHandlerStatePermissions()
```csharp
private void EnforceHandlerStatePermissions()
```
- **用途**: 按当前游戏状态逐个启用/禁用 Handler
- **调用者**: ApplyGameState / EnableActions
- **备注**: 只有 IsRegistered && isActiveAndEnabled 时才允许 Handler 启用

## 内部机制

- **生命周期**: 继承 BaseService，在 Bootstrap [2][3][4] 阶段依次初始化
- **游戏状态过滤**: handler.SupportsState() 决定某个 Handler 是否允许在当前状态激活
- **SetActive 联动**: OnEnable/OnDisable 确保 GameObject 失活时所有输入关闭

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Handler 按优先级排序 | 待做 | 旧 input-manager.md |
| 动态注册/注销 Handler | 远期 | 旧 input-manager.md |
