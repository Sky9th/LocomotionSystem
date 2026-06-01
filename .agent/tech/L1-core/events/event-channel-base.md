# EventChannelBase

`Assets/Scripts/L1_Core/Events/EventChannelBase.cs`

## 调用链

```
EventChannelBase  ← GameEvent<T>, InputEvent<T>, EventChannels 继承或持有
     ↑
  无直接调用者 — 纯抽象根
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| → 被继承 | GameEvent<T> | 父类 |
| → 被继承 | InputEvent<T> (via IInputEvent) | 父类 |
| → 被持有 | EventChannels | 数组元素类型 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `ListenerCount` | `abstract int` | 当前注册的 listener 数量，Editor 和调试用 |
| `OnAnyRaised` (Editor) | `static event` | 任何通道 Raise 时触发，供 Editor 工具订阅 |

## 方法

### ClearAllListeners()
```csharp
public abstract void ClearAllListeners()
```
- **用途**: 清空所有已注册的 listener
- **调用者**: OnDisable 清理、模块销毁时

### NotifyRaised() (Editor only)
```csharp
protected void NotifyRaised()
```
- **用途**: 触发 `OnAnyRaised` 事件，仅在 `#if UNITY_EDITOR` 下编译
- **调用者**: 子类 `Raise()` 方法中调用

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 无 | — | — |
