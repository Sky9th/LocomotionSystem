# CharacterFrameContext · 帧数据总线

> `Actor/CharacterFrameContext.cs` — struct，Actor 每帧填充

## 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| Intent | SCharacterIntent | 导演层产出的意图 |
| Kinematic | SCharacterKinematic | 运动学评估结果 |
| Motor | SCharacterMotor | 运动仿真输出 |
| Discrete | SCharacterDiscrete | 离散状态 (含 MotionSpeedScale/EffectiveMaxSpeed) |
| LocomotionProfile | LocomotionProfile | 角色物理速度配置，Locomotion 模块读取 |
| LocomotionAnimationProfile | LocomotionAnimationProfile | 动画原生速度配置，Locomotion 模块读取 |

## 数据流

```
CharacterActor 填充:
  ctx.Intent = director.Evaluate()
  ctx.LocomotionProfile = locomotionProfile
  ctx.LocomotionAnimationProfile = locomotionAnimationProfile
  ctx.Kinematic = characterKinematic.Evaluate(...)
  
GroundLocomotion 写入:
  ctx.Motor = motor.Evaluate(...)
  ctx.Discrete = stance.Evaluate(...)  // 含 MotionSpeedScale + EffectiveMaxSpeed
  
AnimationBrain 消费:
  characterAnimation.Apply(in ctx)
  
PathfindingAgent 消费:
  agent.SyncLocomotion(in ctx.Discrete)
```
