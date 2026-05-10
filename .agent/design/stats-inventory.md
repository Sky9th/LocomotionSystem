# 全量 Stats 结构树

> 日期: 2026-05-11
> 用途: 设计 SO 管理方案的结构化参考
> 关联: `stats-system.md`
> 能力标注: Consumable = IStatConsumable, Cumulative = IStatCumulative, Derived = IStatDerived
> 未标注 = 纯 Min/Max/Default（无特殊接口）

```
Game Stats
│
├── Character — 角色 (83)
│   │
│   ├── Vitals — 生命体征 (11)
│   │   ├── HP                      生命值                Bounded
│   │   ├── MaxHP                   最大生命值             Bounded
│   │   ├── Hunger                  饥饿度                Consumable  { → HP }
│   │   ├── Thirst                  口渴度                Consumable  { → HP }
│   │   ├── Stamina                 体力值                Consumable  { 运动↓ / 静止↑ }
│   │   ├── MaxStamina              最大体力值             Bounded
│   │   ├── BodyTemp                体温                  Consumable
│   │   ├── Blood                   血量                  Consumable
│   │   ├── Infection               感染值                Cumulative  { → 阈值惩罚 }
│   │   ├── Consciousness           意识值                Bounded
│   │   └── Pain                    疼痛值                Consumable  { 静止↑ }
│   │
│   ├── Attributes — 基础属性 (6)
│   │   ├── Strength                力量                  Bounded
│   │   ├── Agility                 敏捷                  Bounded
│   │   ├── Endurance               体质                  Bounded
│   │   ├── Intelligence            智力                  Bounded
│   │   ├── Perception              感知                  Bounded
│   │   └── Charisma                魅力                  Bounded
│   │
│   ├── Proficiency — 熟练度 (16)
│   │   ├── Combat — 战斗 (6)
│   │   │   ├── BladeProf           刀术熟练度            Cumulative  { → Level }
│   │   │   ├── AxeProf             斧术熟练度            Cumulative
│   │   │   ├── StaffProf           棍术熟练度            Cumulative
│   │   │   ├── PistolProf          手枪熟练度            Cumulative
│   │   │   ├── ShotgunProf         霰弹枪熟练度           Cumulative
│   │   │   └── RifleProf           步枪熟练度            Cumulative
│   │   │
│   │   └── Work — 工作 (10)
│   │       ├── CookProf            烹饪熟练度            Cumulative
│   │       ├── FarmProf            耕种熟练度            Cumulative
│   │       ├── BuildProf           建造熟练度            Cumulative
│   │       ├── GatherProf          采集熟练度            Cumulative
│   │       ├── CraftProf           制作熟练度            Cumulative
│   │       ├── MedicalProf         医疗熟练度            Cumulative
│   │       ├── ResearchProf        研究熟练度            Cumulative
│   │       ├── TradeProf           交易熟练度            Cumulative
│   │       ├── StealthProf         潜行熟练度            Cumulative
│   │       └── SurvivalProf        生存熟练度            Cumulative
│   │
│   ├── Combat — 战斗属性 (8)
│   │   │ * 具体数值由装备的 Weapon Stats + Character Attributes + Proficiency 派生
│   │   ├── ATK                     攻击力                Derived  { ← 武器.Stats }
│   │   ├── DEF                     防御力                Derived  { ← 护甲.Stats }
│   │   ├── HitRate                 命中率                Derived  { ← Perception, Prof }
│   │   ├── DodgeRate               闪避率                Derived  { ← Agility }
│   │   ├── Penetration             破防率                Derived  { ← 武器-护甲克制 }
│   │   ├── CritRate                暴击率                Derived  { ← Perception, Prof }
│   │   ├── AttackSpeed             攻击速度              Derived  { ← 武器.Stats }
│   │   └── CombatNoise             战斗噪音              Derived  { ← 武器.Stats, Movement }
│   │
│   ├── Movement — 移动属性 (7)
│   │   ├── MoveSpeed               移动速度              Derived  { ← Agility, 负重, 姿态 }
│   │   ├── SprintSpeed             冲刺速度              Derived  { ← MoveSpeed }
│   │   ├── SprintCost              冲刺消耗              Derived  { ← Stamina }
│   │   ├── JumpPower               跳跃力                Derived  { ← Strength, Stamina% }
│   │   ├── SneakSpeed              潜行速度              Derived  { ← MoveSpeed }
│   │   ├── CarryWeight             负重上限              Bounded
│   │   └── CurrentWeight           当前负重              Bounded
│   │
│   ├── Needs — 生存需求 (5)
│   │   ├── Morale                  士气值                Bounded
│   │   ├── Sleepiness              困意值                Consumable  { 持续↓ }
│   │   ├── Comfort                 舒适度                Bounded
│   │   ├── SocialNeed              社交需求              Bounded
│   │   └── Boredom                 无聊度                Consumable  { 持续↓ / 娱乐↑ }
│   │
│   ├── Resistance — 抗性 (8)
│   │   ├── BleedResist             流血抗性              Bounded
│   │   ├── PoisonResist            中毒抗性              Bounded
│   │   ├── FireResist              火焰抗性              Bounded
│   │   ├── ColdResist              寒冷抗性              Bounded
│   │   ├── ElectricResist          电击抗性              Bounded
│   │   ├── RadiationResist         辐射抗性              Bounded
│   │   ├── PainResist              疼痛抗性              Bounded
│   │   └── KnockdownResist         击倒抗性              Bounded
│   │
│   ├── Vision — 视觉属性 (4)
│   │   ├── SightRange              视野距离              Bounded
│   │   ├── NightVision             夜视能力              Bounded
│   │   ├── StealthDetect           潜行侦测              Derived  { ← Perception, StealthProf }
│   │   └── FlashResist             闪光抗性              Bounded
│   │
│   ├── Derived — 派生综合 (5)
│   │   ├── HealingRate             恢复速率              Derived  { ← MedicalProf, 休息, 营养 }
│   │   ├── StarvationRate          饥饿加速              Derived  { ← BodyTemp, Infection }
│   │   ├── CombatPower             战力评估              Derived  { ← ATK×HR×CR×AS }
│   │   ├── StealthRating           潜行评级              Derived  { ← StealthProf, Noise, Vision }
│   │   └── SurvivalRating          生存评级              Derived  { 综合 }
│   │
│   └── NPC — 角色专属 (2)
│       ├── Loyalty                 忠诚度                Cumulative  { → Level }
│       └── WorkEfficiency          工作效率              Derived  { ← Morale, Prof }
│
├── Weapon — 武器
│   │
│   └── WeaponBase — 所有武器共用 (3)
│       ├── Weapon_Durability        当前耐久度            Bounded
│       ├── Weapon_MaxDurability     最大耐久度            Bounded
│       └── Weapon_Weight            重量                  Bounded
│   │
│   ├── Melee — 冷兵器 (5)
│   │   │ * 继承 WeaponBase
│   │   ├── Melee_ATK                攻击力                Bounded
│   │   ├── Melee_AttackSpeed        攻击速度              Bounded
│   │   ├── Melee_CritMulti          暴击倍率              Bounded
│   │   ├── Melee_StunChance         眩晕概率              Bounded
│   │   └── Melee_Knockback          击退距离              Bounded
│   │
│   ├── Ranged — 远程基类 (7)
│   │   │ * 继承 WeaponBase
│   │   ├── Ranged_ATK               伤害值                Bounded
│   │   ├── Ranged_Accuracy          散布精度              Bounded
│   │   ├── Ranged_ReloadSpeed       换弹时间              Bounded
│   │   ├── Ranged_MagSize           弹夹容量              Bounded
│   │   ├── Ranged_AmmoCount         当前弹药              Bounded
│   │   ├── Ranged_NoiseRadius       噪音范围              Bounded
│   │   └── Ranged_Recoil            后坐力                Bounded
│   │
│   ├── Firearm — 枪械 (2)
│   │   │ * 继承 WeaponBase + Ranged
│   │   ├── Firearm_FireRate         射速                  Bounded
│   │   └── Firearm_MuzzleVelocity   枪口初速              Bounded
│   │   │
│   │   ├── Pistol — 手枪 (2)
│   │   │   │ * 继承 WeaponBase + Ranged + Firearm
│   │   │   ├── Pistol_HolsterSpeed  拔枪速度              Bounded
│   │   │   └── Pistol_HipFirePenalty 腰射惩罚             Bounded
│   │   │
│   │   ├── Rifle — 步枪 (2)
│   │   │   │ * 继承 WeaponBase + Ranged + Firearm
│   │   │   ├── Rifle_ScopeZoom      瞄准镜倍率            Bounded
│   │   │   └── Rifle_AimTime        瞄准时间              Bounded
│   │   │
│   │   └── Shotgun — 霰弹枪 (2)
│   │       │ * 继承 WeaponBase + Ranged + Firearm
│   │       ├── Shotgun_PelletCount  弹丸数量              Bounded
│   │       └── Shotgun_Spread       散布角                Bounded
│   │
│   └── Bow — 弓/弩 (3)
│       │ * 继承 WeaponBase + Ranged（无 Firearm）
│       ├── Bow_DrawSpeed            拉弓速度              Bounded
│       ├── Bow_ArrowVelocity        箭速                  Bounded
│       └── Bow_HoldStamina          拉弓体力消耗           Bounded
│
├── Building — 建筑 (5)
│   ├── Build_Durability            当前耐久度            Bounded
│   ├── Build_MaxDurability         最大耐久度            Bounded
│   ├── Build_RepairCost            修理消耗              Derived
│   ├── Build_Defence               防御值                Bounded
│   └── Build_WorkSpeed             设施效率              Bounded
│
├── Zombie — 丧尸 (6)
│   ├── Zombie_HP                   生命值                Bounded
│   ├── Zombie_ATK                  攻击力                Bounded
│   ├── Zombie_Speed                移动速度              Bounded
│   ├── Zombie_NoiseReact           听觉范围              Bounded
│   ├── Zombie_SightRange           视觉范围              Bounded
│   └── Zombie_Variant              类型标识              Bounded
│
├── Environment — 环境 (4)
│   ├── Env_FogDensity              雾浓度                Bounded
│   ├── Env_Temperature             气温                  Bounded
│   ├── Env_Humidity                湿度                  Bounded
│   └── Env_TimeOfDay               时间                  Bounded
│
└── Tool — 工具公用 (4)
    ├── Tool_Durability             当前耐久度            Bounded
    ├── Tool_MaxDurability          最大耐久度            Bounded
    ├── Tool_Efficiency             效率系数              Bounded
    └── Tool_Weight                 重量                  Bounded
```
