# Ability Search — 搜索形状资产树

> `L3_Ability/` · 资产清单 · 2026-06-13
>
> 完整 SearchSO 资产树。每个资产定义一个搜索形状，技能通过 AbilityDefSO.search 引用。
> 设计依据：[ability-pipeline-design.md](ability-pipeline-design.md) 维度④ + [ability-inventory.md](ability-inventory.md) 154 技能全量。

---

## JSON Schema

### AbilitySearchSO（基类，各子类共享）

```json
{
  "searchType": "Cone | RayLine | Circle",
  "range": 0.0,
  "targetMask": -1,
  "maxTargets": 0,
  "targetFilter": "Any"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `searchType` | enum | Cone=0, RayLine=1, Circle=2。子类 OnEnable 自设 |
| `range` | float | Cone=锥长, RayLine=射线长, Circle=半径 (m) |
| `targetMask` | int | 物理层遮罩位标记，-1=Everything |
| `maxTargets` | int | 最大命中目标数，≤0=无限制 |
| `targetFilter` | enum | Any=0, Enemy=1, Friendly=2, Self=3 |

### ConeSearchSO : AbilitySearchSO

```json
{
  "searchType": "Cone",
  "range": 2.0,
  "targetMask": -1,
  "maxTargets": 5,
  "targetFilter": "Enemy",
  "angle": 60.0
}
```

| 额外字段 | 类型 | 说明 |
|---------|------|------|
| `angle` | float | 扇形全角 (0-360°)。目标在 angle/2 半角内命中 |

### RaySearchSO : AbilitySearchSO

```json
{
  "searchType": "RayLine",
  "range": 12.0,
  "targetMask": -1,
  "maxTargets": 1,
  "targetFilter": "Enemy",
  "requiresLineOfSight": true
}
```

| 额外字段 | 类型 | 说明 |
|---------|------|------|
| `requiresLineOfSight` | bool | 目标与攻击者间不能有遮挡 |

### CircleSearchSO : AbilitySearchSO

```json
{
  "searchType": "Circle",
  "range": 4.0,
  "targetMask": -1,
  "maxTargets": 0,
  "targetFilter": "Enemy"
}
```

> 无额外字段。range=半径。

---

## 完整资产清单

### 文件路径

```
Assets/Data/Ability/Searches/
├── Cone/                          # 扇形搜索 — 横斩、霰弹、盾墙
├── Ray/                           # 射线搜索 — 刺击、枪械、暗杀
├── Circle/                        # 圆形搜索 — AoE、投掷物、陷阱触发
└── Line/                          # 线段搜索 Phase 4.2+
```

---

### 一、Cone 扇形 (~16 资产)

```json
[
  {
    "id": "Search_Cone_Melee_Blade_Light",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 2.0,
    "angle": 60.0,
    "targetMask": -1,
    "maxTargets": 3,
    "targetFilter": "Enemy",
    "usedBy": ["刀·轻击", "剑道·基础轻击"]
  },
  {
    "id": "Search_Cone_Melee_Staff_Light",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 2.5,
    "angle": 90.0,
    "targetMask": -1,
    "maxTargets": 5,
    "targetFilter": "Enemy",
    "usedBy": ["棍·轻击", "菲律宾魔杖·轻击"]
  },
  {
    "id": "Search_Cone_Melee_Axe_Light",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 2.0,
    "angle": 40.0,
    "targetMask": -1,
    "maxTargets": 2,
    "targetFilter": "Enemy",
    "usedBy": ["斧·轻击"]
  },
  {
    "id": "Search_Cone_Melee_Fist_Light",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 1.8,
    "angle": 60.0,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["空手轻击 (拳击/八极拳/泰拳/咏春/太极/散打 基础轻击)"]
  },
  {
    "id": "Search_Cone_Melee_MiaoDao_Final_BreakingRanks",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 3.0,
    "angle": 150.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["苗刀术·绝学 破阵斩"]
  },
  {
    "id": "Search_Cone_Melee_Baji_Final_TigerClimb",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 2.5,
    "angle": 120.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["八极拳·绝学 猛虎硬爬山"]
  },
  {
    "id": "Search_Cone_Melee_TaiChi_Final_Seal",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 4.0,
    "angle": 180.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["太极拳·绝学 如封似闭"]
  },
  {
    "id": "Search_Cone_Melee_TaiChi_WardOff",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 3.0,
    "angle": 120.0,
    "targetMask": -1,
    "maxTargets": 5,
    "targetFilter": "Enemy",
    "usedBy": ["太极拳·完整 揽雀尾·发劲"]
  },
  {
    "id": "Search_Cone_Melee_KravMaga_Final_Desperation",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 2.0,
    "angle": 60.0,
    "targetMask": -1,
    "maxTargets": 3,
    "targetFilter": "Enemy",
    "usedBy": ["以色列格斗术·绝学 绝境反杀"]
  },
  {
    "id": "Search_Cone_Melee_Sanda_WhipKick",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 3.0,
    "angle": 90.0,
    "targetMask": -1,
    "maxTargets": 3,
    "targetFilter": "Enemy",
    "usedBy": ["散打·完整 鞭腿"]
  },
  {
    "id": "Search_Cone_Ranged_Shotgun_Normal",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 5.0,
    "angle": 50.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["霰弹枪·普通射击"]
  },
  {
    "id": "Search_Cone_Ranged_Shotgun_CloseQuarters",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 4.0,
    "angle": 40.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["霰弹枪·抵近射击"]
  },
  {
    "id": "Search_Cone_Ranged_SuppressiveBlast",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 6.0,
    "angle": 70.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["霰弹枪·火力压制"]
  },
  {
    "id": "Search_Cone_Defensive_ShieldWall",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 2.0,
    "angle": 120.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["防御·盾墙 (蓄力60°→120°)"]
  },
  {
    "id": "Search_Cone_Trap_TripMine",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 3.0,
    "angle": 60.0,
    "targetMask": -1,
    "maxTargets": 3,
    "targetFilter": "Enemy",
    "usedBy": ["陷阱·绊雷 (定向爆破)"]
  },
  {
    "id": "Search_Cone_Melee_Kendo_Final_Iai",
    "path": "Cone/",
    "searchType": "Cone",
    "range": 4.0,
    "angle": 30.0,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["剑道·绝学 一之太刀 (超窄高伤)"]
  }
]
```

---

### 二、Ray 射线 (~22 资产)

```json
[
  {
    "id": "Search_Ray_Melee_Blade_Heavy",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 2.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["刀·重击"]
  },
  {
    "id": "Search_Ray_Melee_Staff_Heavy",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 3.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["棍·重击"]
  },
  {
    "id": "Search_Ray_Melee_Axe_Heavy",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 3.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["斧·重击"]
  },
  {
    "id": "Search_Ray_Melee_PushKick",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["基本功·脚踢"]
  },
  {
    "id": "Search_Ray_Melee_Boxing_Uppercut",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["拳击·终结 KO右直拳", "拳击·爆肝拳"]
  },
  {
    "id": "Search_Ray_Melee_MuayThai_Elbow",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["泰拳·肘击 (近身毁灭)"]
  },
  {
    "id": "Search_Ray_Melee_MuayThai_FlyingKnee",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 3.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["泰拳·精妙 飞膝"]
  },
  {
    "id": "Search_Ray_Melee_Baji_ShoulderBarge",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 2.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["八极拳·完整 贴山靠"]
  },
  {
    "id": "Search_Ray_Melee_Baji_CrushingFist",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["八极拳·精妙 崩拳"]
  },
  {
    "id": "Search_Ray_Melee_WingChun_InchForce",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["咏春·完整 寸劲"]
  },
  {
    "id": "Search_Ray_Melee_WingChun_BiuJee",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["咏春·绝学 标指"]
  },
  {
    "id": "Search_Ray_Melee_Judo_SeoiNage",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["柔道·完整 背负投 (单体投技)"]
  },
  {
    "id": "Search_Ray_Melee_Judo_KesaGatame",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["柔道·精妙 袈裟固 (地面压制)"]
  },
  {
    "id": "Search_Ray_Melee_Eskrima_TempleStrike",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 2.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["菲律宾魔杖·完整 太阳穴打击"]
  },
  {
    "id": "Search_Ray_Melee_Eskrima_Disarm",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["菲律宾魔杖·精妙 缴械斩"]
  },
  {
    "id": "Search_Ray_Melee_Kendo_Tsuki",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 3.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["剑道·完整 突刺 Tsuki (必定暴击)"]
  },
  {
    "id": "Search_Ray_Melee_Kendo_KoteGiri",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["剑道·精妙 小手切"]
  },
  {
    "id": "Search_Ray_Melee_MiaoDao_PiShan",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 3.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["苗刀术·精妙 力劈华山 (单体满血加成)"]
  },
  {
    "id": "Search_Ray_Melee_Sanda_SideKick",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 3.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["散打·绝学 侧踹 (不可格挡)"]
  },
  {
    "id": "Search_Ray_Ranged_Pistol_Normal",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 12.0,
    "requiresLineOfSight": true,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["手枪·普通射击", "手枪·快速拔枪(10m)", "手枪·移动射击"]
  },
  {
    "id": "Search_Ray_Ranged_Rifle_Normal",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 25.0,
    "requiresLineOfSight": true,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["步枪·普通射击", "步枪·快速补射"]
  },
  {
    "id": "Search_Ray_Ranged_Rifle_Sniping",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 50.0,
    "requiresLineOfSight": true,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["步枪·远程狙击"]
  },
  {
    "id": "Search_Ray_Ranged_Rifle_BreathControl",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 30.0,
    "requiresLineOfSight": true,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["步枪·屏息精瞄"]
  },
  {
    "id": "Search_Ray_Throw_Knife",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 15.0,
    "requiresLineOfSight": true,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["投掷·飞刀"]
  },
  {
    "id": "Search_Ray_Stealth_Assassination",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["潜行·暗杀"]
  },
  {
    "id": "Search_Ray_Stealth_Blackjack",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["潜行·闷棍"]
  },
  {
    "id": "Search_Ray_Defensive_ShieldBash",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["防御·盾牌猛击"]
  },
  {
    "id": "Search_Ray_Defensive_Parry",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 2.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["防御·招架反击"]
  },
  {
    "id": "Search_Ray_Defensive_CoveringFire",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 15.0,
    "requiresLineOfSight": true,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["防御·掩护射击"]
  },
  {
    "id": "Search_Ray_Melee_KravMaga_Disarm",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["以色列格斗术·完整 缴械反击"]
  },
  {
    "id": "Search_Ray_Melee_KravMaga_Vitals",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["以色列格斗术·精妙 要害打击"]
  },
  {
    "id": "Search_Ray_Shotgun_StockStrike",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 1.5,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["霰弹枪·枪托挥击"]
  },
  {
    "id": "Search_Ray_Stealth_DistractionWhistle",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 25.0,
    "requiresLineOfSight": true,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Any",
    "usedBy": ["潜行·引诱哨 (瞄准点制造噪音)"]
  },
  {
    "id": "Search_Ray_Melee_Sanda_Takedown",
    "path": "Ray/",
    "searchType": "RayLine",
    "range": 2.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["散打·精妙 接腿摔"]
  }
]
```

---

### 三、Circle 圆形 (~16 资产)

```json
[
  {
    "id": "Search_Circle_Throw_Molotov",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 4.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["投掷·燃烧瓶 (含地面燃烧区8s)"]
  },
  {
    "id": "Search_Circle_Throw_FragGrenade",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 5.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["投掷·破片手雷 (2s引信)"]
  },
  {
    "id": "Search_Circle_Throw_SmokeGrenade",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 5.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["投掷·烟雾弹 (丧尸视野归零+减速)"]
  },
  {
    "id": "Search_Circle_Throw_Flashbang",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 4.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["投掷·震撼弹 (致盲4s+减速6s)"]
  },
  {
    "id": "Search_Circle_Throw_AcidVial",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 3.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["投掷·酸液瓶 (酸性DoT+减甲)"]
  },
  {
    "id": "Search_Circle_Throw_PoisonGas",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 5.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["投掷·毒气瓶 (12s云团扩散至6m)"]
  },
  {
    "id": "Search_Circle_Throw_BaitBottle",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 8.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["投掷·诱饵瓶 (噪音+气味标记8s)"]
  },
  {
    "id": "Search_Circle_Trap_BearTrap",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 1.0,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["陷阱·捕兽夹 (Bleed DoT+定身4s)"]
  },
  {
    "id": "Search_Circle_Trap_GasTrap",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 4.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["陷阱·毒气陷阱 (Poison DoT 10s)"]
  },
  {
    "id": "Search_Circle_Trap_ShockTrap",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 2.0,
    "targetMask": -1,
    "maxTargets": 3,
    "targetFilter": "Enemy",
    "usedBy": ["陷阱·电击陷阱 (眩晕+2m连锁)"]
  },
  {
    "id": "Search_Circle_Trap_AlarmTrap",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 12.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["陷阱·警报器 (噪音6级, 非伤害)"]
  },
  {
    "id": "Search_Circle_Trap_PitfallSpike",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 1.0,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["陷阱·地刺 (非Boss即死)"]
  },
  {
    "id": "Search_Circle_Trap_OilSlick",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 3.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["陷阱·油滑陷阱 (滑倒+减速)"]
  },
  {
    "id": "Search_Circle_Trap_NoiseDecoy",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 10.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["陷阱·噪音诱饵 (噪音6级+灯光8s)"]
  },
  {
    "id": "Search_Circle_Melee_MiaoDao_Whirlwind",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 3.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["苗刀术·完整 回身斩 (360°转身横扫)"]
  },
  {
    "id": "Search_Circle_Defensive_GuardianAura",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 6.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Friendly",
    "usedBy": ["防御·守护光环 (盟友减伤+20%, 自身仇恨+100%)"]
  },
  {
    "id": "Search_Circle_Melee_TaiChi_CloudHands",
    "path": "Circle/",
    "searchType": "Circle",
    "range": 3.0,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Self",
    "usedBy": ["太极拳·精妙 云手·化劲 (自身Buff, 5s自动格挡)"]
  }
]
```

---

### 四、Line 线段 (Phase 4.2+) (~3 资产)

```json
[
  {
    "id": "Search_Line_Trap_RazorWire",
    "path": "Line/",
    "searchType": "RayLine",
    "range": 3.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 0,
    "targetFilter": "Enemy",
    "usedBy": ["陷阱·铁丝网 (持续区, 减速+Slash DoT)"],
    "note": "Phase 4.2+。线段搜索不是独立 SearchSO 子类——用 RayLine + 持续触发 OnEnterArea 模拟。"
  },
  {
    "id": "Search_Line_Trap_Tripwire",
    "path": "Line/",
    "searchType": "RayLine",
    "range": 2.0,
    "requiresLineOfSight": false,
    "targetMask": -1,
    "maxTargets": 1,
    "targetFilter": "Enemy",
    "usedBy": ["陷阱·绊索 (绊倒+硬直, 无伤害)"],
    "note": "Phase 4.2+"
  }
]
```

---

## 汇总

| 形状 | A测闭环 | Phase 2+ | 后期全量 | 说明 |
|------|:------:|:--------:|:------:|------|
| Cone | 2 | 14 | **16** | 横斩/霰弹/盾墙/武学终结技 |
| Ray | 2 | 30 | **32** | 刺击/枪械/暗杀/武学单体 |
| Circle | 1 | 16 | **17** | AoE/投掷物/陷阱/光环 |
| Line | 0 | 2 | **2** | 铁丝网/绊索 (Phase 4.2+) |
| **合计** | **5** | **62** | **67** | |

> **复用原则**：相同参数 (shape+range+angle+filter) 的技能共用同一个 SearchSO。上表已合并同参项，实际新建资产数 ~45 个。

---

## 跨文档索引

| 文档 | 说明 |
|------|------|
| [ability-pipeline-design.md](ability-pipeline-design.md) | 维度④ Search 设计 — 形状、过滤、管道位置 |
| [ability-inventory.md](ability-inventory.md) | 154 技能全量 — 每技能的搜索形状引用 |
| [ability-activation-assets.md](ability-activation-assets.md) | ActivationSO 资产树 — 激活方式与阶段时机 |
| [ability-noise-assets.md](ability-noise-assets.md) | NoiseEventSO 资产树 — 噪音事件与 Tag 依赖 |
