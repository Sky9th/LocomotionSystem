# Motor · 移动速度计算器

> `Character/Locomotion/Ground/Motor.cs` — 内部类，计算 SCharacterMotor

## 调用链

```
被谁调:
  GroundLocomotion.Simulate() → motor.Evaluate(kin, intent, profile, dt)

调谁:
  (纯计算，无外部调用)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | GroundLocomotion | 唯一调用者 |
| 依赖 | SCharacterIntent | 读取 OverrideMovementVelocity/ExternalMovementVelocity |
| 依赖 | SCharacterKinematic | 读取 BodyForward/LocomotionHeading |
| 依赖 | LocomotionProfile | 读取 acceleration/GetSpeedForGait |

## 方法

### Evaluate()
```csharp
internal SCharacterMotor Evaluate(
    in SCharacterKinematic kin, in SCharacterIntent intent,
    LocomotionProfile profile, float dt)
```
- **用途**: 计算当前帧的移动速度输出
- **Override 路径** (`intent.OverrideMovementVelocity=true`):
  1. `externalVel.y = 0` — 只用平面分量
  2. `localVel = ConvertToLocal(externalVel, heading)` — world→local 转换
  3. `currentLocalVelocity = localVel` — 更新缓存（供后续非 override 帧使用）
  4. 返回 `SCharacterMotor(localVel, localVel, externalVel, turnAngle)` — 跳过 Smooth
- **非 Override 路径**: 
  1. `speed = intent.HasMovement ? GetSpeedForGait(gait) : 0`
  2. `Smooth(current, desired, acceleration, dt)` 
  3. `ConvertToWorld(local, heading)` → world planar
  4. 返回 `SCharacterMotor(desired, smoothed, planar, turnAngle)`
- **调用者**: `GroundLocomotion.Simulate()`
- **备注**: Override 路径跳过自身 Smooth 因为 AIPath 已内置平滑（`CalculateAccelerationToReachPoint` + `ClampVelocity`），不应二次平滑。

### ConvertToLocal()
```csharp
private static Vector2 ConvertToLocal(Vector3 world, Vector3 heading)
```
- **用途**: world-space 速度 → local-space（`ConvertToWorld` 的逆运算）
- **返回**: `(Dot(world, right), Dot(world, forward))`
- **备注**: heading 与 world 方向一致时，lateral 分量为 0（纯前进）

### ConvertToWorld()
```csharp
private static Vector3 ConvertToWorld(Vector2 local, Vector3 heading)
```
- **用途**: local-space → world-space

### Smooth()
```csharp
private static Vector2 Smooth(Vector2 cur, Vector2 des, float accel, float dt)
```
- **用途**: 使用 `Vector2.MoveTowards` 平滑，最大步长 = `accel * dt`

### SignedAngle()
```csharp
private static float SignedAngle(Vector3 body, Vector3 heading)
```
- **用途**: BodyForward → LocomotionHeading 有符号夹角（XZ 平面）
- **返回**: clamped [-180°, 180°]

## 设计决策

| 决策 | 原因 |
|------|------|
| Override 路径不调用 Smooth | AIPath 内部已平滑，二次 Smooth 会延迟响应 |
| Override 路径也更新 currentLocalVelocity | 后续切回非 override 时 Smooth 从正确当前值出发 |
| ConvertToLocal 与 ConvertToWorld 对称 | 保持代码可读性，两者复用相同 heading→basis 逻辑 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| WASD 输入重新接入（非 override 路径） | 待做 | Phase 4+ |
