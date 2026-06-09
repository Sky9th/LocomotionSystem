# Damage Source Model — 硬核生存游戏的伤害地基

> 2026-06-08 · 设计根隐喻 · 覆盖所有与伤害相关的系统决策

## 核心原则

**装备决定伤害地基，人物和环境做加减乘除。**

```
伤害 = 装备基底 × 人物修正 × 环境修正 × 命中修正
       ────────   ────────   ────────   ────────
       装备体系     属性/技能    天气/地形    部位/角度
       (固定值)    (±%)        (±%)        (±% × multiplier)
```

这不是一个"平衡性设计"，这是**物理模拟**——跟现实一一对应：

| 现实 | 游戏中的落脚 |
|------|-----------|
| 生锈的刀和精制钢刀切肉深度不同 | 装备定义 damage 基底 |
| 肌肉发达的人挥刀更有力 | 力量给近战加修正 |
| 子弹动能不随开枪的人变化 | 枪械不挂人物修正（感知影响命中，不影响伤害）|
| 砍中脖子 > 砍中手臂 | 命中部位乘数 |
| 凯夫拉挡手枪弹 | 护甲类型 × 子弹穿甲值 |
| 斜角砍人不如正劈 | 命中角度修正 |
| 雨天火药受潮 | 环境系统影响弹药可靠性 |

## 分层归属

```
┌──────────────────────────────────────────────────────────┐
│ 装备层 (Gear)                                             │
│                                                            │
│ 伤害地基。生锈 vs 精工的区别在这里。                        │
│                                                            │
│ MeleeWeapon    →  baseDamage, damageType, staggerValue    │
│ Ammo           →  baseDamage, armorPenetration,           │
│                   postPenMultiplier, staggerValue          │
│ Armor          →  armorValue, resistType[], durability    │
│ MedicalItem    →  healAmount, cleanseTags[]               │
└──────────────┬───────────────────────────────────────────┘
               │ 提供原始数值
               ▼
┌──────────────────────────────────────────────────────────┐
│ Ability 层                                                │
│                                                            │
│ 动作模式。怎么砍 / 怎么开枪 / 怎么包扎。                   │
│ 不定义伤害值。                                             │
│                                                            │
│ AbilityDefSO     → search, activation, cooldown, cost     │
│ targetEffects[]  → 空的！（伤害由装备在运行时注入）         │
│ selfEffects[]    → CostEffectSO（体力/弹药消耗）           │
└──────────────┬───────────────────────────────────────────┘
               │ 动作框架 + 装备注入
               ▼
┌──────────────────────────────────────────────────────────┐
│ 管道层 (Pipeline)                                         │
│                                                            │
│ ⑤ 接收装备基底 → ⑥ 目标侧结算 → ⑦⑧ 广播                 │
│                                                            │
│ ⑤ IEffectModifier   → 人物修正 (力/技/熟练度 for 近战)    │
│ ⑥ IResolutionModifier → 目标修正 (护甲/部位/角度/抗性)    │
└──────────────────────────────────────────────────────────┘
```

## 各伤害类型的基底归属

### 近战伤害

```
基底来自武器，Ability 是动作。

Ability.Blade.LightCut (动作: 小幅度挥砍)
  + MeleeWeapon.RustyKnife  (baseDamage=3,  Slash)  → 3  × 部位 × 护甲
  + MeleeWeapon.SteelBlade  (baseDamage=8,  Slash)  → 8  × 部位 × 护甲
  + MeleeWeapon.FireAxe     (baseDamage=12, Slash)  → 12 × 部位 × 护甲
  + 空手 (无装备)                                     → 0 (或 BareHand 固定值)

Ability.Blade.HeavyChop (动作: 大挥砍, 更长 windup, 更高 stamina)
  + MeleeWeapon.SteelBlade  (baseDamage=8 × 1.5)   → 12 × 部位 × 护甲
  (Heavy 动作自带伤害倍率——这是"用同样的刀使更大的劲")
```

**关键约束**：人物力量加成只能在 ⑤ IEffectModifier 做**乘算**，不改变装备基底本身。一把锈刀在力量 10 的人手里不能让刀变锋利——它只是挥得更有力（+力修正），刀仍然钝（低 baseDamage，低 armorPenetration）。

### 枪械伤害

