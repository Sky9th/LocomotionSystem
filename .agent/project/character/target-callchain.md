# Character 模块——目标调用链

## 1. 核心

- `CharacterFrameContext` 在同帧内逐级传递数据
- 子系统不访问 `GameContext`（不读不写）
- CharacterActor 统一组装 `SCharacterSnapshot` 并推送

---

## 2. 初始化

```
GameManager.Awake() [-500]
  └── PlayerManager.CreatePlayer()
       └── Instantiate(Player.prefab)

       CharacterAnimationController.Awake()
         └── ConfigureRuntimeLayers() → 6 层 + 4 Arbiter

       CharacterLocomotion.Awake()
         └── modelRoot 解析

       CharacterActor.Awake()
         ├── characterAnimation = GetComponent<CharacterAnimationController>()
         ├── locomotion = GetComponentInChildren<CharacterLocomotion>()
         ├── inputModule = new CharacterInputModule(this)
         └── characterKinematic = new CharacterKinematic(transform, locomotion.ModelRoot, profile)

       CharacterActor.OnEnable()
         └── inputModule.Subscribe()

       CharacterLocomotion.OnEnable()
         ├── motor = new LocomotionMotor(transform)
         └── coordinator = new LocomotionCoordinatorHuman()

       CharacterActor.Start()
         ├── locomotion.Initialize(characterAnimation)
         │     └── new LocomotionDriver(motor, profile, alias, animProfile)
         │     └── controller.RegisterDriver()
         ├── characterAnimation.CreateTraversalDriver(alias)
         └── characterAnimation.SetMotor(motor)
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

  ┌─ Step 1: 读取输入 ──────────────────────────────────
  │  inputModule.ReadActions(out ctx.Input);
  │    → ctx.Input  [SCharacterInputActions]
  └──────────────────────────────────────────────────────

  ┌─ Step 2: 读取相机 (外部数据, 来自 GameContext) ──────
  │  GameContext.TryGetSnapshot(out SCameraContext camera);
  │  viewForward = isPlayer ? camera.AnchorRotation * forward : zero
  └──────────────────────────────────────────────────────

  ┌─ Step 3: 计算 Kinematic ─────────────────────────────
  │  ctx.Kinematic = characterKinematic.Evaluate(profile, viewForward, dt)
  │    → ctx.Kinematic  [SCharacterKinematic]
  │        .Position                 = actorTransform.position (地面锁定后)
  │        .BodyForward              = actorTransform.forward (水平)
  │        .LookDirection            = CharacterHeadLook.Evaluate()
  │        .GroundContact            = CharacterGroundDetection + 稳定化
  │        .ForwardObstacleDetection = CharacterObstacleDetection.TryDetect()
  └──────────────────────────────────────────────────────

  ┌─ Step 4: 仿真 Locomotion ────────────────────────────
  │  locomotion.Simulate(ref ctx, profile, viewForward, dt)
  │
  │    ┌─ ctx.Motor = motor.Evaluate(in ctx.Kinematic, profile, in ctx.Input, viewForward, dt)
  │    │    → ctx.Motor  [SLocomotionMotor]
  │    │        .DesiredLocalVelocity  = LocomotionKinematics.ComputeDesiredPlanarVelocity()
  │    │        .ActualLocalVelocity   = SmoothVelocity()
  │    │        .LocomotionHeading     = CharacterHeadLook.EvaluatePlanarHeading()
  │    │        .TurnAngle             = SignedAngle(BodyForward, LocomotionHeading)
  │    │
  │    └─ ctx.Discrete = coordinator.Evaluate(in ctx.Kinematic, in ctx.Motor, profile, in ctx.Input, dt)
  │         → ctx.Discrete  [SLocomotionDiscrete]
  │             .Phase     = PhaseAspect (GroundContact.IsGrounded + 速度)
  │             .Gait      = GaitAspect (MoveAction + Sprint/Run toggle)
  │             .Posture   = PostureAspect (Crouch/Prone/Stand button)
  │             .IsTurning = TurningGraph (TurnAngle + 稳定检测)
  │
  │         → ctx.Traversal  [SLocomotionTraversal]
  │             Idle → (Jump + CanClimb) → Requested
  │             Requested → Committed (1帧后) or Canceled
  │             Committed → Completed (0.45s后)
  └──────────────────────────────────────────────────────

  ┌─ Step 5: 组装 & 推送 ────────────────────────────────
  │  snapshot = new SCharacterSnapshot(
  │      ctx.Kinematic, ctx.Motor, ctx.Discrete, ctx.Traversal)
  │
  │  GameContext.UpdateSnapshot(snapshot);
  │  Dispatcher.Publish(snapshot);
  └──────────────────────────────────────────────────────


═══════════════════════════════════════════════════════════
CharacterAnimationController.Update() [0]
═══════════════════════════════════════════════════════════

  GameContext.TryGetSnapshot(out SCharacterSnapshot snapshot);

  fullBodyArbiter.Update(dt)
    │
    ├── EvaluatePending()
    │    └── TraversalDriver.BuildRequest(snapshot)
    │         → snapshot.Traversal.Stage == Requested + Type == Climb?
    │         → 是 → CharacterAnimationRequest(ClimbUp*) → AcceptRequest
    │                ├── InterruptActive() → LocomotionDriver.OnInterrupted()
    │                └── layer.Play(ClimbUp*)
    │         → 否 → return null
    │
    └── ActivateDefault() → LocomotionDriver.OnResumed()

    ActiveDriver.Update(snapshot, dt)
      └── LocomotionDriver.Update(snapshot, dt)
           └── BaseLayer.FSM.Tick(snapshot)
                ├── Phase==GroundedMoving → Play(runMixer)
                ├── HeadLookLayer.Update(snapshot.Kinematic.LookDirection)
                └── FootLayer.Update()


═══════════════════════════════════════════════════════════
[Animation Phase] OnAnimatorMove
═══════════════════════════════════════════════════════════

  CharacterAnimationController.OnAnimatorMove()
    └── motor.ApplyDeltaPosition(animator.deltaPosition)
         motor.ApplyDeltaRotation(animator.deltaRotation)
```

---

## 4. 攀爬中断链路

```
帧 N:
  CharacterActor → Simulate → TraversalGraph → Requested
  → SCharacterSnapshot.Traversal = {Type=Climb, Stage=Requested, Height=1.2}

帧 N+1:
  AnimationController → Arbiter.EvaluatePending()
    → TraversalDriver.BuildRequest(snapshot)
    → ResolveClimbAlias(1.2) → ClimbUp2meter
    → CharacterAnimationRequest(ClimbUp2meter, Priority=Traversal)
    → AcceptRequest
      ├── LocomotionDriver.OnInterrupted() → isActive=false
      └── layer.Play(ClimbUp2meter)

帧 N+2 ~ K: 动画播放中
  Locomotion 仿真: TraversalGraph.Evaluate → Requested → Committed
  Coordinator: discreteState = CreateActionControlled() → Phase=Idle, Gait=Idle

帧 K: 动画完成
  Arbiter → NormalizedTime >= 0.99 → CompleteActive()
  → TransitionToDefault → LocomotionDriver.OnResumed() → isActive=true
```
