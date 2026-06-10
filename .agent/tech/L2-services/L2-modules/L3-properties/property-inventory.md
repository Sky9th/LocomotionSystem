# Property Inventory — 全量属性与属性树设计

> 日期: 2026-06-10
> 状态: 设计完成，待落地
> 关联: `game-overview.md` · `stats-inventory.md` · `equipment-system.md` · `damage-source-model.md` · `injury-system.md` · `noise-system.md`
> 编辑器: `L3-properties/README.md`

## 一、设计原则

### 1.1 什么东西进 PropertyTree？

| 进 Tree | 不进 Tree |
|---------|----------|
| 实体模板的**固有属性**（Bounded / Consumable / Cumulative） | **Derived 属性**——由 C# 运行时公式实时计算 |
| 决定"这类东西有哪些字段"的结构定义 | 随装备/状态动态变化的临时值 |
| 可在编辑器中配置的数值基线 | 由多个属性派生、不独立存在的合成值 |

**例**: `ATK`（武器面板攻击力）进 Tree——它是武器的固有属性；`HitRate`（最终命中率）不进 Tree——它由 `Perception + Proficiency + Weapon.Accuracy` 派生。

### 1.2 属性类型

| PropertyType | 用途 | 示例 |
|-------------|------|------|
| `Float` | 连续数值（大多数属性） | HP, Stamina, ATK, Weight |
| `Int` | 离散整数 | MagSize, PelletCount, StackSize |
| `Bool` | 开关标记 | IsAutomatic, IsTwoHanded |
| `String` | 文本 | DisplayName, Description |
| `GameplayTag` | 单一标签 | DamageType, GearType, Platform |
| `GameplayTagList` | 标签集合 | CompatibleAmmo, ResistTypes |
| `AssetRef` | 单个资源引用 | Icon, VisualPrefab, AnimationProfile |
| `AssetRefList` | 资源引用数组 | ATK（DamageEffectSO[] 模式） |

### 1.3 Tree 继承规则

- 子 Tree 通过 `InheritsFrom` 继承父 Tree 的全部节点
- 合并方式: **Union by NodeId，祖先优先**
- 子 Tree **只能追加**新节点，不能删除/禁用/替换祖先节点
- 同一 NodeId 出现在子 Tree 中 → **冲突告警**，保留祖先版本，子节点被丢弃。**Template 不存值，无法修改 Default**——如需不同默认值，通过实例层 `StatOverride[]` 覆盖
- 数值变种（同一属性集合、不同 Default）**不建子 Tree**，用 Spawn Config 资产（引用 Tree + StatOverride[] 列表）处理

### 1.3b Template 能做什么 / 不能做什么

| ✅ Template 能做 | ❌ Template 不能做 |
|----|----|
| 定义某类实体有哪些属性 | 设置属性的默认值（那是 PropertyDefSO 的职责） |
| 用文件夹组织属性层级 | 覆盖祖先节点的 DefId、父节点、或位置 |
| 追加父 Tree 没有的新属性 | 删除、禁用、或替换父 Tree 的任何属性 |
| | 在子 Tree 中写 "(ov)" 覆盖数值——Template 不存值 |

> **重要**: PropertyTree = 纯结构（DDL），不含值。`PropertyNode` 没有 value 字段。`PropertyTreeSO.Resolve()` 返回 `Dictionary<string, PropertyDefSO>`（属性集合，不含值）。所有数值覆写在实例层（GearDefSO / ActorDefSO 的 `overridesJson`）或 Spawn Config 资产中完成。

### 1.4 命名约定

- PropertyDef ID: PascalCase，无前缀（如 `MaxHP` 而非 `Actor_MaxHP`）
- 跨族重名: 同一 ID 可被多棵 Tree 引用（如 `Durability` 同时出现在 WeaponBase 和 ArmorBase）
- Tree 名称: PascalCase，表达实体类型（如 `WeaponBase`、`ArmorBase`、`Human`）
- 文件夹节点: PascalCase 名词，组织叶节点（如 `Vitals`、`Combat`、`Resistance`）

---

## 二、Tree 家族总览

```
Properties/
│
├── Actor/                                        ← 角色族（6 Trees, ~64 props）
│   ├── Actor                  (root, 2 props)    所有可交互实体的最小公约数
│   ├── Human : Actor          (+58 props = 60)   玩家/NPC
│   ├── Zombie : Actor         (+4 props = 6)     丧尸
│   ├── Creature : Actor       (TBD, 远期)
│   ├── Mutant : Actor         (TBD, 远期)
│   └── Robot : Actor          (TBD, 远期)
│
├── Equipment/                                    ← 装备族（22 Trees, ~110 unique props）
│   ├── Weapon/
│   │   ├── WeaponBase         (root, 3 props)
│   │   ├── MeleeWeapon        (+8 = 11)
│   │   ├── RangedWeapon       (+8 = 11)
│   │   │   ├── Firearm         (+5 = 16)
│   │   │   │   ├── Pistol      (+2+5 = 23)
│   │   │   │   ├── Rifle       (+2+6 = 24)
│   │   │   │   └── Shotgun     (+2+5 = 23)
│   │   │   └── Bow             (+3+4 = 18)
│   │   └── Throwable           (+2+4 = 9)
│   │
│   ├── Ammo/
│   │   ├── AmmoBase           (root, 9 props)
│   │   ├── PistolAmmo          (override defaults)
│   │   ├── RifleAmmo           (override defaults)
│   │   └── ShotgunShell        (override defaults)
│   │
│   ├── Armor/
│   │   ├── ArmorBase          (root, 9 props)
│   │   ├── HeadArmor           (+2 = 11)
│   │   ├── BodyArmor           (+2 = 11)
│   │   └── LegArmor            (+2 = 11)
│   │
│   ├── Container/                               ← 新增
│   │   └── ContainerBase      (root, 5 props)   背包/腰包/战术背心
│   │
│   ├── Tool/
│   │   └── ToolBase           (root, 7 props)
│   │
│   └── Consumable/
│       ├── ConsumableBase     (root, 4 props)
│       ├── Food                (+4 = 8)
│       ├── Medical             (+4 = 8)
│       └── Material            (+2 = 6)
│
├── Building/                                     ← 建筑族（1 Tree, 9 props）
│   └── Building              (root, 9 props)
│
└── Environment/                                  ← 环境族（1 Tree, 4 props, 远期）
    └── Environment           (root, 4 props)
```

