# StatsTree 层级设计 — 装备 / 建筑 / 工具 / 环境

> 基于 `design/stats-inventory.md` + 硬核生存游戏真实性分析。Actor 族见 `actor-tree-design.md`。
> Armor / Throwable / Ammo 为 stats-inventory.md 未覆盖的新增系统。
> 原则：① Tree 只在 stat 集合有结构性差异时新建；数值变种用 Spawn Config。② 有运行时状态的实体 → StatsTreeSO。

## Tree 家族总览（19 Trees + Spawn Configs）

```
Equipment
├── Weapon
│   ├── WeaponBase                  (3 stats)
│   ├── MeleeWeapon : WeaponBase    (+7 stats = 10)  [Spawn: Blade, Staff, Axe]
│   ├── RangedWeapon : WeaponBase   (+7 stats = 10)  [Spawn: Pistol, Rifle, Shotgun 基型]
│   │   ├── Firearm : RangedWeapon  (+4 stats = 14)
│   │   │   ├── Pistol : Firearm    (+2 stats = 16)  [Spawn: 各型号手枪]
│   │   │   ├── Rifle : Firearm     (+2 stats = 16)  [Spawn: 各型号步枪]
│   │   │   └── Shotgun : Firearm   (+2 stats = 16)  [Spawn: 各型号霰弹枪]
│   │   └── Bow : RangedWeapon      (+3 stats = 13)  (远期)
│   └── Throwable : WeaponBase      (+2 stats = 5)   (远期)
│
├── Ammo
│   ├── AmmoBase                    (9 stats)        [口径/弹种共用]
│   ├── PistolAmmo : AmmoBase       (ov defaults)    [Spawn: 9mm_Standard, 9mm_AP, 9mm_HP, 9mm_Subsonic]
│   ├── RifleAmmo : AmmoBase        (ov defaults)    [Spawn: 5.56_Standard, 5.56_AP, 5.56_HP, 5.56_Subsonic, 5.56_Tracer]
│   └── ShotgunShell : AmmoBase     (ov defaults)    [Spawn: 12ga_Buckshot, 12ga_Slug, 12ga_Breaching]
│
├── Armor
│   ├── ArmorBase                   (9 stats)
│   ├── HeadArmor : ArmorBase       (+2 stats = 11)
│   ├── BodyArmor : ArmorBase       (+2 stats = 11)
│   └── LegArmor : ArmorBase        (+2 stats = 11)
│
└── Tool
    └── ToolBase                    (6 stats)        [Spawn: AxeTool, PickaxeTool, HammerTool, SawTool, HoeTool, KitchenTool]

Building                         (8 stats, 独立根)
Environment                      (4 stats, 独立根, 全局单例)
```

> **Spawn Config**: 同 stat 集合、仅 Default 不同的变种，不建 Tree。格式: `{ baseTree, overrides: { StatId: value } }`。

---

## 一、Weapon 族

### 1.1 WeaponBase（武器根 Tree）

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Base/Durability` | Durability | Bounded | 0 | 500 | 100 | 当前耐久。0=损坏 |
| `Base/MaxDurability` | MaxDurability | Bounded | 1 | 500 | 100 | 最大耐久 |
| `Base/Weight` | Weight | Bounded | 0.1 | 50 | 2 | 重量 (kg) |

### 1.2 MeleeWeapon : WeaponBase

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Combat/ATK` | Melee_ATK | Bounded | 1 | 100 | 15 | 基础攻击力 |
| `Combat/AttackSpeed` | Melee_AttackSpeed | Bounded | 0.5 | 3.0 | 1.0 | 攻击速度倍率 |
| `Combat/CritMulti` | Melee_CritMulti | Bounded | 1.0 | 5.0 | 1.5 | 暴击伤害倍率 |
| `Combat/StunChance` | Melee_StunChance | Bounded | 0 | 100 | 10 | 眩晕概率 (%) |
| `Combat/Knockback` | Melee_Knockback | Bounded | 0 | 10 | 2 | 击退距离 (m) |
| `Combat/Reach` | Reach | Bounded | 0.5 | 3.0 | 1.2 | 攻击距离 (m)。Dagger=0.6 Sword=1.2 Spear=2.5 Staff=2.0 Axe=1.0 |
| `Combat/StaminaCost` | StaminaCost | Bounded | 1 | 30 | 5 | 每次挥击消耗体力 |

