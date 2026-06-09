# Effect Inventory — 全量 Effect 树

> 基于全量 Ability 树 + 策划文档 + Tag 层级推断。每个 Effect 对应一个 `.asset`（不同数值 = 不同资产）。

## Effect 类型

| 类型 | EffectSO 子类 | 用途 | 运行时消费 |
|------|-------------|------|-----------|
| **Damage** | `DamageEffectSO` | 瞬时/持续伤害，路由到防御公式 | ⑤→⑥ 管道 |
| **Impact** | `ImpactEffectSO` | 硬直 + 击退/拉近 | ⑥ 直接校验 |
| **Execute** | `ExecuteEffectSO` | HP 低于阈值即死 | ⑥ 直接校验 |
| **Cost** | `CostEffectSO` | 资源消耗/恢复（正=扣，负=回）| ②→③ 间扣除 |

> Buff/Debuff 效果（Slow, Stun, Regeneration 等）由 BuffEffectSO 承载，Phase 5+ 落地，本次不列。

## Tag 层级约定

Effect 树用 `effectTag` 属性路由。Tag 层级与 Damage/Impact 防御公式的匹配方式：

```
Damage.Physical.Slash   → HasTag("Damage.Physical") 匹配护甲公式
Damage.Elemental.Fire   → HasTag("Damage.Elemental") 匹配元素抗性公式
Impact.Launch           → 不走 Avoidance→Mitigation，直接校验
```

**每个 Effect 的 `effectTag` 必须是叶标签**（无子节点）。

---

## 1. Damage Effects（42 个）

### 1.1 Physical（7 个）

对应 Ability 树：`Melee.*`、`Ranged.*`、`Trap.*`、`Defensive.*`
路由：`Damage.Physical.*` → 护甲公式

| # | 资产名 | effectTag | baseDamage | 特殊 | 关联 Ability |
|---|--------|-----------|:----------:|------|-------------|
| 1 | `Damage_Physical_Slash` | `Damage.Physical.Slash` | 5 | — | Melee.Blade.LightCut, Melee.Axe.LightHack |
| 2 | `Damage_Physical_Slash_Heavy` | `Damage.Physical.Slash` | 15 | — | Melee.Blade.HeavyChop, Melee.Axe.HeavyCleave |
| 3 | `Damage_Physical_Blunt` | `Damage.Physical.Blunt` | 3 | — | Melee.Staff.LightSwing, Universal.PushKick |
| 4 | `Damage_Physical_Blunt_Heavy` | `Damage.Physical.Blunt` | 12 | — | Melee.Staff.HeavySmash, Defensive.ShieldBash |
| 5 | `Damage_Physical_Pierce` | `Damage.Physical.Pierce` | 8 | — | Trap.BearTrap, Ranged.Rifle.NormalFire |
| 6 | `Damage_Physical_Pierce_Heavy` | `Damage.Physical.Pierce` | 20 | — | Trap.PitfallSpike, Ranged.Rifle.LongRangeSniping |
| 7 | `Damage_Physical_Bite` | `Damage.Physical.Bite` | 5 | — | 僵尸攻击（Species.Creature）|

### 1.2 Elemental（12 个）

对应 Ability 树：`Throwable.*`、`Craft.*`、`Trap.*`
路由：`Damage.Elemental.*` → 对应抗性