**总计: 30 Trees, ~185 unique PropertyDef**

---

## 三、Actor 族

> 角色属性树。Human 是玩家/NPC 共用模板，Zombie 直接继承 Actor。Creature/Mutant/Robot 为远期框架。

### 3.1 Actor（根 Tree）

所有可交互实体的最小公约数——只有 HP。

```
Actor/
└── Vitals/
    ├── HP               Float  0~100    def=100    当前生命值
    └── MaxHP            Float  1~500    def=100    最大生命值
```

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 1 | HP | Float | 0 | 100 | 100 | 当前生命值。0=死亡 |
| 2 | MaxHP | Float | 1 | 500 | 100 | 最大生命值 |

### 3.2 Human : Actor

继承 Actor 全部属性，追加 8 个文件夹 + 58 个属性（Total = 60）。

#### Vitals — 生命体征（+9）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 3 | Stamina | Float | 0 | 100 | 100 | 体力。运动消耗，静止恢复 |
| 4 | MaxStamina | Float | 1 | 200 | 100 | 体力上限 |
| 5 | Hunger | Float | 0 | 100 | 100 | 饥饿度。0→HP 持续扣除 |
| 6 | Thirst | Float | 0 | 100 | 100 | 口渴度。0→Stamina 持续扣除 |
| 7 | BodyTemp | Float | 0 | 100 | 50 | 体温。向环境气温漂移，<20 失温 >80 中暑 |
| 8 | Blood | Float | 0 | 100 | 100 | 血量。受伤消耗，0→失血死亡 |
| 9 | Infection | Float | 0 | 100 | 0 | 感染值（累积型）。>10 发热 >30 模糊 >60 衰竭 >90 丧尸化 |
| 10 | Consciousness | Float | 0 | 100 | 100 | 意识值。剧痛/重伤降低，0→昏迷 |
| 11 | Pain | Float | 0 | 100 | 0 | 疼痛值。受伤增加，>60 行动惩罚 |

#### Attributes — 基础属性（+6）

1-10 范围，默认 5。影响所有 Derived 公式。

| # | PropID | Type | Min | Max | Default | 影响 |
|---|--------|------|-----|-----|---------|------|
| 12 | Strength | Float | 1 | 10 | 5 | 近战伤害 / 负重 |
| 13 | Agility | Float | 1 | 10 | 5 | 移速 / 闪避 / 攻速 |
| 14 | Endurance | Float | 1 | 10 | 5 | 减伤 / MaxStamina / 体力恢复 |
| 15 | Intelligence | Float | 1 | 10 | 5 | 科技研究 / 医疗效率 |
| 16 | Perception | Float | 1 | 10 | 5 | 命中 / 暴击 / 侦测 |
| 17 | Charisma | Float | 1 | 10 | 5 | 交易价格 / NPC 士气加成 |

#### Proficiency/Combat — 战斗熟练度（+9）

全部累积型（0→100），使用对应武器时累积。

| # | PropID | Type | Min | Max | Default | 绑定武器 |
|---|--------|------|-----|-----|---------|---------|
| 18 | BladeProf | Float | 0 | 100 | 0 | 刀剑 |
| 19 | AxeProf | Float | 0 | 100 | 0 | 斧 |
| 20 | StaffProf | Float | 0 | 100 | 0 | 棍杖 |
| 21 | UnarmedProf | Float | 0 | 100 | 0 | 徒手/武术套路 |
| 22 | PistolProf | Float | 0 | 100 | 0 | 手枪 |
| 23 | ShotgunProf | Float | 0 | 100 | 0 | 霰弹枪 |
| 24 | RifleProf | Float | 0 | 100 | 0 | 步枪 |
| 25 | ThrowableProf | Float | 0 | 100 | 0 | 投掷物 |
| 26 | DefensiveProf | Float | 0 | 100 | 0 | 防御（盾/闪避/格挡） |

#### Proficiency/Work — 工作熟练度（+12）

全部累积型。NPC 持续从事同类工作提升。

| # | PropID | Type | Min | Max | Default | 工作类型 |
|---|--------|------|-----|-----|---------|---------|
| 27 | CookProf | Float | 0 | 100 | 0 | 烹饪 |
| 28 | FarmProf | Float | 0 | 100 | 0 | 耕种 |
| 29 | BuildProf | Float | 0 | 100 | 0 | 建造 |
| 30 | GatherProf | Float | 0 | 100 | 0 | 采集 |
| 31 | CraftProf | Float | 0 | 100 | 0 | 制作 |
| 32 | MedicalProf | Float | 0 | 100 | 0 | 医疗 |
| 33 | ResearchProf | Float | 0 | 100 | 0 | 研究 |
| 34 | TradeProf | Float | 0 | 100 | 0 | 交易 |
| 35 | StealthProf | Float | 0 | 100 | 0 | 潜行 |
| 36 | SurvivalProf | Float | 0 | 100 | 0 | 生存（生火/净化水/辨识植物） |
| 37 | LockpickingProf | Float | 0 | 100 | 0 | 开锁/破解 |
| 38 | TrapProf | Float | 0 | 100 | 0 | 陷阱布置/拆除 |

#### Needs — 生存需求（+5）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 39 | Morale | Float | 0 | 100 | 50 | 士气。影响 NPC 工作/战斗效率。50=中性 |
| 40 | Sleepiness | Float | 0 | 100 | 0 | 困意（消耗型）。>50 效率惩罚 >80 强制休息 |
| 41 | Comfort | Float | 0 | 100 | 50 | 舒适度。受环境/装备影响 |
| 42 | SocialNeed | Float | 0 | 100 | 50 | 社交需求。独处累积，对话/活动降低 |
| 43 | Boredom | Float | 0 | 100 | 0 | 无聊度（消耗型）。重复工作累积，娱乐降低 |

#### Movement — 负重/移动（+1）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 44 | CarryWeight | Float | 10 | 200 | 50 | 负重上限 (kg)。公式: 50 + Strength×10 |

> **注意**: `CurrentWeight` 不进 Tree——它是 Sum(装备+背包) 的实时计算值（Derived 属性）。运行时由 EquipmentComponent 更新，非模板固有属性。

#### Resistance — 抗性（+11）

