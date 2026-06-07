# StatsTree 层级设计 — Actor 族

> 基于 `design/stats-inventory.md` 的 83+ 全量 Stat 目录，将其映射到 StatsTreeSO 继承链。
> 关联: `stats-system.md`, `stats-inventory.md`, `L3-stats/README.md`

## 设计原则

1. **Tree = 结构不同的 stat 集合**。StatsTreeSO 只在 stat 集合与父级有**结构性差异**（增/删 stat）时才新建。Numerical Variant（同 stat 集合、仅 Default 不同）不进 Tree，用 **Spawn Config** 数据资产替代。
2. **Stat 归属决定**：Bounded / Consumable / Cumulative 三种基类的 stat 进入 Tree；Derived 类型的 stat **不进 Tree**，由 C# 运行时公式计算。
3. **族内基类可追加 stat**（Human 在 Actor 上追加 56 stat）。**Spawn Config 只覆盖 Default**，不增删 stat。
4. **文件夹 = 分组**。文件夹节点仅组织叶子，本身不持有 Def。

## Tree 家族总览（6 Trees + Spawn Configs）

```
Actor               — 根 (2 stats)
├── Human           — +56 stats, 58 total
│   └── [Man / Woman / Child / Elder]  ← Spawn Config（数值变种，非 Tree）
│
├── Zombie          — +4 stats, 6 total
│   └── [Walker / Runner / Spitter / Tank]  ← Spawn Config
│
├── Creature        — TBD
│   └── [Small / Medium / Large]  ← Spawn Config
│
├── Mutant          — TBD (unique Mutation/)
│
└── Robot           — TBD (unique Power/)
    └── [Drone / Sentry / Mech]  ← Spawn Config
```

> **配套文档**: 装备 / 建筑 / 工具 / 环境 Tree 设计见 `equipment-tree-design.md`。
> Spawn Config 是轻量数据资产（ScriptableObject），引用基类 Tree + OverrideValue 列表，运行时 `tree.Resolve()` 后应用覆盖。

---

## 一、Actor（根 Tree）

所有可交互实体的最小公约数。只有两个 stat：

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Vitals/HP` | HP | Bounded | 0 | 100 | 100 | 当前生命值 |
| `Vitals/MaxHP` | MaxHP | Bounded | 1 | 500 | 100 | 最大生命值 |

```
Actor/
└── Vitals/
    ├── HP        [0 ~ 100, def=100]
    └── MaxHP     [1 ~ 500, def=100]
