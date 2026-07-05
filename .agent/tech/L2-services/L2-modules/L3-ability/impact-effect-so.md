# ImpactEffectSO · 冲击效果

> **Last Verified**: 2026-07-05 | **Verification**: All referenced files exist, signatures match code

> `L3_Ability/Config/Effect/ImpactEffectSO.cs` — `sealed class`，继承 `EffectSO`。冲击效果：硬直 + 击退。在 `ActiveAbilitySO.targetEffects[]` 中配置。

## 字段

| 字段 | 类型 | 默认值 | 语义 |
|------|------|:---:|------|
| `reactionLevel` | EHitReactionLevel | Flinch | 受击反应等级。Flinch/Stagger/Knockdown |
| `staggerValue` | float | 0 | 冲击值。0=无硬直。后续用于 Resistance/霸体比较 |
| `knockbackForce` | float | 0 | 击退力度。0=纯硬直无位移 |
| `knockbackDir` | EKnockbackDirection | HitDirection | 击退方向 |

继承自 EffectSO: `effectTag`, `description`, `duration`, `stackable`, `maxStacks`

## 数据流

```
ActiveAbilitySO.targetEffects[] → ExecutionState.BuildDamageInfo()
  → 提取 ImpactEffectSO → 传入 SDamageInfo.ImpactEffect
  → CharacterCombat.OnReaction() → reactionLevel → 选择受击动画
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | ExecutionState | 从 targetEffects 提取，传入 SDamageInfo |
| 被依赖 | CharacterCombat | 读取 reactionLevel + staggerValue |
| 关联 | EHitReactionLevel | 枚举值决定动画选择 |
| 关联 | EKnockbackDirection | 击退方向枚举 |