| # | 资产名 | effectTag | baseDamage | dur(s) | 特殊 | 关联 Ability |
|---|--------|-----------|:----------:|:------:|------|-------------|
| 8 | `Damage_Fire` | `Damage.Elemental.Fire` | 8 | 3 | DoT | Throwable.Molotov |
| 9 | `Damage_Fire_Strong` | `Damage.Elemental.Fire` | 20 | 5 | DoT | Craft.ImprovisedExplosive |
| 10 | `Damage_Cold` | `Damage.Elemental.Cold` | 5 | 2 | — | 环境（Freezing）|
| 11 | `Damage_Cold_Strong` | `Damage.Elemental.Cold` | 12 | 4 | — | 特殊武器 |
| 12 | `Damage_Shock` | `Damage.Elemental.Shock` | 10 | 0 | — | Trap.ShockTrap |
| 13 | `Damage_Shock_Strong` | `Damage.Elemental.Shock` | 25 | 0 | — | 环境（高压电）|
| 14 | `Damage_Acid` | `Damage.Elemental.Acid` | 4 | 4 | DoT | Throwable.AcidVial |
| 15 | `Damage_Acid_Strong` | `Damage.Elemental.Acid` | 10 | 6 | DoT | 高级酸液 |
| 16 | `Damage_Poison` | `Damage.Elemental.Poison` | 3 | 10 | DoT, stackable | Throwable.PoisonGas |
| 17 | `Damage_Poison_Strong` | `Damage.Elemental.Poison` | 8 | 15 | DoT, stackable | 特殊毒气 |
| 18 | `Damage_Radiation` | `Damage.Elemental.Radiation` | 2 | 30 | DoT | 环境（辐射区）|
| 19 | `Damage_Radiation_Strong` | `Damage.Elemental.Radiation` | 5 | 60 | DoT | 核废料区 |

### 1.3 Biological（5 个）

路由：`Damage.Biological.*`

| # | 资产名 | effectTag | baseDamage | dur(s) | 特殊 | 关联 |
|---|--------|-----------|:----------:|:------:|------|------|
| 20 | `Damage_Bleed` | `Damage.Biological.Bleed` | 1 | 5 | DoT, stackable | Melee.Blade.*, Trap.RazorWire |
| 21 | `Damage_Bleed_Strong` | `Damage.Biological.Bleed` | 3 | 10 | DoT, stackable | 重伤出血 |
| 22 | `Damage_Disease` | `Damage.Biological.Disease` | 1 | 30 | DoT, infection | 僵尸咬伤感染 |
| 23 | `Damage_Disease_Virulent` | `Damage.Biological.Disease` | 3 | 60 | DoT, infection | 高级感染 |
| 24 | `Damage_Suffocation` | `Damage.Biological.Suffocation` | 5 | 3 | DoT | Throwable.SmokeGrenade, 毒气室 |

### 1.4 Special（3 个）

| # | 资产名 | effectTag | baseDamage | 特殊 | 关联 |
|---|--------|-----------|:----------:|------|------|
| 25 | `Damage_True` | `Damage.True` | 10 | 无视全部防御 | 斩杀类技能、环境即死 |
| 26 | `Damage_True_Strong` | `Damage.True` | 50 | 无视全部防御 | Boss 技能 |
| 27 | `Damage_Fall` | `Damage.Fall` | 15 | 按高度缩放 | 坠落、被击飞落地 |

### 1.5 武器基础伤害（15 个）

每种武器/弹药的「基础伤害」Effect，由具体 Ability 的 IEffectModifier 加成。