| # | PropID | Type | Min | Max | Default | 对应伤害/效果 |
|---|--------|------|-----|-----|---------|-------------|
| 46 | BleedResist | Float | 0 | 100 | 0 | 割伤 — 降低流血速率 |
| 47 | InfectionResist | Float | 0 | 100 | 0 | 咬伤 — 降低感染累积速率 |
| 48 | BluntResist | Float | 0 | 100 | 0 | 钝器伤 — 降低击倒概率 |
| 49 | FractureResist | Float | 0 | 100 | 0 | 骨折 — 降低骨折概率 |
| 50 | FireResist | Float | 0 | 100 | 0 | 烧伤 — 降低火焰伤害 |
| 51 | PoisonResist | Float | 0 | 100 | 0 | 中毒 — 降低毒素伤害 |
| 52 | ColdResist | Float | 0 | 100 | 0 | 寒冷 — 减缓体温下降 |
| 53 | ElectricResist | Float | 0 | 100 | 0 | 电击 — 降低电击伤害 |
| 54 | RadiationResist | Float | 0 | 100 | 0 | 辐射 — 减缓辐射累积 |
| 55 | PainResist | Float | 0 | 100 | 0 | 疼痛抗性 — 减缓疼痛累积 |
| 56 | KnockdownResist | Float | 0 | 100 | 0 | 击倒抗性 — 降低被击倒概率 |

#### Vision — 视觉属性（+3）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| — | SightRange | Float | 5 | 50 | 20 | 视野距离（米） |
| — | NightVision | Float | 0 | 100 | 0 | 夜视能力。0=无 100=全彩夜视 |
| — | FlashResist | Float | 0 | 100 | 0 | 闪光抗性。降低闪光弹致盲时间 |

> **⚠️ 缩放冲突**: 现有装备族 `FlashResist` 和 `NightVision` 的 Def 范围是 **0~1**（倍率），Human 需要 **0~100**（百分比）。**不能复用同一个 Def**——需为 Human 族创建独立 Def（如 `NightVision_Human`、`FlashResist_Human`），或扩展原 Def 的 Max 到 100 并确认装备侧不受影响。

#### NPC（+1）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 58 | Loyalty | Float | 0 | 100 | 50 | 忠诚度（累积型）。>70 高效率 <30 可能叛逃 |

#### Human Tree 完整结构

```
Human : Actor
├── Vitals/
│   ├── HP                   ← Actor 继承
│   ├── MaxHP                ← Actor 继承
│   ├── Stamina
│   ├── MaxStamina
│   ├── Hunger
│   ├── Thirst
│   ├── BodyTemp
│   ├── Blood
│   ├── Infection
│   ├── Consciousness
│   └── Pain
├── Attributes/
│   ├── Strength
│   ├── Agility
│   ├── Endurance
│   ├── Intelligence
│   ├── Perception
│   └── Charisma
├── Combat/
│   ├── BladeProf
│   ├── AxeProf
│   ├── StaffProf
│   ├── UnarmedProf
│   ├── PistolProf
│   ├── ShotgunProf
│   ├── RifleProf
│   ├── ThrowableProf
│   └── DefensiveProf
├── Work/
│   ├── CookProf
│   ├── FarmProf
│   ├── BuildProf
│   ├── GatherProf
│   ├── CraftProf
│   ├── MedicalProf
│   ├── ResearchProf
│   ├── TradeProf
│   ├── StealthProf
│   ├── SurvivalProf
│   ├── LockpickingProf
│   └── TrapProf
├── Needs/
│   ├── Morale
│   ├── Sleepiness
│   ├── Comfort
│   ├── SocialNeed
│   └── Boredom
├── Movement/
│   └── CarryWeight
├── Resistance/
│   ├── BleedResist
│   ├── InfectionResist
│   ├── BluntResist
│   ├── FractureResist
│   ├── FireResist
│   ├── PoisonResist
│   ├── ColdResist
│   ├── ElectricResist
│   ├── RadiationResist
│   ├── PainResist
│   └── KnockdownResist
├── Vision/
│   ├── SightRange
│   ├── NightVision
│   └── FlashResist
└── NPC/
    └── Loyalty
```

### 3.3 Zombie : Actor

丧尸不从 Human 继承——不需要生存需求、熟练度、抗性。直接从 Actor 继承 HP/MaxHP。

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| — | ATK_Zombie | Float | 1 | 50 | 10 | 丧尸每次攻击伤害 |
| — | ZombieSpeed | Float | 0.5 | 10 | 3 | 移动速度 (m/s) |
| — | NoiseReact | Float | 5 | 100 | 30 | 听觉范围 (m) |
| — | SightRange | Float | 5 | 50 | 15 | 复用 Human 的 SightRange Def。Template 不存值——不同丧尸类型的 SightRange 差异通过 Spawn Config 的 StatOverride[] 覆盖 |

```
Zombie : Actor
├── Vitals/
│   ├── HP                   ← Actor 继承
│   └── MaxHP                ← Actor 继承
├── Combat/
│   └── ATK_Zombie
├── Movement/
│   └── ZombieSpeed
└── Senses/
    ├── NoiseReact
    └── SightRange            ← 覆盖 Default=15
```

### 3.4 Derived Stats — 不进 Tree（18 项）

| Stat | 公式 | 依赖属性 |
|------|------|---------|
| FinalATK | Weapon.ATK × (1 + Strength×0.05 + Prof×0.01) | Weapon.ATK, Strength, Prof |
| FinalDEF | Armor.DEF × (1 + Endurance×0.03) | Armor.DEF, Endurance |
| HitRate | Weapon.Accuracy + Perception×3 + Prof×1 | Weapon.Accuracy, Perception, Prof |
| DodgeRate | Agility×3 + GearBonus | Agility |
| Penetration | Weapon.Penetration vs Armor.Toughness | Weapon.Penetration, Armor |
| CritRate | Perception×2 + Prof×1 | Perception, Prof |
| AttackSpeed | Weapon.AttackSpeed × (1 + Agility×0.02) | Weapon.AttackSpeed, Agility |
| CombatNoise | Weapon.NoiseRadius × (1 - StealthProf×0.005) | Weapon.NoiseRadius, StealthProf |
| MoveSpeed | BaseMove × (1 + Agility×0.03 - WeightPenalty) | Agility, CurrentWeight/CarryWeight |
| SprintSpeed | MoveSpeed × 1.5 | MoveSpeed |
| SprintCost | BaseCost × (1 - Endurance×0.03) | Endurance |
| JumpPower | 1 + Strength×0.3 + Stamina%×0.5 | Strength, Stamina |
| SneakSpeed | MoveSpeed × 0.5 × (1 + StealthProf×0.01) | MoveSpeed, StealthProf |
| HealingRate | Base × (1 + MedicalProf×0.02) × Nutrition | MedicalProf |
| StarvationRate | Base × (1 + BodyTempPenalty + InfectionPenalty) | BodyTemp, Infection |
| CombatPower | ATK × HitRate × CritRate × AttackSpeed | 综合 |
| StealthRating | StealthProf×0.6 + NoiseReduction×0.4 | StealthProf, NoiseRadius |
| WorkEfficiency | Morale×0.5 + Prof×0.5 | Morale, WorkProf |

