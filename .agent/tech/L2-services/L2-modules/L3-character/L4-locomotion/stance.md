# Stance · 离散状态判定

> `Character/Locomotion/Stance.cs` — 纯 C# 类，Phase/Gait/Posture/Turning 判定

## 调用链

```
被谁调:
  GroundLocomotion.Simulate() → stance.Evaluate(in motor, in kin, in inp, profile, dt)

调谁:
  私有方法: EvaluatePhase / EvaluateGait / EvaluatePosture / EvaluateTurning

输出:
  SCharacterDiscrete
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | SCharacterMotor | 读取 ActualPlanarVelocity/TurnAngle |
| 依赖 | SCharacterKinematic | 读取 GroundContact |
| 依赖 | SCharacterInputActions | 读取 Move/Sprint/Crouch/Prone/Stand/Jump 动作 |
| 依赖 | LocomotionProfile | 读取 turnEnterAngle/turnCompletionAngle/canSprint/canCrouch/canProne |
| 输出 | SCharacterDiscrete | Phase/Posture/Gait/IsTurning |

## 内部状态

```csharp
private EMovementGait currentGait = EMovementGait.Idle;     // 当前步态
private EPosture currentPosture = EPosture.Standing;         // 当前姿势
private bool isTurning;                                       // 当前是否转向中
```

## 方法

### Evaluate()
```csharp
internal SCharacterDiscrete Evaluate(
    in SCharacterMotor motor, in SCharacterKinematic kin,
    in SCharacterInputActions inp, LocomotionProfile profile, float dt)
```
- **用途**: 完整离散状态评估 → Phase + Gait + Posture + Turning
- **调用者**: `GroundLocomotion.Simulate()`

### EvaluatePhase()
```csharp
private static ELocomotionPhase EvaluatePhase(in SCharacterKinematic kin, in SCharacterMotor motor)
```
- **用途**: 根据地面接触和速度判定 Phase
- **逻辑**: 非着地 → Airborne；速度≈0 → GroundedIdle；否则 GroundedMoving
- **调用者**: Evaluate

### EvaluateGait()
```csharp
private EMovementGait EvaluateGait(in SCharacterInputActions inp, LocomotionProfile profile)
```
- **用途**: 步态判定 — 按键切换 Sprint/Run（toggle 逻辑）
- **逻辑**: 无输入 → Idle；Sprint 按键切换 → Sprint↔Run
- **调用者**: Evaluate
- **备注**: 持有 currentGait 状态实现 toggle；无输入时设为 Idle

### EvaluatePosture()
```csharp
private EPosture EvaluatePosture(in SCharacterInputActions inp, LocomotionProfile profile)
```
- **用途**: 姿势判定 — Stand ≥ Prone ≥ Crouch 优先级
- **逻辑**: Stand 请求 → Standing；Prone 请求且 canProne → Prone；Crouch 请求且 canCrouch → Crouching
- **调用者**: Evaluate

### EvaluateTurning()
```csharp
private bool EvaluateTurning(in SCharacterMotor motor, in SCharacterKinematic kin,
    LocomotionProfile profile, float dt, ELocomotionPhase phase)
```
- **用途**: 转向判定 — TurnAngle 超过 enterAngle 进入，低于 completionAngle 退出
- **逻辑**: 非着地态时返回 false；abs(TurnAngle) ≥ enter → isTurning=true；abs(TurnAngle) ≤ completion → isTurning=false
- **调用者**: Evaluate

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Walk/Run/Toggle 逻辑在 WalkAction 和 RunAction 接入后完善 | 待做 | 代码预留 |
| Crawl 步态实现 | 待做 | 枚举已定义 |
