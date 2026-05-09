# Character 模块——数据结构设计

> 基于 target-callchain.md 的调用链，为每个环节分配合适的数据结构

---

## 1. 数据总览

```
调用链环节                           数据结构                        命名空间
───────────────────────────────────────────────────────────────────────────
Step 1: InputModule.ReadActions()  →  SCharacterInputActions          Game.Character.Input
Step 2: GameContext(camera)        →  SCameraContext                  全局
Step 3: Kinematic.Evaluate()       →  SCharacterKinematic             全局
Step 4: Motor.Evaluate()           →  SLocomotionMotor                全局
Step 5: Coordinator.Evaluate()     →  SLocomotionDiscrete             Game.Character.Locomotion.*
                                   →  SLocomotionTraversal
Step 6: Actor 组装                  →  SCharacterSnapshot              全局
                                   →  CharacterFrameContext (内部)    Game.Character.Components

消费者:
  CameraManager                    ←  SCharacterSnapshot.Kinematic
  AnimationController              ←  SCharacterSnapshot (转发给 Driver)
  LocomotionDriver                 ←  SCharacterSnapshot (参数)
  TraversalDriver                  ←  SCharacterSnapshot.Traversal (BuildRequest)
  LocomotionDebugOverlay           ←  SCharacterSnapshot (全量)
```

---

## 2. 每个环节的数据结构

### 2.1 Step 1: SCharacterInputActions

```
                                   InputModule
                                      │
    EventDispatcher                    ├── Subscribe<SMoveIAction>
    EventDispatcher                    ├── Subscribe<SLookIAction>
    EventDispatcher                    ├── Subscribe<SCrouchIAction>
    ...                               │
                                      ▼
                              ReadActions() → SCharacterInputActions
```

**当前状态**: ✅ 完整, 无需修改  
**用途**: Character 内部数据, 仅向下传递给 Locomotion  
**不推入 GameContext**: ✓  
**文件**: `Character/Input/SCharacterInputActions.cs`

```
struct SCharacterInputActions:
  MoveAction         SMoveIAction       // 移动摇杆
  LastMoveAction     SMoveIAction       // 上一帧移动 (为空时回退)
  LookAction         SLookIAction       // 鼠标视角
  CrouchAction       SCrouchIAction     // 蹲伏按钮
  ProneAction        SProneIAction      // 匍匐按钮
  WalkAction         SWalkIAction       // 步行按钮
  RunAction          SRunIAction        // 跑步按钮
  SprintAction       SSprintIAction     // 冲刺按钮
  JumpAction         SJumpIAction       // 跳跃按钮
  StandAction        SStandIAction      // 站立按钮
```

### 2.2 Step 2-3: SCharacterKinematic

```
                              CharacterKinematic.Evaluate()
                                      │
    CharacterGroundDetection           ├── 地面检测 (BoxCast + Raycast)
    CharacterHeadLook                  ├── 头部朝向 (yaw/pitch)
    CharacterObstacleDetection         └── 障碍物探针 (前方射线)
                                      │
                                      ▼
                              SCharacterKinematic
```

**当前状态**: ✅ 完整, 字段合理  
**用途**: Character 层的物理运动数据, 供 Locomotion 仿真消费 + CameraManager 读取  
**由 CharacterActor 推送至 GameContext**: ✓ (作为 SCharacterSnapshot 的子字段)  
**文件**: `Character/Kinematic/SCharacterKinematic.cs`

```
struct SCharacterKinematic:
  Position              Vector3          // 世界坐标 (含地面锁定修正)
  BodyForward           Vector3          // 角色朝向 (水平分量)
  LookDirection         Vector2          // 头部朝向 (yaw, pitch)
  GroundContact         SGroundContact   // 地面状态 (着地/距离/斜面)
  ForwardObstacleDetection  SForwardObstacleDetection  // 障碍物检测结果
```

**依赖**: `SGroundContact`, `SForwardObstacleDetection` (均在同一 `Kinematic/` 目录)

### 2.3 Step 4: SLocomotionMotor

```
                              LocomotionMotor.Evaluate()
                                      │
    LocomotionKinematics              ├── 速度计算 (期望/平滑/转换)
    CharacterHeadLook                 └── 运动朝向 (viewForward 水平分量)
                                      │
                                      ▼
                              SLocomotionMotor
```

**当前状态**: ✅ 已从 14 字段缩减到 8 字段  
**输入依赖**: `SCharacterKinematic.BodyForward` (算转向角), `SCharacterInputActions.MoveAction` (算速度)  
**文件**: `Character/Locomotion/Motor/SLocomotionMotor.cs`

