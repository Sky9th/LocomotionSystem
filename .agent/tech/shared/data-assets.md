# Data 资产与 SO 约定

> **源文件**: `Assets/Data/` — 全局 ScriptableObject 资产目录
> 日期: 2026-05-09 | 状态: 已完成

## 调用链

```
被谁调:
  CharacterProfile      → CharacterActor, CharacterKinematic (地面/障碍物参数)
  LocomotionProfile      → Motor, Stance (移动参数)
  AnimationAliasProfile  → AnimationBrain (动画别名映射)
  AnimationProfile       → BaseLayer FSM (动画阈值/速度)
  ModeProfile            → BaseLayer States (步态动画配置)
  GameProfile            → GameService (全局配置)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | L3_Character | Character/Locomotion/Animation Profile 全家桶 |
| 被依赖 | L1_Core | GameProfile 全局配置 |
| 被依赖 | L2_Input | InputAction 配置 |
| 被依赖 | L2_UI | UI 主题/Panel 配置 |

## 目录结构

```
Assets/Data/
├── Character/                         角色相关 SO
│   ├── CharacterProfile.asset         地面/障碍物/头部朝向
│   ├── LocomotionProfile.asset        运动参数
│   ├── AnimationAliasProfile.asset    动画别名
│   ├── AnimationProfile.asset         动画配置(阈值/速度)
│   └── ModeProfiles/                 步态模式
│       ├── Stand_Walk.asset
│       ├── Stand_Run.asset
│       └── Stand_Sprint.asset
├── Animancer/                         动画资产
│   ├── Clips/                         StringAsset (38个)
│   ├── Parameters/                    StringAsset 参数 (4个)
│   ├── HumanTransitions.asset         Animancer Transition Library
│   └── HumanWalkSet.asset
├── InputAction/                       输入配置
├── UI/                                UI 配置
└── GameProfile.asset                  全局游戏配置
```

## CreateAssetMenu 路径

全部在 Unity 右键菜单的 `Game/Character/` 下：

| 类 | Menu 路径 |
|---|---------|
| `CharacterProfile` | `Game/Character/Character Profile` |
| `LocomotionProfile` | `Game/Character/Locomotion Profile` |
| `AnimationAliasProfile` | `Game/Character/Animation Alias Profile` |
| `LocomotionAnimationProfile` | `Game/Character/Animation Profile` |
| `LocomotionModeProfile` | `Game/Character/Mode Profile` |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 无具体规划。 | — | — |
