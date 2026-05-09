# 仿真 Locomotion 最终设计

## 1. 目录结构

```
Character/Locomotion/
├── ILocomotionSimulator.cs          ← 接口
├── GroundLocomotion.cs              ← 默认实现 (走/跑/蹲/转向)
├── SCharacterMotor.cs               ← 运动数据 (4字段)
├── SCharacterDiscrete.cs            ← 状态标签 (4字段)
├── SLocomotionState.cs              ← 包装 Motor + Discrete
└── Config/
    └── LocomotionProfile.cs         ← 参数 SO
```

## 2. 接口

```csharp
// ILocomotionSimulator
internal interface ILocomotionSimulator
{
    void Simulate(ref CharacterFrameContext ctx, LocomotionProfile profile, float dt);
}
```

单方法。`CharacterActor` 只依赖接口，不依赖具体实现。换角色类型只需换实现（`GroundLocomotion` / `FlightLocomotion`）。

## 3. 数据 struct

### SCharacterMotor

```
DesiredLocalVelocity   Vector2     ← LocomotionKinematics.ComputeDesired(...)
ActualLocalVelocity    Vector2     ← SmoothVelocity(desired, accel, dt)
ActualPlanarVelocity   Vector3     ← ConvertLocalToWorld(actualLocal, heading)
TurnAngle              float       ← SignedAngle(BodyForward, LocomotionHeading)
```

4 字段，连续值，纯运动学计算，不依赖任何状态判定。

### SCharacterDiscrete

```
Phase       ELocomotionPhase      ← !IsGrounded→Airborne, speed≈0→Idle, speed>0→Moving
Posture     EPosture              ← Stand/Crouch/Prone 按钮
Gait        EMovementGait         ← 无输入→Idle, 有输入默认Run, Sprint按钮→切换
IsTurning   bool                  ← TurnAngle>enter 且相机稳定→true, <completion→false, 仅地面
```

4 字段，离散标签，由 Motor + Input 判定。

### SLocomotionState

```
Motor    SCharacterMotor
Discrete SCharacterDiscrete
```

包装 struct，作为 SCharacterSnapshot 的子字段。

## 4. GroundLocomotion（默认实现）

### 方法清单

| 方法 | 返回值 | 说明 |
|---|---|---|
| `Simulate(ref ctx, profile, dt)` | void | 入口, 填充 ctx.Motor + ctx.Discrete |
| `EvaluateVelocity(in kinematic, in input, profile, dt)` | SCharacterMotor | 速度计算 + 平滑 + 转向角 |
| `EvaluatePhase(in kinematic, in motor)` | ELocomotionPhase | 地面→Idle/Moving, 空中→Airborne |
| `EvaluateGait(in input, profile)` | EMovementGait | 无输入→Idle, 有输入默认Run, Sprint切换 |
| `EvaluatePosture(in input, profile)` | EPosture | Stand/Crouch/Prone 按钮 |
| `EvaluateTurning(in motor, in kinematic, profile, dt, phase)` | bool | TurnAngle 双阈值 + 相机稳定检测 |

### 内部状态

| 字段 | 类型 | 说明 |
|---|---|---|
| `currentLocalVelocity` | Vector2 | 速度平滑状态 |
| `currentGait` | EMovementGait | 步态保持 |
| `currentPosture` | EPosture | 姿态保持 |
| `isTurning` | bool | 转向状态保持 |
| `lastDesiredYaw` | float | 上一帧 heading 的 Yaw 角（防晃鼠标误触） |
| `lookStabilityTimer` | float | 相机朝向稳定持续时间 |

### EvaluateTurning（转向判定，含相机稳定检测）