**Melee Spawn Configs**（均使用 MeleeWeapon Tree）：

| Config | ATK | Speed | CritMulti | Reach | StaminaCost | StunChance | Knockback | 说明 |
|--------|-----|-------|-----------|-------|-------------|------------|-----------|------|
| **Blade** | 12 | 1.2 | 2.0 | 1.2 | 3 | 5 | 1 | 快/高暴击 |
| **Staff** | 10 | 1.0 | 1.5 | 2.0 | 8 | 15 | 4 | 均衡/长距/高眩晕 |
| **Axe** | 25 | 0.7 | 1.8 | 1.0 | 12 | 5 | 3 | 慢/高伤 |

> 子型号（Dagger/Sword/Machete）是 Blade 的二级 Spawn Config，仅在 Reach/ATK/StaminaCost 上有微调。

### 1.3 RangedWeapon : WeaponBase

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Combat/ATK` | Ranged_ATK | Bounded | 1 | 200 | 20 | 基础伤害（枪械机械能贡献） |
| `Combat/Accuracy` | Ranged_Accuracy | Bounded | 0 | 100 | 70 | 基础命中率 (%) |
| `Combat/ReloadSpeed` | Ranged_ReloadSpeed | Bounded | 0.5 | 3.0 | 1.0 | 换弹速度倍率 |
| `Combat/MagSize` | Ranged_MagSize | Bounded | 1 | 100 | 10 | 弹夹容量 |
| `Ammo/Current` | Ranged_AmmoCount | Bounded | 0 | 500 | 10 | 弹夹内剩余弹药。0=需换弹 |
| `Combat/NoiseRadius` | Ranged_NoiseRadius | Bounded | 5 | 200 | 50 | 击发噪音半径 (m) |
| `Combat/Recoil` | Ranged_Recoil | Bounded | 0 | 100 | 30 | 后坐力。0=无 100=极大 |

### 1.4 Firearm : RangedWeapon

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Combat/FireRate` | Firearm_FireRate | Bounded | 0.5 | 12 | 5 | 每秒射速。0.5=2秒1发 |
| `Combat/MuzzleVelocity` | Firearm_MuzzleVelocity | Bounded | 100 | 1000 | 400 | 枪口初速 (m/s)。弹药 BaseVelocity × 枪管系数 → 最终初速 |
| `Combat/BarrelLength` | BarrelLength | Bounded | 2 | 24 | 6 | 枪管长度 (inch)。影响初速和室内机动性 |
| `Combat/Reliability` | Reliability | Bounded | 10 | 100 | 95 | 单发正常循环概率 (%)。随耐久/污损降低 |

> **MuzzleVelocity 为 Derived**: `MuzzleVelocity = ammo.BaseVelocity × barrelLengthFactor`。BarrelLength 越长初速越高（非线性，16"以上递减）。

**Firearm Spawn Configs**（示例基型）：

| Config | ATK | Acc | Reload | Mag | Noise | Recoil | FireRate | Barrel | Reliability |
|--------|-----|-----|--------|-----|-------|--------|----------|--------|-------------|
| **Pistol_Base** | 15 | 60 | 1.0 | 12 | 40 | 20 | 4 | 4 | 90 |
| **Rifle_Base** | 35 | 85 | 0.7 | 20 | 80 | 35 | 3 | 16 | 95 |
| **Shotgun_Base** | 50 | 40 | 0.5 | 5 | 90 | 50 | 1 | 18 | 85 |

### 1.5 Pistol : Firearm

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Combat/HolsterSpeed` | HolsterSpeed | Bounded | 0.5 | 3.0 | 1.5 | 拔枪/收枪速度倍率 |
| `Combat/HipFirePenalty` | HipFirePenalty | Bounded | 0 | 50 | 15 | 腰射精度惩罚 (%) |

### 1.6 Rifle : Firearm

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Combat/ScopeZoom` | ScopeZoom | Bounded | 1 | 12 | 4 | 瞄准镜倍率 |
| `Combat/AimTime` | AimTime | Bounded | 0.3 | 3.0 | 0.8 | 瞄准时间 (s) |

