# LocomotionSystem - 深度项目分析

> 分析日期：2025-04-25
> 源文件总数：~100个 C# 文件
> 第三方库：Kybernetik Animancer (含 Animancer.FSM)、Cinemachine、Unity Input System、TextMesh Pro

---

## 1. 项目概述

### 1.1 项目定位

这是一个基于 **Unity (URP)** 的**第三人称角色运动系统**项目。该项目是更大游戏"Project 3"（生存/建造/丧尸游戏）的核心移动和动画子系统。

### 1.2 目标游戏概念

根据 `docs/project3-backlog.md`，Project 3 是一款末日生存游戏，包含：
- 资源管理（物品、仓库、背包）
- 生存机制（饥饿、口渴、体力、生命）
- 网格化建造系统（放置/拆除，材料消耗与返还）
- 丧尸 AI（NavMesh、视觉感知、追逐、攻击）
- NPC 招募、指令与任务分配
- 战斗系统（近战、枪械、熟练度）
- 农业、工具、烹饪系统
- 科技树与图纸解锁
- 尸潮触发机制与行为

### 1.3 当前开发状态

- **运动系统流水线** 已完成：Motor（运动学） -> Coordinator（离散状态） -> Animation
- **动画 FSM** 7个状态已全部实现
- **输入系统** 完成（Move/Look + 8个按钮）
- **相机系统** Cinemachine 基础跟随
- **地面检测** 已完成（Ray + BoxCast，含稳定化/防抖）
- **障碍物检测** 已完成（含顶部高度探测）
- **攀爬穿越系统** 图逻辑已建立（早中期实现）
- **脚步动画层** 为存根实现
- **其他游戏系统** 尚未开发

---

## 2. 项目架构总览

```
┌──────────────────────────────────────────────────────────────────────┐
│                      GameManager (Bootstrap)                         │
│                          ExecutionOrder: -500                        │
│                    Singletons: DontDestroyOnLoad                     │
│                                                                      │
│  Discovery: GetComponentsInChildren<BaseService>()                   │
│                                                                      │
│  ┌─ Phase 1: Register ──────────────────────────────────────────┐   │
│  │  EventDispatcher.Discovery → Register(GameContext)            │   │
│  │  All BaseService children → Register(GameContext)             │   │
│  │  Each service calls context.RegisterService<T>(this)         │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─ Phase 2: AttachDispatcher ─────────────────────────────────┐   │
│  │  Each service receives shared EventDispatcher reference      │   │
│  │  OnDispatcherAttached() virtual hook                         │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─ Phase 3: ActivateSubscriptions ────────────────────────────┐   │
│  │  Each service can subscribe to EventDispatcher events        │   │
│  │  OnSubscriptionsActivated() virtual hook                     │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌─ Phase 4: NotifyInitialized (OnServicesReady) ─────────────┐   │
│  │  All services are fully bootstrapped                         │   │
│  │  Cross-service initialization can occur here                 │   │
│  └──────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
```

### 2.1 服务注册表

| 服务 | 职责 | ExecutionOrder |
|---|---|---|
| **EventDispatcher** | 事件总线（最先注册） | default |
| **CameraManager** | Cinemachine 相机控制 | -400 |
| **GameState** | 游戏状态机 | default |
| **InputManager** | 输入动作生命周期管理 | default |
| **PlayerManager** | 玩家生成与快照 | default |
| **TimeScaleManager** | 全局时间缩放到映射 | default |
| **UIManager** | UI 屏幕/叠加层管理 | default |

---

## 3. 核心子系统详解

### 3.1 GameContext - 服务注册表与快照缓存

```csharp
// 服务注册表 (Dictionary<Type, object>)
RegisterService<T>(T service)
TryResolveService<T>(out T service)

// 快照缓存 (Dictionary<Type, object>)
UpdateSnapshot<T>(T snapshot) where T : struct
TryGetSnapshot<T>(out T snapshot) where T : struct
```

GameContext 具有双重角色：
- **服务定位器**：所有服务通过 `RegisterService<T>()` 注册，通过 `TryResolveService<T>()` 查找
- **状态缓存**：`UpdateSnapshot<T>()` 存储不可变结构体快照，`TryGetSnapshot<T>()` 跨系统读取

### 3.2 EventDispatcher - 事件总线

```csharp
// 类型安全的事件系统
Subscribe<TPayload>(Action<TPayload, MetaStruct> handler)
Unsubscribe<TPayload>(Action<TPayload, MetaStruct> handler)
Publish<TPayload>(TPayload payload)
```

- 使用 `Dictionary<Type, List<Delegate>>` 存储监听器
- 每次发布附带 `MetaStruct`（时间戳 + 帧索引）
- 发布时通过快照数组防止迭代中修改
- Inspector 面板显示已注册的监听器数量（`inspectorListeners`）

### 3.3 BaseService - 服务生命周期

