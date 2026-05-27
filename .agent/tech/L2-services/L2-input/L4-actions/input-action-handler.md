# InputActionHandler
> **源文件**: `Assets/Scripts/Inputs/Actions/InputActionHandler.cs`

抽象基类，ScriptableObject。持有 InputActionReference，提供 Initialize/Enable/Disable/Dispose 生命周期。

## 调用链

```
InputService
  ├── InitializeInputHandlers() → handler.InitializeHandler(dispatcher)
  │   └── 解析 InputActionReference → 注册 performed/canceled → Execute
  ├── EnableActions() → handler.Enable()
  │   └── InputAction.Enable()
  ├── DisableActions() → handler.Disable()
  │   └── InputAction.Disable()
  └── OnDestroy() → handler.Dispose()
      └── 取消订阅 InputSystem 回调 + 清空引用

Unity Input System
  └── performed/canceled → handler.Execute(CallbackContext)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | InputService | InputService 持有数组并驱动生命周期 |
| 依赖 | 01-core (EventDispatcherService) | InitializeHandler 时注入，用于 Execute 中发布事件 |
| 依赖 | Unity Input System | InputActionReference + CallbackContext |
| 被继承 | IAPlayerMove, IAPlayerLook, IAPlayerCrouch 等 | 所有具体 Handler 继承此基类 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `IsContextBound` | bool (protected) | Dispatcher 是否已注入，回调是否已注册 |
| `IsEnabled` | bool (protected) | InputAction 是否已启用 |

## 方法

### InitializeHandler()
```csharp
public void InitializeHandler(EventDispatcherService dispatcher)
```
- **用途**: 注入 Dispatcher，解析 InputActionReference，注册 performed/canceled → Execute
- **参数**: `dispatcher` — 事件分发服务
- **调用者**: InputService.InitializeInputHandlers()
- **备注**: 幂等，IsContextBound 后跳过

### Enable()
```csharp
public void Enable()
```
- **用途**: 启用 InputAction
- **调用者**: InputService.EnableActions()
- **备注**: 只在 IsContextBound && !IsEnabled 时执行

### Disable()
```csharp
public void Disable()
```
- **用途**: 禁用 InputAction
- **调用者**: InputService.DisableActions()
- **备注**: 只在 IsEnabled 时执行

### Dispose()
```csharp
public void Dispose()
```
- **用途**: 清理 — 取消 InputSystem 回调注册 + 清空引用
- **调用者**: InputService.OnDestroy()
- **备注**: 防止 Play Mode 退出后有残留订阅

### Execute()
```csharp
protected abstract void Execute(InputAction.CallbackContext context)
```
- **用途**: 子类实现的输入处理逻辑
- **参数**: `context` — Unity Input System 回调上下文
- **调用者**: Unity Input System (performed/canceled)

### OnSupportsState()
```csharp
protected virtual bool OnSupportsState(EGameState state)
```
- **用途**: 子类覆写指定支持的游戏状态，默认所有状态都支持
- **参数**: `state` — 当前游戏状态
- **返回**: true = 允许在当前状态启用
- **调用者**: InputService.EnforceHandlerStatePermissions → SupportsState

### SupportsState()
```csharp
internal bool SupportsState(EGameState state)
```
- **用途**: 内部包装方法，委托给 OnSupportsState
- **参数**: `state` — 当前游戏状态
- **返回**: OnSupportsState(state)
- **调用者**: InputService.EnforceHandlerStatePermissions()

## 使用规则

- Handler 的 Enable/Disable 只能由 InputService 调用，不自行管理
- Dispose 必须在场景卸载或 Handler 销毁时调用，防止重复订阅
- Execute 内只做数据转换 + eventDispatcher.Publish()，不触碰场景对象
- 新增动作：继承 InputActionHandler → 实现 Execute → 在 InputService 序列化数组中注册

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| InitializeHandler 扩展参数（摄像机 Transform、姿态引用注入） | 待做 | 旧 input-manager.md |
