# Session 概要记忆

> 日期: 2026-05-08
> 分支: `feature/character-module-rebuild`
> 状态: Locomotion 基本完成, Traversal 完成, 地面检测重构完成, 待补 HeadLook/Footstep/多层仲裁

---

## 1. 项目概述

Unity URP 第三人称角色运动系统重构。从 `Assets/Scripts/Locomotion/` 旧架构迁移到 `Assets/Scripts/Character/` 新模块化架构。

### 核心原则
- 数据由上至下参数传递，不反向查询 GameContext
- 父模块调用子模块，不跨级调用
- CharacterActor 唯一对外出口（`SCharacterSnapshot` → `GameContext`）
- CharacterRig 统一物理实体写入入口（Transform/Rigidbody/Collider）

---

## 2. 架构总览（更新）

```
Character/
├── Components/
│   ├── CharacterActor.cs              [MB] 组合根, Steps 1-5 调用链
│   ├── CharacterActor.Debug.cs        [partial] Editor 可视化(SphereCast)
│   ├── CharacterFrameContext.cs       [struct] 内部数据总线
│   └── CharacterRig.cs               [纯C#] Transform/Rigidbody/Collider
│         SetKinematic, SetSuppressGroundLock, IgnoreCollisionWith, ZeroVelocity
│
├── Config/
│   └── CharacterProfile.cs            [SO] 地面/障碍物/头部朝向
│         groundProbeHeight/Radius (SphereCast), obstacleMinClimbHeight
│
├── Animation/
│   ├── Components/
│   │   └── AnimationBrain.cs          [MB, EO(-10)] 6层, fullBodyArbiter,
│   │       OnAnimatorMove(SuppressGroundLock→full / else→planar)
│   ├── DriverArbiter.cs              [纯C#] 仲裁+EvaluateDrivers+生命周期
│   │       AcceptRequest→OnStarted, CheckCompletion→OnCompleted
│   ├── Drivers/
│   │   ├── ICharacterAnimationDriver.cs   接口: Evaluate/Drive/OnStarted/
│   │   │       OnCompleted/OnInterrupted/OnResumed/ChannelMask
│   │   ├── BaseCharacterAnimationDriver.cs [MB基类] OnEnable自注册
│   │   ├── Locomotion/
│   │   │   ├── LocomotionDriver.cs   [MB] Continuous, OnResumed→InvalidateCache
│   │   │   ├── BaseLayer.cs          [纯C#] 7状态FSM, AirborneStartY/MaxFallDistance
│   │   │   ├── BaseStateKey.cs       [enum] 7键
│   │   │   └── States/ (7 files)     含落地分级+Y解锁
│   │   └── Traversal/
│   │       └── TraversalDriver.cs     [MB] OneShot: Evaluate读snapshot→障碍物
│   │             OnStarted/Completed/Interrupted→控制物理
│   ├── Requests/
│   │   ├── AnimationRequest.cs       Tags+Resistance+FadeIn/FadeOut+生命周期
│   │   ├── OnCompleteBehavior.cs     [enum] Resume/Stay
│   │   └── OnInterruptedBehavior.cs  [enum] Resume/Cancel
│   └── Config/
│       ├── LocomotionAliasProfile.cs  LandHard/Light/Medium/FromWall, AirLoop
│       │       ClimbUpHalfMeter/1meter/2meter, ClimbDown1meter/2meter
│       ├── LocomotionAnimationProfile.cs  6个落地阈值
│       └── LocomotionModeProfile.cs
│
├── Input/
│   ├── CharacterInputModule.cs       [纯C#] EventDispatcher订阅+输入聚合
│   └── SCharacterInputActions.cs     [struct] 10种输入, Input进入Snapshot
│
├── Kinematic/
│   ├── CharacterKinematic.cs         [纯C#] 地面(SphereCast)/障碍物/朝向
│   ├── SCharacterKinematic.cs        [struct] Position/BodyForward/Heading/
│   │       LookDirection/GroundContact/ForwardObstacleDetection
│   ├── CharacterGroundDetection.cs   [static] SphereCast单探头
│   ├── CharacterObstacleDetection.cs [static] 前方射线+顶部探测(minClimbHeight)
│   ├── CharacterHeadLook.cs          [static] 头部yaw/pitch
│   ├── SGroundContact.cs             [struct]
│   └── SForwardObstacleDetection.cs  [struct] canClimb/canVault/canStepOver
│
├── Locomotion/
│   ├── ILocomotionSimulator.cs       [interface]
│   ├── GroundLocomotion.cs           [纯C#] Motor+Stance
│   ├── Motor.cs                      [纯C#] 速度计算+4 static helpers
│   ├── Stance.cs                     [纯C#] Phase/Gait/Posture/IsTurning
│   │       EvaluateTurning:turning auto-reset (!wantsTurn)
│   ├── SCharacterMotor.cs            [struct]
│   ├── SCharacterDiscrete.cs         [struct] Phase/Posture/Gait/IsTurning
│   ├── SLocomotionState.cs           [struct]
│   └── Config/
│       └── LocomotionProfile.cs       [SO]
│
├── Enums/
│   └── LocomotionEnums.cs            Phase/Gait/Posture
│
└── Structs/
    └── SCharacterSnapshot.cs          Input + Kinematic + Locomotion
```

