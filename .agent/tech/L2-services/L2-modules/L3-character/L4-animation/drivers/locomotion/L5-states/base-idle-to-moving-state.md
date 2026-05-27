# BaseIdleToMovingState · 待机→移动过渡状态

> `Character/Animation/Drivers/Locomotion/States/BaseIdleToMovingState.cs` — LocomotionLayerFsmState，启动转身过渡动画

## 调用链

```
被谁调:
  BaseLayer.FSM → CanEnterState/CanExitState/OnEnterState/Tick

调谁:
  Owner.TrySetState → 尝试转换到 Idle/Moving/AirLoop
  Owner.ForceSetState → 播完后强制设 Moving/Idle
  Owner.PlayFromStart(alias) → 播放 idleToRun180L/R
  Owner.HasCompleted() → 判断是否播完
```

## 状态条件

```
CanEnterState:
  Phase == GroundedMoving && IsTurning

CanExitState:
  !IsTurning

OnEnterState:
  TurnAngle > 0 → PlayFromStart(idleToRun180R)
  TurnAngle <= 0 → PlayFromStart(idleToRun180L)

Tick 转换优先级:
  1. Idle      (GroundedIdle)
  2. Moving    (GroundedMoving && !IsTurning)
  3. AirLoop   (!IsGrounded)
  HasCompleted:
    - GroundedMoving → ForceSetState(Moving)
    - 否则 → ForceSetState(Idle)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | BaseLayer | Owner 引用，读取离散状态 |

## 未来规划

无。
