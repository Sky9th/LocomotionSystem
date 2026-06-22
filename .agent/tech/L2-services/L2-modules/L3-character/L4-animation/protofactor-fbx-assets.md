# PROTOFACTOR FBX 动画资产目录

> **Last Verified:** 2026-06-22
> **Source:** `I:\game\External\Assets\PROTOFACTOR\Ultimate Animation Collection\Animations\`

## 概述

PROTOFACTOR 是第三方动画资产包（Ultimate Animation Collection）。RedDust 从中提取 Human 和 Zombie 的 FBX 动画文件，按 **角色类型 → 武器类型 → 动画类别** 三层结构组织在 `Assets/Art/Animations/PROTOFACTOR/` 下。

FBX 文件仅作为动画剪辑的**原始数据源**导入。运行时动画引用通过 `LocomotionAnimationSetSO` / `GripAnimationTableSO` / `CharacterAnimationProfileSO` 资产间接指向这些 FBX 中的 AnimationClip。

---

## 目录结构

```
Assets/Art/Animations/PROTOFACTOR/
├── Character/                        # 角色模型 (1 fbx)
│   └── FBX Files/                    # Protof-Actor 骨骼绑定
│
├── Human/                            # 人类动画 (222 fbx)
│   ├── Unarmed/                      #   空手 (73 fbx)
│   │   ├── Attack/                   #     38   拳击/踢击/连击
│   │   ├── Reaction/                 #     23   受击/死亡/起身/格挡/眩晕
│   │   └── Special/                  #     12   闪避/嘲讽/战斗Idle
│   │
│   ├── 1H_Blade/                     #   单手近战 (58 fbx)
│   │   ├── Locomotion/               #     21   移动 (Relax + Combat)
│   │   │   ├── Relax/                #
│   │   │   └── Combat/               #         Walk/Run/Crouch Mixer
│   │   ├── Attack/                   #     12   攻击 (非位移，无 _RM)
│   │   ├── Reaction/                 #     21   受击/死亡/起身 (4方向)
│   │   └── Special/                  #      4   闪避/格挡Idle
│   │
│   └── 1H_Sidearm/                   #   单手持枪 (91 fbx)
│       ├── Locomotion/               #     26   移动 (Relax + Combat)
│       │   ├── Relax/                #
│       │   └── Combat/               #         Walk/Run/Crouch Mixer + Aiming
│       ├── Attack/                   #     18   Shoot/Reload/MeleeAttack/Grenade
│       ├── Reaction/                 #     14   受击/死亡/起身
│       └── Special/                  #     33   闪避/Cover全套/Peek/Strafe
│
├── Zombie/                           # 僵尸动画 (33 fbx)
│   └── Unarmed/                      #   空手 (仅 Unarmed)
│       ├── Locomotion/               #     14   Relax: IdleA~D + WalkA~D/H~I
│       │   ├── Relax/                #         Combat: Crawl + WalkE~G
│       │   └── Combat/               #
│       ├── Attack/                   #      4   3HitCombo/Attack1/Attack2/BiteAttack
│       ├── Reaction/                 #      7   GetHit×4方向 Light + GetHitHeavy×2方向 + Death
│       └── Special/                  #      8   FeastA~D/IdleKneel/KneelA~B/GetBackUp
│
└── Traversal/                        # 攀爬通用 (4 fbx)
    └── (Human + Zombie 共享)