| # | 资产名 | effectTag | baseDamage | 对应 Ability 分支 |
|---|--------|-----------|:----------:|------------------|
| 28 | `Damage_Weapon_BladeLight` | `Damage.Physical.Slash` | 5 | Melee.Blade.LightCut |
| 29 | `Damage_Weapon_BladeHeavy` | `Damage.Physical.Slash` | 15 | Melee.Blade.HeavyChop |
| 30 | `Damage_Weapon_AxeLight` | `Damage.Physical.Slash` | 7 | Melee.Axe.LightHack |
| 31 | `Damage_Weapon_AxeHeavy` | `Damage.Physical.Slash` | 18 | Melee.Axe.HeavyCleave |
| 32 | `Damage_Weapon_StaffLight` | `Damage.Physical.Blunt` | 3 | Melee.Staff.LightSwing |
| 33 | `Damage_Weapon_StaffHeavy` | `Damage.Physical.Blunt` | 12 | Melee.Staff.HeavySmash |
| 34 | `Damage_Weapon_Pistol` | `Damage.Physical.Pierce` | 12 | Ranged.Pistol.NormalFire |
| 35 | `Damage_Weapon_Pistol_Akimbo` | `Damage.Physical.Pierce` | 10 | Ranged.Pistol.Akimbo（双持单发低，总量高）|
| 36 | `Damage_Weapon_Rifle` | `Damage.Physical.Pierce` | 25 | Ranged.Rifle.NormalFire |
| 37 | `Damage_Weapon_Rifle_Snipe` | `Damage.Physical.Pierce` | 50 | Ranged.Rifle.LongRangeSniping |
| 38 | `Damage_Weapon_Shotgun` | `Damage.Physical.Pierce` | 8 | Ranged.Shotgun.NormalFire（单颗弹丸）|
| 39 | `Damage_Weapon_Shotgun_CQB` | `Damage.Physical.Pierce` | 15 | Ranged.Shotgun.CloseQuarters |
| 40 | `Damage_Weapon_ThrowingKnife` | `Damage.Physical.Pierce` | 10 | Throwable.ThrowingKnife |
| 41 | `Damage_Weapon_FragGrenade` | `Damage.Physical.Pierce` | 40 | Throwable.FragGrenade（AOE）|
| 42 | `Damage_Weapon_BearTrap` | `Damage.Physical.Pierce` | 8 | Trap.BearTrap |

---

## 2. Impact Effects（5 个）

不走 Avoidance→Mitigation→Absorption，由 HitReactionComponent 直接校验 `staggerValue` vs 目标霸体阈值。

| # | 资产名 | effectTag | stagger | knockback | dir | 关联 Ability |
|---|--------|-----------|:-------:|:---------:|-----|-------------|
| 43 | `Impact_Light` | `Impact.Light` | 5 | 0 | — | 所有轻攻击（Blade.LightCut, Axe.LightHack, Staff.LightSwing）|
| 44 | `Impact_Medium` | `Impact.Medium` | 15 | 1 | HitDirection | 重攻击（Blade.HeavyChop, Axe.HeavyCleave）|
| 45 | `Impact_Heavy` | `Impact.Heavy` | 30 | 3 | HitDirection | Staff.HeavySmash, Defensive.ShieldBash |
| 46 | `Impact_Launch` | `Impact.Launch` | 50 | 10 | HitDirection | 爆炸（FragGrenade, ImprovisedExplosive）|
| 47 | `Impact_Pull` | `Impact.Pull` | 10 | 5 | TowardCaster | 抓取技能（Scorpion-style, 陷阱拖拽）|

> `knockbackForce` 为非负标量，方向由 `knockbackDir` 枚举控制（`TowardCaster` = 拉向施法者）。

---

## 3. Execute Effects（3 个）

| # | 资产名 | effectTag | hpThreshold | 关联 Ability |
|---|--------|-----------|:-----------:|-------------|
| 48 | `Execute_LowHP_10` | `Execute.Threshold` | 0.10 | 基础处决技能（Melee 通用）|
| 49 | `Execute_LowHP_20` | `Execute.Threshold` | 0.20 | 进阶处决（Stealth.Assassination）|
| 50 | `Execute_LowHP_30` | `Execute.Threshold` | 0.30 | 高级处决（Boss 弱点阶段）|

---

## 4. Cost Effects（14 个）

正数 = 消耗，负数 = 恢复。用于 `selfEffects[]`，②→③ 间扣除。

### 4.1 Stamina 消耗（3 个）

关联：所有 `Melee.*`、`Defensive.*`、`Universal.CombatRoll`

| # | 资产名 | statDef.Id | amount | 关联 Ability |
|---|--------|-----------|:------:|-------------|
| 51 | `Cost_Stamina_Small` | `Stamina` | 15 | 轻攻击、CombatRoll、PushKick |
| 52 | `Cost_Stamina_Medium` | `Stamina` | 30 | 重攻击、ShieldBash、GuardStance |
| 53 | `Cost_Stamina_Large` | `Stamina` | 50 | 特殊技、HeavySmash、大翻滚 |

