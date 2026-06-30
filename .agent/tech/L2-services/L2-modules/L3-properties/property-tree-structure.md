# PropertyTree 继承结构设计

> 日期: 2026-06-30
> 状态: 设计讨论中 — WeaponBase 重构 + Equipment 层引入
> 关联: `property-inventory.md` · `damage-source-model.md`

## 一、设计原则

1. **继承表达 "is-a"** — 子节点是其父节点的子类型，具有父节点的全部属性
2. **叶子层才存值** — 中间层定义结构，叶子树（Pistol、Blade 等）才产生实际 Preset
3. **不建空中间层** — 如果一层没有独有属性，要么合并到上层，要么确认未来会扩展才保留
4. **槽位是通用能力** — 任何实体都可能有槽位（Entity.Slots），容器物品不是特殊种族

---

## 二、完整继承树

```
Entity                                    [DisplayName, Icon, Description, Weight, Tags, Slots/]
│                                         根节点：所有可交互实体的最小公约数
│
├── Equipment : Entity                    [Durability, Presentation/, Behavior/]
│   │                                    所有需要装备/持握/穿戴的物品
│   │
│   ├── WeaponBase : Equipment            [ATK, AttackSpeed, AttackRange, NoiseRadius, IsTwoHanded]
│   │   │                                 所有能造成伤害的装备
│   │   │
│   │   ├── MeleeWeapon : WeaponBase      分类层：伤害=武器自身。当前无独有属性
│   │   │   ├── Blade                     [+BleedChance]
│   │   │   ├── Blunt                     [—]  钝器分类
│   │   │   ├── Axe                       [+ArmorPierce]
│   │   │   └── Polearm                   [—]  长柄分类
│   │   │
│   │   ├── RangedWeapon : WeaponBase     [Accuracy, Recoil, ReloadSpeed, MagSize,
│   │   │   │                              AmmoCount, CompatibleAmmo]
│   │   │   │                             所有需要弹药的远程武器
│   │   │   │
│   │   │   ├── Firearm : RangedWeapon    [FireRate, MuzzleVelocity, BarrelLength,
│   │   │   │   │                          Reliability, IsAutomatic, GearType,
│   │   │   │   │                          +Slots/Scope, +Slots/Magazine, +Slots/Muzzle]
│   │   │   │   │                         火器机制：枪管+自动机+配件槽
│   │   │   │   │
│   │   │   │   ├── Pistol : Firearm      [+HolsterSpeed, +HipFirePenalty]
│   │   │   │   ├── Rifle : Firearm       [+ScopeZoom, +AimTime]
│   │   │   │   └── Shotgun : Firearm     [+PelletCount, +Spread]
│   │   │   │
│   │   │   └── Bow : RangedWeapon        [+DrawSpeed, +ArrowVelocity, +HoldStamina]
│   │   │                                 人力蓄能机制，与 Firearm 根本不同
│   │   │
│   │   └── Throwable : WeaponBase        [+BlastRadius, +FuseTime]
│   │                                     一次性消耗，投掷出手即销毁
│   │
│   ├── ArmorBase : Equipment             [DEF, Coverage, TraumaTransfer, ResistTypes,
│   │   │                                  MoveSpeedPenalty, StaminaRegenPenalty]
│   │   │
│   │   ├── HeadArmor : ArmorBase         [+FlashResist, +NightVision]
│   │   ├── BodyArmor : ArmorBase         [+KnockdownResist, +CarryWeightBonus]
│   │   └── LegArmor : ArmorBase          [+MoveSpeedMod, +SneakSpeed]
│   │
│   └── ToolBase : Equipment              [Efficiency, MaterialTier, StaminaCostPerUse,
│                                           ToolType, Fuel/Charge]
│                                         斧/镐/锤/锯/厨具共用
│
│   (背包是 Equipment 叶子——有耐久、有表现层。
│    弹药箱不装备，是独立的世界物品——直接继承 Entity，走 Slots/ 表达容器能力)
│
├── ConsumableBase : Entity               [ConsumeTime, StackSize, ConsumableType]
│   │                                     一次性使用，无耐久
│   ├── Food : ConsumableBase             [+Nutrition, +Hydration, +MoraleBonus, +ShelfLife]
│   ├── Medical : ConsumableBase          [+HealAmount, +BleedReduction, +InfectionCleanse, +PainRelief]
│   └── Material : ConsumableBase         [+ScarcityTier, +MaterialType_Tag]
│
├── AmmoBase : Entity                     [BaseDamage, Penetration, BulletWeight,
│   │                                      OverPenetration, RecoilFactor, AmmoReliability,
│   │                                      FoulingRate, DamageType, CompatiblePlatform]
│   │                                     弹药属性决定弹道终端物理特性
│   ├── PistolAmmo : AmmoBase
│   ├── RifleAmmo : AmmoBase
│   └── ShotgunShell : AmmoBase
│
├── Actor : Entity                        [HP, MaxHP]
│   │                                     所有可交互角色的最小公约数
│   ├── Human : Actor                     [Stamina, MaxStamina, Hunger, Thirst, BodyTemp,
│   │   │                                  Blood, Infection, Consciousness, Pain,
│   │   │                                  Strength, Agility, Endurance, Intelligence,
│   │   │                                  Perception, Charisma, 21 Proficiency,
│   │   │                                  Morale, Sleepiness, Comfort, SocialNeed, Boredom,
│   │   │                                  CarryWeight, 16 Resistance, SightRange,
│   │   │                                  NightVision_Human, FlashResist_Human, Loyalty,
│   │   │                                  +Slots/RightHand, +Slots/LeftHand, +Slots/Head,
│   │   │                                  +Slots/Chest, +Slots/LeftLeg, +Slots/RightLeg,
│   │   │                                  +Slots/LeftFoot, +Slots/RightFoot, +Slots/Back]
│   │   │
│   │   └── Zombie : Actor                [+ATK_Zombie, +ZombieSpeed, +NoiseReact, +SightRange]
│   │
│   (Creature, Mutant, Robot — 远期预留)
│
├── Building : Entity                     [DEF_Building, MaterialType_Building, Flammability,
│   │                                      SoundDampening, WeatherResist, WorkSpeed_Building,
│   │                                      RestComfort, Durability]
│   │                                     建筑可被破坏/维修，需要独立 HP
│   │
│   └── (Wall, Floor, Door, Workstation — 远期叶子)
│
└── Environment : Entity                  [FogDensity, Temperature_Env, Humidity, TimeOfDay]
                                          全局单例，WeatherService 写入
```

---

## 三、Equipment 子树 → 详见 `property-tree-equipment.md`
