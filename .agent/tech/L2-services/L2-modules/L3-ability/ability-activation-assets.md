# Ability Activation — 激活方式资产树

> `L3_Ability/` · 资产清单 · 2026-06-13
>
> 完整 AbilityActivationSO 资产树。每个资产定义技能「怎么放」——输入模型、动画阶段、取消策略。
> 设计依据：[ability-pipeline-design.md](ability-pipeline-design.md) 维度③ + [ability-inventory.md](ability-inventory.md) 154 技能全量。

---

## JSON Schema

### AbilityActivationSO

```json
{
  "activationType": "Instant",
  "maxChargeTime": 0.0,
  "autoReleaseAtFullCharge": false,
  "animationAsset": null,
  "animationLayer": "FullBody",
  "animationSpeed": 1.0,
  "rootMotion": false,
  "windupDuration": 0.1,
  "fireWindowDuration": 0.15,
  "canCancelWindup": true,
  "canCancelRecovery": false
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `activationType` | enum | Instant=0, Charged=1, Channel=2, Toggle=3 |
| `maxChargeTime` | float | 蓄力最大时长(s)。>0 且 Charged 型时有效。Phase 4.2+ |
| `autoReleaseAtFullCharge` | bool | 蓄满自动释放。Phase 4.2+ |
| `animationAsset` | ref | Animancer StringAsset 动画引用 (Phase 4.1b) |
| `animationLayer` | enum | FullBody=0 (锁移动), UpperBody=1 (不锁) |
| `animationSpeed` | float | 动画播放倍率 (0.1-3.0)。唯一调参旋钮 |
| `rootMotion` | bool | 是否使用动画根运动驱动位移 |
| `windupDuration` | float | 前摇时长(s)。动画开始→进入激发窗口 |
| `fireWindowDuration` | float | 激发窗口时长(s)。AbilityDriver 执行命中检测 |
| `canCancelWindup` | bool | 前摇期间是否可被翻滚/格挡打断 |
| `canCancelRecovery` | bool | 后摇期间是否可被下一技能/翻滚打断 |

> **Recovery 计算**：`recovery = clipLength / animationSpeed - (windup + fire) / animationSpeed`。由 AbilityDriver 运行时计算，不存储在 SO 中。

---

## 完整资产清单

### 文件路径

```
Assets/Data/Ability/Activations/
├── Instant/                       # 瞬发 — A测全部技能
├── Charged/                       # 蓄力 — Phase 4.2+
├── Channel/                       # 持续引导 — Phase 4.2+
└── Toggle/                        # 开关切换 — Phase 4.2+
```

---

### 一、Instant 瞬发 (~8 资产)

> A 测全部技能用瞬发。按动画层和手感分档。

```json
[
  {
    "id": "Activation_Instant",
    "path": "",
    "activationType": "Instant",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["通用占位 — 测试用，已存在"],
    "status": "✅ Done"
  },
  {
    "id": "Activation_Instant_LightMelee",
    "path": "Instant/",
    "activationType": "Instant",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.1,
    "rootMotion": false,
    "windupDuration": 0.1,
    "fireWindowDuration": 0.15,
    "canCancelWindup": true,
    "canCancelRecovery": true,
    "usedBy": [
      "刀·轻击", "棍·轻击", "斧·轻击", "空手轻击",
      "拳击·轻击连段", "泰拳·轻击", "八极拳·轻击",
      "咏春·轻击(连环冲拳)", "太极拳·轻击", "散打·轻击",
      "菲律宾魔杖·轻击棍花", "剑道·基础轻击", "苗刀术·基础轻击"
    ]
  },
  {
    "id": "Activation_Instant_HeavyMelee",
    "path": "Instant/",
    "activationType": "Instant",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.4,
    "fireWindowDuration": 0.2,
    "canCancelWindup": true,
    "canCancelRecovery": false,
    "usedBy": [
      "刀·重击", "棍·重击", "斧·重击", "空手重击",
      "拳击·上勾拳/摆拳", "泰拳·鞭腿/膝撞",
      "八极拳·重击", "太极拳·重击", "柔道·投技",
      "菲律宾魔杖·劈击", "剑道·唐竹割", "苗刀术·下劈",
      "以色列格斗术·膝肘组合", "散打·重击"
    ]
  },
  {
    "id": "Activation_Instant_Firearm",
    "path": "Instant/",
    "activationType": "Instant",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.1,
    "canCancelWindup": false,
    "canCancelRecovery": true,
    "usedBy": [
      "手枪·普通射击", "手枪·快速拔枪(出枪+60%)",
      "霰弹枪·普通射击", "霰弹枪·抵近射击",
      "步枪·普通射击", "步枪·快速补射",
      "步枪·架枪(需掩体)"
    ]
  },
  {
    "id": "Activation_Instant_CombatRoll",
    "path": "Instant/",
    "activationType": "Instant",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.2,
    "rootMotion": true,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.3,
    "canCancelWindup": false,
    "canCancelRecovery": true,
    "usedBy": ["基本功·翻滚闪避 (位移3m, iFrame 0.3s)"],
    "note": "overrideExclusion=true — 无视互斥门控"
  },
  {
    "id": "Activation_Instant_SpecialAction",
    "path": "Instant/",
    "activationType": "Instant",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.15,
    "fireWindowDuration": 0.1,
    "canCancelWindup": true,
    "canCancelRecovery": true,
    "usedBy": [
      "基本功·脚踢 (不参与连击链)",
      "霰弹枪·枪托挥击", "防御·盾牌猛击",
      "防御·招架 (受击前0.3s触发免疫)"
    ]
  },
  {
    "id": "Activation_Instant_Throw",
    "path": "Instant/",
    "activationType": "Instant",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.3,
    "fireWindowDuration": 0.1,
    "canCancelWindup": true,
    "canCancelRecovery": true,
    "usedBy": [
      "投掷·燃烧瓶", "投掷·破片手雷", "投掷·烟雾弹",
      "投掷·震撼弹", "投掷·酸液瓶", "投掷·毒气瓶",
      "投掷·飞刀", "投掷·诱饵瓶"
    ]
  },
  {
    "id": "Activation_Instant_Place",
    "path": "Instant/",
    "activationType": "Instant",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 1.5,
    "fireWindowDuration": 0.1,
    "canCancelWindup": true,
    "canCancelRecovery": false,
    "usedBy": [
      "陷阱·捕兽夹(1.5s)", "陷阱·绊雷(2.5s)", "陷阱·警报器(1s)",
      "陷阱·毒气陷阱(2s)", "陷阱·电击陷阱(2s)",
      "陷阱·铁丝网(3s)", "陷阱·油滑陷阱(1.5s)",
      "陷阱·绊索(1.5s)", "陷阱·噪音诱饵(1s)",
      "陷阱·地刺(11s: 挖坑8s+放尖刺3s)"
    ]
  }
]
```

---

### 二、Charged 蓄力 (~5 资产, Phase 4.2+)

```json
[
  {
    "id": "Activation_Charged_HeavyStrike",
    "path": "Charged/",
    "activationType": "Charged",
    "maxChargeTime": 1.5,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.2,
    "canCancelWindup": true,
    "canCancelRecovery": false,
    "usedBy": ["Phase 4.2+ 蓄力重击 (近战通用, 3档蓄力: 0.5/1.0/1.5s)"]
  },
  {
    "id": "Activation_Charged_Bow",
    "path": "Charged/",
    "activationType": "Charged",
    "maxChargeTime": 2.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.1,
    "canCancelWindup": true,
    "canCancelRecovery": true,
    "usedBy": ["Phase 4.2+ 弓弩 (拉弓蓄力, 满弓提示)"]
  },
  {
    "id": "Activation_Charged_ShieldWall",
    "path": "Charged/",
    "activationType": "Charged",
    "maxChargeTime": 2.0,
    "autoReleaseAtFullCharge": true,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": true,
    "canCancelRecovery": false,
    "usedBy": ["防御·盾墙 (蓄力越久越宽: 60°→120°)"]
  },
  {
    "id": "Activation_Charged_Kendo_Iai",
    "path": "Charged/",
    "activationType": "Charged",
    "maxChargeTime": 3.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.1,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["剑道·绝学 一之太刀 (3s蓄力, 不可取消, 5.0×伤害)"]
  },
  {
    "id": "Activation_Charged_FuseControl",
    "path": "Charged/",
    "activationType": "Charged",
    "maxChargeTime": 3.0,
    "autoReleaseAtFullCharge": true,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.1,
    "canCancelWindup": true,
    "canCancelRecovery": true,
    "usedBy": ["投掷·引信控制被动 (长按cook手雷 0-3s)"]
  }
]
```

---

### 三、Channel 持续引导 (~8 资产, Phase 4.2+)

```json
[
  {
    "id": "Activation_Channel_Medical",
    "path": "Channel/",
    "activationType": "Channel",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": true,
    "usedBy": [
      "医疗·快速包扎(1.5s)", "医疗·止血带(2s)", "医疗·骨折固定(3s)",
      "医疗·解毒(2s)", "医疗·消毒清创(2.5s)"
    ],
    "note": "可中断引导; 移速-50%; overrideExclusion=true"
  },
  {
    "id": "Activation_Channel_CombatMedic",
    "path": "Channel/",
    "activationType": "Channel",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": true,
    "usedBy": ["医疗·战地急救 (4s引导, 取消移速惩罚)"],
    "note": "战斗中可引导, 移速-50% 但我有战地医师被动→取消惩罚"
  },
  {
    "id": "Activation_Channel_CPR",
    "path": "Channel/",
    "activationType": "Channel",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["医疗·心肺复苏 (8s可中断, 复活倒地队友HP 20%)"]
  },
  {
    "id": "Activation_Channel_Craft",
    "path": "Channel/",
    "activationType": "Channel",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": true,
    "usedBy": [
      "工艺·临时修理(3s)", "工艺·武器打磨(2s)", "工艺·弹药复装(5s/发)",
      "工艺·简易炸药(8s)", "工艺·武器改装(5s)", "工艺·零部件拆解(2s/件)",
      "工艺·陷阱制作(6s)", "工艺·化学提炼(8s)"
    ]
  },
  {
    "id": "Activation_Channel_SuppressiveBlast",
    "path": "Channel/",
    "activationType": "Channel",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 3.0,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["霰弹枪·火力压制 (3s持续射击, 丧尸移速-50%)"]
  },
  {
    "id": "Activation_Channel_BreathControl",
    "path": "Channel/",
    "activationType": "Channel",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 3.0,
    "canCancelWindup": false,
    "canCancelRecovery": true,
    "usedBy": ["步枪·屏息精瞄 (3s屏息, 体力持续消耗8/s)"]
  },
  {
    "id": "Activation_Channel_Survival",
    "path": "Channel/",
    "activationType": "Channel",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": true,
    "usedBy": [
      "生存·生火(3s)", "生存·净水(5s/1s)", "生存·搭建庇护所(15s)",
      "生存·屠宰(5s/具)"
    ]
  },
  {
    "id": "Activation_Channel_Flamethrower",
    "path": "Channel/",
    "activationType": "Channel",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.2,
    "fireWindowDuration": 0.0,
    "canCancelWindup": true,
    "canCancelRecovery": true,
    "usedBy": ["Phase 4.2+ 火焰喷射器 (持续, 消耗燃料)"]
  }
]
```

---

### 四、Toggle 开关 (~7 资产, Phase 4.2+)

```json
[
  {
    "id": "Activation_Toggle_StealthMode",
    "path": "Toggle/",
    "activationType": "Toggle",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["潜行·潜行模式 (移速-25%, 噪音-80%, 可见度-40%)"]
  },
  {
    "id": "Activation_Toggle_GuardStance",
    "path": "Toggle/",
    "activationType": "Toggle",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["防御·格挡姿态 (格挡率+60%, 移速-30%)"]
  },
  {
    "id": "Activation_Toggle_Akimbo",
    "path": "Toggle/",
    "activationType": "Toggle",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["手枪·双持 (射速×2, 散布+35%, 换弹时间翻倍)"]
  },
  {
    "id": "Activation_Toggle_BracedFire",
    "path": "Toggle/",
    "activationType": "Toggle",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["步枪·架枪 (需掩体, 后坐力-50%, 无法移动, 移动即取消)"]
  },
  {
    "id": "Activation_Toggle_HunkerDown",
    "path": "Toggle/",
    "activationType": "Toggle",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["防御·铁壁 (减伤+40%, 无法移动, 霸体, 5s可提前取消)"]
  },
  {
    "id": "Activation_Toggle_Flashlight",
    "path": "Toggle/",
    "activationType": "Toggle",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "UpperBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["Phase 4.2+ 手电筒 (锥形光照, 可见度管理)"]
  },
  {
    "id": "Activation_Toggle_Camouflage",
    "path": "Toggle/",
    "activationType": "Toggle",
    "maxChargeTime": 0.0,
    "autoReleaseAtFullCharge": false,
    "animationLayer": "FullBody",
    "animationSpeed": 1.0,
    "rootMotion": false,
    "windupDuration": 0.0,
    "fireWindowDuration": 0.0,
    "canCancelWindup": false,
    "canCancelRecovery": false,
    "usedBy": ["潜行·原地伪装 (静止3s后可见度-80%, 持续至移动, 最大30s)"]
  }
]
```

---

## 汇总

| 类型 | A测闭环 | Phase 2+ | 后期全量 | 代表技能 |
|------|:------:|:--------:|:------:|---------|
| Instant | 8 | 0 | **8** | 全部A测战斗/投掷/陷阱/基本功 |
| Charged | 0 | 5 | **5** | 蓄力重击、弓弩、盾墙、一之太刀、引信控制 |
| Channel | 0 | 8 | **8** | 医疗引导、制作、火力压制、屏息、生存动作 |
| Toggle | 0 | 7 | **7** | 潜行、格挡、双持、架枪、铁壁、手电筒、伪装 |
| **合计** | **8** | **20** | **28** | |

---

## 阶段时机规则

```
         windupDuration    fireWindowDuration    recovery(计算)
         ├─────────────────┼─────────────────────┼─────────────────┤
