# SGroundContact · 地面接触结构体

> `Character/Kinematic/SGroundContact.cs` — [Serializable] struct，地面接触检测结果

## 调用链

```
创建者:
  CharacterGroundDetection.EvaluateGroundContact() → new SGroundContact(...)
  CharacterKinematic.Accumulate/Stabilize → WithIsGrounded/WithStateDuration

消费者:
  CharacterKinematic → 地面锁定判断
  Stance.EvaluatePhase() → IsGrounded 判定 Phase
  BaseAirLoopState.CanEnterState → 读取 DistanceToGround
  BaseAirLandState.CanEnterState → 读取 DistanceToGround < threshold
```

## 公开属性

```csharp
public bool IsGrounded { get; }                    // 是否接触地面
public float DistanceToGround { get; }              // 距地面距离
public bool IsWalkableSlope { get; }               // 是否可走的斜坡
public Vector3 ContactPoint { get; }                // 接触点世界坐标
public Vector3 ContactNormal { get; }               // 接触面法线
public float StateDuration { get; }                 // 当前状态持续时长

public static SGroundContact None { get; }          // 空值 (IsGrounded=false, Distance=infinity)
```

## 方法

### WithIsGrounded()
```csharp
public SGroundContact WithIsGrounded(bool isGrounded)
```
- **用途**: 返回修改 IsGrounded 后的新实例（immutable 模式）
- **调用者**: `CharacterKinematic.Stabilize()`

### WithStateDuration()
```csharp
public SGroundContact WithStateDuration(float stateDuration)
```
- **用途**: 返回修改 StateDuration 后的新实例
- **调用者**: `CharacterKinematic.Accumulate()`

### None
```csharp
public static SGroundContact None => new(false, float.PositiveInfinity, false, Vector3.zero, Vector3.up);
```
- **用途**: 空值常量

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterGroundDetection | 创建者 |
| 被依赖 | CharacterKinematic | 地面锁定判断 |
| 被依赖 | Stance | Phase 判定 |
| 被依赖 | BaseLayer FSM States | AirLoop/AirLand 条件判定 |

## 未来规划

无。