```

---

## 二、Human : Actor

继承 Actor 的全部 stat，追加 5 个文件夹 + 44 个 stat。

### Vitals（生命体征）

在 Actor 的 `HP, MaxHP` 基础上追加 9 个：

| Stat | 类型 | Min | Max | Def | consumeRate | restoreRate | 说明 |
|------|------|-----|-----|-----|-------------|-------------|------|
| Stamina | Consumable | 0 | 100 | 100 | 0 (事件驱动) | 20/s (静止) | 体力。运动消耗，静止恢复 |
| MaxStamina | Bounded | 1 | 200 | 100 | — | — | 体力上限 |
| Hunger | Consumable | 0 | 100 | 100 | 0.01/s | 0 | 饥饿。0→扣 HP (5/s Physiology) |
| Thirst | Consumable | 0 | 100 | 100 | 0.015/s | 0 | 口渴。0→扣 Stamina |
| BodyTemp | Consumable | 0 | 100 | 50 | 0.03/s (向环境) | 0.03/s (向环境) | 体温。向环境气温漂移。<20 失温 >80 中暑 |
| Blood | Consumable | 0 | 100 | 100 | 0 (事件驱动) | 0.1/s (自然凝血) | 血量。流血时消耗。0→死亡 |
| Infection | Cumulative | 0 | 100 | 0 | — | — | 感染值。4 阶段: >10 发热 >30 意识模糊 >60 器官衰竭 >90 丧尸化 |
| Consciousness | Bounded | 0 | 100 | 100 | — | — | 意识。剧痛/重伤↓。0→昏迷 |
| Pain | Consumable | 0 | 100 | 0 | 0 (事件驱动) | 5/s (静止) | 疼痛。受伤↑。>60 行动惩罚 |

### Attributes（基础属性）— 6 项

| Stat | 类型 | Min | Max | Default | 说明 |
|------|------|-----|-----|---------|------|
| Strength | Bounded | 1 | 10 | 5 | 力量。→ 近战伤害 / 负重 |
| Agility | Bounded | 1 | 10 | 5 | 敏捷。→ 移速 / 闪避 |
| Endurance | Bounded | 1 | 10 | 5 | 体质。→ 减伤 / MaxStamina |
| Intelligence | Bounded | 1 | 10 | 5 | 智力。→ 科技 / 医疗 / 研究效率 |
| Perception | Bounded | 1 | 10 | 5 | 感知。→ 命中 / 暴击 / 侦测 |
| Charisma | Bounded | 1 | 10 | 5 | 魅力。→ 交易价格 / NPC 士气 |

### Proficiency / Combat（战斗熟练度）— 9 项

与 Ability Tag 的武器类别一一对应。全部 Cumulative，使用对应武器/能力时累积。

| Stat | 类型 | Min | Max | Default | 绑定 Tag |
|------|------|-----|-----|---------|----------|
| BladeProf | Cumulative | 0 | 100 | 0 | `Ability.Melee.Blade` |
| AxeProf | Cumulative | 0 | 100 | 0 | `Ability.Melee.Axe` |
| StaffProf | Cumulative | 0 | 100 | 0 | `Ability.Melee.Staff` |
| UnarmedProf | Cumulative | 0 | 100 | 0 | `Ability.Melee.Routines` (11 种武术套路) |
| PistolProf | Cumulative | 0 | 100 | 0 | `Ability.Ranged.Pistol` |
| ShotgunProf | Cumulative | 0 | 100 | 0 | `Ability.Ranged.Shotgun` |
| RifleProf | Cumulative | 0 | 100 | 0 | `Ability.Ranged.Rifle` |
| ThrowableProf | Cumulative | 0 | 100 | 0 | `Ability.Throwable` (8 种投掷物) |
| DefensiveProf | Cumulative | 0 | 100 | 0 | `Ability.Defensive` (盾/防/闪避) |

### Proficiency / Work（工作熟练度）— 12 项

全部 Cumulative。通过执行对应工作获取经验。

| Stat | 类型 | Min | Max | Default | 说明 |
|------|------|-----|-----|---------|------|
| CookProf | Cumulative | 0 | 100 | 0 | 烹饪 |
| FarmProf | Cumulative | 0 | 100 | 0 | 耕种 |
| BuildProf | Cumulative | 0 | 100 | 0 | 建造 |
| GatherProf | Cumulative | 0 | 100 | 0 | 采集 |
| CraftProf | Cumulative | 0 | 100 | 0 | 制作 |
| MedicalProf | Cumulative | 0 | 100 | 0 | 医疗 |
| ResearchProf | Cumulative | 0 | 100 | 0 | 研究 |
| TradeProf | Cumulative | 0 | 100 | 0 | 交易 |
| StealthProf | Cumulative | 0 | 100 | 0 | 潜行 |
| SurvivalProf | Cumulative | 0 | 100 | 0 | 生存 |
| LockpickingProf | Cumulative | 0 | 100 | 0 | 开锁/破解。绑定 `Ability.Lockpicking` |
| TrapProf | Cumulative | 0 | 100 | 0 | 陷阱布置/拆除。绑定 `Ability.Trap` |

### Needs（生存需求）— 5 项

| Stat | 类型 | Min | Max | Def | consumeRate | restoreRate | 说明 |
|------|------|-----|-----|-----|-------------|-------------|------|
| Morale | Bounded | 0 | 100 | 50 | — | — | 士气。影响工作/战斗效率。50=中性 |
| Sleepiness | Consumable | 0 | 100 | 0 | 0.008/s | 3/s (睡眠) | 困意。>50 效率惩罚 >80 强制休息 |
| Comfort | Bounded | 0 | 100 | 50 | — | — | 舒适度。受环境/装备影响 |
| SocialNeed | Bounded | 0 | 100 | 50 | — | — | 社交需求。独处↑ 对话/活动↓ |
| Boredom | Consumable | 0 | 100 | 0 | 0.005/s | 5/s (娱乐) | 无聊度。重复工作↑ 娱乐↓ |

### Movement（负重/移动）— 2 项

基础移动相关，Bounded。实际移速由 Derived 公式计算。

| Stat | 类型 | Min | Max | Default | 说明 |
|------|------|-----|-----|---------|------|
| CarryWeight | Bounded | 10 | 200 | 50 | 负重上限 (kg)。公式: 50 + Strength×10 |
| CurrentWeight | Bounded | 0 | 200 | 0 | 当前负重 (kg)。Sum(装备+背包)。实时更新 |

### Resistance（抗性）— 9 项

覆盖 injury-system.md 的 5 种伤害类型 + 4 种环境/状态抗性。

| Stat | 类型 | Min | Max | Default | 对应伤害 |
|------|------|-----|-----|---------|----------|
| BleedResist | Bounded | 0 | 100 | 0 | 割伤 (Laceration) — 降低流血速率 |
| InfectionResist | Bounded | 0 | 100 | 0 | 咬伤 (Bite) — 降低感染累积速率 |
| BluntResist | Bounded | 0 | 100 | 0 | 钝器伤 (Blunt) — 降低击倒概率 |
| FractureResist | Bounded | 0 | 100 | 0 | 骨折 (Fracture) — 降低骨折概率 |
| FireResist | Bounded | 0 | 100 | 0 | 烧伤 (Burn) |
| PoisonResist | Bounded | 0 | 100 | 0 | 中毒 — 降低毒素伤害 |
| ColdResist | Bounded | 0 | 100 | 0 | 寒冷 — 减缓体温下降 |
| ElectricResist | Bounded | 0 | 100 | 0 | 电击 — 降低电击伤害 |
| RadiationResist | Bounded | 0 | 100 | 0 | 辐射 — 减缓辐射累积 |
| PainResist | Bounded | 0 | 100 | 0 | 疼痛抗性。减缓疼痛累积 |
| KnockdownResist | Bounded | 0 | 100 | 0 | 击倒抗性。降低被击倒概率 |

### Vision（视觉属性）— 3 项

| Stat | 类型 | Min | Max | Default | 说明 |
|------|------|-----|-----|---------|------|
| SightRange | Bounded | 5 | 50 | 20 | 视野距离（米） |
| NightVision | Bounded | 0 | 100 | 0 | 夜视能力。0=无 100=全彩 |
| FlashResist | Bounded | 0 | 100 | 0 | 闪光抗性。降低闪光弹致盲时间 |

### NPC — 1 项（仅 NPC）

| Stat | 类型 | Min | Max | Default | 说明 |
|------|------|-----|-----|---------|------|
| Loyalty | Cumulative | 0 | 100 | 50 | 忠诚度。>70 高效率 <30 可能叛逃 |

### Human Tree 完整结构

```
Human : Actor
├── Vitals/
│   ├── HP               [0~100,   def=100]   ← Actor 继承
│   ├── MaxHP            [1~500,   def=100]   ← Actor 继承
│   ├── Stamina          [0~100,   def=100]   Consumable
│   ├── MaxStamina       [1~200,   def=100]
│   ├── Hunger           [0~100,   def=100]   Consumable
│   ├── Thirst           [0~100,   def=100]   Consumable
│   ├── BodyTemp         [0~100,   def=50]
│   ├── Blood            [0~100,   def=100]   Consumable
│   ├── Infection        [0~100,   def=0]     Cumulative
│   ├── Consciousness    [0~100,   def=100]
│   └── Pain             [0~100,   def=0]     Consumable
│
├── Attributes/
│   ├── Strength         [1~10,    def=5]
│   ├── Agility          [1~10,    def=5]
│   ├── Endurance        [1~10,    def=5]
│   ├── Intelligence     [1~10,    def=5]
│   ├── Perception       [1~10,    def=5]
│   └── Charisma         [1~10,    def=5]
│
├── Combat/                   ← 战斗熟练度 (9)
│   ├── BladeProf        [0~100,   def=0]     Cumulative
│   ├── AxeProf          [0~100,   def=0]     Cumulative
│   ├── StaffProf        [0~100,   def=0]     Cumulative
│   ├── UnarmedProf      [0~100,   def=0]     Cumulative
│   ├── PistolProf       [0~100,   def=0]     Cumulative
│   ├── ShotgunProf      [0~100,   def=0]     Cumulative
│   ├── RifleProf        [0~100,   def=0]     Cumulative
│   ├── ThrowableProf    [0~100,   def=0]     Cumulative
│   └── DefensiveProf    [0~100,   def=0]     Cumulative
│
├── Work/                     ← 工作熟练度 (12)
│   ├── CookProf         [0~100,   def=0]     Cumulative
│   ├── FarmProf         [0~100,   def=0]     Cumulative
│   ├── BuildProf        [0~100,   def=0]     Cumulative
│   ├── GatherProf       [0~100,   def=0]     Cumulative
│   ├── CraftProf        [0~100,   def=0]     Cumulative
│   ├── MedicalProf      [0~100,   def=0]     Cumulative
│   ├── ResearchProf     [0~100,   def=0]     Cumulative
│   ├── TradeProf        [0~100,   def=0]     Cumulative
│   ├── StealthProf      [0~100,   def=0]     Cumulative
│   ├── SurvivalProf     [0~100,   def=0]     Cumulative
│   ├── LockpickingProf  [0~100,   def=0]     Cumulative
│   └── TrapProf         [0~100,   def=0]     Cumulative
│
├── Needs/
│   ├── Morale           [0~100,   def=50]
│   ├── Sleepiness       [0~100,   def=0]     Consumable
│   ├── Comfort          [0~100,   def=50]
│   ├── SocialNeed       [0~100,   def=50]
│   └── Boredom          [0~100,   def=0]     Consumable
│
├── Movement/
│   ├── CarryWeight      [10~200,  def=50]
│   └── CurrentWeight    [0~200,   def=0]
│
├── Resistance/
│   ├── BleedResist      [0~100,   def=0]
│   ├── InfectionResist  [0~100,   def=0]
│   ├── BluntResist      [0~100,   def=0]
│   ├── FractureResist   [0~100,   def=0]
│   ├── FireResist       [0~100,   def=0]
│   ├── PoisonResist     [0~100,   def=0]
│   ├── ColdResist       [0~100,   def=0]
│   ├── ElectricResist   [0~100,   def=0]
│   └── RadiationResist  [0~100,   def=0]
│
├── Vision/
│   ├── SightRange       [5~50,    def=20]
│   ├── NightVision      [0~100,   def=0]
│   └── FlashResist      [0~100,   def=0]
│
└── NPC/
    └── Loyalty          [0~100,   def=50]    Cumulative