```
struct SLocomotionMotor:
  DesiredLocalVelocity   Vector2          // 摇杆输入方向 × moveSpeed
  DesiredPlanarVelocity  Vector3          // 期望速度 (世界空间)
  ActualLocalVelocity    Vector2          // 平滑后的局部速度
  ActualPlanarVelocity   Vector3          // 平滑后的世界速度
  ActualSpeed            float            // 实际速度标量
  LocomotionHeading      Vector3          // 运动方向 (可能与 BodyForward 不同)
  TurnAngle              float            // BodyForward 与 LocomotionHeading 夹角
  IsLeftFootOnFront      bool             // 脚步 (未实现)
```

### 2.4 Step 5: SLocomotionDiscrete + SLocomotionTraversal

```
                              LocomotionCoordinator.Evaluate()
                                      │
    PhaseAspect                       ├── GroundContact → Phase
    GaitAspect                        ├── MoveAction → Gait
    PostureAspect                     ├── 按钮 → Posture
    LocomotionTurningGraph            ├── TurnAngle → IsTurning
    LocomotionTraversalGraph          └── Obstacle + Jump → Traversal
                                      │
                                      ▼
                              SLocomotionDiscrete
                              SLocomotionTraversal
```

**当前状态**: ✅ 完整, 字段合理  
**文件**: `Character/Locomotion/Coordination/Structs/`

```
struct SLocomotionDiscrete:
  Phase                 ELocomotionPhase     // GroundedIdle / Moving / Airborne / Landing
  Posture               EPosture             // Standing / Crouching / Prone
  Gait                  EMovementGait        // Idle / Walk / Run / Sprint
  Condition             ELocomotionCondition  // Normal / Injured (预留)
  IsTurning             bool                 // 原地转向中?

struct SLocomotionTraversal:
  Type                  ELocomotionTraversalType   // None / Climb / Vault / StepOver
  Stage                 ELocomotionTraversalStage  // Idle / Requested / Committed / Completed / Canceled
  ObstacleHeight        float                      // 障碍物高度 (m)
  ObstaclePoint         Vector3                    // 障碍物命中点
  TargetPoint           Vector3                    // 目标位置 (障碍物顶部)
  FacingDirection       Vector3                    // 穿越方向
```

### 2.5 CharacterFrameContext (内部数据总线) — 新增

在各步骤之间传递数据的容器, 仅在 CharacterActor.Update() 同帧内使用:

```csharp
// CharacterActor 内部持有, 每帧 struct 分配 → 栈上, 零 GC
internal struct CharacterFrameContext
{
    public SCharacterInputActions Input;
    public SCharacterKinematic Kinematic;
    public SLocomotionMotor Motor;
    public SLocomotionDiscrete Discrete;
    public SLocomotionTraversal Traversal;
}
```

**用途**: 在同帧内按调用顺序逐步填充, 确保数据前后依赖的步骤能访问到前序结果

**文件位置建议**: `Character/Components/CharacterFrameContext.cs` (与 CharacterActor 同目录, 因为它是 Actor 的内部机制)

### 2.6 SCharacterSnapshot (对外快照) — 重命名

当前 `SLocomotion` 重命名, 移除 `Animation` 字段 (动画输出是视觉驱动, 不应该是数据):

```csharp
// 替代当前 SLocomotion, 去掉 Animation 字段
[Serializable]
public struct SCharacterSnapshot
{
    public SCharacterKinematic Kinematic { get; }
    public SLocomotionMotor Motor { get; }
    public SLocomotionDiscrete DiscreteState { get; }
    public SLocomotionTraversal Traversal { get; }

    public static SCharacterSnapshot Default => new(
        SCharacterKinematic.Default,
        SLocomotionMotor.Default,
        SLocomotionDiscrete.Default,
        SLocomotionTraversal.None);
}
```

**文件位置建议**: `Character/Locomotion/Agent/SCharacterSnapshot.cs` (当前 SLocomotion.cs 位置)

---

## 3. 数据流向图

```
CharacterActor.Update()
  │
  ├── inputModule.ReadActions()
  │       → ctx.Input      [SCharacterInputActions]
  │
  ├── GameContext.TryGetSnapshot<SCameraContext>() → viewForward
  │
  ├── characterKinematic.Evaluate(profile, viewForward, dt)
  │       → ctx.Kinematic   [SCharacterKinematic]
  │
  ├── locomotion.Simulate(ref ctx, profile, viewForward, dt)
  │       ├── motor.Evaluate(in ctx.Kinematic, profile, in ctx.Input, ...)
  │       │       → ctx.Motor        [SLocomotionMotor]
  │       ├── coordinator.Evaluate(in ctx.Kinematic, in ctx.Motor, ...)
  │       │       → ctx.Discrete     [SLocomotionDiscrete]
  │       └── coordinator.CurrentTraversal
  │               → ctx.Traversal    [SLocomotionTraversal]
  │
  └── snapshot = new SCharacterSnapshot(
           ctx.Kinematic, ctx.Motor, ctx.Discrete, ctx.Traversal)
      │
      ├── GameContext.UpdateSnapshot(snapshot)
      │       → CameraManager [-400] (下帧读取)
      │       → CharacterAnimationController [0]
      │            └── fullBodyArbiter.Update()
      │                 └── LocomotionDriver.Update(snapshot, dt)
      │
      └── Dispatcher.Publish(snapshot)
              → LocomotionDebugOverlay
              → 未来: AI, 音效, 网络同步
```

