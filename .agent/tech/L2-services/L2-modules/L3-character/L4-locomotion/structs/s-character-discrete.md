# SCharacterDiscrete · 离散状态结构体

> `Character/Locomotion/SCharacterDiscrete.cs` — [Serializable] readonly struct，角色离散状态

## 调用链

```
创建者:
  Stance.Evaluate() → new SCharacterDiscrete(...)

消费者:
  CharacterActor → LastDiscrete 缓存
  BaseIdleState.CanEnterState → Phase==GroundedIdle && !IsTurning
  BaseMovingState.CanEnterState → Phase==GroundedMoving && !IsTurning
  BaseTurnInPlaceState.CanEnterState → Phase==GroundedIdle && IsTurning
  BaseIdleToMovingState.CanEnterState → Phase==GroundedMoving && IsTurning
  BaseTurnInMovingState.CanEnterState → Phase==GroundedMoving && IsTurning
  BaseAirLoopState.CanEnterState → !IsGrounded
  BaseMovingState.Tick → Gait 决定播放哪个 Mixer
  SprintStaminaRule.ShouldActivate → Gait==Sprint
  CharacterActor.Debug → Gizmo 标签
```

## 公开属性

```csharp
public ELocomotionPhase Phase { get; }       // 运动阶段 (GroundedIdle/GroundedMoving/Airborne/Landing)
public EPosture Posture { get; }             // 姿势 (Standing/Crouching/Prone)
public EMovementGait Gait { get; }           // 步态 (Idle/Walk/Run/Sprint/Crawl)
public bool IsTurning { get; }               // 是否正在转向
public static SCharacterDiscrete Default { get; }  // (GroundedIdle/Standing/Idle/false)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | Stance | 创建者 |
| 被依赖 | BaseLayer FSM States | 所有 7 个状态的 CanEnterState/Tick 判定 |
| 被依赖 | SprintStaminaRule | Gait 判定 |

## 未来规划

无。