---

## 3. 稳态帧调用链（更新）

```
CharacterActor.Update() [EO 0]
  ctx.Input    = inputModule.ReadActions()
  ctx.Kinematic = characterKinematic.Evaluate(profile, viewForward, dt, hasInput)
    → CharacterGroundDetection.EvaluateGroundContact
      → SphereCast(膝盖高度+0.5m, radius 0.25m, down 10.5m)
    → isGrounded = distanceToGround < 0.15f
    → FreezePositionY(isGrounded)
    → if isGrounded && distance < lockMax: SetGroundedY + ZeroVelocity
  locomotionSimulator.Simulate(ref ctx, profile, dt)
    → Motor.Evaluate → ctx.Motor
    → Stance.Evaluate → ctx.Discrete (turning auto-reset)
  snapshot = new SCharacterSnapshot(ctx.Input, ctx.Kinematic, SLocomotionState)
  characterAnimation.Apply(in snapshot)

AnimationBrain.Apply(in snapshot)
  fullBodyArbiter.Resolve(snapshot, dt)
    → EvaluateDrivers(snapshot)    ← TraversalDriver 读 snapshot 提交请求
    → ProcessQueue (OnInterrupted → OnStarted)
    → CheckCompletion (OnCompleted, 无 layer.Stop 自然交叉淡入淡出)
    → ActiveDriver.Drive(snapshot) ← FSM.Tick

AnimationBrain.OnAnimatorMove()
  if SuppressGroundLock: ApplyPosition(delta)     ← 攀爬/落地 完整XYZ
  else:                 ApplyPositionPlanar(delta) ← 常态 XZ
```

---

## 4. 已实现

### Traversal 攀爬系统
- TraversalDriver: Evaluate读snapshot.Input+Kinematic, 障碍物检测, 高度→别名
- 生命周期回调: OnStarted(解锁Y+忽略碰撞+Kinematic), OnCompleted(瞬吸Y+恢复), OnInterrupted(恢复)
- 碰撞: Physics.IgnoreCollision + SetKinematic 穿墙攀爬

### 动画落地系统
- 坠落距离追踪: AirLoop记录起点Y, 逐帧追踪MaxFallDistance
- 按距离分级: ≤1m→LandLight, ≤3m→LandMedium, >3m→LandHard
- 分级触发阈值: landLightTrigger(0.3m), landMediumTrigger(0.6m), landHardTrigger(1.0m)
- 最小坠落过滤: landMinFallDistance(0.2m), AirLoop入口检查
- Y轴解锁: SuppressGroundLock 在 AirLoop/AirLand 期间抑制锁地
- 落地后FSM退出: Idle→Moving→IdleToMoving→TurnInPlace→Force Idle

### 动画过渡
- layer.Stop 移除: 攀爬→Locomotion 用 Animancer TransitionAsset 0.25s 交叉淡入淡出
- OnResumed→InvalidateAnimationCache: 防止T-Pose

