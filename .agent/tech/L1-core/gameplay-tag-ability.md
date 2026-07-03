# GameplayTag — Ability 模块产出 Tag 域

> `L1_Core/RdTag/` · 2026-06-29 · 动画包驱动重构
>
> 流派 = 动画包。abilityTag 路径编码：`{Melee|Ranged}.{流派}.{武器}.{技能}`

## 结构树

```
Ability
├── Definition
│   ├── Active
│   │   ├── Melee
│   │   │   ├── Unarmed
│   │   │   │   ├── Punch
│   │   │   │   ├── Kick
│   │   │   │   └── Grab
│   │   │   ├── OneHanded
│   │   │   │   ├── Blade / LightCut, HeavyChop, Block
│   │   │   │   ├── Axe / LightHack, HeavyCleave, Block
│   │   │   │   ├── Club / LightSwing, HeavySmash, Block
│   │   │   │   └── Dagger / QuickStab, Slash, Backstab
│   │   │   ├── TwoHanded
│   │   │   │   ├── Greatsword / LightSwing, HeavySmash, Block
│   │   │   │   ├── Axe2H / LightCleave, HeavyCleave, Block
│   │   │   │   ├── Club2H / LightSwing, HeavySmash, Block
│   │   │   │   └── Polearm / Thrust, Sweep, Block
│   │   │   ├── DualWield
│   │   │   │   ├── Blade / CrossSlash, SpinningCut
│   │   │   │   └── Axe / DoubleCleave, Whirlwind
│   │   │   ├── Fencing
│   │   │   │   ├── Rapier / Lunge, Riposte, Feint
│   │   │   │   └── Dagger / QuickStab, Parry
│   │   │   └── Shield
│   │   │       ├── Blade / ShieldBash, GuardStance, Counter
│   │   │       └── Axe / ShieldCrush, HeavyGuard
│   │   │
│   │   ├── Ranged
│   │   │   ├── Pistol1H / NormalFire           ← 预留，缺动画
│   │   │   ├── Pistol2H / NormalFire, AimedShot, QuickReload
│   │   │   ├── DualPistol / AkimboFire, SuppressiveFire, CrossFire
│   │   │   ├── Rifle / NormalFire, BracedFire, FollowUpShot, MeleeStrike
│   │   │   ├── Shotgun / NormalFire, CloseQuarters, CombatLoad, MeleeStrike
│   │   │   ├── Bow / NormalFire, ChargedShot, MultiArrow
│   │   │   ├── Launcher / NormalFire, AimedFire
│   │   │   └── Heavy / SpinUp, SuppressiveFire, Overheat
│   │   │
│   │   ├── Trap / BearTrap, TripMine, ...
│   │   ├── Medical / QuickBandage, Tourniquet, ...
│   │   ├── Survival / MakeFire, PurifyWater, ...
│   │   ├── Craft / FieldRepair, WeaponSharpening, ...
│   │   ├── Trade / Haggle, Appraise, ...
│   │   ├── Lockpicking / Lockpick, ForceEntry, ...
│   │   └── Universal / CombatRoll, EmergencyReload, PushKick
│   │
│   └── Passive
│       ├── StatModify
│       ├── TriggerAttack
│       └── GrantBuff
│
├── Tree / Innate, Talent, Routine, Learned, Boss, Mutation, Faction
├── Execute / Threshold
├── Damage / Physical, Elemental, Biological, True, Fall
│   └── Physical / Slash, Blunt, Pierce, Bite, Crush, Explosive
├── Effect / Buff, Debuff, DoT, Status, Condition, Immunity, Functional
├── Impact / Light, Medium, Heavy, Launch, Pull
└── Cost / Stamina, HP, Ammo, Durability, Fuel, Charge

## 装备过滤

武器实体持有 Type + Grip 两个标签。AbilityTreeSO 双重过滤：

```
compatibleWeaponTags = [Entity.Weapon.Melee.Blade]
compatibleGripTags   = [Grip.Melee.OneHanded]
```

abilityTag 层级编码流派+武器：
```
Definition.Active.Melee.OneHanded.Blade.LightCut
                        ↑        ↑       ↑
                      流派    武器类型   技能名
```

## 字段映射

| SO 字段 | 引用节点 | 匹配方式 |
|---------|---------|---------|
| `AbilitySO.abilityTag` | `Definition.*` (叶标签) | Parent 前缀（互斥） |
| `AbilityTreeSO.treeTags` | `Tree.*` | HasTag 前缀 |
| `AbilityTreeSO.compatibleWeaponTags` | `Entity.Weapon.*` | HasTag 前缀 |
| `AbilityTreeSO.compatibleGripTags` | `Grip.*` | HasTag 前缀 |
| `DamageEffectSO.effectTag` | `Damage.*` | HasTag 前缀 |
| `BuffEffectSO.effectTag` | `Effect.*` | 写入 OwnedTags |
| `BuffEffectSO.grantedTags` | `Effect.*` | 写入 OwnedTags |
| `EffectSO.applicationBlockedTags` | `Effect.*` | HasTag 前缀 |
| `ImpactEffectSO.effectTag` | `Impact.*` | HasTag 前缀 |
| `ExecuteEffectSO.effectTag` | `Execute.*` | 精确匹配 |
| `CostEffectSO.effectTag` | `Cost.*` | HasTag 前缀 |