所有服务继承自 `BaseService`，提供严格的生命周期：
```
Register(GameContext) → OnRegister(context)    // Phase 1: 注册
AttachDispatcher()    → OnDispatcherAttached()  // Phase 2: 附加事件总线
ActivateSubscriptions() → OnSubscriptionsActivated() // Phase 3: 激活事件订阅
NotifyInitialized()   → OnServicesReady()       // Phase 4: 所有服务就绪
```

内建服务缓存 (`serviceCache`) 防止跨服务依赖的重复解析。

---

## 4. 输入系统

### 4.1 输入流水线

```
Unity Input System (InputActionAsset)
    ↓
InputActionHandler (ScriptableObject 基类)
    ↓ Execute(InputAction.CallbackContext)
    ↓ 发布结构化 DTO 到 EventDispatcher
    ↓
InputManager (生命周期管理)
    ↓ 根据 GameState 启用/禁用处理程序
    ↓ EnforceHandlerStatePermissions()
    ↓
LocomotionInputModule (Agent 内部)
    ↓ 订阅 Dispatcher 事件（9种输入 + 相机）
    ↓ 聚合到 SLocomotionInputActions
    ↓ 每帧清空单帧信号（IsRequested/IsReleased）
```

### 4.2 输入处理程序清单

| 处理程序 | 输入动作 | 发布类型 | 状态限制 |
|---|---|---|---|
| `IAPlayerMove` | Move (Vector2) | `SMoveIAction` | Playing |
| `IAPlayerLook` | Look (Vector2) | `SLookIAction` | Playing |
| `IAPlayerCrouch` | Crouch (Button) | `SCrouchIAction` | Playing |
| `IAPlayerJump` | Jump (Button) | `SJumpIAction` | Playing |
| `IAPlayerProne` | Prone (Button) | `SProneIAction` | Playing |
| `IAPlayerRun` | Run (Button) | `SRunIAction` | Playing |
| `IAPlayerSprint` | Sprint (Button) | `SSprintIAction` | Playing |
| `IAPlayerStand` | Stand (Button) | `SStandIAction` | Playing |
| `IAPlayerWalk` | Walk (Button) | `SWalkIAction` | Playing |
| `IASystemTimeSlow` | TimeSlow (Button) | `STimeScaleIAction` | Playing |
| `IASystemTimeResume` | TimeResume (Button) | `STimeScaleIAction` | Playing |
| `IAUIEscape` | Escape (Button) | `SUIEscapeIAction` | Any |

### 4.3 按钮输入状态机

`SButtonInputState` 包装了按钮的 4 种状态：
- `IsPressed`：当前是否按下
- `IsRequested`：本帧刚按下（Pressed + Performed）
- `IsReleased`：本帧刚释放（NotPressed + Canceled）
- `Phase`：Unity Input System 原始阶段

所有按钮动作（Crouch/Jump/Prone/Run/Sprint/Stand/Walk）使用相同的模式。

### 4.4 LocomotionInputModule - Agent 内部输入聚合

```
内部字段：
  moveAction, lastMoveAction     ← SMoveIAction
  lookAction                     ← SLookIAction
  crouch/prone/walk/run/sprint/jump/stand Action ← 对应 IAction
  cameraControl                  ← SCameraContext (仅玩家 Agent)

每帧读取:
  ReadActions(out SLocomotionInputActions)
    → 聚合所有按钮
    → 清空单帧信号（IsRequested/IsReleased）
  
  ReadCameraControl(out hasControl, out SCameraContext)
    → 仅 isPlayer=true 的 Agent 接收
```

延迟类型检测通过 `PutAction<T>()` 中的 `typeof()` 分支实现，避免了泛型约束在 delegate 中的复杂性，保持类型安全。

---

## 5. 运动系统核心流水线

### 5.1 LocomotionAgent.Simulate() 完整流程

```
每帧 Update:
  1. 从 LocomotionInputModule 读取聚合输入
     → SLocomotionInputActions inputActions
     → SCameraContext cameraControl (仅玩家)

  2. 计算 viewForward
     → hasCameraControl ? cameraControl.AnchorRotation * Vector3.forward : Vector3.zero

  3. Motor.Evaluate(profile, inputActions, viewForward, deltaTime)
     → 计算运动学（期望速度、实际平滑速度）
     → 计算 movementHeading（玩家朝向 = 相机视角方向）
     → 计算 head look 方向
     → 计算 turn angle（身体朝向 vs 运动朝向）
     → 地面检测 + 稳定性处理
     → 障碍物检测 + 顶部探测
     → 输出 SLocomotionMotor

  4. locomotionCoordinator.Evaluate(motorOutput, profile, inputActions, deltaTime)
     → 计算 SLocomotionDiscrete（Phase/Gait/Posture/Condition/IsTurning）
     → 计算 SLocomotionTraversal（穿越请求生命周期）
     → 如果穿越 Committed → 覆盖离散状态为 ActionControlled

  5. animancerPresenter.Evaluate(baseSnapshot, deltaTime)
     → 更新所有动画层
     → 输出 SLocomotionAnimation

  6. 组装最终快照
     → SLocomotion(motor, discrete, traversal, animation)

  7. PushSnapshot()
     → GameContext.UpdateSnapshot(snapshot)
     → EventDispatcher.Publish(snapshot)
```