### 地面检测重构
- BoxCast+Raycast → 单 SphereCast(膝盖0.5m, radius 0.25m, down 10.5m)
- isGrounded = distanceToGround < 0.15f (防止空中误判接地)
- 锁地条件: IsGrounded && distanceToGround < groundLockMaxDistance (防止瞬移吸附)
- ZeroVelocity: 锁地时清 Rigidbody 速度 (防止斜坡物理滑)
- CharacterProfile简化: groundProbeHeight/Radius 替代 3个旧参数

### 障碍物检测改进
- 高度计算: topPoint.y - actorPosition.y (从脚底算，非命中点)
- obstacleMinClimbHeight=0.3m (过滤过矮物体)
- 顶部探头起点提升到 maxClimbHeight*2 (防止高障碍物误判)
- IsTurning auto-reset: !wantsTurn 时自动关闭 (防止落地后卡转向)

### 动画统一命名
- Land 前缀: LandHard/Light/Medium/FromWall (统一落地系列)
- ClimbUpHalfMeter/ClimbUp1meter/ClimbUp2meter
- ClimbDown1meter/ClimbDown2meter
- AirLoop (保持不变)
- 全部来自 PROTOFACTOR Climbing Animset (统一动画包)

### 接口演进
- ICharacterAnimationDriver: +Evaluate, +OnStarted, +OnCompleted
- SCharacterSnapshot: +Input 字段 (同帧一致)
- AnimationRequest: +FadeIn/FadeOut 字段
- CharacterRig: +SetSuppressGroundLock, +SetKinematic, +IgnoreCollisionWith, +ZeroVelocity
- CharacterProfile: +groundProbeHeight, +groundProbeRadius, +obstacleMinClimbHeight, +groundLockMaxDistance
- LocomotionAnimationProfile: +6个落地阈值

---

## 5. 有骨架待实现

| 模块 | 现状 | 说明 |
|------|------|------|
| **HeadLook 头部注视** | `UpdateHeadLook()` 空 TODO | Vector2Mixer 已创建，无驱动逻辑 |
| **Footstep 脚步声** | Layer 6 + mask 已绑 | 无 FootLayer 驱动类 |
| **UpperBody/Additive/Facial 仲裁** | Layer 1-3 + mask 已绑 | 无对应 Arbiter |
| **Vault / StepOver** | canVault/canStepOver 字段返回 false | 无障碍物检测逻辑 |
| **Crawl 爬行** | Gait 枚举有 Crawl | Stance 从未赋值 |
| **姿势影响物理** | canCrouch/canProne 开关有 | 碰撞体高度/速度不变 |

## 6. 设计决策

| 决策 | 原因 |
|------|------|
| Component Driver | Inspector可视, OnEnable自注册 |
| 从快照读输入(方案B) | 单一订阅点, 同帧一致, 不用EventDispatcher |
| Evaluate 接口分离 | Driver 非活跃时也能提交请求 |
| SphereCast 单探头 | 斜坡接触点干净, 替代不稳定 BoxCast |
| Y轴单一路径 | 常态不碰Y, 攀爬/落地 SuppressGroundLock 解锁 |
| CharacterRig 统一物理入口 | SetKinematic+IgnoreCollision+ZeroVelocity 集中管理 |
| 事件驱动 Traversal | Evaluate 中读快照, 不轮询 Update |
| 攀爬物理: Kinematic+IgnoreCollision | 穿墙+不坠落, 完成时瞬吸到位 |
| 落地动画分级 | 按坠落距离选 Light/Medium/Hard, 不同触发阈值 |
| 移除 layer.Stop | Animancer TransitionAsset 自动交叉淡入淡出 |

## 7. 下一步

1. HeadLook 头部注视驱动逻辑
2. Footstep 脚步声系统
3. UpperBody/Additive/Facial 多层仲裁
4. Vault/StepOver 障碍物检测
5. 姿势物理联动(碰撞体/速度)
6. 多角色测试

---

## 8. 提交记录 (本轮)

```
44ba4f6 fix: landing-to-locomotion transition, turning lock, and ground snap timing
f1bc88f feat: rename climbing/landing animations and add fall-distance landing system
93cf3f7 feat: implement TraversalDriver with event-driven evaluation and lifecycle callbacks
8d0ca8e fix: replace BoxCast+Raycast with SphereCast for ground detection
```
