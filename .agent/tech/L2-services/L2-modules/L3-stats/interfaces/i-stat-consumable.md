# IStatConsumable
> **源文件**: `Assets/Scripts/Stats/Interfaces/IStatConsumable.cs`

按间隔持续扣减的能力的接口契约（饥饿、口渴、体力消耗等）。

## 调用链

```
被谁调: (接口保留，无运行时调用)
调谁: (无)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| (保留) | StatDefSO | StatDefSO 不实现此接口，使用同名字段替代 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Rate` | `float` | 每秒消耗量 |
| `Interval` | `float` | 消耗间隔（0 = 每帧） |

## 方法

无方法定义。

## 使用规则

- StatDefSO 不实现 IStatConsumable，改在 Inspector 勾选 `isConsumable`
- 外部判断用 `Def.IsConsumable`（检查 isConsumable bool + consumeRate > 0）
- Tick 逻辑见 StatInstance.TickConsume()

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 接口无运行时用途，保留为文档 | — | 设计文档 stats-system.md |