### 5.2 LocomotionMotor - 运动学引擎

```
LocomotionMotor 内部状态:
  currentVelocity          ← Vector3, 当前世界空间实际速度
  currentLocalVelocity     ← Vector2, 局部空间速度
  previousRawGroundContact ← SGroundContact, 前一帧原始地面
  previousGroundContact    ← SGroundContact, 前一帧稳定地面

LocomotionMotor.Evaluate() 每帧:
  1. UpdateKinematics():
     - 解析 move action（若当前帧为空则使用上一帧）
     - 计算 desiredLocalVelocity = ComputeDesiredPlanarVelocity(moveAction, moveSpeed)
     - 平滑速度 = SmoothVelocity(currentLocal, desired, acceleration, deltaTime)
     - 转换到世界空间（基于 locomotionHeading）

  2. LocomotionHeadLook.Evaluate():
     - 计算局部朝向（从相机方向到身体朝向的局部欧拉角）
     - 受 maxHeadYawDegrees / maxHeadPitchDegrees 限制

  3. LocomotionKinematics.ComputeSignedPlanarTurnAngle():
     - 计算身体朝向与运动朝向之间的有符号角度（水平面）
     - Vector3.SignedAngle(bodyForward, headingForward, Vector3.up)
     - 正值 = 右转

  4. EvaluateGroundContactAndApplyConstraints():
     - 原始地面检测（Ray + BoxCast）
     - 累积态持续时间
     - 防抖稳定化（reacquireDebounce）
     - 地面锁定（修正 Y 坐标）
     - 更新 Rigidbody FreezePositionY 约束

  5. LocomotionObstacleDetection.TryDetectForwardObstacle():
     - 前方射线检测
     - 判断斜坡 vs 障碍物
     - 高度探针（从 maxClimbHeight 向下射线）
     - 计算 canClimb/canVault/canStepOver

  6. 组装 SLocomotionMotor 输出
```

#### 5.2.1 地面检测（LocomotionGroundDetection）

```
EvaluateGroundContact() 两步法:
  1. TrySampleStandingByBox() - BoxCast 向下
     → 最稳定的着地测试 → isGrounded, isWalkableSlope
  
  2. TrySampleDistanceByRay() - 射线向下
     → 长距离测距 → distanceToGround
     → 如果已着地，用 BoxCast 的 normal 替代 Ray 的 normal

地面稳定化（LocomotionMotor 内部）:
  1. AccumulateGroundContactStateDuration()
     → 同态累积时间，换态重置为 0
  
  2. StabilizeGroundContact()
     → reacquireDebounceDuration: 离地后必须等待的最短重新着地时间
     → 仅当 (raw IsGrounded && canReacquire) 时才允许着地
```

#### 5.2.2 障碍物检测（LocomotionObstacleDetection）

```
TryDetectForwardObstacle():
  1. 前方射线（probeVerticalOffset 高度）
     → 无命中 → 返回无命中快照
     
  2. 判断 surfaceAngle = 法线与向上的夹角
     → isSlope = surfaceAngle <= maxSlopeAngle
     → isObstacle = !isSlope

  3. 如果 isObstacle:
     → 高度探针起点 = hitPoint + forward * 0.05, y = actor.y + maxClimbHeight + 0.05
     → 向下射线 maxClimbHeight * 2 距离
     → 命中 → hasTopSurface, canClimb = (obstacleHeight <= maxClimbHeight)

  4. 返回 SForwardObstacleDetection（19个字段）
     HasHit, HasTopSurface, IsSlope, IsObstacle,
     CanClimb, CanVault, CanStepOver,
     Distance, ObstacleHeight, SurfaceAngle,
     Point, Normal, TopPoint, TopNormal, Direction,
     Collider, HitLayer
```

### 5.3 速度运动学（LocomotionKinematics）

```
ComputeDesiredPlanarVelocity():
  X = 左右 (strafe), Y = 前后 (forward)
  计算: normalized(rawInput) * intensity * moveSpeed
  其中 intensity = Clamp01(rawInput.magnitude)

ConvertLocalToWorldPlanarVelocity():
  forward = locomotionHeading.y=0, normalize
  right = Cross(Vector3.up, forward)
  result = forward * local.y + right * local.x

ComputeSignedPlanarTurnAngle():
  将身体朝向和运动朝向投影到 XZ 平面
  Vector3.SignedAngle(bodyFlat, headingFlat, Vector3.up)
  正值 = 向右转

SmoothVelocity():
  3D: Vector3.MoveTowards(current, desired, acceleration * deltaTime)
  2D: Vector2.MoveTowards(current, desired, acceleration * deltaTime)
```

### 5.4 头部朝向（LocomotionHeadLook）

