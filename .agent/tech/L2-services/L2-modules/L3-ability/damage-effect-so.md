# DamageEffectSO — 实体伤害通道

> `L3_Ability/Config/Effect/DamageEffectSO.cs` · 2026-06-11 · Updated 2026-07-05
> **Last Verified**: 2026-07-05 | **Verification**: All referenced files exist, signatures match code

## 定位

伤害通道资产。**只来自实体**（武器/身体/陷阱/投掷物/环境），不来自技能。每个 `DamageEffectSO` 代表一个独立的伤害类型通道，携带 `baseValue` 基底伤害。

技能侧伤害修正使用 `DamageModifierEffectSO`。

## 字段

| 字段 | 类型 | 来源 | 语义 |
|------|------|------|------|
| `effectTag` | RdTagDefSO | 继承 EffectSO | 伤害类型路由键（如 `Damage.Physical.Slash`） |
| `description` | string | 继承 EffectSO | 策划可读说明 |
| `duration` | float | 继承 EffectSO | ≤0=瞬时，>0=每 tick DOT |
| `baseValue` | float | 实体 | 伤害基底。刀=15，子弹=12 |

## 管道计算

```
ExecutionState.BuildDamageInfo:
  ① 从实体收集通道: weaponEntity.Preset.GetDamageEffects() → DamageEffectSO[]
  ② 从技能收集修正: ability.targetEffects.OfType<DamageModifierEffectSO>()
  ③ 配对: for each 通道, find 修正 where targetTag matches 通道.effectTag
     → outgoing = baseValue × (1 + ΣmodPercent) + ΣmodAdd
```

## 与 DamageModifierEffectSO 的边界

| | DamageEffectSO | DamageModifierEffectSO |
|---|---|---|
| 来源 | 实体（武器/身体） | 技能 |
| 核心字段 | `baseValue` | `targetTag` + `modAdd` + `modPercent` |
| 出现在 targetEffects | ❌ 不允许 | ✅ |
| 管道角色 | 伤害通道 | 伤害修正 |

## 资产

10 个实体通道资产（baseValue>0）：
- Physical: Slash(15), Blunt(12), Pierce(12), Bite(8), Crush(18), Explosive(25)
- Biological: Bleed(5 DOT), Suffocation(5 DOT)
- Special: Fall(15), True(10)

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被引用 | MeleeWeaponSO.GetDamageEffects | 从 PropertyTree Weapon/ATK 读取 |
| 被引用 | RangedWeaponSO.GetDamageEffects | 远程弹药容器链读取 |
| 消费 | ExecutionState.CollectEntityChannels | 收集后参与 tag 匹配 |
