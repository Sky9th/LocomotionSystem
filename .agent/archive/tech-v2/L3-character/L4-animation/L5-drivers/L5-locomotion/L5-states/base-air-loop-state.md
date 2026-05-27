# BaseAirLoopState · 空中循环状态

> `Character/Animation/Drivers/Locomotion/States/BaseAirLoopState.cs` — LocomotionLayerFsmState，空中下落循环动画

## 调用链

```
被谁调:
  BaseLayer.FSM → CanEnterState/OnEnterState/Tick

调谁:
  Owner.Play(alias) → 播放 AirLoop
  Owner.TrySetState → 尝试转换到 AirLand
  Owner.Rig?.SetSuppressGroundLock(true) → 抑制地面锁定
```

## 状态条件

```
CanEnterState:
  !GroundContact.IsGrounded
  && DistanceToGround >= landMinFallDistance

OnEnterState:
  Play(AirLoop)
  AirborneStartY = Position.y
  MaxFallDistance = 0
  Rig.SetSuppressGroundLock(true)   ← 空中解冻 Y 轴

Tick:
  fall = AirborneStartY - Position.y
  MaxFallDistance = max(MaxFallDistance, fall)
  尝试 → AirLand (DistanceToGround < threshold)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | BaseLayer | Owner 引用，读取 AnimProfile 阈值 |
| 依赖 | CharacterRig | 通过 Owner 抑制地面锁定 |

## 未来规划

无。