```
EvaluatePlanarHeading():
  viewForward.y = 0; normalize
  → 玩家相机朝向的水平分量

Evaluate():
  1. 计算 targetRotation = LookRotation(viewForward)
  2. 计算 bodyRotation（从 modelRoot.forward 或 rootTransform.rotation）
  3. localDelta = Inverse(bodyRotation) * targetRotation
  4. yaw = NormalizeAngle180(localDelta.eulerAngles.y)
     pitch = -NormalizeAngle180(localDelta.eulerAngles.x)
  5. Clamp yaw by maxHeadYawDegrees, pitch by maxHeadPitchDegrees
  6. 返回 Vector2(yaw, pitch)
```

---

## 6. 离散运动状态系统

### 6.1 架构

```
ILocomotionCoordinator (接口)
    ↓
LocomotionCoordinatorBase (抽象基类)
    ├── LocomotionGraph (组合 Phase + Gait + Posture Aspect)
    ├── LocomotionTraversalGraph (穿越状态)
    └── LocomotionTurningGraph (原地转向状态)
    ↓
LocomotionCoordinatorHuman (具体实现)
    → 仅注入默认图实例，未来可扩展人类特有逻辑
```

### 6.2 状态切面（Aspect）

每个 Aspect 独立维护一个正交维度：

| Aspect | 枚举类型 | 状态 |
|---|---|---|
| **PhaseAspect** | `ELocomotionPhase` | GroundedIdle / GroundedMoving / Airborne / Landing |
| **GaitAspect** | `EMovementGait` | Idle / Walk / Run / Sprint / Crawl |
| **PostureAspect** | `EPosture` | Standing / Crouching / Prone |
| Condition | `ELocomotionCondition` | Normal / InjuredLight / InjuredHeavy (占位) |

#### PhaseAspect
```
!IsGrounded → Airborne
IsGrounded && ActualPlanarVelocity.sqrMagnitude <= 0 → GroundedIdle
IsGrounded && ActualPlanarVelocity.sqrMagnitude > 0 → GroundedMoving
```

#### GaitAspect
```
MoveAction.Performed → gait = Idle → Run (默认移动步态)
Sprint.Button.IsRequested → 切换 Sprint/Run
MoveAction.Canceled → Idle
```

#### PostureAspect
```
优先级: Stand > Prone > Crouch > 保持当前
Stand.IsRequested → Standing
Prone.IsRequested → Prone
Crouch.IsRequested → Crouching
```

### 6.3 LocomotionTurningGraph

原地转向的时间稳定化图：

```
状态变量:
  lastDesiredYaw ← 上一次运动朝向的偏航角
  lookStabilityTimer ← 朝向稳定连续时间
  isTurningState ← 是否处于转向状态

进入条件:
  !isTurningState
  && abs(TurnAngle) >= turnEnterAngle (65°)
  && lookStabilityTimer >= lookStabilityDuration (0.15s)

退出条件:
  abs(TurnAngle) <= turnCompletionAngle (5°)
  → isTurningState = false

lookStabilityTimer:
  yawDelta <= lookStabilityAngle (2°) → timer += deltaTime
  yawDelta > lookStabilityAngle → timer = 0
```

### 6.4 LocomotionTraversalGraph

穿越请求的 4 阶段生命周期：

```
Idle → 检测到障碍物可攀爬 + 跳跃按钮按下 → Requested → Committed → Completed
                                                                     → Canceled

Requested (下一帧):
  - 如果条件仍满足 → Committed
  - 否则 → Canceled

Committed:
  - committedTimer += deltaTime
  - committedTimer >= 0.45s → Completed
  - 触发条件: 移动中遇到障碍物 + 跳跃键按下

Canceled/Completed:
  - 下一帧重置回 Idle
```

穿越触发条件：
```
Phase == GroundedIdle || GroundedMoving
&& JumpAction.Button.IsRequested || JumpAction.Button.IsPressed
&& ForwardObstacleDetection.CanClimb
```

穿越 Committed 后覆盖离散状态：
```
CreateActionControlled():
  Phase = GroundedIdle, Gait = Idle, IsTurning = false
  保留 Posture 和 Condition
```

### 6.5 LocomotionCoordinatorBase.Evaluate() 完整流程

```
1. Graph.Evaluate(motor, actions) → SLocomotionDiscrete
   → PhaseAspect.Update, GaitAspect.Update, PostureAspect.Update

2. TraversalGraph.Evaluate(motor, actions, discrete, deltaTime)
   → 根据当前阶段执行 Idle/Requested/Committed 逻辑

3. 如果穿越阶段 == Committed:
   → currentState = CreateActionControlled(discrete)  // 锁定状态

4. 否则:
   → TurningGraph.Evaluate(turnAngle, heading, profile, deltaTime, discrete)
   → 组装: new SLocomotionDiscrete(phase, posture, gait, condition, isTurning)
```

---

## 7. 动画系统

### 7.1 LocomotionAnimancerPresenter

MonoBehaviour 桥接层，位于 Agent 的子 GameObject 上：

