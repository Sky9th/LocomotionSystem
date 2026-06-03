# StatModifier
> **源文件**: `Assets/Scripts/Stats/StatModifier.cs`

修改器定义，通过委托回调影响 Stat 的计算。

## 调用链

```
被谁调:
  ToggleModifierRule 构造函数      ← new StatModifier { Owner, Apply }
  StatInstance.AddModifier(m)     ← 添加到 modifiers 列表
  StatInstance.RemoveByOwner(o)   ← 按 Owner 移除
  StatInstance.CollectModifiers() ← 遍历并执行每个 m.Apply(stat, ctx)

调谁:
  Apply(StatInstance, ModifierContext) ← 回调委托，由修改器创建者定义
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | StatInstance | modifiers 列表持有 StatModifier 引用 |
| 被依赖 | 02-character Rules | Rule 创建和持有 Modifier 实例 |
| 依赖 | ModifierContext | Apply 委托接收 ctx，修改 Addend/Multiplier |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Owner` | `object` | 创建者引用，用于 RemoveByOwner 时回收 |
| `Apply` | `Action<StatInstance, ModifierContext>` | 应用委托，在 CollectModifiers 时执行 |

## 方法

无方法。

## 使用规则

- 创建者负责设置 Owner 并在效果结束时调用 RemoveByOwner
- 多个 Modifier 的 Apply 委托在 CollectModifiers 中依次执行，互不知晓
- Apply 中修改 ModifierContext 的 Addend（加法槽）或 Multiplier（乘法槽）
- 合并公式：`(baseRate + sum(Addend)) * product(Multiplier)`

## 示例

```csharp
// 冲刺时 Stamina 消耗变为 3 倍
new StatModifier
{
    Owner = this,
    Apply = (s, ctx) => ctx.Multiplier = 3f
}

// 装备护甲时减少消耗
new StatModifier
{
    Owner = armor,
    Apply = (s, ctx) => ctx.Addend += 5f
}
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 增加持续时间/过期自动移除 | 远期 | 设计文档 stats-system.md |
