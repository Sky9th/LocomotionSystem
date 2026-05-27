# SCharacterKinematic · 运动学输出结构体

> `Character/Kinematic/SCharacterKinematic.cs` — [Serializable] struct，运动学评估聚合输出

## 调用链

```
创建者:
  CharacterKinematic.Evaluate() → new SCharacterKinematic(...)

消费者:
  CharacterActor → LastKinematic 缓存
  GroundLocomotion → Simulate() 读取 Kinematic 字段
  Motor.Evaluate() → 读取 BodyForward/LocomotionHeading
  Stance.Evaluate() → 读取 GroundContact
  AnimationBrain.UpdateHeadLook() → 读取 LookDirection
  CharacterActor.Debug → Gizmo 绘制
```

## 公开属性

```csharp
public Vector3 Position { get; }                              // 角色世界位置
public Vector3 BodyForward { get; }                           // 身体朝向 (归一化)
public Vector3 LocomotionHeading { get; }                     // 运动朝向 (归一化)
public Vector2 LookDirection { get; }                         // 头部注视 (yaw/pitch 归一化 [-1,1])
public SGroundContact GroundContact { get; }                  // 地面接触信息
public SForwardObstacleDetection ForwardObstacleDetection { get; }  // 障碍检测结果

public static SCharacterKinematic Default { get; }            // 默认值
```

## 方法

### SCharacterKinematic 构造
```csharp
public SCharacterKinematic(Vector3 position, Vector3 bodyForward, Vector3 locomotionHeading,
    Vector2 lookDirection, SGroundContact groundContact, SForwardObstacleDetection forwardObstacleDetection)
```
- **用途**: 全字段构造，内部做朝向归一化（sqrMagnitude <= Epsilon 时 fallback 到 Vector3.forward）
- **调用者**: `CharacterKinematic.Evaluate()`

### Default
```csharp
public static SCharacterKinematic Default => new(...);
```
- **用途**: 默认空值，位置=0/朝向=forward/地面=None/障碍=None

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | SGroundContact | 地面接触字段 |
| 依赖 | SForwardObstacleDetection | 障碍检测字段 |
| 被依赖 | CharacterKinematic | 创建者 |
| 被依赖 | CharacterActor | 缓存和读取 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 扩展包含 Traversal 上下文 | 远期 | 代码预留 |