---

## 四、Equipment 族 — Weapon

> 装备决定伤害地基（damage-source-model.md）。武器属性定义武器的固有物理特性，Ability 是动作模式不持有伤害值。

### 4.1 WeaponBase（武器根 Tree）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 101 | Durability | Float | 0 | 500 | 100 | 当前耐久。0=损坏 |
| 102 | MaxDurability | Float | 1 | 500 | 100 | 最大耐久 |
| 103 | Weight | Float | 0.1 | 50 | 2 | 重量 (kg) |

```
WeaponBase/
└── Base/
    ├── Durability
    ├── MaxDurability
    └── Weight
```

### 4.2 MeleeWeapon : WeaponBase（+6 Combat + 2 Tags = +8）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 104 | ATK | AssetRefList | — | — | — | 近战伤害基底 → DamageEffectSO[]。**注意**: 当前实现为 AssetRefList，引用 Damage Effect 资产 |
| 105 | AttackSpeed | Float | 0.5 | 3.0 | 1.0 | 攻击速度倍率 |
| 106 | CritMulti | Float | 1.0 | 5.0 | 1.5 | 暴击伤害倍率 |
| 107 | StunChance | Float | 0 | 1.0 | 0.1 | 眩晕概率 |
| 108 | Knockback | Float | 0 | 10 | 2 | 击退距离 (m) |
| 109 | Reach | Float | 0.5 | 3.0 | 1.2 | 攻击距离 (m) |
| 110 | StaminaCost | Float | 1 | 30 | 5 | 每次挥击消耗体力 |
| — | DamageType | GameplayTag | — | — | — | 伤害类型（已有） |
| — | IsTwoHanded | Bool | — | — | false | 是否双手武器（已有） |

```
MeleeWeapon : WeaponBase
├── Base/                   ← WeaponBase 继承
├── Combat/
│   ├── ATK
│   ├── AttackSpeed
│   ├── CritMulti
│   ├── StunChance
│   ├── Knockback
│   ├── Reach
│   └── StaminaCost
├── Tags/
│   ├── DamageType
│   └── IsTwoHanded
├── Presentation/           ← [补齐] 每把武器都需要
│   ├── Icon
│   ├── VisualPrefab
│   └── DisplayName
└── Behavior/               ← [补齐]
    ├── AnimationProfile
    └── AudioProfile
```

### 4.3 RangedWeapon : WeaponBase（+7 Combat + 1 Compat = +8）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 111 | ATK_Ranged | Float | 1 | 200 | 20 | 远程基础伤害（枪械机械能贡献） |
| 112 | Accuracy | Float | 0 | 1.0 | 0.7 | 基础命中率 |
| 113 | ReloadSpeed | Float | 0.5 | 3.0 | 1.0 | 换弹速度倍率 |
| 114 | MagSize | Int | 1 | 100 | 10 | 弹夹容量 |
| 115 | AmmoCount | Int | 0 | 500 | 10 | 弹夹内剩余弹药。**运行时值**——Spawn 时初始化为 MagSize，战斗中实时变化。Tree 中仅定义其存在性和约束 |
| 116 | NoiseRadius | Float | 5 | 200 | 50 | 击发噪音半径 (m) |
| 117 | Recoil | Float | 0 | 100 | 30 | 后坐力 |
| — | CompatibleAmmo | GameplayTagList | — | — | — | 兼容弹药口径（已有） |

> **ATK 命名冲突澄清**: MeleeWeapon.ATK 是 `AssetRefList`（引用 DamageEffectSO[]），RangedWeapon 继承 WeaponBase 后独立添加了 `Combat/ATK` 节点。但 damage-source-model.md 明确: **远程伤害地基在弹药**——枪的 ATK 不使用（消费者应读 Ammo.BaseDamage）。RangedWeapon 的 `Combat/ATK` 保持为空/不使用，由文档约定而非代码强制。

```
RangedWeapon : WeaponBase
├── Base/                   ← WeaponBase 继承
├── Combat/
│   ├── ATK                  ← AssetRefList（沿用）
│   ├── Accuracy
│   ├── ReloadSpeed
│   ├── MagSize
│   ├── AmmoCount
│   ├── NoiseRadius
│   └── Recoil
└── Compat/
    └── CompatibleAmmo
```

### 4.4 Firearm : RangedWeapon（+4 Combat + 2 Tags = +6）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 118 | FireRate | Float | 0.5 | 12 | 5 | 每秒射速 |
| 119 | MuzzleVelocity | Float | 100 | 1000 | 400 | 枪口初速 (m/s) |
| 120 | BarrelLength | Float | 2 | 24 | 6 | 枪管长度 (inch) |
| 121 | Reliability | Float | 0.1 | 1.0 | 0.95 | 单发正常循环概率 |
| — | IsAutomatic | Bool | — | — | false | 是否全自动（已有） |
| — | GearType | GameplayTag | — | — | — | 装备类型标签（已有） |

```
Firearm : RangedWeapon
├── ...                     ← RangedWeapon 继承
├── Combat/
│   ├── FireRate
│   ├── MuzzleVelocity
│   ├── BarrelLength
│   └── Reliability
└── Tags/
    ├── IsAutomatic
    └── GearType
```

### 4.5 Pistol : Firearm（+2 Combat + 5 Presentation/Behavior = +7）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 122 | HolsterSpeed | Float | 0.5 | 3.0 | 1.5 | 拔枪/收枪速度倍率 |
| 123 | HipFirePenalty | Float | 0 | 0.5 | 0.15 | 腰射精度惩罚 |

