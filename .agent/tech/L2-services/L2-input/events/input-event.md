# InputEvent<T>

`Assets/Scripts/Services/L2_Input/Events/InputEvent.cs`

## 调用链

```
Unity Input System
  │  performed / canceled
  ▼
InputEvent<T>.OnPerformed(ctx) / OnCanceled(ctx)    ← 子类覆写
  │  Raise(T)
  ▼
订阅方 handler (由 Register 注册)
  │
  ▼
PlayerInput / PlayerDirector / ...
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| → 继承 | EventChannelBase | 父类 |
| → 实现 | IInputEvent | 接口 |
| ← 继承 | SprintInputEvent 等 (×6) | 子类 |
| ← 管理 | InputService | Initialize/Enable/Disable/Dispose |
| ← 订阅 | PlayerInput | 通过 EventChannels.Get<T>() 获取 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `ListenerCount` | `override int` | 当前注册 listener 数 |

## 方法

### InitializeEvent() / EnableEvent() / DisableEvent() / DisposeEvent()
- 实现 IInputEvent，管理 Unity Input System 生命周期

### Register(handler) / Unregister(handler) / Raise(payload) / ClearAllListeners()
- 通道 API，与 GameEvent<T> 相同

### OnPerformed(ctx) / OnCanceled(ctx) (abstract)
```csharp
protected abstract void OnPerformed(InputAction.CallbackContext ctx);
protected abstract void OnCanceled(InputAction.CallbackContext ctx);
```
- **用途**: 子类覆写，翻译 Input System 回调为 `Raise(T)`
- **调用者**: Unity Input System

## 内部机制

- `OnBind(InputAction)`: `runtimeAction.performed += OnPerformed; runtimeAction.canceled += OnCanceled;`
- `OnUnbind(InputAction)`: 逆操作
- `IsContextBound` 标记防止重复绑定
- Listener 列表不序列化

## 使用规则

- 具体子类必须加 `[CreateAssetMenu]` 才能作为 `.asset` 创建
- 子类覆写 `OnPerformed`/`OnCanceled`，翻译后调用 `Raise(T)`
- 在 InputService 的 `inputEvents` 数组中注册才能被管理

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 无 | — | — |