```

**Human 合计**: 11 Vitals + 6 Attributes + 21 Proficiency (9 Combat + 12 Work) + 5 Needs + 2 Movement + 9 Resistance + 3 Vision + 1 NPC = **58 stats**（含 Actor 继承的 2 个）

---

## 三、Human 变种
所有 Human 变种使用同一个 `Human` Tree，仅通过 Spawn Config（轻量 SO 资产）覆盖 Default。

| Spawn Config | 覆盖 Stat | OverrideValue | 说明 |
|-------------|-----------|---------------|------|
| **Man** | Strength, Endurance | 6, 6 | 成年男性，+1 力/体 |
| **Woman** | Agility, Perception | 6, 6 | 成年女性，+1 敏/感 |
| **Child** | MaxHP, Strength, Agility, Endurance | 60, 3, 7, 3 | 弱体强敏 |
| **Elder** | MaxStamina, Strength, Endurance, Intelligence, Charisma | 60, 3, 3, 7, 7 | 体衰智增 |

> 均不建子 Tree。Elder 的 Sleepiness 消耗加速通过 Physiology 实现。

---

## 四、Zombie : Actor

丧尸不从 Human 继承——它们不需要生存需求、熟练度、抗性。直接从 Actor 继承 `HP, MaxHP`，追加专有 stat。

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Vitals/HP` | HP | Bounded | 0 | 500 | 100 | ← 覆盖 Actor Default |
| `Vitals/MaxHP` | MaxHP | Bounded | 1 | 500 | 100 | |
| `Combat/ATK` | ATK | Bounded | 1 | 50 | 10 | 每次攻击伤害 |
| `Movement/Speed` | Speed | Bounded | 0.5 | 10 | 3 | 移动速度 (m/s) |
| `Senses/NoiseReact` | NoiseReact | Bounded | 5 | 100 | 30 | 听觉范围 (m) |
| `Senses/SightRange` | SightRange | Bounded | 5 | 50 | 15 | 视觉范围 (m) |