```
Pistol : Firearm
├── ...                     ← Firearm 继承
├── Combat/
│   ├── HolsterSpeed
│   └── HipFirePenalty
├── Presentation/
│   ├── Icon
│   ├── VisualPrefab
│   └── DisplayName
└── Behavior/
    ├── AnimationProfile
    └── AudioProfile
```

### 4.6 Rifle : Firearm（+2 Combat + 6 Presentation/Behavior = +8）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 124 | ScopeZoom | Float | 1 | 20 | 4 | 瞄准镜倍率 |
| 125 | AimTime | Float | 0.3 | 3.0 | 0.8 | 瞄准时间 (s) |

```
Rifle : Firearm
├── ...                     ← Firearm 继承
├── Combat/
│   ├── ScopeZoom
│   └── AimTime
├── Presentation/
│   ├── Icon
│   ├── VisualPrefab
│   ├── DisplayName
│   └── Description
└── Behavior/
    ├── AnimationProfile
    └── AudioProfile
```

### 4.7 Shotgun : Firearm（+2 Combat + 5 Presentation/Behavior = +7）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 126 | PelletCount | Int | 3 | 15 | 8 | 弹丸数量 |
| 127 | Spread | Float | 2 | 30 | 12 | 散布角 (度) |

```
Shotgun : Firearm
├── ...                     ← Firearm 继承
├── Combat/
│   ├── PelletCount
│   └── Spread
├── Presentation/           ← [补齐]
│   ├── Icon
│   ├── VisualPrefab
│   ├── DisplayName
│   └── Description
└── Behavior/               ← [补齐]
    ├── AnimationProfile
    └── AudioProfile
```

### 4.8 Bow : RangedWeapon（+3 Combat + 4 Presentation/Behavior = +7）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 128 | DrawSpeed | Float | 0.5 | 3.0 | 1.0 | 拉弓速度倍率 |
| 129 | ArrowVelocity | Float | 50 | 300 | 150 | 箭矢速度 (m/s) |
| 130 | HoldStamina | Float | 0 | 20 | 5 | 满弓时每秒消耗体力 |

```
Bow : RangedWeapon
├── ...                     ← RangedWeapon 继承
├── Combat/
│   ├── DrawSpeed
│   ├── ArrowVelocity
│   └── HoldStamina
├── Presentation/           ← [补齐]
│   ├── Icon
│   ├── VisualPrefab
│   └── DisplayName
└── Behavior/               ← [补齐]
    ├── AnimationProfile
    └── AudioProfile
```

### 4.9 Throwable : WeaponBase（+2 Combat + 4 Presentation/Behavior = +6）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 131 | BlastRadius | Float | 0 | 50 | 5 | 爆炸/效果半径 (m) |
| 132 | FuseTime | Float | 0.5 | 30 | 3 | 引信时间 (s) |

```
Throwable : WeaponBase
├── Base/                   ← WeaponBase 继承
├── Combat/
│   ├── BlastRadius
│   └── FuseTime
├── Presentation/           ← [补齐]
│   ├── Icon
│   ├── VisualPrefab
│   └── DisplayName
└── Behavior/               ← [补齐]
    ├── AnimationProfile
    └── AudioProfile
```

---

## 五、Equipment 族 — Ammo

> 弹药属性决定弹道终端的物理特性。damage-source-model.md 明确: **枪械伤害地基在弹药，枪是发射器**。

### 5.1 AmmoBase（弹药根 Tree）— 9 props

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 133 | BaseDamage | Float | 1 | 100 | 15 | 弹头基础伤害。与 Weapon ATK 加算得到最终伤害 |
| 134 | Penetration | Float | 0 | 20 | 2 | 穿透值。与目标 Armor DEF 对抗 |
| 135 | BulletWeight | Float | 20 | 800 | 115 | 弹头重量 (grain)。重弹=高穿透高后座 |
| 136 | OverPenetration | Float | 0 | 1.0 | 0.3 | 穿透掩体/目标倾向。0=停靶 1=穿到底 |
| 137 | RecoilFactor | Float | 0.5 | 2.0 | 1.0 | 后座力倍率。+P弹=1.3 亚音速=0.7 |
| 138 | AmmoReliability | Float | 0.5 | 1.0 | 1.0 | 击发可靠性。旧弹/手装弹<1.0 |
| 139 | FoulingRate | Float | 0 | 5.0 | 1.0 | 枪管污损倍率。腐蚀弹=3.0+ |

> AmmoBase 同时复用已有定义: `Weight`(弹药单发重量), `NoiseRadius`(击发噪音), `DamageType`(伤害类型), `Platform`(平台兼容)

```
AmmoBase/
├── Base/{Weight}
├── Combat/{BaseDamage, Penetration, NoiseRadius, BulletWeight, OverPenetration, AmmoReliability, FoulingRate}
├── Recoil/{RecoilFactor}
├── Tags/{DamageType}
└── Compat/{Platform}
```

### 5.2 口径子树（覆盖 Default）

| Tree | BaseDamage | Penetration | NoiseRadius | BulletWeight | 兼容武器 |
|------|-----------|-------------|-------------|-------------|---------|
| PistolAmmo (9mm) | 15(ov) | 2(ov) | 40(ov) | 115(ov) | Pistol |
| RifleAmmo (5.56mm) | 35(ov) | 8(ov) | 80(ov) | 62(ov) | Rifle |
| ShotgunShell (12ga) | 50(ov) | 4(ov) | 90(ov) | 438(ov) | Shotgun |

> 弹种变种（FMJ/JHP/AP/Subsonic）不建子树——用实例层 `StatOverride[]` 覆盖。

---

## 六、Equipment 族 — Armor

> 护甲三层防护模型: DEF（减伤值）+ Coverage（防护面积）+ TraumaTransfer（冲击传导）。

### 6.1 ArmorBase（防具根 Tree）— 9 props

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 140 | DEF | Float | 0 | 100 | 10 | 基础防御值 |
| 141 | Coverage | Float | 0.1 | 1.0 | 0.6 | 防护面积比例。未覆盖部位 DEF=0 |
| 142 | TraumaTransfer | Float | 0 | 1.0 | 0.5 | 冲击传导。被挡住时仍受 伤害×Trauma% |
| 143 | MoveSpeedPenalty | Float | 0 | 0.5 | 0.1 | 移速降低比例 |
| 144 | StaminaRegenPenalty | Float | 0 | 0.5 | 0.1 | 体力恢复速度降低比例 |

