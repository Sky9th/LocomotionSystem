# Motor · 速度与转角计算

> `Character/Locomotion/Motor.cs` — 纯 C# 类，输入 → 期望速度 → 平滑 → 世界速度 + 转向角

## 调用链

```
被谁调:
  GroundLocomotion.Simulate() → motor.Evaluate(in kin, in inp, profile, dt)

调谁:
  内部静态方法: ComputeDesired / Smooth / ConvertToWorld / SignedAngle

输出:
  SCharacterMotor
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | SCharacterKinematic | 读取 BodyForward/LocomotionHeading |
| 依赖 | SCharacterInputActions | 读取 MoveAction |
| 依赖 | LocomotionProfile | 读取 moveSpeed/acceleration |
| 输出 | SCharacterMotor | DesiredLocalVelocity/ActualLocalVelocity/ActualPlanarVelocity/TurnAngle |

## 内部状态

```csharp
private Vector2 currentLocalVelocity;  // 当前局部空间速度（平滑结果）
```

## 方法

### Evaluate()
```csharp
internal SCharacterMotor Evaluate(
    in SCharacterKinematic kin, in SCharacterInputActions inp,
    LocomotionProfile profile, float dt)
```
- **用途**: 计算期望速度 → 加速度平滑 → 转向角 → 输出 SCharacterMotor
- **参数**:
  - `kin` — 运动学数据（BodyForward/LocomotionHeading）
  - `inp` — 输入动作（MoveAction）
  - `profile` — 移动配置（moveSpeed/acceleration）
  - `dt` — 帧时间
- **返回**: 包含期望速度/实际速度/转向角的完整运动输出
- **调用者**: `GroundLocomotion.Simulate()`
- **备注**: 优先使用当前帧 move，无输入时 fallback lastMoveAction

### ComputeDesired()
```csharp
private static Vector2 ComputeDesired(SIActionMove action, float speed)
```
- **用途**: 输入动作 → 局部空间期望速度
- **参数**: `action` — 移动输入；`speed` — 最大速度
- **返回**: Vector2(x=左右, y=前后)，带强度缩放
- **备注**: 无输入或无速度时返回 Vector2.zero

### Smooth()
```csharp
private static Vector2 Smooth(Vector2 cur, Vector2 des, float accel, float dt)
```
- **用途**: Vector2.MoveTowards 加速度平滑
- **调用者**: Evaluate

### ConvertToWorld()
```csharp
private static Vector3 ConvertToWorld(Vector2 local, Vector3 heading)
```
- **用途**: 局部坐标 → 世界平面速度（X→X, Y→Z）
- **备注**: 当前为直接映射，heading 仅用于朝向参考

### SignedAngle()
```csharp
private static float SignedAngle(Vector3 body, Vector3 heading)
```
- **用途**: 身体朝向与运动朝向的水平有符号夹角（正值=右转）
- **调用者**: Evaluate

## 内部机制

### 速度计算流程

```
1. move = 当前帧有输入 ? inp.MoveAction : inp.LastMoveAction（fallback）
2. desired = ComputeDesired(move, moveSpeed)
   → normalized(rawInput) * Clamp01(rawInput.magnitude) * speed
3. currentLocalVelocity = Smooth(currentLocalVelocity, desired, acceleration, dt)
   → Vector2.MoveTowards
4. planar = Vector3(currentLocalVelocity.x, 0, currentLocalVelocity.y)
5. turnAngle = SignedAngle(bodyForward, locomotionHeading)
   → Vector3.SignedAngle(XZ投影, 水平投影, up)
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Gait 对应不同加速度（走/跑/冲刺不同加速值） | 待做 | 代码 TODO |
