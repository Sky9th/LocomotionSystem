# GameStateService · 游戏状态机

> `Core/GameStateService.cs` — 全局状态切换 + ESC 键处理 + Cursor 控制，继承 BaseService

## 调用链

```
被谁调:
  GameService.Bootstrap()                    → Register()
  EventDispatcher                            → HandleEscapeIntent (订阅 SIActionUIEscape)
  外部系统 (UI 等)                            → RequestState() / ForceState()
  自身 OnRegister()                          → 发布初始状态

调谁:
  GameContext                                → RegisterService(), PublishState()
  Dispatcher                                 → Publish(SGameState)
  Cursor (Unity)                             → visible / lockState 设置
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GameContext | 注册 + 发布状态 |
| 依赖 | EventDispatcher | 订阅 ESC + 发布状态变更 |
| 依赖 | 03-input | 接收 SIActionUIEscape |
| 被依赖 | GameService | 监听状态变更触发 TeardownSession |
| 被依赖 | TimeService | 监听状态变更控制暂停 |
| 被依赖 | UI | 通过 UIService 监听状态变更切换面板 |

## 状态枚举

```csharp
enum EGameState {
    Initializing = 0,   // 启动中
    MainMenu = 10,      // 主菜单
    Playing = 20,       // 游戏中
    Paused = 30         // 暂停
}
```

状态流：`Initializing → MainMenu ⇄ Playing ⇄ Paused`

## 公开属性

```csharp
public EGameState CurrentState { get; }      // 当前状态
public EGameState PreviousState { get; }     // 上一个状态
public bool HasInitialized { get; }          // 是否已完成初始化
```

## 方法

### OnRegister()
```csharp
protected override bool OnRegister(GameContext context)
```
- **用途**: 注册自身 → 设置初始状态 → `PublishState(SGameState)`
- **备注**: Editor 下非 Core 场景时，初始状态自动设为 `Playing`（跳过主菜单）

### OnDispatcherAttached()
```csharp
protected override void OnDispatcherAttached()
```
- **用途**: 设置 `hasInitialized = true`，`ForceState(currentState)` 正式广播

### OnSubscriptionsActivated()
```csharp
protected override void OnSubscriptionsActivated()
```
- **用途**: 订阅 `SIActionUIEscape` → `HandleEscapeIntent`

### RequestState()
```csharp
public bool RequestState(EGameState nextState)
```
- **用途**: 请求切换状态（不强制，相同状态忽略）
- **返回**: 是否成功切换
- **调用者**: 外部系统（UI 按钮、游戏逻辑等）

### ForceState()
```csharp
public void ForceState(EGameState nextState)
```
- **用途**: 强制切换状态（忽略相同状态检查）
- **调用者**: `OnDispatcherAttached()` — 初始化时广播初始状态

### ApplyState()
```csharp
private bool ApplyState(EGameState nextState, bool force)
```
- **用途**: 执行状态切换 — `previousState = currentState` → `ApplyCursorMode` → `PublishState(SGameState)`
- **备注**: `hasInitialized` 守卫 — 未初始化前拒绝切换

### HandleEscapeIntent()
```csharp
private void HandleEscapeIntent(SIActionUIEscape payload, MetaStruct meta)
```
- **用途**: ESC 键处理 — `Playing → Paused`，`Paused → Playing`
- **调用者**: EventDispatcher 回调
- **备注**: 只有 MainMenu 状态下 ESC 无响应

### ApplyCursorMode()
```csharp
private void ApplyCursorMode(EGameState state)
```
- **用途**: 根据状态设置 Cursor 可见性和锁定模式
  - `MainMenu / Paused` → 可见 + 无锁定
  - `Playing` → 可见 + Confined（限制在窗口内）

### OnDestroy()
```csharp
private void OnDestroy()
```
- **用途**: 取消 ESC 订阅

## 内部机制

- `hasInitialized` 守卫 — `OnDispatcherAttached` 之前拒绝所有状态切换
- Editor 检测 — `OnRegister` 中通过 `activeScene != "Core"` 自动设置 `initialState = Playing`

## 使用规则

- **状态切换必须通过 GameStateService** — 外部不能直接改 Time.timeScale 或控制 Cursor
- **RequestState 用于业务逻辑，ForceState 用于系统初始化** — 避免误用

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Modal 状态支持 — 弹窗时 Playing 状态不切 Paused | 待做 | UI Modal 层尚未实现 |
| Loading 状态独立 — 从 Playing/Paused 分离为独立状态 | 待做 | 当前通过 SSceneTransition 表示加载，非状态机内 |
