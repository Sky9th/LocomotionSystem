# IStatRestorable · 可恢复接口

> `Stats/Interfaces/IStatRestorable.cs` — 按间隔持续向 MaxValue 恢复的能力的接口契约

## 用途

标记 Stat 具有随时间自动恢复的能力（体力、生命值自然恢复等）。StatDefSO 不实现此接口，改为在 Inspector 勾选 `isRestorable` + 填写 `restoreRate` / `restoreInterval`。

此接口保留为文档契约，说明可恢复 Stat 应有的数据签名。

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

```csharp
float Rate { get; }       // 每秒恢复量
float Interval { get; }   // 恢复间隔（0 = 每帧）
```

## 方法

无方法定义。

## 内部机制

无运行时实现。此接口为纯设计契约。

## 使用规则

- StatDefSO 不实现 IStatRestorable，改在 Inspector 勾选 `isRestorable`
- 外部判断用 `Def.IsRestorable`（检查 isRestorable bool + restoreRate > 0）
- Tick 逻辑见 StatInstance.TickRestore()

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 接口无运行时用途，保留为文档 | — | 设计文档 stats-system.md |