按下按键  │   前摇 (可取消?)  │   激发窗口 (命中检测)  │   后摇 (可取消?)   │
         │                  │                      │                   │
         └── canCancelWindup                      └── canCancelRecovery
```

| 技能类型 | windup | fire | 取消策略 |
|---------|--------|------|---------|
| 轻击 | 0.1s | 0.15s | windup✅ recovery✅ |
| 重击 | 0.4s | 0.2s | windup✅ recovery❌ |
| 枪械 | 0s | 0.1s | windup❌ recovery✅ |
| 翻滚 | 0s | 0.3s (iFrame) | windup❌ recovery✅ |
| 放置陷阱 | 1-11s | 0.1s | windup✅ recovery❌ |
| 蓄力 | 0s (charge段) | 0.1-0.2s | windup✅ recovery❌ |
| Channel | 0s | 持续 | windup❌ recovery✅ |
| Toggle | 0s | 0s (瞬时切换) | windup❌ recovery❌ |

---

## 跨文档索引

| 文档 | 说明 |
|------|------|
| [ability-pipeline-design.md](ability-pipeline-design.md) | 维度③ Activation 设计 — 输入模型、动画、阶段时机 |
| [ability-inventory.md](ability-inventory.md) | 154 技能全量 — 每技能的激活类型 |
| [ability-search-assets.md](ability-search-assets.md) | SearchSO 资产树 — 搜索形状与参数 |
| [ability-noise-assets.md](ability-noise-assets.md) | NoiseEventSO 资产树 — 噪音事件 |
