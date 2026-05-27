# SCharacterMotor · 运动输出结构体

> `Character/Locomotion/SCharacterMotor.cs` — [Serializable] struct，速度/转角输出

## 调用链

```
创建者:
  Motor.Evaluate() → new SCharacterMotor(...)

消费者:
  CharacterActor → LastMotor 缓存
  Stance.Evaluate() → 读取 ActualPlanarVelocity/TurnAngle
  BaseMovingState.Tick → 读取 ActualLocalVelocity 驱动 Mixer
  BaseTurnInMovingState → 读取 DesiredLocalVelocity 判定前向意图
  BaseLayer.ApplyTurnStepRotation → 读取 TurnAngle
  CharacterActor.Debug → 速度/角度显示
```

## 公开属性

```csharp
public Vector2 DesiredLocalVelocity { get; }    // 局部空间期望速度 (x=左右, y=前后)
public Vector2 ActualLocalVelocity { get; }      // 局部空间实际速度 (加速度平滑后)
public Vector3 ActualPlanarVelocity { get; }      // 世界平面实际速度
public float TurnAngle { get; }                   // 身体→运动朝向有符号转角 (正值=右转)
public static SCharacterMotor Default { get; }    // 零值
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | Motor | 创建者 |
| 被依赖 | Stance | 读取 TurnAngle 和速度 |
| 被依赖 | BaseLayer | 读取 TurnAngle 做 ApplyTurnStepRotation |
| 被依赖 | BaseMovingState | 读取 ActualLocalVelocity 驱动 Mixer |

## 未来规划

无。
