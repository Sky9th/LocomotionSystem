# SCharacterDiscrete · 角色离散运动状态

> `L4_Locomotion/Structs/SCharacterDiscrete.cs` — readonly struct，Stance 输出

## 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| Phase | ELocomotionPhase | Airborne / GroundedIdle / GroundedMoving |
| Posture | EPosture | Standing / Crouching / Prone |
| Gait | EMovementGait | Idle / Walk / Run / Sprint / Crawl |
| IsTurning | bool | 是否处于转向动画 |
| MotionSpeedScale | float | 有效速度/步态速度比值，Stance 缓存计算 |
| EffectiveMaxSpeed | float | gaitSpeed × MotionSpeedScale，Pathfinding 直接消费 |

## 数据来源

- Phase → Stance.EvaluatePhase (kin + motor)
- Gait/Posture → SCharacterIntent 透传
- IsTurning → Stance.EvaluateTurning (motor + profile)
- MotionSpeedScale → Stance 缓存 (profile + animProfile，仅 gait/posture 变化时重算)
- EffectiveMaxSpeed → Stance 计算 (gaitSpeed × MotionSpeedScale)

## 消费者

- AnimationBrain → 读 MotionSpeedScale 做动画速度匹配
- PathfindingAgent.SyncLocomotion → 读 EffectiveMaxSpeed 设置 ai.maxSpeed
