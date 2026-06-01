# L2-input/events — 输入事件通道

## 层级定位

**L2** — InputEvent<T> 继承 L1 的 EventChannelBase + IInputEvent 接口，由 InputService 管理生命周期。

## 调用链

```
InputService
  │  InitializeInputEvents → 绑定 InputAction 回调
  │  EnableInputEvents     → InputAction.Enable()
  ▼
InputEvent<T> : EventChannelBase, IInputEvent
  │  OnPerformed(ctx)  ← Unity Input System
  │  OnCanceled(ctx)   ← Unity Input System
  │  Raise(T)          → 通知订阅方
  │
  └── 具体事件类型 (×6)
        ├── SprintInputEvent
        ├── SecondaryInteractEvent
        ├── PrimaryInteractEvent
        ├── CrouchInputEvent
        ├── ProneInputEvent
        └── StandInputEvent
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|------------|------|
| IInputEvent | — | 接口，供 InputService 统一管理生命周期 |
| InputEvent<T> | 继承 EventChannelBase, 实现 IInputEvent | 泛型通道 |
| 具体事件 | 继承 InputEvent<T> | 定义 OnPerformed 翻译逻辑 |
| InputService | 持有 EventChannelBase[] | Initialize/Enable/Disable/Dispose |

## 设计决策

| 决策 | 原因 |
|------|------|
| InputEvent<T> 直接整合 Input System 回调 | Unity 是真实发布者，不需要中间 Relay |
| 每个输入事件一个具体类 + 一个 .asset | 类型安全 + Inspector 可见 + 可独立创建 |
| Raise(T) 在被覆写的 OnPerformed 中调用 | 子类定义翻译逻辑（如 bool 读 ReadValueAsButton） |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 添加 Move/Look 模拟事件 | 待建 | Phase 4 TODO |
| 添加 Jump 事件接入 | 待建 | 当前硬编码 false |

## 子文档索引

| 文档 | 说明 |
|------|------|
| [i-input-event.md](i-input-event.md) | IInputEvent — 生命周期接口 |
| [input-event.md](input-event.md) | InputEvent<T> — 泛型输入通道基类 |
| [button-input-events.md](button-input-events.md) | 具体按钮事件 (×6) |