```
Zombie : Actor
├── Vitals/
│   ├── HP               [0~500,   def=100]   ← Actor 继承
│   └── MaxHP            [1~500,   def=100]   ← Actor 继承
├── Combat/
│   └── ATK              [1~50,    def=10]
├── Movement/
│   └── Speed            [0.5~10,  def=3]
└── Senses/
    ├── NoiseReact       [5~100,   def=30]
    └── SightRange       [5~50,    def=15]
```

**Zombie 合计**: 2 继承 + 4 新增 = **6 stats**

### Zombie Spawn Configs（数值变种，非 Tree）

全部使用 `Zombie` Tree。

| Spawn Config | HP | Speed | ATK | 覆盖 | 特点 |
|-------------|-----|-------|-----|------|------|
| **Walker** | 150 (ov) | 2 (ov) | 10 (def) | — | 慢、肉、数量最多 |
| **Runner** | 60 (ov) | 8 (ov) | 12 (ov) | NoiseReact=60 ov | 快、脆、听觉灵敏 |
| **Spitter** | 80 (ov) | 4 (ov) | 18 (ov) | SightRange=25 ov | 远程酸液攻击 |
| **Tank** | 500 (ov) | 1.5 (ov) | 30 (ov) | NoiseReact=10 ov | 巨型、拆建筑 |