### 1.7 Shotgun : Firearm

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Combat/PelletCount` | PelletCount | Bounded | 3 | 15 | 8 | 弹丸数量 |
| `Combat/Spread` | Spread | Bounded | 2 | 30 | 12 | 散布角 (度) |

### 1.8 Bow : RangedWeapon（远期）

不继承 Firearm。

| 路径 | Stat | Min | Max | Default | 说明 |
|------|------|-----|-----|---------|------|
| `Combat/DrawSpeed` | DrawSpeed | 0.5 | 3.0 | 1.0 | 拉弓速度倍率 |
| `Combat/ArrowVelocity` | ArrowVelocity | 50 | 300 | 150 | 箭矢速度 (m/s) |
| `Combat/HoldStamina` | HoldStamina | 0 | 20 | 5 | 满弓时每秒消耗体力 |

### 1.9 Throwable : WeaponBase（远期）

| 路径 | Stat | Min | Max | Default | 说明 |
|------|------|-----|-----|---------|------|
| `Combat/BlastRadius` | BlastRadius | 0 | 20 | 5 | 爆炸/效果半径 (m) |
| `Combat/FuseTime` | FuseTime | 0.5 | 10 | 3 | 引信时间 (s) |

---

## 二、Ammo 族

口径决定基础弹道特性，弹种在此基础上微调。口径兼容性由物品系统管理。

### 2.1 AmmoBase（弹药根 Tree）

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Combat/BaseDamage` | BaseDamage | Bounded | 1 | 100 | 15 | 弹头基础伤害 |
| `Combat/Penetration` | Penetration | Bounded | 0 | 20 | 2 | 穿透值。与目标 Armor 对抗 |
| `Combat/NoiseRadius` | NoiseRadius | Bounded | 5 | 200 | 50 | 击发噪音半径 (m) |
| `Base/Weight` | Weight | Bounded | 0.001 | 0.1 | 0.01 | 单发重量 (kg) |
| `Combat/BulletWeight` | BulletWeight | Bounded | 20 | 800 | 115 | 弹头重量 (grain)。重弹=高穿透高后座；轻弹=高速低穿透 |
| `Combat/OverPenetration` | OverPenetration | Bounded | 0 | 100 | 30 | 穿透目标/掩体倾向。0=停靶 100=穿多目标 |
| `Combat/RecoilFactor` | RecoilFactor | Bounded | 0.5 | 2.0 | 1.0 | 后座倍率。+P=1.3 亚音速=0.7 |
| `Combat/Reliability` | AmmoReliability | Bounded | 50 | 100 | 100 | 击发可靠性 (%)。旧弹/手装弹<100 |
| `Combat/FoulingRate` | FoulingRate | Bounded | 0 | 5.0 | 1.0 | 枪管污损倍率。腐蚀弹=3.0+ |

### 2.2 口径基线（继承 AmmoBase，仅覆盖 Default）

| Tree | BaseDamage | Penetration | NoiseRadius | Weight | BulletWeight | 兼容武器 |
|------|-----------|-------------|-------------|--------|-------------|----------|
| **PistolAmmo** (9mm) | 15 | 2 | 40 | 0.01 | 115 | Pistol |
| **RifleAmmo** (5.56mm) | 35 | 8 | 80 | 0.012 | 62 | Rifle |
| **ShotgunShell** (12ga) | 50 | 4 | 90 | 0.04 | 438 | Shotgun |

### 2.3 弹种 Spawn Configs（不建 Tree）

**9mm 弹种**（基: PistolAmmo）：

| Config | BaseDmg | Pen | Noise | BulletWt | OverPen | Recoil | Reliability | Fouling | 说明 |
|--------|---------|-----|-------|----------|---------|--------|-------------|---------|------|
| **9mm_Standard** | 15 | 2 | 40 | 115 | 45 | 1.0 | 100 | 1.0 | FMJ 标准 |
| **9mm_AP** | 12 | 5 | 45 | 124 | 85 | 1.1 | 95 | 1.2 | 穿甲 |
| **9mm_HP** | 22 | 1 | 40 | 147 | 10 | 1.2 | 98 | 1.0 | 空尖 |
| **9mm_Subsonic** | 13 | 2 | 20 | 147 | 35 | 0.7 | 98 | 1.5 | 亚音速 |

**5.56mm 弹种**（基: RifleAmmo）：