> ArmorBase 同时复用已有: `Durability`, `MaxDurability`, `Weight`, `ResistTypes`（已有 `Resist/` 文件夹——**保持现有路径不变**，避免破坏已有实例覆写）

```
ArmorBase/
├── Base/{Durability, MaxDurability, Weight}
├── Resist/{ResistTypes}                  ← 保持现有路径
├── Combat/{DEF, Coverage, TraumaTransfer}
└── Penalty/{MoveSpeedPenalty, StaminaRegenPenalty}
```

### 6.2 HeadArmor : ArmorBase（+2）

> 文件夹沿用现有 export 的 `Combat/`（非 Bonus/），避免路径变更破坏已有覆写

```
HeadArmor : ArmorBase
└── Combat/{FlashResist, NightVision}
```

### 6.3 BodyArmor : ArmorBase（+2）

```
BodyArmor : ArmorBase
└── Combat/{KnockdownResist, CarryWeight}
```

### 6.4 LegArmor : ArmorBase（+2）

```
LegArmor : ArmorBase
└── Combat/{MoveSpeed, SneakSpeed}
```

---

## 七、Equipment 族 — Container（背包/容器）

> 背包是 GDD 明确的核心装备类型，决定负重上限。不从 Weapon/Armor/Tool 继承——它们不是武器/护甲/工具。

### 7.1 ContainerBase（容器根 Tree）— 5 props

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 169 | CarryWeightBonus | Float | 5 | 200 | 20 | 负重上限加成 (kg) |
| 170 | MoveSpeedPenalty_Container | Float | 0 | 0.3 | 0.05 | 移速惩罚比例 |
| 171 | SlotCount | Int | 1 | 50 | 10 | 内部格子数 |

> 复用已有: `Durability`, `MaxDurability`, `Weight`, `Icon`, `VisualPrefab`, `DisplayName`

```
ContainerBase/
├── Base/{Durability, MaxDurability, Weight}
├── Capacity/{CarryWeightBonus, SlotCount}
├── Penalty/{MoveSpeedPenalty_Container}
└── Presentation/{Icon, VisualPrefab, DisplayName}
```

不同背包类型（小背包/登山包/战术背心/腰包）均为 Spawn Config，共用 ContainerBase。

---

## 九、Equipment 族 — Tool

> 工具是生存种田的核心——伐木、采矿、建造、耕地、烹饪都需工具。工具属性围绕耐久和效率。

### 7.1 ToolBase（工具根 Tree）— 7 props

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 145 | Efficiency | Float | 0.1 | 3.0 | 1.0 | 工作效率倍率 |
| 146 | MaterialTier | Int | 1 | 6 | 1 | 1=石 2=铜 3=铁 4=钢 5=合金 6=碳化物 |
| 147 | StaminaCostPerUse | Float | 0.5 | 10 | 2 | 每次使用消耗体力 |
| 148 | ToolType | GameplayTag | — | — | — | 工具类型: Axe/Pickaxe/Hammer/Saw/Hoe/Kitchen |

> 复用已有: `Durability`, `MaxDurability`, `Weight`

```
ToolBase/
├── Base/{Durability, MaxDurability, Weight}
├── Work/{Efficiency, MaterialTier, StaminaCostPerUse}
└── Tags/{ToolType}
```

不同工具（斧/镐/锤/锯/锄/厨具）均为 Spawn Config，共用 ToolBase。

---

## 十、Equipment 族 — Consumable

> 消耗品涵盖食物、饮料、药品、材料。A测必须——农业烹饪系统和伤病系统直接依赖。

### 8.1 ConsumableBase（消耗品根 Tree）— 4 props

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 149 | ConsumeTime | Float | 0.1 | 10 | 1.0 | 使用时间 (s) |
| 150 | StackSize | Int | 1 | 999 | 20 | 最大堆叠数 |
| 151 | ConsumableType | GameplayTag | — | — | — | 消耗品类型标签 |

> 复用已有: `Weight`

```
ConsumableBase/
├── Base/{Weight, ConsumeTime, StackSize}
└── Tags/{ConsumableType}
```

### 8.2 Food : ConsumableBase（+4）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 152 | Nutrition | Float | 0 | 100 | 10 | 饥饿恢复值 |
| 153 | Hydration | Float | 0 | 100 | 0 | 口渴恢复值 |
| 154 | MoraleBonus | Float | 0 | 50 | 0 | 食用后士气加成 |
| 155 | SpoilageTime | Float | 0 | 720 | 48 | 变质时间 (h)。0=不会坏 |

```
Food : ConsumableBase
├── ...                     ← ConsumableBase 继承
├── Nutrition/{Nutrition, Hydration}
└── Quality/{MoraleBonus, SpoilageTime}
```

### 8.3 Medical : ConsumableBase（+4）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 156 | HealAmount | Float | 0 | 100 | 20 | HP 恢复量 |
| 157 | BleedStop | Float | 0 | 1.0 | 0 | 止血效果。1.0=完全止血 |
| 158 | InfectionCleanse | Float | 0 | 100 | 0 | 感染值降低量 |
| 159 | PainRelief | Float | 0 | 100 | 0 | 疼痛值降低量 |

```
Medical : ConsumableBase
├── ...                     ← ConsumableBase 继承
├── Heal/{HealAmount, BleedStop, InfectionCleanse, PainRelief}
└── Quality/{MoraleBonus}   ← 复用 Food 的 MoraleBonus
```

### 8.4 Material : ConsumableBase（+2）

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 160 | Rarity | Int | 1 | 5 | 1 | 稀有度: 1=普通 2=罕见 3=稀有 4=史诗 5=传说 |
| 161 | MaterialType | GameplayTag | — | — | — | 材料类型: Wood/Stone/Metal/Fabric/Chemical |

```
Material : ConsumableBase
├── ...                     ← ConsumableBase 继承
└── Quality/{Rarity, MaterialType}
```

---

## 十一、Building 族

> 建筑有独立 HP、可被丧尸攻击破坏、需维修。与建造系统和尸潮系统联动。

