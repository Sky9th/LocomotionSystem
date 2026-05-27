# Player GameObject 组件清单

> 日期: 2026-05-09
> 状态: Phase 1 基本完成，音效已接通

## GameObject 层级

```
Player (Root)                           ← 物理根
  ├─ Rigidbody
  ├─ CapsuleCollider
  ├─ CharacterActor              [MB]  组合根
  ├─ CharacterAudio              [MB]  音效
  ├─ AudioBody (AudioSource @腰部，子GO)
  └─ AudioFoot (AudioSource @脚底，子GO)
  │
  └─ Model                            ← 视觉根 (Animator + 模型)
      ├─ Animator
      ├─ NamedAnimancerComponent
      ├─ SkinnedMeshRenderer
      ├─ AnimationBrain           [MB, EO(-10)]  动画控制器
      │
      ├─ LocomotionDriver (子GO) [MB]  连续动画
      └─ TraversalDriver  (子GO) [MB]  一次性动画
```

## Root vs Model

| | Root | Model |
|---|---|---|
| 用途 | 物理碰撞 / 世界位置 | 视觉表现 / 动画 |
| 组件 | Rigidbody, CapsuleCollider, CharacterActor | Animator, Renderer, AnimationBrain |
| 运动方式 | `SetPosition` / `ApplyPosition` (物理入口) | `ApplyModelRotation` / `ApplyModelPosition` |
| 角色驱动 | Locomotion / GroundLock 写 Root | 动画 Root Motion 写 Model |

## 组件清单

| 组件 | 所在 GO | 类型 | 作用 |
|------|--------|------|------|
| Rigidbody | Root | Unity | 物理 |
| CapsuleCollider | Root | Unity | 碰撞 |
| CharacterActor | Root | MB | 组合根，Update 调用链 |
| CharacterAudio | Root | MB | 脚步/受击/死亡音效 |
| AudioSource (Body) | Root/AudioBody | Unity | 身体音效 3D 定位 |
| AudioSource (Foot_L) | Root/AudioFoot_L | Unity | 左脚 3D 定位 |
| AudioSource (Foot_R) | Root/AudioFoot_R | Unity | 右脚 3D 定位 |
| Animator | Model | Unity | Mecanim 驱动 |
| NamedAnimancerComponent | Model | Animancer | 动画播放器 |
| AnimationBrain | Model | MB | 6层动画 + 仲裁 + HeadLook + RootMotion |
| LocomotionDriver | Model/子GO | MB | 连续动画 FSM |
| TraversalDriver | Model/子GO | MB | 一次性攀爬动画 |

## 数据依赖（SerializeField）

| 组件 | 字段 | 类型 |
|------|------|------|
| CharacterActor | characterProfile | CharacterProfile |
| | locomotionProfile | LocomotionProfile |
| AnimationBrain | aliasProfile | AnimationAliasProfile |
| | animationProfile | LocomotionAnimationProfile |
| | upperBodyMask ~ footMask | AvatarMask × 5 |
| LocomotionDriver | aliasProfile | AnimationAliasProfile |
| | animationProfile | LocomotionAnimationProfile |
| | locomotionProfile | LocomotionProfile |
| TraversalDriver | aliasProfile | AnimationAliasProfile |
| CharacterAudio | config | CharacterAudioConfigSO |