```
Start():
  1. 获取 NamedAnimancerComponent、Animator 组件
  2. 创建 3 个 AnimancerLayer（Base=0, HeadLook=1, Footstep=2）
  3. 实例化 LocomotionAnimationController

OnAnimatorMove():
  - 如果 forwardRootMotion 启用
  - 将 animator.deltaPosition/deltaRotation 应用到 LocomotionMotor
  - motor.ApplyDeltaPosition/ApplyDeltaRotation → 更新 Transform

Evaluate(SLocomotion, deltaTime) → SLocomotionAnimation:
  1. controller.UpdateAnimations(snapshot, deltaTime)
  2. BuildAnimationSnapshot(controller.AnimationSnapshots)
```

### 7.2 动画层接口

```
ILocomotionAnimationLayer:
  AnimancerLayer Layer { get; set; }
  int LayerIndex { get; }
  string LayerName { get; }
  void Update(in LocomotionAnimationContext context);
  SLocomotionAnimationLayerSnapshot AnimationSnapshot { get; }
```

### 7.3 LocomotionAnimationController

```
UpdateAnimations(SLocomotion snapshot, float deltaTime):
  1. 构建 LocomotionAnimationContext（封装所有共享依赖）
  2. 遍历所有 ILocomotionAnimationLayer，调用 layer.Update(in context)
  3. 收集每个层的 AnimationSnapshot 到 Dictionary

层顺序:
  [0] BaseLayer     - 全身体运动动画（无 Mask）
  [1] HeadLookLayer - 头部朝向叠加（AvatarMask: headMask）
  [2] FootLayer     - 脚步点叠加（AvatarMask: footMask）
```

### 7.4 BaseLayer - FSM 驱动的基层

```
BaseStateKey 枚举:
  Idle, TurnInPlace, TurnInMoving, IdleToMoving, Moving, AirLoop, AirLand

状态转换:
               ┌──────────┐
      ┌───────→│   Idle   │←───────┐
      │        └────┬─────┘        │
      │   ┌────────┼────────┐     │
      │   ▼        ▼        ▼     │
      │ TurnIn  IdleTo   Moving   │
      │ Place   Moving   /TurnIn  │
      │   │        │     Moving   │
      │   └────────┼────────┘     │
      │            ▼              │
      │        AirLoop ──→ AirLand│
      └───────────────────────────┘
```

#### 7.4.1 状态详解

**BaseIdleState**
```
CanEnter: Phase == GroundedIdle
OnEnter: Play(idleL)
Tick:
  尝试 → TurnInPlace (GroundedIdle && IsTurning)
  尝试 → IdleToMoving (GroundedMoving && IsTurning)
  尝试 → Moving (GroundedMoving && !IsTurning)
  尝试 → AirLoop (!IsGrounded)
  保持 → PlayIfChanged(idleL)
```

**BaseTurnInPlaceState**
```
CanEnter: Phase == GroundedIdle && IsTurning
OnEnter: Play(turnInPlace90R or turnInPlace90L based on TurnAngle sign)

CanExit: 
  - IdleToMoving 条件满足 || Moving 条件满足
  - abs(TurnAngle) < turnExitAngle (20°) （提前退出）
  - 动画播放完成

Tick:
  尝试 → AirLoop
  TurnAngleStepRotationApplier.TryApply (模型逐步旋转)
  PlayIfChanged(selectedAlias)
  尝试 → IdleToMoving
  尝试 → Idle
```

**BaseIdleToMovingState**
```
CanEnter: Phase == GroundedMoving && IsTurning
OnEnter: Play(idleToRun180R 或 idleToRun180L based on TurnAngle)
CanExit: !IsTurning

Tick:
  尝试 → Idle
  尝试 → Moving
  尝试 → AirLoop
  动画播放完成 → 如果仍 Moving → ForceSetState(Moving)
               → 否则 → ForceSetState(Idle)
```

**BaseMovingState**
```
CanEnter: Phase == GroundedMoving && !IsTurning

Tick:
  尝试 → TurnInMoving
  尝试 → Idle
  尝试 → AirLoop
  ResolveMovingAlias(Gait) → 播放 walkMixer/runMixer/sprint
  UpdateMovementMixerParameterIfNeeded:
    - 找到 Vector2MixerState
    - parameter = ActualLocalVelocity / moveSpeed, Clamp01
    - vector2Mixer.Parameter = parameter
  TurnAngleStepRotationApplier.TryApply
```

**BaseTurnInMovingState**
```
CanEnter: Phase == GroundedMoving && IsTurning
          && desiredLocalVelocity.y >= moveSpeed*0.9 (主要向前)
          && abs(desiredLocalVelocity.x) <= moveSpeed*0.1 (少量横向)

OnEnter: Play(turnInWalk180L/R or turnInRun180L/R or turnInSprint180L/R based on Gait)

Tick:
  如果不是纯向前意图 → ForceSetState(Moving)
  尝试 → Moving
  尝试 → Idle
  尝试 → AirLoop
  动画完成 → 如果仍然 IsTurning → 选择新转向动画并 PlayFromStart
          → 如果 !IsTurning && Moving → ForceSetState(Moving)
          → 否则 → ForceSetState(Idle)
```

