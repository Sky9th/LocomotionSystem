# BaseAirLandState · 落地状态

> `Character/Animation/Drivers/Locomotion/States/BaseAirLandState.cs` — LocomotionLayerFsmState，根据坠落高度分级落地动画

## 调用链

```
被谁调:
  BaseLayer.FSM → CanEnterState/OnEnterState/Tick

调谁:
  Owner.Play(alias) → 播放 LandLight/LandMedium/LandHard
  Owner.TrySetState → 播完后回 Idle/Moving/IdleToMoving/TurnInPlace
  Owner.ForceSetState → 兜底回 Idle
  Owner.Rig?.SetSuppressGroundLock → 落地时恢复地面锁定
```

## 状态条件

```
CanEnterState:
  DistanceToGround < threshold (根据坠落级别)

OnEnterState:
  fallDist <= landLightMaxFallDistance → LandLight
  fallDist <= landMediumMaxFallDistance → LandMedium
  其他 → LandHard
  Rig.SetSuppressGroundLock(false)   ← 播完后恢复锁定

Tick (动画播完后):
  1. TrySetState(Idle)
  2. TrySetState(Moving)
  3. TrySetState(IdleToMoving)
  4. TrySetState(TurnInPlace)
  5. ForceSetState(Idle)   ← 兜底
  Rig.SetSuppressGroundLock(false)
```

### 落地分级
```
触发阈值:
  landLightTriggerDistance / landMediumTriggerDistance / landHardTriggerDistance

动画选择:
  fallDist <= landLightMaxFallDistance  → LandLight
  fallDist <= landMediumMaxFallDistance → LandMedium
  其他 → LandHard
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | BaseLayer | Owner 引用，读取 AnimProfile 落地区间 |
| 依赖 | CharacterRig | 通过 Owner 恢复地面锁定 |

## 未来规划

无。