### 11.1 Building（独立根 Tree）— 9 props

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 162 | DEF_Building | Float | 0 | 100 | 20 | 建筑减伤值 |
| 163 | MaterialType_Building | Int | 0 | 4 | 0 | 0=木 1=石 2=金属 3=混凝土 4=复合 |
| 164 | Flammability | Float | 0 | 1.0 | 0.5 | 可燃性。0=防火 1=速燃 |
| 165 | SoundDampening | Float | 0 | 100 | 20 | 隔音 (%) |
| 166 | WeatherResist | Float | 0 | 100 | 50 | 耐候性。0=雨中腐烂 100=完全防水 |
| 167 | WorkSpeed_Building | Float | 0.1 | 3.0 | 1.0 | 设施工作效率 |
| 168 | RestComfort | Float | 0 | 100 | 20 | 休息舒适度。影响 Sleepiness 恢复速度。0=地面 100=高级床 |

> 复用已有: `Durability`, `MaxDurability`

```
Building/
├── Vitals/{Durability, MaxDurability, WeatherResist}
├── Combat/{DEF_Building, MaterialType_Building, Flammability, SoundDampening}
└── Work/{WorkSpeed_Building}
```

不同建筑类型（围墙/地板/门/窗/农田/床/哨塔/工作台）均为 Spawn Config。

---

## 十二、Environment 族（远期）

> 全局单例 Tree，由 TimeService/WeatherService 驱动。A测暂缓。

| # | PropID | Type | Min | Max | Default | 说明 |
|---|--------|------|-----|-----|---------|------|
| 168 | FogDensity | Float | 0 | 100 | 0 | 雾浓度 |
| 169 | Temperature_Env | Float | -30 | 50 | 20 | 气温 (°C)。影响 BodyTemp 漂移 |
| 170 | Humidity | Float | 0 | 100 | 50 | 湿度 |
| 171 | TimeOfDay | Float | 0 | 24 | 8 | 当前时间（小时）。由 TimeService 写入 |

```
Environment/
├── Atmosphere/{FogDensity, Temperature_Env, Humidity}
└── Time/{TimeOfDay}
```

---

## 十四、已有 vs 需新建 — 差距分析

### 11.1 已有 PropertyDef（properties_export.json, 50 个）

| 类别 | 已有 | 说明 |
|------|------|------|
| Float | 37 | Accuracy, AimTime, ArrowVelocity, AttackSpeed, BarrelLength, BlastRadius, CarryWeight, CritMulti, DrawSpeed, Durability, FireRate, FlashResist, FuseTime, HipFirePenalty, HoldStamina, HolsterSpeed, Knockback, KnockdownResist, MaxDurability, MoveSpeed, MuzzleVelocity, NightVision, NoiseRadius, Reach, Recoil, Reliability, ReloadSpeed, ScopeZoom, SneakSpeed, Spread, StaminaCost, StunChance, Weight 等 |
| Int | 3 | MagSize, AmmoCount, PelletCount |
| Bool | 2 | IsAutomatic, IsTwoHanded |
| String | 2 | DisplayName, Description |
| GameplayTag | 3 | DamageType, GearType, Platform |
| GameplayTagList | 2 | CompatibleAmmo, ResistTypes |
| AssetRef | 4 | Icon, VisualPrefab, AnimationProfile, AudioProfile |
| AssetRefList | 1 | ATK |

### 11.2 需新建 PropertyDef（约 120 个）

| 族 | 需新建 | 具体 |
|----|--------|------|
| Actor | ~55 | HP, MaxHP, Stamina, MaxStamina, Hunger, Thirst, BodyTemp, Blood, Infection, Consciousness, Pain, Strength, Agility, Endurance, Intelligence, Perception, Charisma, BladeProf, AxeProf, StaffProf, UnarmedProf, PistolProf, ShotgunProf, RifleProf, ThrowableProf, DefensiveProf, CookProf, FarmProf, BuildProf, GatherProf, CraftProf, MedicalProf, ResearchProf, TradeProf, StealthProf, SurvivalProf, LockpickingProf, TrapProf, Morale, Sleepiness, Comfort, SocialNeed, Boredom, CarryWeight(已有?), CurrentWeight, BleedResist, InfectionResist, BluntResist, FractureResist, FireResist, PoisonResist, ColdResist, ElectricResist, RadiationResist, SightRange, Loyalty, ATK_Zombie, ZombieSpeed, NoiseReact |
| Ammo | 7 | BaseDamage, Penetration, BulletWeight, OverPenetration, RecoilFactor, AmmoReliability, FoulingRate |
| Armor | 5 | DEF, Coverage, TraumaTransfer, MoveSpeedPenalty, StaminaRegenPenalty |
| Tool | 4 | Efficiency, MaterialTier, StaminaCostPerUse, ToolType |
| Consumable | 11 | ConsumeTime, StackSize, ConsumableType, Nutrition, Hydration, MoraleBonus, SpoilageTime, HealAmount, BleedStop, InfectionCleanse, PainRelief, Rarity, MaterialType |
| Building | 6 | DEF_Building, MaterialType_Building, Flammability, SoundDampening, WeatherResist, WorkSpeed_Building |
| Environment | 4 | FogDensity, Temperature_Env, Humidity, TimeOfDay |

### 11.3 需调整的已有 Tree

| Tree | 操作 | 说明 |
|------|------|------|
| AmmoBase | **扩展** | 从 3 叶 → 9 叶。补 7 个新 Def 节点 + 结构调整 |
| ArmorBase | **扩展** | 从 4 叶 → 9 叶。补 5 个新 Def 节点 + 结构调整 |
| MeleeWeapon | **补齐** | 加 Presentation/{Icon,VisualPrefab,DisplayName} + Behavior/{AnimationProfile,AudioProfile} |
| Bow | **补齐** | 加 Presentation + Behavior 文件夹和节点 |
| Throwable | **补齐** | 加 Presentation + Behavior 文件夹和节点 |
| Shotgun | **补齐** | 补 DisplayName, Description, AnimationProfile, AudioProfile |
| Test, aaa | **删除** | 测试产物，清理 |

### 11.4 需新建的 Tree（10 棵）

| Tree | 父级 | 优先级 |
|------|------|--------|
| Actor | (root) | P4 |
| Human | Actor | P4 |
| Zombie | Actor | P5 |
| ToolBase | (root) | P2 |
| ConsumableBase | (root) | P2 |
| Food | ConsumableBase | P2 |
| Medical | ConsumableBase | P2 |
| Material | ConsumableBase | P2 |
| Building | (root) | P3 |
| Environment | (root) | P5 |

---

## 十三、GameplayTag 依赖

> **⚠️ 阻塞前提**: 以下 GameplayTag 根是 GameplayTag/GameplayTagList 类型属性的**前置条件**。标签不存在 → 属性在编辑器中无值可选 → Tree 不可用。**必须在创建对应 PropertyDef 之前或同步创建 Tag 资产。**

