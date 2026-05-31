# Motor · 速度与转角计算

> `L4_Locomotion/Ground/Motor.cs` — 纯 C# 类，速度平滑 + 转向角

## 调用链

```
被谁调:
  GroundLocomotion.Simulate() → motor.Evaluate(in kin, in intent, profile, dt)

输出:
  SCharacterMotor
```

## 方法

### Evaluate()
```csharp
internal SCharacterMotor Evaluate(
    in SCharacterKinematic kin, in SCharacterIntent intent,
    LocomotionProfile profile, float dt)
```
- 速度 = `intent.HasMovement ? gaitSpeed : 0f`（全步态速度，不做 scale 乘法）
- 速度平滑: `Vector2.MoveTowards(current, desired, acceleration * dt)`
- 转向角: `SignedAngle(bodyForward, locomotionHeading)`
- MotionSpeedScale 由 Stance 计算，不在 Motor 中处理
