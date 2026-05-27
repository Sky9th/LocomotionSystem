# ModifierContext · 修改器上下文

> `Stats/ModifierContext.cs` — 修改器合并中间结果，作为 Addend/Multiplier 的累加容器

## 调用链

```
被谁调:
  StatInstance.CollectModifiers()  ← new ModifierContext()，遍历 modifiers 传入
  StatModifier.Apply(stat, ctx)   ← 各修改器通过 ctx 写入影响值
  StatInstance.TickConsume/TickRestore ← 读取 ctx.Addend 和 ctx.Multiplier 计算结果

调谁: (无 — 纯数据容器)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | StatInstance | CollectModifiers 创建并持有 |
| 被依赖 | StatModifier | Apply 委托接收 ctx 参数 |

## 公开属性

| 属性 | 类型 | 用途 | 默认值 |
|------|------|------|--------|
| `Addend` | `float` | 加法修正值 | `0` |
| `Multiplier` | `float` | 乘法倍率 | `1f` |

## 方法

无方法。

## 使用规则

- StatInstance.CollectModifiers 中创建新的 ModifierContext，初始 Addend=0, Multiplier=1
- 每个 Modifier 的 Apply 回调修改 ctx，多个 Modifier 的累加自然合并
- TickConsume/Restore 公式：`(baseRate + Addend) * Multiplier * (ticks or dt)`
- 不提供链式调用或复杂合并规则，保持简单

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 无具体规划 | — | — |