---

## 四、Creature / Mutant / Robot（远期框架）

暂留。具体 stat 在各自系统开发时补充。变种（Small/Medium/Large, Drone/Sentry/Mech）均为 Spawn Config。

```
Creature : Actor          Mutant : Actor          Robot : Actor
├── Vitals/               ├── Vitals/              ├── Vitals/
├── Combat/               ├── Combat/              ├── Combat/
├── Movement/             ├── Movement/            ├── Movement/
└── Senses/               └── Mutation/            └── Power/
```

---

## 五、不进 Tree 的 Derived Stats

以下 stat 由 C# 运行时公式计算，**不作为 StatDefinitionSO 进入 Tree**：

### Combat（8 项）

| Stat | 来源 |
|------|------|
| ATK | Weapon.BaseATK × (1 + Strength×0.05 + Prof×0.01) |
| DEF | Armor.BaseDEF × (1 + Endurance×0.03) |
| HitRate | Weapon.BaseHit + Perception×3 + Prof×1 |
| DodgeRate | Agility×3 + gear bonuses |
| Penetration | Weapon.Penetration - Armor.Toughness |
| CritRate | Perception×2 + Prof×1 |
| AttackSpeed | Weapon.BaseSpeed × (1 + Agility×0.02) |
| CombatNoise | Weapon.BaseNoise × (1 - StealthProf×0.005) |

### Movement（5 项 — CarryWeight/CurrentWeight 已移入 Tree 作 Bounded，此处为公式参考）

| Stat | 来源 |
|------|------|
| MoveSpeed | BaseMoveSpeed × (1 + Agility×0.03 - WeightPenalty) |
| SprintSpeed | MoveSpeed × 1.5 |
| SprintCost | BaseSprintCost × (1 - Endurance×0.03) |
| JumpPower | 1 + Strength×0.3 + Stamina% × 0.5 |
| SneakSpeed | MoveSpeed × 0.5 × (1 + StealthProf×0.01) |

> **CarryWeight** 初始值 = 50 + Strength×10，在树中为 Bounded 节点（def=50），角色生成时通过公式修改。CurrentWeight 实时计算 Sum(装备+背包)。

### Derived 综合（7 项）

| Stat | 来源 |
|------|------|
| HealingRate | Base × (1 + MedicalProf×0.02) × NutritionMultiplier |
| StarvationRate | Base × (1 + BodyTempPenalty + InfectionPenalty) |
| CombatPower | ATK × HitRate × CritRate × AttackSpeed — 仅供参考 |
| StealthRating | StealthProf × 0.6 + NoiseReduction × 0.4 |
| StealthDetect | Perception × 3 + StealthProf × 1 — 反侦测 |
| WorkEfficiency | Morale × 0.5 + Prof × 0.5 — NPC 工作效率 |
| SurvivalRating | 综合各项生存指标 |

---

## 六、Stat 总数汇总

| Tree | Bounded | Consumable | Cumulative | 合计 | Spawn Configs |
|------|---------|------------|------------|------|---------------|
| Actor | 2 | 0 | 0 | **2** | — |
| Human (+Actor) | 32 | 7 | 21 | **58** | Man, Woman, Child, Elder |
| Zombie (+Actor) | 6 | 0 | 0 | **6** | Walker, Runner, Spitter, Tank |
| Creature | TBD | TBD | TBD | TBD | Small, Medium, Large |
| Mutant | TBD | TBD | TBD | TBD | — |
| Robot | TBD | TBD | TBD | TBD | Drone, Sentry, Mech |
| **Tree 内合计 (已定义)** | | | | **64** | |
| Derived (不进 Tree) | — | — | — | **18** | |
| **Actor 族全量** | | | | **82** | |

> 6 Trees + 14 Spawn Configs。装备/建筑/环境 stat 见 `equipment-tree-design.md`。

---

## 七、实现优先级

| 阶段 | 产出 | Stats | 说明 |
|------|------|-------|------|
| A测 (当前) | Actor + Human Trees | 58 | 供 Player/NPC 使用 |
| A测 | 4 Human Spawn Configs | 0 新增 | Man/Woman/Child/Elder |
| B测 | Zombie Tree + 4 Spawn Configs | 6 | 对接丧尸 AI |
| 远期 | Creature / Mutant / Robot Trees | TBD | 具体 stat 在系统开发时补充 |
