# BaseTurnInMovingState · 移动中转身状态

> `Character/Animation/Drivers/Locomotion/States/BaseTurnInMovingState.cs` — LocomotionLayerFsmState，移动中 180 度转身动画

## 调用链

```
被谁调:
  BaseLayer.FSM → CanEnterState/OnEnterState/Tick

调谁:
  Owner.TrySetState → 尝试转换到 Moving/Idle/AirLoop
  Owner.ForceSetState → 条件不满足时强制回 Moving
  Owner.PlayFromStart(alias) → 播放 turnIn*/sprint*180L/R
  Owner.HasCompleted() → 判断是否播完
```

## 状态条件

```
CanEnterState:
  Phase == GroundedMoving && IsTurning
  && DesiredLocalVelocity.y >= moveSpeed * 0.9   (主要向前)
  && abs(DesiredLocalVelocity.x) <= moveSpeed * 0.1  (少量横向)

OnEnterState:
  TurnAngle > 0:
    Walk → turnInWalk180R
    Run  → turnInRun180R
    其他 → turnInSprint180R
  TurnAngle <= 0:
    Walk → turnInWalk180L
    Run  → turnInRun180L
    其他 → turnInSprint180L

Tick 转换优先级:
  1. 非前向意图 → ForceSetState(Moving)
  2. TrySetState(Moving)
  3. TrySetState(Idle)
  4. TrySetState(AirLoop)
  HasCompleted:
    - IsTurning → 重选动画 PlayFromStart（循环）
    - !IsTurning && Moving → ForceSetState(Moving)
    - 否则 → ForceSetState(Idle)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | BaseLayer | Owner 引用，读取 Motor/Discrete |

## 未来规划

无。