```
基底来自弹药，枪只是发射器。

Ability.Rifle.NormalFire (动作: 扣扳机)
  + Ammo.556_FMJ      (baseDamage=12, armorPen=5,  stagger=3)  → 12 × 部位 × 护甲
  + Ammo.556_JHP      (baseDamage=12, armorPen=0,  stagger=8)  → 12 × 1.5(创伤) × 护甲
  + Ammo.556_AP       (baseDamage=12, armorPen=15, stagger=2)  → 12 × 0.7(穿后) × 护甲

角色感知属性: 影响命中率、散布、瞄准速度，不影响伤害。
            → Perception 不注册 ⑤ IEffectModifier。
```

**关键约束**：子弹动能是物理常数。9mm 从 Glock 打出去跟从 MP5 打出去是一个动能（假设同样枪管长度）。区别在枪管长度、膛线、射速——这些是枪的属性，不影响弹头终端动能。

### 投掷物伤害

```
基底来自投掷物自身，不来自投掷者力量。

Ability.Throwable.Molotov (动作: 投掷)
  + Throwable.Molotov     (baseDamage=8, Fire, DoT3s, radius=3m)
  → 力量不影响火焰温度。投掷者力量影响投掷距离，不影响伤害。
```

### 治疗

```
基底来自医疗物品，不来自施救者医术。

Ability.Medical.QuickBandage (动作: 包扎)
  + MedicalItem.Bandage       (healAmount=20, cleanseTags=[Bleed])
  + MedicalItem.HerbalPoultice (healAmount=10, cleanseTags=[Infection])
  → 医术等级影响包扎速度和成功率，不影响绷带的物理止血效果。
```

## Effect 树的重新归属

```
EffectSO 资产根据"谁决定这个数值"分为三类：

1. 装备体系 Effect
   ├─ 近战武器基底 (Slash 3/5/8/12, Blunt 3/5/10, Pierce 8/15...)
   ├─ 弹药基底 (Pierce_556_FMJ/JHP/AP, Pierce_9mm_*, Pierce_12Gauge_*)
   ├─ 投掷物基底 (Fire_8, Acid_4, Poison_3...)
   └─ 医疗物品基底 (Heal_20, Heal_10...)
   → 由装备系统管理，Ability 不直接引用

2. Ability 内联 Effect
   ├─ CostEffectSO (Stamina_Small/Medium/Large, Ammo_*, ...)
   └─ 特殊动作自带的效果 (PushKick 的击退、CombatRoll 的无敌帧)
   → 由 Ability 直接持有

3. 环境 / 被动 Effect
   ├─ 环境伤害 (Cold_5, Radiation_2, Fall_15, Suffocation_5)
   ├─ 被动触发 (BearTrap 的 Pierce+Bleed+Impact)
   └─ 状态效果 (Disease, Poison tick, Bleed tick)
   → 由环境系统 / 被动技能 / DoT 系统管理
```

## 与 Ability Pipeline 的接口

AbilityExecutor 在 ⑤ Effects 阶段：
1. 遍历 Ability.targetEffects[]（通常为空，Cost 走 selfEffects）
2. 从装备系统获取 DamageSource
3. 用装备基底构造 SResolvedHit.IncomingDamage
4. 经 IEffectModifier 链（近战有力量修正，枪械无）
5. 发送到目标 HitReactionComponent

```
AbilityExecutor.TryActivate():
  ...
  ⑤ Effects:
    weapon = Equipment.GetWeapon()
    if (weapon != null)
        effect = weapon.GetDamageEffect()
        hit.IncomingDamage = effect.baseDamage   // 装备地基
    else
        hit.IncomingDamage = 0                   // 空手

    // IEffectModifier 链: 只有近战注册了回调
    EffectCallback?.Invoke(ctx, hit, target)
    // → 力量加成、武器熟练度等只对近战生效

    target.HitReactionComponent.Resolve(hit)
  ...
```

## 设计决策记录

| 决策 | 原因 |
|------|------|
| 装备定义伤害基底，Ability 不持有 Damage Effect | 硬核生存——现实映射。生锈的刀和精制钢刀是不同的物理物体。技能是"怎么用"，不是"刀有多利" |
| 近战有人物修正，枪械没有 | 人挥刀的力量影响切割深度。子弹动能不随人变。现实物理。 |
| 医疗物品决定治疗量，医术不改变 | 绷带的吸收量是物理属性。医术影响包扎速度和感染概率，不影响吸血量。 |
| 投掷物伤害固定 | 火焰温度不随投掷者改变。投掷力量影响距离和精度，不影响伤害。 |
| 弹药本身是 Damage Effect，枪是发射器 | FMJ/JHP/AP 是三种不同的物理物体——对应三个不同的 EffectSO。换弹 = 换装备，不是换技能。 |
