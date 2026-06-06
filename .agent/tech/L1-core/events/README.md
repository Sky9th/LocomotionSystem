# L1-core/events — SO Event Channel 基础设施

## 层级定位

**L1** — 事件通道的抽象基类和通用组件，不依赖任何业务层（L2/L3/L4/L5）。

## 调用链

```
InputService (L2)
  │  持有 InputEvent 资产，管理生命周期
  │
  ▼
EventChannelBase  ← 所有事件通道的抽象根
  ├── GameEvent<T>            ← 通用事件通道（系统事件用）
  └── EventHub (MB)      ← 集中持有事件引用，驱动 IEventListener
        │
        │  OnEnable/OnDisable
        ▼
      IEventListener           ← 模块纯类实现的订阅约定
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|------------|------|
| EventChannelBase | — | 根本身，无依赖 |
| GameEvent<T> | 继承 EventChannelBase | 系统事件发布方持有并调用 `Raise()` |
| EventHub | 持有 EventChannelBase[] | `Get<T>()` 供外部查找；`OnEnable` 驱动 listener |
| IEventListener | — | 接口，无依赖；由模块纯类实现 |

## 设计决策

| 决策 | 原因 |
|------|------|
| EventChannelBase 不含 Listener 管理 | 给 GameEvent<T> 和 InputEvent<T> 独立设计空间 |
| EventHub 是 MB | 需要 `OnEnable`/`OnDisable` 生命周期，驱动订阅 |
| IEventListener 只有 BindEvents/UnbindEvents | 最小接口，不与具体事件类型耦合 |
| EventHub.RegisterListener() 手动注册 | 纯类不是 Component，无法用 GetComponents 发现 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Editor 拓扑扫描工具 | 计划 | SO Event Channel 讨论 |

## 子文档索引

| 文档 | 说明 |
|------|------|
| [event-channel-base.md](event-channel-base.md) | EventChannelBase — 抽象根 |
| [game-event.md](game-event.md) | GameEvent<T> — 通用事件通道 |
| [event-channels.md](event-channels.md) | EventHub — 引用集中 + 驱动 |
| [i-event-listener.md](i-event-listener.md) | IEventListener — 订阅约定接口 |
