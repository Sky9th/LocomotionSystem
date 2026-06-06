# Physiology — 生理规则子系统

> `L3_Character/Stats/Physiology/` · 2026-06-07

## 定位

角色的身体固有规律——物理/生理层面的持久化行为。帧驱动，永久生效。

与 Buff 的本质区别：

| | Physiology | Buff |
|---|---|---|
| 来源 | 种族/身体固有 | 外部施加 |
| 生命周期 | 永久 | 有时限 |
| 触发方式 | 帧驱动 / 状态驱动 | 事件驱动 |
| 示例 | 冲刺耐力 ×3、饥饿扣血 | 力量药水、中毒掉血 |

## 继承树

```
Physiology (抽象根)
  Apply(CharacterStats, CharacterFrameContext, dt)

  ├── StateDrivenModifier       状态驱动修改器
  │     ShouldActivate(ctx)     条件判断（状态满足 → 挂 StatModifier）
  │     StatPath()              目标属性路径
  │     └── StaminaMetabolism   冲刺代谢：Gait==Sprint → stamina consume ×3

  └── ChainDepletion             连锁枯竭
        SourcePath()             来源属性路径
        TargetPath()             目标属性路径
        DamagePerSec()           每秒伤害值
        └── HungerDamagesHp     饥饿伤害：Hunger==0 → HP -5/s
```

## StatModifier 叠加

Physiology 和 Buff 共享同一个 `StatModifier` 列表，由 `StatInstance.ApplyRates(dt)` 统一消费：

```
SprintStaminaRule:   Multiplier = 3f
Stamina Buff:        Multiplier = 0.5f
→ ctx.Multiplier = 3f × 0.5f = 1.5f
→ delta = -consumeRate × 1.5f × dt
```

## 与 Ability 的关系

- Physiology ≠ Ability。生理规则不经过 Ability 管道。
- 旧的 `DamageRule` / `BatchDamageRule` / `PassiveGainRule` 已被 Ability 管道接管，已删除。
