# GameEvent<T>

`Assets/Scripts/L1_Core/Events/GameEvent.cs`

## 调用链

```
系统模块 (L2-L3)
  │  new GameEvent<T>() 或继承创建资产
  │  Raise(T)     → 通知所有 listener
  │  Register()   ← 订阅方
  │  Unregister() ← 订阅方
  ▼
GameEvent<T>  →  继承 EventChannelBase
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| → 继承 | EventChannelBase | 父类 |
| ← 使用 | 所有非输入的发布方 | 持有并调用 Raise() |
| ← 使用 | 所有非输入的订阅方 | 调用 Register() |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `ListenerCount` | `override int` | 当前注册 listener 数 |

## 方法

### Register(handler)
```csharp
public void Register(Action<T> handler)
```
- **用途**: 订阅事件
- **参数**: `handler` — 回调
- **调用者**: 订阅方 OnEnable 中

### Unregister(handler)
```csharp
public void Unregister(Action<T> handler)
```
- **用途**: 取消订阅
- **调用者**: 订阅方 OnDisable 中

### Raise(payload)
```csharp
public void Raise(T payload)
```
- **用途**: 发布事件，通知所有注册的 listener
- **参数**: `payload` — 事件负载
- **调用者**: 发布方

### ClearAllListeners()
- **用途**: 清空 listener 列表

## 内部机制

- listener 列表通过 `List<Action<T>>` 管理，不序列化
- `Raise()` 使用倒序遍历，防止回调中修改列表
- Editor 下 `Raise()` 额外调用 `NotifyRaised()` 触发拓扑高亮

## 使用规则

- `GameEvent<T>` 是 **abstract** 类，必须创建具体子类并加 `[CreateAssetMenu]` 才能作为 `.asset` 使用
- 仅用于系统事件（SceneLoad、GameState），输入事件使用 `InputEvent<T>`

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 无 | — | — |
