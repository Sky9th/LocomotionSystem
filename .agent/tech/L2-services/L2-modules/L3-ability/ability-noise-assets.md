# Ability Noise — 噪音事件资产树

> `L3_Ability/` · 资产清单 · 2026-06-13
>
> 完整 NoiseEventSO 资产树 + Noise Tag 依赖树。每个资产定义一个噪音事件——技能激活时广播，AI 听觉系统消费。
> 设计依据：[ability-pipeline-design.md](ability-pipeline-design.md) 维度⑧ + [ability-inventory.md](ability-inventory.md) 噪音分级。

---

## 生命周期

```
③ Activation 启动
    │
    └──→ NoiseEventSO 发布 SNoiseEvent { noiseType, level, decayRadius, position }
              │
              └──→ AI 听觉系统订阅 HitEventSO / NoiseChannel
                       │
                       ├── HasTag("Noise.Combat") → 战斗追击行为
                       ├── HasTag("Noise.World")  → 警戒/调查行为
                       └── HasTag("Noise.Alert")  → 高优先级警报行为
```

> Noise 在 ③ 释放时发布（不论是否命中）。⑧ 是唯一使用 EventChannel 的维度。

---

## 一、Noise Tag 依赖树 (17 资产)

> NoiseEventSO.noiseType 引用这些 Tag。必须先于 NoiseEventSO 创建。
> 完整 Tag 树文档: [gameplay-tag.md](../../../L1-core/gameplay-tag.md#5-noise--ai-听觉行为路由)

### JSON Schema — GameplayTagDefinitionSO

```json
{
  "leafName": "WeaponFire",
  "parent": "Tag_Combat",
  "description": "枪械射击声。AI 追击+呼救行为。"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `leafName` | string | 本层级名称片段。由文件名 `Tag_{leafName}` 自动派生 |
| `parent` | ref | 父级 Tag SO。根标签为 null |
| `description` | string | 策划可读描述 |

> `FullTag` 运行时缓存：`parent != null ? $"{parent.FullTag}.{leafName}" : leafName`

### 完整 Tag 树

```
Assets/Data/Core/Tags/Noise/
│
├── Tag_Noise.asset                     leafName="Noise"            parent=null
│
├── Combat/
│   ├── Tag_Combat.asset                leafName="Combat"           parent=Tag_Noise
│   ├── Tag_WeaponFire.asset            leafName="WeaponFire"       parent=Tag_Combat
│   ├── Tag_MeleeSwing.asset            leafName="MeleeSwing"       parent=Tag_Combat
│   ├── Tag_Explosion.asset             leafName="Explosion"        parent=Tag_Combat
│   └── Tag_Impact.asset                leafName="Impact"           parent=Tag_Combat
│
├── World/
│   ├── Tag_World.asset                 leafName="World"            parent=Tag_Noise
│   ├── Tag_Footstep.asset              leafName="Footstep"         parent=Tag_World
│   ├── Tag_Door.asset                  leafName="Door"             parent=Tag_World
│   ├── Tag_ItemUse.asset               leafName="ItemUse"          parent=Tag_World
│   └── Tag_BodyFall.asset              leafName="BodyFall"         parent=Tag_World
│
└── Alert/
    ├── Tag_Alert.asset                 leafName="Alert"            parent=Tag_Noise
    ├── Tag_Voice.asset                 leafName="Voice"            parent=Tag_Alert
    ├── Tag_Death.asset                 leafName="Death"            parent=Tag_Alert
    ├── Tag_Alarm.asset                 leafName="Alarm"            parent=Tag_Alert
    ├── Tag_TrapTrigger.asset           leafName="TrapTrigger"      parent=Tag_Alert
    └── Tag_Distraction.asset           leafName="Distraction"      parent=Tag_Alert
```

### Tag JSON 资产清单

```json
[
  { "id": "Tag_Noise",              "leafName": "Noise",         "parent": null,             "description": "噪音根标签。AI 听觉行为路由入口。" },
  { "id": "Tag_Combat",             "leafName": "Combat",        "parent": "Tag_Noise",      "description": "战斗类噪音。枪声/近战挥击/爆炸/撞击。" },
  { "id": "Tag_WeaponFire",         "leafName": "WeaponFire",    "parent": "Tag_Combat",     "description": "枪械射击声。AI 追击+呼救行为。" },
  { "id": "Tag_MeleeSwing",         "leafName": "MeleeSwing",    "parent": "Tag_Combat",     "description": "近战武器挥动声。AI 警戒但不追击。" },
  { "id": "Tag_Explosion",          "leafName": "Explosion",     "parent": "Tag_Combat",     "description": "爆炸声。AI 全速奔袭+高优先级。" },
  { "id": "Tag_Impact",             "leafName": "Impact",        "parent": "Tag_Combat",     "description": "撞击/命中声。武器命中=高噪，身体碰撞=低噪。" },
  { "id": "Tag_World",              "leafName": "World",         "parent": "Tag_Noise",      "description": "环境/物理世界噪音。脚步/门/物品使用。" },
  { "id": "Tag_Footstep",           "leafName": "Footstep",      "parent": "Tag_World",      "description": "脚步声。跑步/走路/潜行不同等级。" },
  { "id": "Tag_Door",               "leafName": "Door",          "parent": "Tag_World",      "description": "开关门声。砸门/撬锁分属不同等级。" },
  { "id": "Tag_ItemUse",            "leafName": "ItemUse",       "parent": "Tag_World",      "description": "物品使用声。敲打/制作/修理/医疗工具。" },
  { "id": "Tag_BodyFall",           "leafName": "BodyFall",      "parent": "Tag_World",      "description": "尸体倒地声。暗杀不触发，普通击杀触发。" },
  { "id": "Tag_Alert",              "leafName": "Alert",         "parent": "Tag_Noise",      "description": "警戒触发类噪音。喊叫/死亡/警报/陷阱。" },
  { "id": "Tag_Voice",              "leafName": "Voice",         "parent": "Tag_Alert",      "description": "人声。呼喊/求救/指挥。丧尸对人类语音敏感。" },
  { "id": "Tag_Death",              "leafName": "Death",         "parent": "Tag_Alert",      "description": "死亡尖叫。极高优先级，吸引附近全部丧尸。" },
  { "id": "Tag_Alarm",              "leafName": "Alarm",         "parent": "Tag_Alert",      "description": "机械警报。汽车/建筑警报器，持续长距离。" },
  { "id": "Tag_TrapTrigger",        "leafName": "TrapTrigger",   "parent": "Tag_Alert",      "description": "陷阱触发声。捕兽夹/地刺/电击触发。" },
  { "id": "Tag_Distraction",        "leafName": "Distraction",   "parent": "Tag_Alert",      "description": "主动引诱声。哨子/诱饵瓶/噪音诱饵，玩家制造。" }
]
```

> `parent` 字段在 JSON 中为 Tag ID 字符串，导入时解析为 GUID 引用。

---

## 二、NoiseEventSO JSON Schema

```json
{
  "noiseType": "Noise.Combat.WeaponFire",
  "level": 5,
  "decayRadius": 60.0
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `noiseType` | ref → GameplayTagDefinitionSO | 噪音类型标签。AI 行为路由 |
| `level` | float | 噪音等级。0=无声, 1-6 越大传播越远 |
| `decayRadius` | float | 衰减半径(m)。超出此距离 AI 听不到 |

---

## 三、完整 NoiseEventSO 资产清单

### 文件路径

```
Assets/Data/Ability/Noises/
├── Level1_VeryLow/                 # Lv1 — < 3m 反应半径
├── Level2_Low/                     # Lv2 — ~5m
├── Level3_Medium/                  # Lv3 — ~15m
├── Level4_High/                    # Lv4 — ~30m
├── Level5_VeryHigh/                # Lv5 — ~60m
├── Level6_Deafening/              # Lv6 — ~120m
└── Special/                        # 特殊规则 — Phase 4.2+
```

---

### Level 1 — 极低 (< 3m)

```json
[
  {
    "id": "Noise_Lv1_StealthMode",
    "path": "Level1_VeryLow/",
    "noiseType": "Noise.World.Footstep",
    "level": 1,
    "decayRadius": 3.0,
    "usedBy": ["潜行·潜行模式 (Toggle, 噪音-80%)", "潜行·暗杀", "潜行·闷棍"]
  },
  {
    "id": "Noise_Lv1_BreathHold",
    "path": "Level1_VeryLow/",
    "noiseType": "Noise.Combat.MeleeSwing",
    "level": 1,
    "decayRadius": 2.0,
    "usedBy": ["步枪·屏息精瞄 (自身动作, 无对外噪音)"]
  },
  {
    "id": "Noise_Lv1_Lockpick",
    "path": "Level1_VeryLow/",
    "noiseType": "Noise.World.ItemUse",
    "level": 1,
    "decayRadius": 2.0,
    "usedBy": ["撬锁·撬锁 (灵巧手指被动-80%)", "撬锁·电子锁破解", "撬锁·安全检查"]
  }
]
```

---

### Level 2 — 低 (~5m)

```json
[
  {
    "id": "Noise_Lv2_PistolSuppressed",
    "path": "Level2_Low/",
    "noiseType": "Noise.Combat.WeaponFire",
    "level": 2,
    "decayRadius": 5.0,
    "usedBy": ["手枪·普通射击 (加装消音器, Phase 2+)"]
  },
  {
    "id": "Noise_Lv2_StealthMove",
    "path": "Level2_Low/",
    "noiseType": "Noise.World.Footstep",
    "level": 2,
    "decayRadius": 5.0,
    "usedBy": ["潜行·潜行模式中移动", "潜行·尸体搬运"]
  },
  {
    "id": "Noise_Lv2_PushKick",
    "path": "Level2_Low/",
    "noiseType": "Noise.Combat.MeleeSwing",
    "level": 2,
    "decayRadius": 5.0,
    "usedBy": ["基本功·脚踢 (Blunt 0.3×, 小动作)"]
  },
  {
    "id": "Noise_Lv2_ThrowingKnife",
    "path": "Level2_Low/",
    "noiseType": "Noise.Combat.MeleeSwing",
    "level": 2,
    "decayRadius": 5.0,
    "usedBy": ["投掷·飞刀 (瞬发无声投掷)"]
  },
  {
    "id": "Noise_Lv2_KeyImpression",
    "path": "Level2_Low/",
    "noiseType": "Noise.World.ItemUse",
    "level": 2,
    "decayRadius": 5.0,
    "usedBy": ["撬锁·钥匙模具 (精细操作)"]
  },
  {
    "id": "Noise_Lv2_Assessment",
    "path": "Level2_Low/",
    "noiseType": "Noise.World.ItemUse",
    "level": 2,
    "decayRadius": 5.0,
    "usedBy": ["医疗·诊断 (0.5s目视检查)"]
  }
]
```

---

### Level 3 — 中 (~15m)

```json
[
  {
    "id": "Noise_Lv3_MeleeSwing",
    "path": "Level3_Medium/",
    "noiseType": "Noise.Combat.MeleeSwing",
    "level": 3,
    "decayRadius": 15.0,
    "usedBy": [
      "刀·轻击", "棍·轻击",
      "拳击·轻击连段", "泰拳·轻击", "八极拳·轻击",
      "咏春·轻击", "太极拳·轻击", "散打·轻击",
      "菲律宾魔杖·轻击棍花", "剑道·基础轻击", "苗刀术·基础轻击"
    ]
  },
  {
    "id": "Noise_Lv3_CombatRoll",
    "path": "Level3_Medium/",
    "noiseType": "Noise.World.Footstep",
    "level": 3,
    "decayRadius": 15.0,
    "usedBy": ["基本功·翻滚闪避 (位移3m)"]
  },
  {
    "id": "Noise_Lv3_Running",
    "path": "Level3_Medium/",
    "noiseType": "Noise.World.Footstep",
    "level": 3,
    "decayRadius": 15.0,
    "usedBy": ["通用·跑步移动", "潜行·快速脱离 (冲刺5m)"]
  },
  {
    "id": "Noise_Lv3_ShieldBash",
    "path": "Level3_Medium/",
    "noiseType": "Noise.Combat.Impact",
    "level": 3,
    "decayRadius": 15.0,
    "usedBy": ["防御·盾牌猛击 (击退2m+硬直)"]
  },
  {
    "id": "Noise_Lv3_StockStrike",
    "path": "Level3_Medium/",
    "noiseType": "Noise.Combat.Impact",
    "level": 3,
    "decayRadius": 15.0,
    "usedBy": ["霰弹枪·枪托挥击"]
  },
  {
    "id": "Noise_Lv3_BlockImpact",
    "path": "Level3_Medium/",
    "noiseType": "Noise.Combat.Impact",
    "level": 3,
    "decayRadius": 15.0,
    "usedBy": ["防御·格挡成功撞击声", "防御·招架成功反击"]
  },
  {
    "id": "Noise_Lv3_MakeFire",
    "path": "Level3_Medium/",
    "noiseType": "Noise.World.ItemUse",
    "level": 3,
    "decayRadius": 15.0,
    "usedBy": ["生存·生火 (打火机+木材)"]
  }
]
```

---

### Level 4 — 高 (~30m)

```json
[
  {
    "id": "Noise_Lv4_MeleeHeavy",
    "path": "Level4_High/",
    "noiseType": "Noise.Combat.MeleeSwing",
    "level": 4,
    "decayRadius": 30.0,
    "usedBy": [
      "刀·重击", "棍·重击", "斧·重击", "拳击·摆拳/上勾拳",
      "泰拳·鞭腿/膝撞", "八极拳·重击", "太极拳·重击",
      "柔道·投技", "菲律宾魔杖·劈击", "剑道·唐竹割",
      "苗刀术·下劈", "以色列格斗术·膝肘组合", "散打·重击"
    ]
  },
  {
    "id": "Noise_Lv4_MeleeHit",
    "path": "Level4_High/",
    "noiseType": "Noise.Combat.Impact",
    "level": 4,
    "decayRadius": 30.0,
    "usedBy": [
      "斧·轻击命中 (斧重, 命中声大)", "近战终结技命中",
      "苗刀术·回身斩 (360°横扫命中)", "苗刀术·力劈华山命中"
    ]
  },
  {
    "id": "Noise_Lv4_SmokeGrenade",
    "path": "Level4_High/",
    "noiseType": "Noise.Combat.Explosion",
    "level": 4,
    "decayRadius": 30.0,
    "usedBy": ["投掷·烟雾弹 (非致命爆炸)"]
  },
  {
    "id": "Noise_Lv4_AcidVial",
    "path": "Level4_High/",
    "noiseType": "Noise.Combat.Explosion",
    "level": 4,
    "decayRadius": 30.0,
    "usedBy": ["投掷·酸液瓶 (玻璃碎裂声)"]
  },
  {
    "id": "Noise_Lv4_PoisonGas",
    "path": "Level4_High/",
    "noiseType": "Noise.Combat.Explosion",
    "level": 4,
    "decayRadius": 30.0,
    "usedBy": ["投掷·毒气瓶 (容器破裂)"]
  },
  {
    "id": "Noise_Lv4_BearTrapTrigger",
    "path": "Level4_High/",
    "noiseType": "Noise.Alert.TrapTrigger",
    "level": 4,
    "decayRadius": 30.0,
    "usedBy": ["陷阱·捕兽夹触发 (金属咬合声)"]
  },
  {
    "id": "Noise_Lv4_GasTrapTrigger",
    "path": "Level4_High/",
    "noiseType": "Noise.Alert.TrapTrigger",
    "level": 4,
    "decayRadius": 30.0,
    "usedBy": ["陷阱·毒气陷阱触发"]
  },
  {
    "id": "Noise_Lv4_ShockTrapTrigger",
    "path": "Level4_High/",
    "noiseType": "Noise.Alert.TrapTrigger",
    "level": 4,
    "decayRadius": 30.0,
    "usedBy": ["陷阱·电击陷阱触发 (电弧声)"]
  },
  {
    "id": "Noise_Lv4_DistractionWhistle",
    "path": "Level4_High/",
    "noiseType": "Noise.Alert.Distraction",
    "level": 4,
    "decayRadius": 30.0,
    "usedBy": ["潜行·引诱哨 (瞄准点制造4级噪音+气味标记)"]
  },
  {
    "id": "Noise_Lv4_ForceEntry",
    "path": "Level4_High/",
    "noiseType": "Noise.World.Door",
    "level": 4,
    "decayRadius": 30.0,
    "usedBy": ["撬锁·暴力破锁 (砸门/撞门)"]
  }
]
```

---

### Level 5 — 极高 (~60m)

```json
[
  {
    "id": "Noise_Lv5_Pistol",
    "path": "Level5_VeryHigh/",
    "noiseType": "Noise.Combat.WeaponFire",
    "level": 5,
    "decayRadius": 60.0,
    "usedBy": [
      "手枪·普通射击", "手枪·快速拔枪", "手枪·双持",
      "手枪·移动射击"
    ]
  },
  {
    "id": "Noise_Lv5_AxeHeavy",
    "path": "Level5_VeryHigh/",
    "noiseType": "Noise.Combat.Impact",
    "level": 5,
    "decayRadius": 60.0,
    "usedBy": ["斧·重击命中 (全近战最大单次噪音)"]
  },
  {
    "id": "Noise_Lv5_Molotov",
    "path": "Level5_VeryHigh/",
    "noiseType": "Noise.Combat.Explosion",
    "level": 5,
    "decayRadius": 60.0,
    "usedBy": ["投掷·燃烧瓶 (火焰爆发声)"]
  },
  {
    "id": "Noise_Lv5_Flashbang",
    "path": "Level5_VeryHigh/",
    "noiseType": "Noise.Combat.Explosion",
    "level": 5,
    "decayRadius": 60.0,
    "usedBy": ["投掷·震撼弹 (巨响+闪光)"]
  },
  {
    "id": "Noise_Lv5_ThermalCut",
    "path": "Level5_VeryHigh/",
    "noiseType": "Noise.World.ItemUse",
    "level": 5,
    "decayRadius": 60.0,
    "usedBy": ["撬锁·热切割 (高温金属切割, 极慢+极高噪音)"]
  },
  {
    "id": "Noise_Lv5_BaitBottle",
    "path": "Level5_VeryHigh/",
    "noiseType": "Noise.Alert.Distraction",
    "level": 5,
    "decayRadius": 60.0,
    "usedBy": ["投掷·诱饵瓶着陆 (5级噪音+气味标记)"]
  },
  {
    "id": "Noise_Lv5_CoveringFire",
    "path": "Level5_VeryHigh/",
    "noiseType": "Noise.Combat.WeaponFire",
    "level": 5,
    "decayRadius": 60.0,
    "usedBy": ["防御·掩护射击 (3s连发压制)"]
  }
]
```

---

### Level 6 — 震耳 (~120m)

```json
[
  {
    "id": "Noise_Lv6_Rifle",
    "path": "Level6_Deafening/",
    "noiseType": "Noise.Combat.WeaponFire",
    "level": 6,
    "decayRadius": 120.0,
    "usedBy": [
      "步枪·普通射击", "步枪·屏息精瞄", "步枪·架枪", "步枪·快速补射"
    ]
  },
  {
    "id": "Noise_Lv6_Shotgun",
    "path": "Level6_Deafening/",
    "noiseType": "Noise.Combat.WeaponFire",
    "level": 6,
    "decayRadius": 120.0,
    "usedBy": [
      "霰弹枪·普通射击", "霰弹枪·抵近射击", "霰弹枪·火力压制"
    ]
  },
  {
    "id": "Noise_Lv6_FragGrenade",
    "path": "Level6_Deafening/",
    "noiseType": "Noise.Combat.Explosion",
    "level": 6,
    "decayRadius": 120.0,
    "usedBy": ["投掷·破片手雷 (2s引信, 全图警告)"]
  },
  {
    "id": "Noise_Lv6_TripMine",
    "path": "Level6_Deafening/",
    "noiseType": "Noise.Combat.Explosion",
    "level": 6,
    "decayRadius": 120.0,
    "usedBy": ["陷阱·绊雷引爆 (定向爆破)"]
  },
  {
    "id": "Noise_Lv6_AlarmTrap",
    "path": "Level6_Deafening/",
    "noiseType": "Noise.Alert.Alarm",
    "level": 6,
    "decayRadius": 120.0,
    "usedBy": ["陷阱·警报器触发 (噪音6级+信号弹升空, 8s)"]
  },
  {
    "id": "Noise_Lv6_NoiseDecoy",
    "path": "Level6_Deafening/",
    "noiseType": "Noise.Alert.Alarm",
    "level": 6,
    "decayRadius": 120.0,
    "usedBy": ["陷阱·噪音诱饵 (5s倒计时后噪音6级+灯光8s)"]
  },
  {
    "id": "Noise_Lv6_ImprovisedExplosive",
    "path": "Level6_Deafening/",
    "noiseType": "Noise.Combat.Explosion",
    "level": 6,
    "decayRadius": 120.0,
    "usedBy": ["工艺·简易炸药 (1.8×, 可投掷)"]
  },
  {
    "id": "Noise_Lv6_Sniping",
    "path": "Level6_Deafening/",
    "noiseType": "Noise.Combat.WeaponFire",
    "level": 6,
    "decayRadius": 180.0,
    "usedBy": ["步枪·远程狙击 (狙击姿态, 传播半径+50% = 180m)"]
  },
  {
    "id": "Noise_Lv6_CarAlarm",
    "path": "Level6_Deafening/",
    "noiseType": "Noise.Alert.Alarm",
    "level": 6,
    "decayRadius": 120.0,
    "usedBy": ["Phase 4.2+ 汽车警报 (环境触发/玩家触发)"]
  }
]
```

---

### Special — 特殊规则 (Phase 4.2+)

```json
[
  {
    "id": "Noise_Special_Silent",
    "path": "Special/",
    "noiseType": "Noise.World.Footstep",
    "level": 0,
    "decayRadius": 0.0,
    "usedBy": ["潜行·暗杀 (无声杀戮被动=完全无声)", "潜行·闷棍 (无声杀戮)"],
    "note": "level=0 表示不发布噪音事件。AbilityDefSO.noise 可留空或引用此 Silent 资产。"
  },
  {
    "id": "Noise_Special_HordeCall",
    "path": "Special/",
    "noiseType": "Noise.Alert.Alarm",
    "level": 6,
    "decayRadius": 9999.0,
    "usedBy": ["尸潮号角 (全图, 特殊AI行为—聚集+进攻)"],
    "note": "Phase 4.2+。decayRadius=9999 表示全图传播。"
  }
]
```

---

## 汇总

### 按等级统计

| 等级 | A测闭环 | Phase 2+ | 后期全量 | 反应半径 |
|------|:------:|:--------:|:------:|:------:|
| Lv1 极低 | 0 | 3 | **3** | < 3m |
| Lv2 低 | 0 | 6 | **6** | ~5m |
| Lv3 中 | 1 (刀轻击) | 6 | **7** | ~15m |
| Lv4 高 | 1 (棍重击) | 9 | **10** | ~30m |
| Lv5 极高 | 1 (手枪) | 6 | **7** | ~60m |
| Lv6 震耳 | 1 (霰弹) | 8 | **9** | ~120m |
| Special | 0 | 2 | **2** | 0 / 全图 |
| **合计** | **4** | **40** | **44** | |

### 按 NoiseType 统计

| Tag | 资产数 | 用途 |
|-----|:------:|------|
| `Noise.Combat.WeaponFire` | 5 | 手枪/步枪/霰弹/狙击/消音手枪 |
| `Noise.Combat.MeleeSwing` | 4 | 轻击/重击/脚踢/飞刀 |
| `Noise.Combat.Explosion` | 6 | 手雷/绊雷/燃烧瓶/烟雾弹/震撼弹/简易炸药 |
| `Noise.Combat.Impact` | 4 | 盾击/枪托/格挡/斧重击命中 |
| `Noise.World.Footstep` | 4 | 潜行/跑步/翻滚/屏息 |
| `Noise.World.Door` | 1 | 暴力破锁 |
| `Noise.World.ItemUse` | 4 | 生火/热切割/撬锁/诊断 |
| `Noise.Alert.TrapTrigger` | 3 | 捕兽夹/毒气陷阱/电击陷阱 |
| `Noise.Alert.Distraction` | 2 | 引诱哨/诱饵瓶 |
| `Noise.Alert.Alarm` | 4 | 警报器/噪音诱饵/汽车警报/尸潮号角 |

> **复用原则**：同一 (noiseType + level + decayRadius) 的技能共用同一个 NoiseEventSO。上表已合并同参项。

---

## 跨文档索引

| 文档 | 说明 |
|------|------|
| [ability-pipeline-design.md](ability-pipeline-design.md) | 维度⑧ Broadcast — Noise 在管道中的位置与发布时机 |
| [ability-inventory.md](ability-inventory.md) | 154 技能全量 — 噪音分级表 + 每技能噪音等级 |
| [gameplay-tag.md](../../../L1-core/gameplay-tag.md) | GameplayTag 完整资产树 — Noise Tag 17 资产定义 |
| [ability-search-assets.md](ability-search-assets.md) | SearchSO 资产树 — 搜索形状 |
| [ability-activation-assets.md](ability-activation-assets.md) | ActivationSO 资产树 — 激活方式 (Noise 在 ③ 发布) |
