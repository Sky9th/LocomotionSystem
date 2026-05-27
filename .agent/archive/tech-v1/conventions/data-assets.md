# Data 资产与 SO 约定

> 日期: 2026-05-09
> 状态: 已完成重构

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

## CreateAssetMenu 路径统���

全部在 Unity 右键菜单的 `Game/Character/` 下：

| 类 | Menu路径 |
|---|---------|
| `CharacterProfile` | `Game/Character/Character Profile` |
| `LocomotionProfile` | `Game/Character/Locomotion Profile` |
| `AnimationAliasProfile` | `Game/Character/Animation Alias Profile` |
| `LocomotionAnimationProfile` | `Game/Character/Animation Profile` |
| `LocomotionModeProfile` | `Game/Character/Mode Profile` |