**BaseAirLoopState**
```
CanEnter: !IsGrounded
OnEnter: Play(AirLoop)
Tick:
  尝试 → AirLand (DistanceToGround < landDistanceThreshold(0.5m))
```

**BaseAirLandState**
```
CanEnter: DistanceToGround < 0.5m
CanExit: true (任何状态都可以立即退出)
OnEnter: Play(AirLand)
Tick:
  动画完成 → 尝试 Idle/Moving/TurnInPlace/TurnInMoving
```

#### 7.4.2 条件系统

```
ICheck<TContext>:
  bool Evaluate(in TContext context);

组合器:
  AndCondition<TContext, TLeft, TRight>  → L.Evaluate && R.Evaluate
  OrCondition<TContext, TLeft, TRight>   → L.Evaluate || R.Evaluate
  NotCondition<TContext, TCheck>         → !C.Evaluate

CheckExtensions:
  context.Check<TCheck>()  → default(TCheck).Evaluate(in context)
```

所有的 FSM 条件都是 `readonly struct`，通过 `default(T)` 实例化，避免分配。

#### 7.4.3 TurnAngleStepRotationApplier

```
TryApply():
  1. abs(TurnAngle) <= 0 → 跳过
  2. turnSpeed = animationProfile.GetTurnSpeed(posture, gait, isMoving)
  3. maxStep = turnSpeed * deltaTime
  4. step = Min(maxStep, absAngle)
  5. deltaAngle = Sign(TurnAngle) * step
  6. motor.ApplyDeltaRotation(Quaternion.AngleAxis(deltaAngle, Vector3.up))
```

### 7.5 HeadLookLayer

```
Vector2MixerState 驱动:
  X = smoothedYaw   (归一化到 [-1, 1])
  Y = smoothedPitch (归一化到 [-1, 1])
  
  targetYaw = Clamp(look.x / maxHeadYaw, -1, 1)
  targetPitch = Clamp(look.y / maxHeadPitch, -1, 1)
  
  平滑: smoothedYaw = MoveTowards(smoothedYaw, targetYaw, headLookSmoothingSpeed * deltaTime)

初始化:
  将所有子动画的 Speed=0, Weight=1, NormalizedTime=1
  (预烘焙姿势，只通过 parameter 驱动混合)
```

### 7.6 FootLayer - 存根实现

```
Update():
  - 不播放任何动画
  - 仅更新 snapshot 输出
  - 注释掉的代码显示原计划使用 alias.runUp 和 alias.sprint
```

---

## 8. 相机系统

### 8.1 CameraManager

```
初始化:
  1. 验证 GameProfile 和 CinemachineBrain
  2. 创建本地玩家 Anchor（Follow/LookAt 目标）
  3. 应用串行化的虚拟相机目标

每帧:
  Update(): TickLocalPlayerAnchor()
    1. 将 anchor 移动到 Agent 位置 + verticalOffset
    2. 应用 look delta 旋转（yaw 无限制，pitch 限制到 maxPitchDegrees）
    3. 发布 SCameraContext 到 EventDispatcher
    4. 清空 lastLookAction

  LateUpdate(): PushCameraSnapshotToContext()
    1. 更新 GameContext 中的 SCameraContext 快照
```

### 8.2 相机 Anchor 旋转

```
ApplyLookRotationToAnchor():
  1. lookDelta = SLookIAction.Delta * gameProfile.cameraLookRotationSpeed
  2. pitch += lookDelta.y, clamp(-maxPitchDegrees, maxPitchDegrees)
  3. yaw += lookDelta.x
  4. anchor.rotation = Quaternion.Euler(pitch, yaw, 0)
```

### 8.3 运动 Heading

```
Agent 的运动朝向 = 相机 Anchor 的前向水平分量
LocomotionHeadLook.EvaluatePlanarHeading(viewForward, transform)
  viewForward.y = 0; normalize
  → 角色朝向相机看的方向移动
```

---

## 9. 项目配置参数

### 9.1 LocomotionProfile

| 参数 | 默认值 | 描述 |
|---|---|---|
| `moveSpeed` | 4 | 最大移动速度 |
| `acceleration` | 5 | 加速度 (m/s²) |
| `maxGroundSlopeAngle` | 55° | 最大可走斜面角 |
| `groundStandBoxHalfExtents` | (0.15, 0.03, 0.15) | 地面检测 BoxCast 半尺寸 |
| `groundRayLength` | 10 | 地面距离探测射线长度 |
| `groundReacquireDebounceDuration` | 0 | 重新着地防抖时间 |
| `enableGroundLocking` | true | 启用地面锁定 |
| `obstacleProbeDistance` | 0.75 | 障碍物探测距离 |
| `obstacleMaxClimbHeight` | 2 | 最大可攀爬高度 |
| `maxHeadYawDegrees` | 75° | 头部最大偏航角 |
| `maxHeadPitchDegrees` | 75° | 头部最大俯仰角 |
| `turnEnterAngle` | 65° | 进入转向阈值 |
| `turnExitAngle` | 20° | 退出转向阈值 |
| `turnDebounceDuration` | 0.25 | 转向防抖时间 |
| `lookStabilityAngle` | 2° | 朝向稳定判定角度 |
| `lookStabilityDuration` | 0.15 | 朝向稳定判定时间 |
| `turnCompletionAngle` | 5° | 转向完成角度 |

