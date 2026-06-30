# PropertyTree — Actor 子树

> 日期: 2026-06-30 · 状态: 待推理
> 关联: `property-tree-structure.md`

```
Actor : Entity                                     所有可交互角色的最小公约数
│  继承: Common/DisplayName, Icon, Description, Weight, Tags
│         Slots/
│
├── Actor/Vitals/
│   ├── HP               Float     当前生命值
│   └── MaxHP            Float     最大生命值
│
├── Human : Actor                                  玩家/NPC
│   ├── Vitals/
│   │   ├── Stamina          Float     体力
│   │   ├── MaxStamina       Float     体力上限
│   │   ├── Hunger           Float     饥饿度
│   │   ├── Thirst           Float     口渴度
│   │   ├── BodyTemp         Float     体温
│   │   ├── Blood            Float     血量
│   │   ├── Infection        Float     感染值
│   │   ├── Consciousness    Float     意识值
│   │   └── Pain             Float     疼痛值
│   ├── Attributes/
│   │   ├── Strength         Float     力量
│   │   ├── Agility          Float     敏捷
│   │   ├── Endurance        Float     体质
│   │   ├── Intelligence     Float     智力
│   │   ├── Perception       Float     感知
│   │   └── Charisma         Float     魅力
│   ├── Combat/
│   │   ├── BladeProf        Float     刀剑熟练度
│   │   ├── AxeProf          Float     斧熟练度
│   │   ├── StaffProf        Float     棍杖熟练度
│   │   ├── UnarmedProf      Float     徒手熟练度
│   │   ├── PistolProf       Float     手枪熟练度
│   │   ├── ShotgunProf      Float     霰弹枪熟练度
│   │   ├── RifleProf        Float     步枪熟练度
│   │   ├── ThrowableProf    Float     投掷熟练度
│   │   ├── DefensiveProf    Float     防御熟练度
│   │   └── BowProf           Float     弓熟练度 (0-100 累积型)
│   ├── Work/
│   │   ├── CookProf         Float     烹饪
│   │   ├── FarmProf         Float     耕种
│   │   ├── BuildProf        Float     建造
│   │   ├── GatherProf       Float     采集
│   │   ├── CraftProf        Float     制作
│   │   ├── MedicalProf      Float     医疗
│   │   ├── ResearchProf     Float     研究
│   │   ├── TradeProf        Float     交易
│   │   ├── StealthProf      Float     潜行
│   │   ├── SurvivalProf     Float     生存
│   │   ├── LockpickingProf  Float     开锁
│   │   ├── TrapProf         Float     陷阱
│   │   ├── FishingProf      Float     钓鱼 (0-100 累积型)
│   │   └── ButcheringProf   Float     屠宰 (0-100 累积型)
│   ├── Needs/
│   │   ├── Morale           Float     士气
│   │   ├── Sleepiness       Float     困意
│   │   ├── Comfort          Float     舒适度
│   │   ├── SocialNeed       Float     社交需求
│   │   ├── Boredom          Float     无聊度
│   │   └── Hygiene           Float     卫生度 (0=肮脏 100=洁净)
│   ├── Movement/
│   │   ├── CarryWeight      Float     负重上限
│   │   ├── Acceleration     Float     移动加速度
│   │   └── MaxSlopeAngle    Float     最大坡度
│   ├── Body/
│   │   ├── Height                  Float     身高
│   │   ├── ObstacleProbeVertical   Float     障碍探测垂直
│   │   ├── ObstacleProbeDistance   Float     障碍探测距离
│   │   ├── ObstacleMinClimb        Float     最小翻越
│   │   ├── ObstacleMaxClimb        Float     最大翻越
│   │   ├── MaxHeadYaw              Float     头部水平
│   │   └── MaxHeadPitch            Float     头部垂直
│   ├── Resistance/
│   │   ├── BleedResist        Float     流血抗性
│   │   ├── SlashResist        Float     斩击抗性
│   │   ├── PierceResist       Float     穿刺抗性
│   │   ├── BiteResist         Float     咬伤抗性 (物理伤害, 区别于InfectionResist管感染累积)
│   │   ├── BluntResist        Float     钝器抗性
│   │   ├── FractureResist     Float     骨折抗性
│   │   ├── FireResist         Float     火焰抗性
│   │   ├── PoisonResist       Float     中毒抗性
│   │   ├── ColdResist         Float     寒冷抗性
│   │   ├── ShockResist        Float     电击抗性 (对齐 Damage.Elemental.Shock)
│   │   ├── AcidResist         Float     酸蚀抗性
│   │   ├── RadiationResist    Float     辐射抗性
│   │   ├── DiseaseResist      Float     疾病抗性 (对应 Damage.Biological.Disease)
│   │   ├── InfectionResist    Float     感染抗性
│   │   ├── PainResist         Float     疼痛抗性
│   │   └── KnockdownResist    Float     击倒抗性
│   ├── Vision/
│   │   ├── SightRange         Float     视野距离
│   │   ├── NightVision        Float     夜视能力
│   │   └── FlashResist        Float     闪光抗性
│   ├── NPC/
│   │   └── Loyalty            Float     忠诚度
│   └── Slots/                              身体槽位
│       ├── RightHand    Struct<SlotDef>
│       ├── LeftHand     Struct<SlotDef>
│       ├── Head         Struct<SlotDef>
│       ├── Chest        Struct<SlotDef>
│       ├── LeftLeg      Struct<SlotDef>
│       ├── RightLeg     Struct<SlotDef>
│       ├── LeftFoot     Struct<SlotDef>
│       ├── RightFoot    Struct<SlotDef>
│       └── Back         Struct<SlotDef>
│
├── Zombie : Actor                                 丧尸
│   ├── Combat/ATK_Zombie        Float     攻击力
│   ├── Movement/ZombieSpeed     Float     移速
│   ├── Senses/NoiseReact        Float     听觉范围
│   └── Senses/SightRange        Float     视野
│
(Creature, Mutant, Robot — 远期预留)
```

## 设计决策

| 属性 | 决策 | 原因 |
|------|------|------|
| SuffocationResist | 不做 | 窒息为环境机制，非战斗伤害类型 |
| ExplosiveResist | 待追加 | 等 Explosive 进入 Damage 标签树后再补 |
| True / Fall 抗性 | 不做 | True/Fall 为特殊伤害类型，有意不设抗性 |

## 远期

| 维度 | 暂缓原因 |
|------|----------|
| Willpower | 意志力属性，影响精神抗性 / 士气恢复 |
| Fatigue | 疲劳维度，长期体力透支的累积惩罚 |
| MentalHealth / Sanity | 精神健康 / 理智系统，远期深度机制 |