### 12.1 已有标签（从 gameplay-tag.md 确认存在）

| 根 | 叶标签示例 | 用途 |
|----|-----------|------|
| `Damage.Physical.*` | Slash, Blunt, Pierce, Bite | DamageType 属性值 |
| `Damage.Elemental.*` | Fire, Cold, Shock, Acid, Poison, Radiation | 元素伤害 |
| `Damage.Biological.*` | Bleed, Disease, Suffocation | 生物伤害 |
| `State.*` | Idle, Dead, Combat.Attacking 等 | 角色状态（不进属性树） |
| `Skill.*` | Combat.Melee, Ranged, etc. | 技能分类 |
| `Equip.*` | Slot.Head, Type.MeleeWeapon 等 | 装备槽位/类型 |
| `Actor.*` | Species.Human, Identity.Player 等 | 角色身份 |
| `Effect.*` | Buff.Fortify, Debuff.Slow 等 | Buff/Debuff 标签 |
| `Noise.*` | Combat.WeaponFire, World.Footstep 等 | 噪音类型 |

### 12.2 需新建标签

| 根 | 叶标签示例 | 用途 | 优先级 |
|----|-----------|------|--------|
| `Equipment.Part.*` | Receiver, Barrel, Slide, Magazine, Trigger, Grip, Muzzle, Optic, Underbarrel | GearType 属性值 | P1 |
| `Platform.*` | Glock, AR15, AK, Remington | Platform 属性值 | P1 |
| `Caliber.*` | 9mm, 5.56mm, 12ga, 7.62mm, .45ACP | CompatibleAmmo 属性值 | P1 |
| `Tool.*` | Axe, Pickaxe, Hammer, Saw, Hoe, Kitchen | ToolType 属性值 | P2 |
| `Consumable.*` | Food, Drink, Medical, Material | ConsumableType 属性值 | P2 |
| `Building.*` | Wall, Floor, Door, Furniture, Defense, Storage | 建筑类型分类 | P3 |
| `Material.*` | Wood, Stone, Metal, Fabric, Chemical, Electronic | MaterialType 属性值 | P2 |

---

## 十五、A测优先级

| 阶段 | 产出 | 新建 Def | 新建/修改 Tree |
|------|------|----------|---------------|
| **P0 清理** | 删除 Test/aaa 测试数据 | 0 | 删 2 |
| **P1 装备补齐** | AmmoBase +7, ArmorBase +5, 武器叶子补 Presentation/Behavior | 12 | 改 6 |
| **P1 标签补齐** | Equipment.Part + Platform + Caliber 标签 | 0 (Tag资产) | ~20 Tag assets |
| **P2 工具+消耗品** | ToolBase + ConsumableBase + Food + Medical + Material | 20 | 新建 5 |
| **P2 标签** | Tool.* + Consumable.* + Material.* 标签 | 0 | ~12 Tag assets |
| **P3 建筑** | Building Tree | 6 | 新建 1 |
| **P4 Actor 族** | Actor + Human（最大族, ~55 Def） | 55 | 新建 2 |
| **P5 远期** | Zombie + Environment + Creature/Mutant/Robot | 12+ | 新建 5+ |

---

## 十六、设计决策记录

| 决策 | 原因 |
|------|------|
| Derived 不进 Tree | 公式计算值不应有独立 Default——它们由源属性决定。放 Tree 里会造成"ATK 的 Default 和公式计算结果不同"的二义性 |
| 武器 ATK 为 AssetRefList | damage-source-model.md: 近战伤害地基是武器固有属性，引用 DamageEffectSO[]。远程伤害地基在弹药，枪只贡献机械参数 |
| 熟练度全部进 Human | A测只有 Human 角色。Zombie 不从 Human 继承——它不需要熟练度/需求/抗性 |
| 数值变种不建子树 | 子 Tree 只能追加节点，不能"同一 stat 不同 Default"。Spawn Config + StatOverride 覆盖 Default 更轻量 |
| Consumable 分 Food/Medical/Material 三子树 | 虽然都"可消耗"，但属性集合结构性不同：Food 有 Nutrition/Spoilage，Medical 有 HealAmount/InfectionCleanse，Material 有 Rarity。子树表达结构差异 |
| Tool 只有一棵 Tree | 所有工具共享 Durability/Weight/Efficiency/MaterialTier/StaminaCostPerUse。斧和镐的区别仅 Default 不同——走 Spawn Config |
| Building 独立根 | 建筑的 HP/DEF/Flammability/SoundDampening 与任何装备都不同，不从任何树继承 |
| Presentation/Behavior 补齐到叶子层 | 当前继承链中 MeleeWeapon/RangedWeapon 没有 UI/表现属性，不提升到基类避免影响已有结构。每叶子补齐 |
| CurrentWeight 不进 Tree | Derived 属性——Sum(装备+背包)实时计算，非模板固有属性。运行时由 EquipmentComponent 更新 |
| PainResist/KnockdownResist 进 Human | injury-system.md 有 Pain 和 Knockdown 机制，BodyArmor 引用 KnockdownResist 作为 Bonus。交叉验证发现原设计缺失，已补 |
| FlashResist/NightVision 需独立 Def | 装备族用 0~1 倍率，Actor 族用 0~100 百分比。同一 Def 跨族复用会导致 Clamp 截断，必须创建独立 Def（加 `_Human` 后缀） |
| Armor 子节点文件夹保持 Combat/ | 现有 export 已用 `Combat/`，改成 `Bonus/` 会破坏已有实例覆写路径。保持兼容 |
| Container 族独立于 Weapon/Armor/Tool | 背包不是武器/护甲/工具——CarryWeightBonus + SlotCount 是其独有属性。GDD 明确需要背包系统 |
| RestComfort 进 Building | Sleepiness 恢复需要区分"睡地上"和"睡床上"。Building 不只是一个 HP 容器 |
| Template 不能覆写 Default | PropertyNode 无 value 字段，合并算法祖先优先。所有 "(ov)" 标注已修正为 "通过 Spawn Config 的 StatOverride[]" |
| GameplayTag 根是阻塞前置条件 | GameplayTag/GameplayTagList 属性在对应 Tag 不存在时无值可选。Tag 资产必须先于或同步于 PropertyDef 创建 |
