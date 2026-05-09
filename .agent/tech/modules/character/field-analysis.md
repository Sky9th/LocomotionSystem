# SLocomotionMotor 字段归属分析

> 背景： Locomotion 子系统中断运行时（如攀爬动画、受击），不应继续产出快照数据。
> 但 CameraManager、动画层等仍需要 Position、GroundContact、LookDirection 等数据。

---

## 1. SLocomotionMotor 当前字段 (14个)

| # | 字段 | 类型 | 计算位置 |
|---|---|---|---|
| 1 | Position | Vector3 | actorTransform.position 快照 |
| 2 | DesiredLocalVelocity | Vector2 | 输入 + 速度计算 |
| 3 | DesiredPlanarVelocity | Vector3 | 局部→世界转换 |
| 4 | ActualLocalVelocity | Vector2 | SmoothVelocity() |
| 5 | ActualPlanarVelocity | Vector3 | SmoothVelocity() + 世界转换 |
| 6 | ActualSpeed | float | ActualPlanarVelocity.magnitude |
| 7 | LocomotionHeading | Vector3 | viewForward 水平分量 |
| 8 | BodyForward | Vector3 | modelRoot.forward 水平分量 |
| 9 | LookDirection | Vector2 | LocomotionHeadLook.Evaluate() |
| 10 | GroundContact | SGroundContact | 地面检测 + 稳定化 |
| 11 | ForwardObstacleDetection | SForwardObstacleDetection | 前方射线 + 高度探针 |
| 12 | TurnAngle | float | SignedAngle(BodyForward, LocomotionHeading) |
| 13 | IsLeftFootOnFront | bool | 脚步检测（未实现） |
| 14 | (constructor 隐含) | — | LocomotionHeading / BodyForward normalize |

---

## 2. 按消费者分类

### 2.1 外部消费者 (跨系统, 非 Locomotion 内部)

| 消费者 | 读取的字段 | 用途 | 需要 Locomotion 运行? |
|---|---|---|---|
| **CameraManager** | `Motor.Position` | Anchor 跟随位置 | ❌ 需要始终更新 |
| **LocomotionDebugOverlay** | 全部 14 个字段 | 实时调试显示 | — |

### 2.2 Locomotion 动画消费者 (FSM 内部)

| 消费者 | 读取的字段 | 用途 | 需要 Locomotion 运行? |
|---|---|---|---|
| BaseAirLoopState | `GroundContact.IsGrounded` | 进入/退出条件 | ❌ 飞行也需要 |
| BaseAirLandState | `GroundContact.DistanceToGround` | 落地判定 | ❌ 飞行也需要 |
| BaseMovingState | `ActualLocalVelocity` | Mixer 参数 | ✓ 移动时需要 |
| BaseTurnInMovingState | `DesiredLocalVelocity`, `TurnAngle` | 前向意图 + 选动画 | ✓ 转向时需要 |
| HeadLookLayer | `LookDirection` | Vector2Mixer 参数 | ❌ 头部始终需要 |
| TurnAngleStepRotationApplier | `TurnAngle` | 逐步旋转模型 | ✓ 转向时需要 |

### 2.3 Locomotion 逻辑消费者 (Coordinator 内部)

| 消费者 | 读取的字段 | 用途 | 需要 Locomotion 运行? |
|---|---|---|---|
| PhaseAspect | `GroundContact.IsGrounded` | Phase 判定 | ✓ 运动状态判定 |
| GaitAspect | `ActualPlanarVelocity` | Gait 判定 | ✓ 步态判定 |
| TraversalGraph | `ForwardObstacleDetection` | 可攀爬判定 | ✓ 穿越触发 |
| TurningGraph | `TurnAngle`, `LocomotionHeading` | 转向判定 | ✓ 原地转向 |

---

## 3. 判定结果

### 应提升至 Character 层 (不依赖 Locomotion 运行)

| 字段 | 理由 |
|---|---|
| **Position** | 角色世界坐标。CameraManager 跟踪、任何外部系统读取。 |
| **BodyForward** | 角色朝向。动画、AI、相机都可能需要。 |
| **LookDirection** | 头部朝向。HeadLookLayer 始终需要，即使 Locomotion 暂停。 |
| **GroundContact** | 地面状态。飞行/下落/落地判定都要用。Locomotion 暂停时依然需要知道是否在地面。 |
| **ForwardObstacleDetection** | 障碍物检测。Traversal 可中断 Locomotion，但触发条件依赖此数据。 |