### 4.2 HP 消耗/恢复（3 个）

| # | 资产名 | statDef.Id | amount | 关联 Ability |
|---|--------|-----------|:------:|-------------|
| 54 | `Cost_HP_Small` | `HP` | 10 | 自残类技能（血魔法）|
| 55 | `Cost_HP_Medium` | `HP` | 25 | 紧急技能代价 |
| 56 | `Cost_HP_Restore` | `HP` | -20 | Medical.QuickBandage（恢复）|

### 4.3 弹药消耗（4 个）

关联：`Ranged.*`、`Throwable.*`

| # | 资产名 | statDef.Id | amount | 关联 Ability |
|---|--------|-----------|:------:|-------------|
| 57 | `Cost_Ammo_Pistol` | `Ammo` | 1 | Ranged.Pistol.* |
| 58 | `Cost_Ammo_Rifle` | `Ammo` | 1 | Ranged.Rifle.* |
| 59 | `Cost_Ammo_Shotgun` | `Ammo` | 1 | Ranged.Shotgun.* |
| 60 | `Cost_Ammo_Throwable` | `Ammo` | 1 | Throwable.*（每投掷物消耗 1 弹药）|

### 4.4 其他消耗（4 个）

| # | 资产名 | statDef.Id | amount | 关联 Ability |
|---|--------|-----------|:------:|-------------|
| 61 | `Cost_Durability_Small` | `Durability` | 1 | 武器使用（Melee.* 每次命中）|
| 62 | `Cost_Durability_Block` | `Durability` | 3 | 格挡（Melee.*.Block, Defensive.Parry）|
| 63 | `Cost_Fuel_Small` | `Fuel` | 1 | Craft.ImprovisedExplosive, Throwable.Molotov |
| 64 | `Cost_Charge_Small` | `Charge` | 5 | Trap.ShockTrap, 能量武器 |

---

## 5. Known Gaps（Phase 5+ 落地）

以下 Effect 类型当前 C# 类尚未实现，对应的 Ability 暂时无法获得效果。交叉验证审计发现的 17 个缺口 Ability 全部集中于此。

### 5.1 HealEffectSO（治疗）

| 缺口 Ability | 说明 |
|-------------|------|
| Medical.QuickBandage | 目标恢复 HP |
| Medical.Tourniquet | 止血 + 移除 Bleed |
| Medical.Splint | 修复骨折（移除 Cripple）|
| Medical.CPR | 复活倒地目标 |
| Medical.Detoxification | 移除 Poison/Disease |
| Medical.DisinfectWound | 移除 Infection |
| Medical.CombatMedic | 复合治疗 + 临时 Buff |

> 当前权宜方案：CostEffectSO(HP, -20) 放在 selfEffects 中。语义不正确——"治疗"不是"负消耗"。Phase 5+ 需落地 `HealEffectSO(healAmount, canOverheal)`。

### 5.2 CleanseEffectSO（净化/移除状态）

| 缺口 Ability | 说明 |
|-------------|------|
| Medical.Detoxification | 移除 Poison、Disease |
| Medical.DisinfectWound | 移除 Infection |
| Medical.Tourniquet | 移除 Bleed |
| Medical.Splint | 移除 Cripple |

> 机制：`CleanseEffectSO` 携带一个 `GameplayTag[]` 列表，从目标 `OwnedTags` 中移除匹配的 Buff/Debuff 标签。

### 5.3 BuffEffectSO / StatusEffectSO（增益/减益/状态）

| 缺口 Ability | 说明 |
|-------------|------|
| Defensive.GuardianAura | 周围友方 +防御 Buff |
| Defensive.ShieldWall | 队形防御 Buff |
| Craft.WeaponSharpening | 武器临时 +伤害 Buff |
| Throwable.Flashbang | 致盲 Debuff（非伤害）|
| Throwable.SmokeGrenade | 遮蔽视野（环境效果）|
| Trap.BearTrap | 定身 Root |
| Trap.OilSlick | 减速 Slow + 滑倒 |
| Stealth.StealthMode | 隐身状态 |
| Stealth.ImprovisedCamouflage | 伪装/潜行加成 |