---

## 4. 需要修改的项

### 4.1 重命名

| 旧 | 新 | 原因 |
|---|---|---|
| `SLocomotion` | `SCharacterSnapshot` | 当前名字暗示"仅有 Locomotion 数据", 实际包含 Kinematic + Motor + Discrete + Traversal |
| `SLocomotion.cs` | `SCharacterSnapshot.cs` | 文件重命名 |

### 4.2 删除

| 项 | 原因 |
|---|---|
| `SLocomotionAnimation` struct | 动画输出是视觉驱动, 不应该是数据快照的一部分 |
| `SLocomotionAnimationLayerSnapshot` struct | 同上 |
| `SLocomotion.Animation` 字段 | 同上 |

### 4.3 新增

| 项 | 位置 |
|---|---|
| `CharacterFrameContext` struct | `Character/Components/CharacterFrameContext.cs` |

### 4.4 消费者更新

| 文件 | 旧引用 | 新引用 |
|---|---|---|
| `CameraManager.cs` | `TryGetSnapshot<SCharacterKinematic>()` → `.Position` | `TryGetSnapshot<SCharacterSnapshot>()` → `.Kinematic.Position` |
| `LocomotionDriver.cs` | `TryGetSnapshot<SLocomotion>()` | 参数 `Update(SCharacterSnapshot snapshot, dt)` |
| `TraversalDriver.cs` | `TryGetSnapshot<SLocomotion>()` → `.Traversal` | 参数 `BuildRequest(SCharacterSnapshot snapshot)` → `.Traversal` |
| `LocomotionDebugOverlay.cs` | `TryGetSnapshot<SLocomotion>()` | `TryGetSnapshot<SCharacterSnapshot>()` |
| `LocomotionAgent.Debug.cs` | `snapshot.Motor.*` | `snapshot.Motor.*` (子字段不变) |
| `CharacterLocomotion.cs` | `PushSnapshot()` 操作 GameContext | 移除, 改为 `Simulate(ref ctx)` |
| `LocomotionAnimationController.cs` | `UpdateAnimations(SLocomotion, dt)` | `UpdateAnimations(SCharacterSnapshot, dt)` |
| `LocomotionAnimationContext.cs` | `Snapshot` 字段类型 `SLocomotion` | `SCharacterSnapshot` |
| FSM States/Conditions (所有) | `Owner.Snapshot` 类型 | 不变 (通过 BaseLayer 的 `context.Snapshot`) |

### 4.5 不修改的项

| 项 | 原因 |
|---|---|
| `SCharacterInputActions` | 字段完整, Character 内部数据, 不推 GameContext ✓ |
| `SCharacterKinematic` | 字段完整, 作为 `SCharacterSnapshot.Kinematic` 子字段 ✓ |
| `SLocomotionMotor` | 8 字段 (已缩减), 作为 `SCharacterSnapshot.Motor` 子字段 ✓ |
| `SLocomotionDiscrete` + `SLocomotionTraversal` | 字段完整, 作为子字段 ✓ |
| `SGroundContact`, `SForwardObstacleDetection` | `SCharacterKinematic` 的子依赖, 结构不变 ✓ |

---

## 5. LocomotionDriver 接口变更

当前 Driver 通过 `GameContext.TryGetSnapshot` 拉取数据。改为参数传入需要修改 `ICharacterAnimationDriver`:

```csharp
// 当前
public interface ICharacterAnimationDriver
{
    void Update(float deltaTime);
    CharacterAnimationRequest BuildRequest();
    ...
}

// 目标
public interface ICharacterAnimationDriver
{
    void Update(SCharacterSnapshot snapshot, float deltaTime);
    CharacterAnimationRequest BuildRequest(SCharacterSnapshot snapshot);
    ...
}
```

`CharacterAnimationController.Update()` 负责读取快照并传递给每个 Driver:
```csharp
private void Update()
{
    GameContext.Instance.TryGetSnapshot(out SCharacterSnapshot snapshot);

    fullBodyArbiter.Update(snapshot, Time.deltaTime);
    // ...
}

// DriverArbiter.Update 也需要接受 snapshot 参数
```

**影响文件**: `ICharacterAnimationDriver.cs`, `DriverArbiter.cs`, `CharacterAnimationController.cs`, `LocomotionDriver.cs`, `TraversalDriver.cs`