### 应保留在 Locomotion 层 (仅运动时有效)

| 字段 | 理由 |
|---|---|
| **DesiredLocalVelocity** | 输入意图 → 速度计算。Locomotion 特有逻辑。 |
| **DesiredPlanarVelocity** | 同上，世界空间版。 |
| **ActualLocalVelocity** | 平滑后的速度。Mixer 参数需要，但仅移动时。 |
| **ActualPlanarVelocity** | 同上，世界空间版。 |
| **ActualSpeed** | 速度标量。Debug 用途。 |
| **LocomotionHeading** | 运动方向（可能不同于 BodyForward = 侧向移动）。 |
| **TurnAngle** | 身体朝向 vs 运动朝向的夹角。纯运动概念。 |
| **IsLeftFootOnFront** | 脚步检测。纯运动概念。 |

---

## 4. 建议方案

### 新增 `SCharacterKinematic` struct (Character 层)

```csharp
// 新文件: Assets/Scripts/Character/Structs/SCharacterKinematic.cs (建议)

public struct SCharacterKinematic
{
    public Vector3 Position { get; }
    public Vector3 BodyForward { get; }
    public Vector2 LookDirection { get; }
    public SGroundContact GroundContact { get; }
    public SForwardObstacleDetection ForwardObstacleDetection { get; }
}
```

### 缩减后的 `SLocomotionMotor`

```csharp
// 保留 8 个运动专属字段

public struct SLocomotionMotor
{
    public Vector2 DesiredLocalVelocity { get; }
    public Vector3 DesiredPlanarVelocity { get; }
    public Vector2 ActualLocalVelocity { get; }
    public Vector3 ActualPlanarVelocity { get; }
    public float ActualSpeed { get; }
    public Vector3 LocomotionHeading { get; }
    public float TurnAngle { get; }
    public bool IsLeftFootOnFront { get; }
}
```

### `SLocomotion` 结构调整

```
SLocomotion
  ├─ Kinematic: SCharacterKinematic (新, Character 层)
  │    ├─ Position
  │    ├─ BodyForward
  │    ├─ LookDirection
  │    ├─ GroundContact
  │    └─ ForwardObstacleDetection
  ├─ Motor: SLocomotionMotor (缩减)
  │    ├─ DesiredLocalVelocity
  │    ├─ ActualSpeed
  │    ├─ LocomotionHeading
  │    ├─ TurnAngle
  │    └─ ...
  ├─ DiscreteState: SLocomotionDiscrete (不变)
  └─ Traversal: SLocomotionTraversal (不变)
```

### 影响范围

| 修改项 | 说明 |
|---|---|
| `LocomotionMotor.Evaluate()` | 返回值拆分: 输出 `SCharacterKinematic` + `SLocomotionMotor` |
| `LocomotionAgent.Simulate()` | PushSnapshot 发布两个快照 |
| `CameraManager.HandleLocomotionSnapshot` | `payload.Motor.Position` → `payload.Kinematic.Position` |
| `HeadLookLayer` | `context.Snapshot.Motor.LookDirection` → `context.Kinematic.LookDirection` |
| FSM Conditions | `Motor.GroundContact.IsGrounded` → `Kinematic.GroundContact.IsGrounded` |
| `LocomotionDriver` | 构建 `LocomotionAnimationContext` 时传入两个数据源 |
| `LocomotionDebugOverlay` | 读取路径调整 |
| `TraversalDriver` | BuildRequest 读取 `Kinematic.ForwardObstacleDetection` |
| SLocomotion 构造 | 新增 `Kinematic` 字段 |

### 后续收益

1. **Locomotion 暂停时**： Character 层持续计算 Kinematic 快照，Camera 持续跟随，HeadLook 持续工作
2. **新子系统接入**： AbilitySystem、CombatSystem 直接从 `SCharacterKinematic` 读取 Position/Facing/Ground，不依赖 Locomotion
3. **地面检测可独立**： 将 `LocomotionGroundDetection` 从 Motor 移到 Character 层组件，任意系统可复用
4. **障碍物检测可独立**： 同理，TraversalSystem 可直接读取，不通过 Locomotion