> BuffEffectSO 在架构设计中已预留（Phase 5+），通过 `Effect.Buff.*` 和 `Effect.Debuff.*` Tag 路由。StatusEffectSO 用于二元状态（隐身/定身/致盲），由 `Effect.Status.*` Tag 路由。

### 5.4 ItemConsumeEffectSO（物品消耗）

| 缺口 | 说明 |
|------|------|
| Throwable 类技能（Molotov, FragGrenade 等 8 个）| 当前用 `CostEffectSO(Ammo_Throwable)` 权宜 |
| Craft 类技能（AmmoReloading, ImprovisedExplosive, CraftTrap 等）| 产出/消耗物品栏物品 |

> 投掷物消耗的是物品栏物理物品，不是 Stat.Pool.Ammo。Phase 2+ 需 `ItemConsumeEffectSO(itemId, count)`。当前方案可工作，但需在 Phase 2+ 迁移。

---

## 总计

| 类别 | 数量 | effectTag 根 |
|------|:---:|-------------|
| Damage — Physical | 7 | `Damage.Physical.*` |
| Damage — Elemental | 12 | `Damage.Elemental.*` |
| Damage — Biological | 5 | `Damage.Biological.*` |
| Damage — Special | 3 | `Damage.True`, `Damage.Fall` |
| Damage — Weapon | 15 | `Damage.Physical.*`（按武器细分）|
| Impact | 5 | `Impact.*` |
| Execute | 3 | `Execute.Threshold` |
| Cost | 14 | —（无 effectTag，走 selfEffects）|
| **合计** | **64** | |

---

## 目录结构

```
Assets/Data/Ability/Effects/
├── Damage/
│   ├── Physical/
│   │   ├── Damage_Physical_Slash.asset
│   │   ├── Damage_Physical_Slash_Heavy.asset
│   │   ├── Damage_Physical_Blunt.asset
│   │   ├── Damage_Physical_Blunt_Heavy.asset
│   │   ├── Damage_Physical_Pierce.asset
│   │   ├── Damage_Physical_Pierce_Heavy.asset
│   │   └── Damage_Physical_Bite.asset
│   ├── Elemental/
│   │   ├── Fire/
│   │   │   ├── Damage_Fire.asset
│   │   │   └── Damage_Fire_Strong.asset
│   │   ├── Cold/
│   │   │   ├── Damage_Cold.asset
│   │   │   └── Damage_Cold_Strong.asset
│   │   ├── Shock/
│   │   │   ├── Damage_Shock.asset
│   │   │   └── Damage_Shock_Strong.asset
│   │   ├── Acid/
│   │   │   ├── Damage_Acid.asset
│   │   │   └── Damage_Acid_Strong.asset
│   │   ├── Poison/
│   │   │   ├── Damage_Poison.asset
│   │   │   └── Damage_Poison_Strong.asset
│   │   └── Radiation/
│   │       ├── Damage_Radiation.asset
│   │       └── Damage_Radiation_Strong.asset
│   ├── Biological/
│   │   ├── Bleed/
│   │   │   ├── Damage_Bleed.asset
│   │   │   └── Damage_Bleed_Strong.asset
│   │   ├── Disease/
│   │   │   ├── Damage_Disease.asset
│   │   │   └── Damage_Disease_Virulent.asset
│   │   └── Suffocation/
│   │       └── Damage_Suffocation.asset
│   ├── Special/
│   │   ├── Damage_True.asset
│   │   ├── Damage_True_Strong.asset
│   │   └── Damage_Fall.asset
│   └── Weapon/
│       ├── Blade/Damage_Weapon_BladeLight.asset, ...Heavy.asset
│       ├── Axe/Damage_Weapon_AxeLight.asset, ...Heavy.asset
│       ├── Staff/Damage_Weapon_StaffLight.asset, ...Heavy.asset
│       ├── Pistol/Damage_Weapon_Pistol.asset, ...Akimbo.asset
│       ├── Rifle/Damage_Weapon_Rifle.asset, ...Snipe.asset
│       ├── Shotgun/Damage_Weapon_Shotgun.asset, ...CQB.asset
│       ├── ThrowingKnife/Damage_Weapon_ThrowingKnife.asset
│       ├── FragGrenade/Damage_Weapon_FragGrenade.asset
│       └── BearTrap/Damage_Weapon_BearTrap.asset
├── Impact/
│   ├── Impact_Light.asset
│   ├── Impact_Medium.asset
│   ├── Impact_Heavy.asset
│   ├── Impact_Launch.asset
│   └── Impact_Pull.asset
├── Execute/
│   ├── Execute_LowHP_10.asset
│   ├── Execute_LowHP_20.asset
│   └── Execute_LowHP_30.asset
└── Cost/
    ├── Stamina/Cost_Stamina_Small.asset, ...Medium.asset, ...Large.asset
    ├── HP/Cost_HP_Small.asset, ...Medium.asset, Cost_HP_Restore.asset
    ├── Ammo/Cost_Ammo_Pistol.asset, ...Rifle.asset, ...Shotgun.asset, ...Throwable.asset
    ├── Durability/Cost_Durability_Small.asset, Cost_Durability_Block.asset
    ├── Fuel/Cost_Fuel_Small.asset
    └── Charge/Cost_Charge_Small.asset
```

