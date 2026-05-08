# Character 模块——完整架构与调用链

> 更新时间: 2025-04-29
> Step 1-5 ✅ | Animation 框架 ✅ | Driver 待建

---

## 1. 目录结构

```
Character/
├── Components/
│   ├── CharacterActor.cs              (MB) 组合根
│   ├── CharacterFrameContext.cs       (struct) 内部数据总线
│   └── CharacterRig.cs               (纯 C#) 物理实体入口
│
├── Config/
│   └── CharacterProfile.cs            (SO)
│
├── Animation/                         ← 动画子系统
│   ├── Components/
│   │   └── CharacterAnimationController.cs  (MB) [ExecutionOrder(10)]
│   ├── DriverArbiter.cs               ← 仲裁器
│   ├── Drivers/
│   │   ├── ICharacterAnimationDriver.cs   ← 接口
│   │   ├── BaseCharacterAnimationDriver.cs ← Component 基类
│   │   └── Locomotion/ ([待建])
│   │       └── LocomotionDriver.cs, BaseLayer.cs, States/
│   ├── Requests/
│   │   ├── AnimationRequest.cs
│   │   ├── OnInterruptedBehavior.cs
│   │   └── OnCompleteBehavior.cs
│   └── Config/
│       ├── LocomotionAliasProfile.cs
│       ├── LocomotionAnimationProfile.cs
│       └── LocomotionModeProfile.cs
│
├── Input/
│   ├── CharacterInputModule.cs
│   └── SCharacterInputActions.cs
│
├── Kinematic/
│   ├── CharacterKinematic.cs
│   ├── SCharacterKinematic.cs
│   ├── CharacterGroundDetection.cs
│   ├── CharacterObstacleDetection.cs
│   ├── CharacterHeadLook.cs
│   ├── SGroundContact.cs
│   └── SForwardObstacleDetection.cs
│
├── Locomotion/
│   ├── ILocomotionSimulator.cs
│   ├── GroundLocomotion.cs
│   ├── Motor.cs
│   ├── Stance.cs
│   ├── SCharacterMotor.cs
│   ├── SCharacterDiscrete.cs
│   ├── SLocomotionState.cs
│   └── Config/LocomotionProfile.cs
│
├── Enums/LocomotionEnums.cs
│
└── Structs/SCharacterSnapshot.cs
```

---

## 2. 初始化链路

```
GameManager.Awake() [-500]
  └── PlayerManager.CreatePlayer()

    CharacterAnimationController.Awake()  [ExecutionOrder 10]
      └── ConfigureRuntimeLayers() → 6 层 + fullBodyArbiter

    CharacterActor.Awake()
      ├── characterAnimation = GetComponentInChildren<CharacterAnimationController>()
      ├── characterRig = new CharacterRig(transform, characterAnimation.transform)
      ├── inputModule = new CharacterInputModule(this)
      ├── characterKinematic = new CharacterKinematic(transform, transform, characterRig)
      └── locomotionSimulator = new GroundLocomotion()

    LocomotionDriver.OnEnable()   ← Component 自注册
      └── controller.RegisterDriver(this) → fullBodyArbiter.RegisterDriver

    CharacterActor.OnEnable()
      └── inputModule.Subscribe()

    CharacterActor.Start()
      └── characterAnimation.SetRig(characterRig)
```

---

## 3. 稳态帧