| Config | BaseDmg | Pen | Noise | BulletWt | OverPen | Recoil | Reliability | Fouling | 说明 |
|--------|---------|-----|-------|----------|---------|--------|-------------|---------|------|
| **5.56_Standard** | 35 | 8 | 80 | 62 | 60 | 1.0 | 100 | 1.0 | M855 标准 |
| **5.56_AP** | 28 | 14 | 85 | 62 | 95 | 1.1 | 95 | 1.3 | M995 穿甲 |
| **5.56_HP** | 50 | 4 | 80 | 55 | 15 | 0.9 | 98 | 1.0 | 空尖 |
| **5.56_Subsonic** | 30 | 8 | 35 | 77 | 50 | 0.7 | 95 | 1.8 | 亚音速重弹 |
| **5.56_Tracer** | 33 | 8 | 85 | 62 | 55 | 1.0 | 98 | 1.5 | 曳光。+5% HitRate (extra modifier) |

**12ga 弹种**（基: ShotgunShell）：

| Config | BaseDmg | Pen | Noise | BulletWt | OverPen | Recoil | Reliability | Fouling | 独有 |
|--------|---------|-----|-------|----------|---------|--------|-------------|---------|------|
| **12ga_Buckshot** | 50 | 4 | 90 | 438 | 30 | 1.0 | 100 | 1.0 | 8 弹丸散布 |
| **12ga_Slug** | 75 | 10 | 95 | 438 | 70 | 1.5 | 100 | 1.0 | 独头弹 |
| **12ga_Breaching** | 30 | 15 | 100 | 438 | 20 | 1.3 | 95 | 2.0 | 对 Building ×3 |

> **伤害公式**: `finalDamage = (weapon.ATK + ammo.BaseDamage) × attributeMods`。武器 ATK = 枪械机械能（枪管/膛线），弹药 BaseDamage = 弹头动能。

---

## 三、Armor 族

### 3.1 ArmorBase（防具根 Tree）

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Base/Durability` | Durability | Bounded | 0 | 500 | 100 | 当前耐久 |
| `Base/MaxDurability` | MaxDurability | Bounded | 1 | 500 | 100 | 最大耐久 |
| `Base/Weight` | Weight | Bounded | 0.1 | 30 | 3 | 重量 (kg) |
| `Combat/DEF` | DEF | Bounded | 0 | 100 | 10 | 基础防御值 |
| `Combat/Coverage` | Coverage | Bounded | 10 | 100 | 60 | 防护面积 (%)。未覆盖部位 DEF=0 |
| `Combat/TraumaTransfer` | TraumaTransfer | Bounded | 0 | 100 | 50 | 冲击传导 (%)。被挡住时仍受 `伤害×Trauma%` 的钝伤 |
| `Combat/MoveSpeedPenalty` | MoveSpeedPenalty | Bounded | 0 | 50 | 10 | 移速降低 (%) |
| `Combat/StaminaRegenPenalty` | StaminaRegenPenalty | Bounded | 0 | 50 | 10 | 体力恢复速度降低 (%) |
| `Combat/NoiseGenBonus` | NoiseGenBonus | Bounded | 0 | 30 | 5 | 移动时额外噪音。链甲=15 软甲=3 |

> **Armor 变种均建 Tree**（HeadArmor/BodyArmor/LegArmor 各有新增 stat，非数值变种）。

### 3.2 HeadArmor : ArmorBase

| 路径 | Stat | Min | Max | Default | 说明 |
|------|------|-----|-----|---------|------|
| `Bonus/FlashResist` | FlashResistBonus | 0 | 50 | 20 | 闪光抗性加成 |
| `Bonus/NightVision` | NightVisionBonus | 0 | 50 | 10 | 夜视加成 |

### 3.3 BodyArmor : ArmorBase

| 路径 | Stat | Min | Max | Default | 说明 |
|------|------|-----|-----|---------|------|
| `Bonus/KnockdownResist` | KnockdownBonus | 0 | 50 | 15 | 击倒抗性加成 |
| `Bonus/CarryWeight` | CarryWeightBonus | 0 | 50 | 10 | 负重加成 (kg) |

### 3.4 LegArmor : ArmorBase

| 路径 | Stat | Min | Max | Default | 说明 |
|------|------|-----|-----|---------|------|
| `Bonus/MoveSpeed` | MoveSpeedBonus | 0 | 30 | 10 | 移速加成 (%) |
| `Bonus/SneakSpeed` | SneakSpeedBonus | 0 | 30 | 5 | 潜行速度加成 (%) |

**Armor Spawn Configs（示例）**:

| Config | DEF | Coverage | Trauma | Weight | 独有 Bonus | 说明 |
|--------|-----|----------|--------|--------|-----------|------|
| **RiotHelmet** : HeadArmor | 5 | 70 | 60 | 2 | Flash+30 | 防暴头盔 |
| **BallisticMask** : HeadArmor | 8 | 40 | 40 | 1 | NV+20 | 防弹面具 |
| **PlateCarrier** : BodyArmor | 20 | 60 | 30 | 5 | Knockdown+20 Carry+15 | 插板背心 |
| **Chainmail** : BodyArmor | 25 | 80 | 70 | 8 | Knockdown+10 | 锁子甲 |
| **CombatBoots** : LegArmor | 8 | 50 | 50 | 2 | Move+15 Sneak+5 | 战斗靴 |
| **Greaves** : LegArmor | 12 | 30 | 60 | 5 | Move+5 | 胫甲 |

---

## 四、Tool 族

### 4.1 ToolBase（工具根 Tree）

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Base/Durability` | Durability | Bounded | 0 | 300 | 80 | 当前耐久 |
| `Base/MaxDurability` | MaxDurability | Bounded | 1 | 300 | 80 | 最大耐久 |
| `Base/Weight` | Weight | Bounded | 0.1 | 20 | 2 | 重量 (kg) |
| `Work/Efficiency` | Efficiency | Bounded | 0.1 | 3.0 | 1.0 | 工作效率倍率 |
| `Work/MaterialTier` | MaterialTier | Bounded | 1 | 6 | 1 | 1=石 2=铜 3=铁 4=钢 5=合金 6=碳化物。影响效率乘数+可采集资源等级 |
| `Work/StaminaCost` | StaminaCostPerUse | Bounded | 0.5 | 10 | 2 | 每次使用消耗体力 |

