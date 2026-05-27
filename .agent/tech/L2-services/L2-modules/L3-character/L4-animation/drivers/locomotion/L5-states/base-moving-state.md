# BaseMovingState · 移动状态

> `Character/Animation/Drivers/Locomotion/States/BaseMovingState.cs` — LocomotionLayerFsmState，地面移动动画

## 调用链

```
被谁调:
  BaseLayer.FSM → CanEnterState/Tick

调谁:
  Owner.TrySetState → 尝试转换到 TurnInMoving/Idle/AirLoop
  Owner.PlayIfChanged(alias) → 根据 Gait 播放 walkMixer/runMixer/sprint
  Owner.ApplyTurnStepRotation() → 程序化转身
  mixer.Parameter = ActualLocalVelocity / moveSpeed → Vector2Mixer 驱动
```

## 状态条件

```
CanEnterState:
  Phase == GroundedMoving && !IsTurning

Tick 转换优先级:
  1. TurnInMoving  (GroundedMoving && IsTurning)
  2. Idle          (GroundedIdle)
  3. AirLoop       (!IsGrounded)
  默认: PlayIfChanged(gait → alias)
```

### 动画选择
```
gait → alias:
  Walk   → walkMixer
  Run    → runMixer
  Sprint → sprint
  其他   → walkMixer (fallback)
```

### Mixer 参数
```
parameter = ActualLocalVelocity / moveSpeed
if parameter.sqrMagnitude > 1 → Normalize()
mixer.Parameter = parameter
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | BaseLayer | Owner 引用，读取 Motor/Discrete 驱动 Mixer |

## 未来规划

无。