### 9.2 LocomotionAnimationProfile

| 参数 | 默认值 | 描述 |
|---|---|---|
| `headLookSmoothingSpeed` | 540°/s | 头部朝向平滑速度 |
| `defaultInPlaceTurnSpeed` | 100°/s | 原地转向速度 |
| `defaultMovingTurnSpeed` | 360°/s | 移动中转向速度 |
| `landDistanceThreshold` | 0.5m | 落地判定距离阈值 |

### 9.3 LocomotionAliasProfile

Animancer StringAsset 别名映射：
- 空闲: idleL, idleR
- 空中: AirLoop, AirLand
- 待机转移动: idleToRun180L, idleToRun180R
- 步行: walkMixer, walkForward/Left/Right/Backward
- 跑步: runMixer
- 冲刺: sprint
- 移动中转: turnInWalk180L/R, turnInRun180L/R, turnInSprint180L/R
- 原地转: turnInPlace90L/R, turnInPlace180L/R
- 头部朝向: lookMixer, lookUp/Down/Left/Right
- 攀爬: ClimbUp0_5meter/1meter/2meter
- 参数驱动: HeadLookX, HeadLookY, VelocityX, VelocityY

---

## 10. 数据流图

### 10.1 输入 → 输出完整数据流

```
┌──────────────────────┐
│  Unity Input System  │
└────────┬─────────────┘
         │ InputActionHandler.Execute()
         ▼
┌──────────────────────┐
│  EventDispatcher     │  ← 发布结构化事件
└────────┬─────────────┘
         │ Subscribe
         ▼
┌──────────────────────────────────────────────────────────┐
│             LocomotionAgent (C# 类)                       │
│                                                          │
│  ┌─────────────────────────────────────────────────────┐ │
│  │             LocomotionInputModule                    │ │
│  │  • 维护所有输入动作的当前帧状态                     │ │
│  │  • 每帧调用 ReadActions() 聚合 + 清空单帧信号      │ │
│  │  • 输出 SLocomotionInputActions                    │ │
│  └───────────────────────┬─────────────────────────────┘ │
│                          │                               │
│                          ▼                               │
│  ┌─────────────────────────────────────────────────────┐ │
│  │                LocomotionMotor                      │ │
│  │  • 速度运动学（期望 + 平滑）                       │ │
│  │  • 运动朝向（viewForward → heading）               │ │
│  │  • 头部朝向（yaw/pitch）                           │ │
│  │  • 转向角度计算                                    │ │
│  │  • 地面检测 + 稳定化 + 锁定                        │ │
│  │  • 障碍物检测 + 高度探测                           │ │
│  │  • 输出 SLocomotionMotor                           │ │
│  └───────────────────────┬─────────────────────────────┘ │
│                          │                               │
│                          ▼                               │
│  ┌─────────────────────────────────────────────────────┐ │
│  │      LocomotionCoordinatorHuman                     │ │
│  │  • LocomotionGraph → Phase/Gait/Posture Aspect     │ │
│  │  • LocomotionTurningGraph → IsTurning               │ │
│  │  • LocomotionTraversalGraph → 穿越生命周期          │ │
│  │  • 输出 SLocomotionDiscrete + SLocomotionTraversal │ │
│  └───────────────────────┬─────────────────────────────┘ │
│                          │                               │
│                          ▼                               │
│  ┌─────────────────────────────────────────────────────┐ │
│  │         LocomotionAnimancerPresenter                │ │
│  │  • LocomotionAnimationController                    │ │
│  │  • BaseLayer (7-state FSM via Animancer.FSM)       │ │
│  │  • HeadLookLayer (Vector2Mixer)                    │ │
│  │  • FootLayer (stub)                                │ │
│  │  • 输出 SLocomotionAnimation                       │ │
│  └───────────────────────┬─────────────────────────────┘ │
│                          │                               │
│                          ▼                               │
│  ┌─────────────────────────────────────────────────────┐ │
│  │           SLocomotion (最终快照)                     │ │
│  │  • Motor         (运动学 + 探针)                    │ │
│  │  • DiscreteState (Phase/Gait/Posture/Condition)    │ │
│  │  • Traversal     (穿越请求/执行状态)               │ │
│  │  • Animation     (动画层快照)                      │ │
│  └───────────────────────┬─────────────────────────────┘ │
└──────────────────────────────────────────────────────────┘
                          │
                          ▼
┌──────────────────────────────────────────────────────────┐
│  GameContext.UpdateSnapshot(SLocomotion)                 │
│  EventDispatcher.Publish(SLocomotion)                    │
└──────────────────────┬───────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────┐
│  消费者:                                                 │
│  • CameraManager → 追踪角色位置                          │
│  • LocomotionDebugOverlay → 显示实时状态                 │
│  • 未来系统 → AI, 音效, 特效, 网络                       │
└──────────────────────────────────────────────────────────┘
```