**Tool Spawn Configs**（均使用 ToolBase）：

| Config | Efficiency | MaterialTier | Weight | MaxDur | StaminaCost | 用途 |
|--------|-----------|-------------|--------|--------|-------------|------|
| **AxeTool** | 1.0 | 1 | 3 | 100 | 3 | 伐木 |
| **PickaxeTool** | 1.0 | 1 | 4 | 120 | 5 | 采矿 |
| **HammerTool** | 1.0 | 1 | 2 | 150 | 2 | 建造/修理 |
| **SawTool** | 1.0 | 1 | 2 | 100 | 2 | 木材加工 |
| **HoeTool** | 1.0 | 1 | 2 | 80 | 2 | 耕地 |
| **KitchenTool** | 0.8 | 1 | 1 | 60 | 1 | 烹饪 |

> MaterialTier 升级时需要新的 Spawn Config（如 IronAxe 使用 MaterialTier=3, Efficiency=1.3 等）。

---

## 五、Building（独立根）

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Vitals/Durability` | Durability | Bounded | 0 | 500 | 200 | 当前耐久。0=摧毁 |
| `Vitals/MaxDurability` | MaxDurability | Bounded | 1 | 500 | 200 | 最大耐久 |
| `Combat/DEF` | Defence | Bounded | 0 | 100 | 20 | 减伤值 |
| `Combat/MaterialType` | MaterialType | Bounded | 0 | 4 | 0 | 0=木 1=石 2=金属 3=混凝土 4=复合。决定耐久曲线/修理方式 |
| `Combat/Flammability` | Flammability | Bounded | 0 | 100 | 50 | 可燃性。0=防火 100=速燃。Derived from MaterialType |
| `Combat/SoundDampening` | SoundDampening | Bounded | 0 | 100 | 20 | 隔音 (%)。石=80 木=20 金属=10(共振) |
| `Vitals/WeatherResist` | WeatherResistance | Bounded | 0 | 100 | 50 | 耐候性。0=雨中腐烂 100=完全防水 |
| `Work/Speed` | WorkSpeed | Bounded | 0.1 | 3.0 | 1.0 | 设施工作效率 |

> **RepairCost** = Derived: `DamageRatio × MaterialCost`。
> 不同建筑类型（围墙/哨塔/工坊/农田）均为 Spawn Config。

---

## 六、Environment（全局单例，远期）

| 路径 | Stat | 类型 | Min | Max | Default | 说明 |
|------|------|------|-----|-----|---------|------|
| `Atmosphere/FogDensity` | FogDensity | Bounded | 0 | 100 | 0 | 雾浓度 |
| `Atmosphere/Temperature` | Temperature | Bounded | -30 | 50 | 20 | 气温 (°C)。影响 BodyTemp 漂移 |
| `Atmosphere/Humidity` | Humidity | Bounded | 0 | 100 | 50 | 湿度 |
| `Time/TimeOfDay` | TimeOfDay | Bounded | 0 | 24 | 8 | 时间。由 TimeService 写入 |

---

## 七、全部汇总

| 家族 | Trees | Stats | Spawn Configs | 继承深度 |
|------|-------|-------|---------------|---------|
| Actor | 6 | 64 | 14 | 0→2 |
| Weapon | 9 | 49 | 3+ (Melee/Ranged/Firearm variants) | 0→4 |
| Ammo | 4 | 9 | 12 | 0→1 |
| Armor | 4 | 14 | 6+ | 0→1 |
| Tool | 1 | 6 | 6 | 0 |
| Building | 1 | 8 | N (per building type) | 0 |
| Environment | 1 | 4 | — | 0 |
| **合计** | **26** | **154** | **41+** | |

### 完整 Stat 清单（154 个）

| 类别 | 数量 | 涵盖 |
|------|------|------|
| Actor 共用 | 2 | HP, MaxHP |
| Human 独有 | 56 | Vitals+9, Attributes+6, Prof+21, Needs+5, Movement+2, Resist+9, Vision+3, NPC+1 |
| Zombie 独有 | 4 | ATK, Speed, NoiseReact, SightRange |
| WeaponBase | 3 | Durability, MaxDurability, Weight |
| MeleeWeapon | 7 | ATK, AttackSpeed, CritMulti, StunChance, Knockback, Reach, StaminaCost |
| RangedWeapon | 7 | ATK, Accuracy, ReloadSpeed, MagSize, AmmoCount, NoiseRadius, Recoil |
| Firearm | 4 | FireRate, MuzzleVelocity, BarrelLength, Reliability |
| Pistol | 2 | HolsterSpeed, HipFirePenalty |
| Rifle | 2 | ScopeZoom, AimTime |
| Shotgun | 2 | PelletCount, Spread |
| Bow | 3 | DrawSpeed, ArrowVelocity, HoldStamina |
| Throwable | 2 | BlastRadius, FuseTime |
| AmmoBase | **9** | BaseDamage, Penetration, NoiseRadius, Weight, BulletWeight, OverPenetration, RecoilFactor, AmmoReliability, FoulingRate |
| ArmorBase | **9** | Durability, MaxDurability, Weight, DEF, Coverage, TraumaTransfer, MoveSpeedPenalty, StaminaRegenPenalty, NoiseGenBonus |
| HeadArmor | 2 | FlashResistBonus, NightVisionBonus |
| BodyArmor | 2 | KnockdownBonus, CarryWeightBonus |
| LegArmor | 2 | MoveSpeedBonus, SneakSpeedBonus |
| ToolBase | **6** | Durability, MaxDurability, Weight, Efficiency, MaterialTier, StaminaCostPerUse |
| Building | **8** | Durability, MaxDurability, DEF, MaterialType, Flammability, SoundDampening, WeatherResist, WorkSpeed |
| Environment | 4 | FogDensity, Temperature, Humidity, TimeOfDay |
| **合计** | **154** | |

> 118 → 154（+36）。主要增量: Ammo +5, Armor +5, Melee +2, Firearm +2, Building +4, Tool +2。
> 26 Trees（砍掉 25 个 Numerical Variant Trees）+ 41+ Spawn Configs。

---

## 八、装备 → Actor 桥接

装备 Tree 的 stat 通过 **StatModifier** 作用于 Actor：

```
Player 装备 IronSword (Blade Spawn Config → MeleeWeapon Tree 实例)
  → OnEquip()
    → stats.Get("Combat/ATK").AddModifier(sword, ATK.Current 作为 Addend)
    → stats.Get("Combat/Reach").AddModifier(sword, Reach.Current 作为 Addend)
    → stats.Get("Combat/StaminaCost").AddModifier(sword, ...)

Player 装填 9mm_AP (PistolAmmo Tree 实例)
  → OnReload(ammo)
    → weapon.AddModifier(ammo, BaseDamage + Penetration + OverPenetration...)
    → 击发 → Ammo/Current -= 1 → ammo 消耗 → Modifier 自动回收
```

装备卸下/弹药耗尽时 `RemoveByOwner(owner)` 回收全部 Modifier。