```csharp
private bool EvaluateTurning(in SCharacterMotor motor, in SCharacterKinematic kin,
    LocomotionProfile profile, float dt, ELocomotionPhase phase)
{
    // 只在地面允许转向
    if (phase != ELocomotionPhase.GroundedIdle && phase != ELocomotionPhase.GroundedMoving)
        { isTurning = false; return false; }

    // 相机 Yaw 稳定性检测（防止快速晃鼠标误触发）
    var yaw = Mathf.Atan2(kin.LocomotionHeading.x, kin.LocomotionHeading.z) * Mathf.Rad2Deg;
    var yawDelta = Mathf.Abs(Mathf.DeltaAngle(yaw, lastDesiredYaw));
    lastDesiredYaw = yaw;

    if (yawDelta <= profile.lookStabilityAngle) lookStabilityTimer += dt;
    else lookStabilityTimer = 0f;

    var absAngle = Mathf.Abs(motor.TurnAngle);
    var wantsTurn = absAngle >= profile.turnEnterAngle;
    var lookStable = lookStabilityTimer >= profile.lookStabilityDuration;
    var turnDone = absAngle <= profile.turnCompletionAngle;

    if (!isTurning && wantsTurn && lookStable) isTurning = true;
    else if (isTurning && turnDone) isTurning = false;

    return isTurning;
}
```

### 静态工具函数（内嵌, 不单独建文件）

| 函数 | 说明 |
|---|---|
| `ComputeDesired(moveAction, moveSpeed)` | 摇杆→局部速度 |
| `Smooth(current, desired, accel, dt)` | MoveTowards |
| `ConvertToWorld(local, heading)` | 局部→世界 |
| `SignedAngle(bodyForward, heading)` | 朝向夹角 |

## 5. Profile 驱动角色差异

```
LocomotionProfile (SO)
├── moveSpeed                float   人类4, 僵尸1.5
├── acceleration             float   人类5, 僵尸2
├── canSprint                bool    人类true, 僵尸false
├── canCrouch                bool    人类true, 僵尸false
├── canProne                 bool    人类true, 僵尸false
├── turnEnterAngle           float   65°
├── turnCompletionAngle      float   5°
├── lookStabilityAngle       float   2°       ← 相机 Yaw 变化阈值
└── lookStabilityDuration    float   0.15s    ← 相机需稳定的持续时间
```

（删除了旧方案中 `turnExitAngle`、`turnDebounceDuration` 两个未被实际使用的字段）

不同角色只需换 Profile 资产，零代码修改。

## 6. 调用链

```
CharacterActor.Update()
  ctx.Input    = inputModule.ReadActions()
  ctx.Kinematic = characterKinematic.Evaluate(profile, viewForward, dt)
  locomotionSimulator.Simulate(ref ctx, locomotionProfile, dt)    ← Step 4
  snapshot = new SCharacterSnapshot(ctx.Kinematic,
              new SLocomotionState(ctx.Motor, ctx.Discrete))
  GameContext.UpdateSnapshot(snapshot)
```

## 7. 需要新增/修改的文件

| 操作 | 文件 |
|---|---|
| 新建 | `Locomotion/ILocomotionSimulator.cs` |
| 新建 | `Locomotion/GroundLocomotion.cs` |
| 新建 | `Locomotion/SCharacterMotor.cs` |
| 新建 | `Locomotion/SCharacterDiscrete.cs` |
| 新建 | `Locomotion/SLocomotionState.cs` |
| 新建 | `Locomotion/Config/LocomotionProfile.cs` |
| 修改 | `Components/CharacterFrameContext.cs` (+Motor +Discrete) |
| 修改 | `Structs/SCharacterSnapshot.cs` (+Locomotion) |
| 修改 | `Components/CharacterActor.cs` (+simulator + Simulate) |

## 8. 对比旧 Apect 方案

| | 旧 | 新 |
|---|---|---|
| 文件数 | 6 (Aspect×3 + Graph + Interface + Coordinator) | 2 (Interface + GroundLocomotion) |
| 参数冗余 | 所有函数必须接收 kinematic/motor/input | 每个函数只取需要的 |
| 角色类型 | 不支持 | `new XxxLocomotion : ILocomotionSimulator` |
| Profile | 8个字段, 无行为开关 | + canSprint/canCrouch/canProne |
