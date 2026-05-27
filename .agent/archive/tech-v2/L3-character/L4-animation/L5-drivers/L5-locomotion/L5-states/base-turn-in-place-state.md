# BaseTurnInPlaceState · 原地转身状态

> `Character/Animation/Drivers/Locomotion/States/BaseTurnInPlaceState.cs` — LocomotionLayerFsmState，地面站立时转身动画

## 调用链

```
被谁调:
  BaseLayer.FSM → CanEnterState/OnEnterState/Tick

调谁:
  Owner.TrySetState → 尝试转换到 AirLoop/IdleToMoving/Idle
  Owner.Play(alias) → 播放 turnInPlace90L/R
  Owner.PlayIfChanged(alias) → 持续保持
  Owner.ApplyTurnStepRotation() → 程序化转身
```

## 状态条件

```
CanEnterState:
  Phase == GroundedIdle && IsTurning

OnEnterState:
  TurnAngle > 0 → Play(turnInPlace90R)
  TurnAngle <= 0 → Play(turnInPlace90L)

Tick 转换优先级:
  1. AirLoop       (!IsGrounded)
  2. IdleToMoving  (GroundedMoving && IsTurning)
  3. Idle          (GroundedIdle && !IsTurning)
  持续: ApplyTurnStepRotation()
  持续: PlayIfChanged(selectedAlias)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | BaseLayer | Owner 引用，读取离散状态/ApplyTurnStepRotation |

## 未来规划

无。