---

## 11. 设计模式与架构决策

### 11.1 设计模式清单

| 模式 | 应用位置 |
|---|---|
| **服务定位器** | `GameContext` → 所有 `BaseService` 子类 |
| **事件总线** | `EventDispatcher` → 跨系统通信 |
| **策略模式** | `ILocomotionCoordinator` → 不同角色类型的运动规则 |
| **切面/Aspect** | `PhaseAspect`/`GaitAspect`/`PostureAspect` → 正交状态维度 |
| **有限状态机** | `Animancer.FSM` → BaseLayer 动画状态；`EGameState` → 游戏状态 |
| **条件组合器** | `AndCondition`/`OrCondition`/`NotCondition` → 泛型FSM条件 |
| **管道/流水线** | `Input → Motor → Coordinator → Animation` |
| **ScriptableObject 配置** | `LocomotionProfile`/`LocomotionAnimationProfile` 等 |
| **不可变 DTO** | 所有 `S*` 结构体使用 `readonly struct` + get-only 属性 |
| **单例** | `GameManager.Instance`, `GameContext.Instance` |

### 11.2 关键架构决策

1. **没有使用 DI 框架**：自建 `GameContext` 服务注册表，避免引入第三方依赖
2. **分阶段引导**：4阶段初始化（Register/Attach/Activate/Ready），确保确定性顺序
3. **ScriptableObject 驱动配置**：所有可调参数通过 ScriptableObject 暴露，支持 Inspector 实时修改
4. **快照缓存模式**：`GameContext.UpdateSnapshot<T>()` / `TryGetSnapshot<T>()`，避免频繁 Unity API 调用
5. **零分配条件系统**：FSM 条件使用 `readonly struct` + `default(T)` 实例化，避免 GC 压力
6. **泛型 DTO 延迟类型检测**：`LocomotionInputModule.PutAction<T>()` 使用 `typeof()` 分支而非 `switch` 表达式，兼容 Unity IL2CPP
7. **Rigidbody.Y 冻结**：着地时冻结 Y 轴，离地时解冻，通过 Rigidbody 而非直接设置 Transform 管理物理
8. **地面稳定化**：两阶段（raw → stabilized），含防抖逻辑防止地面/空中状态闪烁
9. **运动朝向分离**：身体朝向（body forward）与运动朝向（locomotion heading）分离，支持不跟随朝向的移动
10. **动画与逻辑分离**：`SLocomotionDiscrete` 驱动动画 FSM 条件，动画选择基于离散状态而非原始速度/角度值

---

## 12. 性能要点

### 12.1 分配压力点
- `EventDispatcher.Publish()` → `handlers.ToArray()` 每帧分配（已知设计权衡）
- `LocomotionDebugOverlay.Refresh()` → 多个 `StringBuilder` Clear/Append 操作
- `LocomotionInputModule.PutAction<T>()` → struct boxing via `(object)`

### 12.2 已优化点
- 所有 `S*` 结构体为 `readonly struct`，栈分配
- FSM 条件检查为 `readonly struct`，零分配
- 服务解析结果缓存在 `BaseService.serviceCache` 中
- `CheckExtensions.Check<TCheck>()` 使用 `default(T)` 避免分配

---

## 13. 开发状态与待办

### 13.1 已完成
- [x] 服务注册表与事件总线
- [x] 输入系统（Move/Look + 8 按钮）
- [x] Cinemachine 相机跟随
- [x] 速度运动学（平滑 + 局部/世界空间转换）
- [x] 地面检测（Ray + BoxCast，含稳定化）
- [x] 障碍物检测（前方射线 + 顶部高度探针）
- [x] 离散运动状态（Phase/Gait/Posture/Turning/Traversal）
- [x] 动画 FSM（7个状态 + TurnAngleStepRotation）
- [x] 头部朝向混合层
- [x] 根运动转发
- [x] 实时调试 UI（LocomotionDebugOverlay）
- [x] Editor Gizmo 渲染（运动方向、地面探测、障碍物探测）
- [x] 自定义 Logger（结构化 + 循环引用检测）

### 13.2 待完善
- [ ] 脚步动画层（FootLayer 为存根）
- [ ] 攀爬动画集成（ClimbUp 别名已定义但未驱动）
- [ ] Vault/StepOver 穿越类型实现
- [ ] EMovementGait.Crawl 实现
- [ ] ELocomotionCondition 实际应用
- [ ] ELocomotionPhase.Landing 驱动
- [ ] 下落/跳跃动画区分
- [ ] 多角色 LocomotionAgent 支持（目前未测试）

### 13.3 未来系统（Project 3 Backlog）
全部 21 个任务，详见 `docs/project3-backlog.md`：
- P0: 资源系统、生存指标、建造系统、丧尸AI、玩家基础控制、地图原型
- P1: NPC AI/分配/招募、战斗系统/武器切换/熟练度/热武器、农业、工具
- P2: 烹饪、科技树、图纸、尸潮（触发/规模/行为）