## 依赖的 Tag 资产

以下 Tag 需要存在。标记「需创建」的为当前项目缺失，需在导入 Effect 前补齐。

```
Tag 层级                        状态        用途
─────────────────────────────────────────────────────────
Damage.Physical                 (文件夹)    HasTag 前缀 → 护甲公式
Damage.Physical.Slash           需创建      叶标签 → Slash 类 Effect
Damage.Physical.Blunt           需创建      叶标签 → Blunt 类 Effect
Damage.Physical.Pierce          需创建      叶标签 → Pierce 类 Effect
Damage.Physical.Bite            需创建      叶标签 → Bite 类 Effect
Damage.Elemental                (文件夹)    HasTag 前缀 → 元素抗性公式
Damage.Elemental.Fire           需创建      叶标签 → Fire 类 Effect
Damage.Elemental.Cold           需创建      叶标签 → Cold 类 Effect
Damage.Elemental.Shock          需创建      叶标签 → Shock 类 Effect
Damage.Elemental.Acid           需创建      叶标签 → Acid 类 Effect
Damage.Elemental.Poison         需创建      叶标签 → Poison 类 Effect
Damage.Elemental.Radiation      需创建      叶标签 → Radiation 类 Effect
Damage.Biological               (文件夹)    HasTag 前缀 → 生物抗性公式
Damage.Biological.Bleed         需创建      叶标签 → Bleed 类 Effect
Damage.Biological.Disease       需创建      叶标签 → Disease 类 Effect
Damage.Biological.Suffocation   需创建      叶标签 → Suffocation 类 Effect
Damage.True                     需创建      叶标签 → True 类 Effect
Damage.Fall                     需创建      叶标签 → Fall 类 Effect
Impact                          (文件夹)    Impact 公式路由
Impact.Light                    需创建      叶标签 → 轻硬直
Impact.Medium                   需创建      叶标签 → 中硬直
Impact.Heavy                    需创建      叶标签 → 击倒
Impact.Launch                   需创建      叶标签 → 击飞
Impact.Pull                     需创建      叶标签 → 拉近
Execute.Threshold               需创建      叶标签 → 斩杀阈值
```

> 全部 22 个叶标签均需创建。文件夹标签 (`Damage.Physical`, `Damage.Elemental`, `Damage.Biological`, `Impact`) 用于 `HasTag` 前缀匹配，本身不被 Effect 直接引用。
