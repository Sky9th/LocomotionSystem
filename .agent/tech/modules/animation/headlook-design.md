# HeadLook 头部注视

> 日期: 2026-05-09
> 状态: ✅ 已实现
> 范围: Phase 1 / LocomotionSystem 完结

## 数据流

```
CharacterKinematic.Evaluate()
  → CharacterHeadLook.Evaluate(viewForward, modelRoot, rootTransform, profile)
  → Vector2(yaw, pitch) 归一化到 [-1,1]
  → SCharacterKinematic.LookDirection
  → SCharacterSnapshot.Kinematic.LookDirection
  → AnimationBrain.UpdateHeadLook(snapshot)
    → 平滑 (MoveTowards, speed × dt)
    → headLookMixer.Parameter = (smoothedYaw, smoothedPitch)
```

## 实现要点

| 功能 | 位置 | 说明 |
|------|------|------|
| 归一化 | `CharacterHeadLook.Evaluate` | `yaw/maxYaw` → `[-1,1]` |
| 平滑 | `AnimationBrain.UpdateHeadLook` | `MoveTowards` + `headLookSmoothingSpeed` |
| 冻结子动画 | `AnimationBrain.FreezeHeadLookChildren` | Speed=0, Weight=1, Time=1 |
| Mixer 创建 | `AnimationBrain.Awake` | `headLookLayer.TryPlay(lookMixer)` |

## 归属

HeadLook 是角色级动画行为。Mixer 由 AnimationBrain 持有并在 `Apply()` 中每帧更新，任何驱动状态下头部都跟随视线。