```

---

## 动画类别定义

| 类别 | 含义 | 特点 |
|------|------|------|
| **Locomotion** | 移动/待机/空中/落地 | 有 Relax 和 Combat 两个子集；大部分有 `_RM` (Root Motion) 变体 |
| **Attack** | 攻击/射击/装弹/投掷 | 原地播放，通常**无 `_RM`**——攻击期间不应有位移 |
| **Reaction** | 受击/死亡/起身/格挡/眩晕 | Heavy 版有 `_RM`（后退踉跄），Light 版无（原地抖动） |
| **Special** | 闪避/嘲讽/掩体进出/Idle变体 | 闪避有 `_RM`，Idle 类无 |
| **Traversal** | 攀爬/翻越 | 通用，不区分角色/武器 |

---

## _RM (Root Motion) 规则

- **`_RM` 后缀** = 动画包含根骨骼位移，角色实际移动跟随动画
- **无 `_RM`** = 动画原地播放（In-Place），不产生位移
- **同一动画名同时存在时**：取 `_RM`，丢弃非 RM（如 `WalkA_RM.fbx` > `WalkA.fbx`）
- **仅存在非 RM 时**：直接取非 RM（如 `ShootPrimaryCrouching`——开枪本就不该有位移）

### 常见 _RM 分布

| 类别 | 有 _RM | 无 _RM（固有原地） |
|------|--------|-------------------|
| Locomotion Walk/Run/Sprint | ✅ | — |
| Locomotion CrouchMove | ✅ | — |
| Attack Melee | 部分 (Combo 有) | 单次挥砍 |
| Attack Shoot/Reload | — | ✅ 全部 |
| Reaction GetHitHeavy | ✅ | — |
| Reaction GetHitLight | — | ✅ 全部 |
| Reaction Death/Dead | — | ✅ 全部 |
| Special Dodge | ✅ | — |
| Special Cover/Strafe | ✅ | — |
| Special Idle/Taunt | — | ✅ 全部 |

---

## PROTOFACTOR 源 → RedDust 映射

| RedDust 目录 | PROTOFACTOR 源 Animset | 备注 |
|-------------|----------------------|------|
| `Human/Unarmed/` | `Combat Bare Fists Animset` | 拳击/踢击战斗 |
| `Human/1H_Blade/Locomotion/` | `1Handed Melee Weapon Animset` | 已导入 (v0.20.0) |
| `Human/1H_Blade/{Attack,Reaction,Special}/` | `1Handed Melee Weapon Animset` | 2026-06-22 导入 |
| `Human/1H_Sidearm/Locomotion/` | `2Handed Gun Animset` (⚠ 注意：源名为 2H，实际持枪姿态为双手持一把手枪，RedDust 作为 1H_Sidearm 使用) | 已导入 (v0.20.0) |
| `Human/1H_Sidearm/{Attack,Reaction,Special}/` | `2Handed Gun Animset` | 2026-06-22 导入 |
| `Zombie/Unarmed/` | `Zombie Animset` | 已导入 (v0.20.0) |
| `Traversal/` | `Climbing Animset` | 通用 |
| `Character/` | `Protof-Actor` | 骨骼/模型 |

---

## 当前缺口

| 缺口 | 说明 |
|------|------|
| `Human/Unarmed/Locomotion/` | 空手移动动画尚未从 `Basic Locomotion Animset` 导入 |
| `Human/1H_Blade/Locomotion/Relax/` | 仅 IdleL + Sprint，缺 Walk/Run Mixer 方向动画 |
| `Human/1H_Sidearm/Attack/` | 缺非瞄准姿态的 Shoot（仅有 Crouching 版） |
| `Human/{1H_Blade,1H_Sidearm}/` 缺 TurnInPlace | 原地转向动画未找到对应 PROTOFACTOR 源 |
| `Zombie/` 仅 TypeA | TypeB/TypeE 的 FBX 尚未导入 |
| `Human/` 无 Female 变体 | 当前仅有 Male/StyleA |
| 未来武器类型 | 2H_Melee, 2H_Rifle, DualWield, Shield 等 PROTOFACTOR 源存在但 RedDust 尚未导入 |

---

## 与 SO 动画数据的关系

```
FBX (原始数据)                     SO (Unity 资产)                    运行时
─────────────────────────────────────────────────────────────────────────
PROTOFACTOR/*/Attack/       →   LocomotionAnimationSetSO.turnInPlace*
PROTOFACTOR/*/Locomotion/   →   LocomotionAnimationSetSO.idleL
                                  .walkMixer / .runMixer / .sprint
                                  .airLight / .airHard
                                  .landLight / .landHard
                                  .climbUp* / .climbDown* / .landFromWall
PROTOFACTOR/Traversal/      →   (同上 traversal 字段)
                              →   GripAnimationTableSO           →  AnimationBrain
                              →   CharacterAnimationProfileSO        ├─ LocomotionDriver
                              →   LocomotionAnimationConfigSO          │   └─ BaseLayer (FSM)
                              →   AnimationModeConfigSO                └─ TraversalDriver (TODO)
```

> **Attack/Reaction/Special 的 FBX 目前仅在 SO 层有 Traversal 字段的部分支持。** Attack/Reaction 动画的驱动方式和 SO 类型尚未设计——这些 FBX 暂为"待消费"状态。

---

## 文件命名约定

FBX 文件在 PROTOFACTOR 源中统一命名为 `Humanoid@{AnimName}.fbx`。导入 RedDust 时保持原名不变（Unity 会自动去掉 Humanoid@ 前缀生成 AnimationClip 名称）。

- **Root Motion**: `_{RM}` 后缀，如 `WalkA_RM.fbx`
- **武器后缀**: `1hMelee` / `2HandedGun` / `2Guns` 等标识所属武器类型
- **方向**: `Back` / `Front` / `Left` / `Right` / `Forward` / `Backwards`
- **轻重**: `Light` / `Heavy`
- **⚠ 严禁导入 `2Guns` 后缀的文件**——那是双持手枪动画，非本项目使用的单持姿态