```
CameraManager.Update() [-400]
  └── GameContext.TryGetSnapshot<SCharacterSnapshot>()
       └── .Kinematic.Position → anchor.position


═══════════════════════════════════════════════════════════
CharacterActor.Update() [0]
═══════════════════════════════════════════════════════════

  var ctx = new CharacterFrameContext();

  ┌─ Step 1: 读取输入 ──────────────────
  │  inputModule.ReadActions(out ctx.Input)
  └─────────────────────────────────────

  ┌─ Step 2: 读取相机 ──────────────────
  │  inputModule.ReadCameraControl(out cameraControl)
  │  viewForward = isPlayer ? camera.AnchorRotation * forward : zero
  └─────────────────────────────────────

  ┌─ Step 3: 计算 Kinematic ────────────
  │  ctx.Kinematic = characterKinematic.Evaluate(characterProfile, viewForward, dt)
  └─────────────────────────────────────

  ┌─ Step 4: 仿真 Locomotion ───────────
  │  locomotionSimulator.Simulate(ref ctx, locomotionProfile, dt)
  │    → Motor.Evaluate → ctx.Motor
  │    → Stance.Evaluate → ctx.Discrete
  └─────────────────────────────────────

  ┌─ Step 5: 组装 & 推送 ───────────────
  │  snapshot = new SCharacterSnapshot(ctx.Kinematic,
  │              new SLocomotionState(ctx.Motor, ctx.Discrete))
  │  characterAnimation.Apply(in snapshot)
  │  GameContext.UpdateSnapshot(snapshot)
  └─────────────────────────────────────


═══════════════════════════════════════════════════════════
CharacterAnimationController.Apply(in snapshot)  [EO 10]
═══════════════════════════════════════════════════════════

  fullBodyArbiter.Resolve(snapshot, dt)

    ┌─ 1. 处理请求队列 ──────────────────────
    │  foreach SubmitRequest (Driver 主动提交)
    │    → 裁决 → Accept/Reject
    │    → OnInterrupted / OnResumed
    │  清空队列
    │
    ├─ 2. 检查动画完成 ──────────────────────
    │  NormalizedTime >= 0.99 → Complete/Stay
    │
    └─ 3. ActiveDriver.Drive(snapshot, dt) ──
         └── LocomotionDriver.Drive(snapshot, dt)
              └── BaseLayer.Update(snapshot, dt)
                   └── FSM.Tick → Play / PlayMixer / TurnStep

  UpdateHeadLook(snapshot)  [stub]


═══════════════════════════════════════════════════════════
[Animation Phase] OnAnimatorMove
═══════════════════════════════════════════════════════════

  characterRig.ApplyModelPosition(animator.deltaPosition)
  characterRig.ApplyModelRotation(animator.deltaRotation)
```

---

## 4. Driver 钩子模型

```
Driver (Component, 主动):
  Update()                     → 自己检测条件 → SubmitRequest(this, request)
  Drive(snapshot, dt)          → Arbiter 调用 → 驱动动画逻辑
  OnInterrupted(by)            → 被别人的请求打断
  OnResumed()                  → 恢复为 Active
  OnEnable()                   → RegisterDriver → Arbiter
  OnDisable()                  → UnregisterDriver → Arbiter

Arbiter (纯调度):
  RegisterDriver / UnregisterDriver
  SubmitRequest → 入队, 本帧处理
  Release       → Driver 自己结束自己
  Resolve       → 队列处理 → 完成检查 → Drive
```

---

## 5. 场景示例

### Locomotion (Continuous)

```
帧 N:
  Arbiter.Resolve:
    请求队列空 → 激活 LocomotionDriver → Drive(snapshot)
      → BaseLayer.Tick → Play(runMixer)
```

### Traversal 中断 Locomotion

```
帧 N:
  TraversalDriver.Update():
    检测条件 → SubmitRequest(ClimbUp, R=10)

  Arbiter.Resolve:
    队列: [ClimbUp(R=10)]
    ActiveRequest==null → Accept
    Interrupt Locomotion → Play(ClimbUp)

帧 N+1~K: 动画播放中

帧 K: NormalizedTime>=0.99 → Complete → Resume → Locomotion.Resumed → Drive
```

### Dance (Self-Release)

```
帧 N:
  DanceDriver.Update():
    检测 → SubmitRequest(DanceMixer, R=100, OnComplete=Stay)

  Arbiter.Resolve → Accept → Play(DanceMixer)

帧 M (取消):
  DanceDriver.Update():
    检测取消键 → Arbiter.Release(this)

  Arbiter → 恢复默认 → Locomotion.Resumed → Drive
```

### Dance 被打断

```
帧 M (Death 请求):
  DeathDriver.Update():
    SubmitRequest(Death, R=999)

  Arbiter.Resolve:
    999 >= 100 → Accept
    DanceDriver.OnInterrupted(deathRequest)
    Play(Death)
```
