# BaseIdleState · 站立待机状态

> `Character/Animation/Drivers/Locomotion/States/BaseIdleState.cs` — LocomotionLayerFsmState，站立待机动画

## 调用链

```
被谁调:
  BaseLayer.FSM → CanEnterState/OnEnterState/Tick

调谁:
  Owner.TrySetState → 尝试转换到 TurnInPlace/IdleToMoving/Moving/AirLoop
  Owner.Play(alias) → 播放 idleL
  Owner.PlayIfChanged(alias) → 持续保持 idleL
```

## 状态条件

```
CanEnterState:
  Phase == GroundedIdle && !IsTurning

OnEnterState:
  Play(idleL)

Tick 转换优先级:
  1. TurnInPlace  (GroundedIdle && IsTurning)
  2. IdleToMoving (GroundedMoving && IsTurning)
  3. Moving       (GroundedMoving && !IsTurning)
  4. AirLoop      (!IsGrounded)
  默认: PlayIfChanged(idleL)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | BaseLayer | Owner 引用，读取离散状态 |

## 未来规划

无。
