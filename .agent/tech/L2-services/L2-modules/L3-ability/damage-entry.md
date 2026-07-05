# DamageEntry — 单通道伤害数据

> `L3_Ability/Structs/DamageEntry.cs` · 2026-07-05
> **Last Verified**: 2026-07-05 | **Verification**: File exists, signatures match code

## 定位

`SDamageInfo` 的 per-channel 数据组件。每个 `DamageEntry` 对应一个实体伤害通道（武器/身体），经过技能修正后的 outgoing 伤害值。取代旧的 `Amount` float + `EffectTags` 数组扁平结构。

## 字段

| 字段 | 类型 | 语义 |
|------|------|------|
| `Tag` | RdTag | 伤害类型标签，Reactor 侧按此路由抗性 |
| `Amount` | float | 施展方 outgoing 伤害（目标减免前） |
| `Duration` | float | 0=瞬时伤害，>0=DOT 持续时长（秒） |
| `Interval` | float | DOT 跳间隔（秒），0=未指定 |

## 便捷属性

| 属性 | 逻辑 |
|------|------|
| `IsInstant` | `Duration <= 0f` |
| `IsDot` | `Duration > 0f` |

## 管道上下文

```
ExecutionState.BuildDamageInfo:
  for each 实体通道 × 技能修正(tag匹配):
    amount = baseValue × (1 + ΣmodPercent) + ΣmodAdd
    → new DamageEntry(tag, amount, duration, interval)
    → SDamageInfo.Damage[]

AbilityReactor.Resolve:
  foreach entry in hit.Damage:
    IsDot → DOT TODO
    IsInstant → ResolutionCallback → ApplyDamage
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 构造 | ExecutionState.BuildDamageInfo | 按通道生成 |
| 消费 | SDamageInfo.Damage[] | 持有数组 |
| 消费 | AbilityReactor.Resolve | 遍历分流 instant/DOT |
| 消费 | CharacterCombat.OnResolveDamage | 按 Tag 路由抗性（TODO） |
