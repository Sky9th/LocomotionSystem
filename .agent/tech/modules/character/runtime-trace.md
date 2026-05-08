# 运行时链路追踪

> 更新日期：2025-04-26
> 涵盖从场景加载到稳态帧的完整执行序列

---

## 目录

1. [执行顺序总览](#1-执行顺序总览)
2. [帧0: 启动引导](#2-帧0-启动引导)
3. [帧0: Player 实例化](#3-帧0-player-实例化)
4. [帧0: 第一帧 Update](#4-帧0-第一帧-update)
5. [帧1+: 稳态循环](#5-帧1-稳态循环)
6. [攀爬中断链路](#6-攀爬中断链路)
7. [事件订阅清单](#7-事件订阅清单)
8. [已知问题与检查点](#8-已知问题与检查点)

---

## 1. 执行顺序总览

```
Execution Order:
  ┌──────────────────────────────────────────────────────────────┐
  │ -500  GameManager.Awake()                                    │
  │         └─ Bootstrap (Phase 1→4)                            │
  │              └─ Phase 4: PlayerManager.CreatePlayer()       │
  │                   └─ Instantiate(Player.prefab)              │
  │                        ├─ Character.Awake()                  │
  │                        ├─ CharacterAnimationController.Awake()
  │                        ├─ LocomotionAgent.Awake()             │
  │                        ├─ Character.OnEnable()               │
  │                        ├─ LocomotionAgent.OnEnable()          │
  │                        │    ├─ new LocomotionMotor()          │
  │                        │    ├─ new LocomotionCoordinatorHuman()
  │                        │    ├─ new LocomotionInputModule()    │
  │                        │    └─ inputModule.Subscribe()        │
  │                        ├─ Character.Start()                  │
  │                        │    ├─ new LocomotionDriver()         │
  │                        │    ├─ new TraversalDriver()          │
  │                        │    └─ controller.RegisterDriver()   │
  │                        └─ LocomotionAnimancerPresenter.Start()
  │                             └─ resolve agent, animator        │
  │                                                                │
  │ -400  CameraManager.Update()                                  │
  │         └─ TickLocalPlayerAnchor()                            │
  │              └─ hasLocomotionPosition=false → RETURN          │
  │                                                                │
  │    0  (undefined order between same-order components)         │
  │         LocomotionAgent.Update()                              │
  │           └─ Simulate()                                       │
  │                ├─ motor.Evaluate() → 地面锁定                 │
  │                ├─ coordinator.Evaluate()                      │
  │                └─ PushSnapshot()                              │
  │                     ├─ GameContext.UpdateSnapshot(SLocomotion)│
  │                     └─ Dispatcher.Publish(SLocomotion)        │
  │                          └─ CameraManager.HandleLocomotion... │
  │                               └─ hasLocomotionPosition=true   │
  │                                                                │
  │         CharacterAnimationController.Update()                 │
  │           └─ FullBodyArbiter.Update()                         │
  │                ├─ EvaluatePending()                           │
  │                │    ├─ TraversalDriver.BuildRequest()         │
  │                │    │    └─ TryGetSnapshot → Requested? → null│
  │                │    └─ ActivateDefault()                      │
  │                │         └─ LocomotionDriver.OnResumed()      │
  │                └─ ActiveDriver.Update()                       │
  │                     └─ LocomotionDriver.Update()              │
  │                          ├─ EnsureInitialized()               │
  │                          │    └─ new LocomotionAnimController │
  │                          │         ├─ new BaseLayer(Layer0)   │
  │                          │         ├─ new HeadLookLayer(L4)   │
  │                          │         └─ new FootLayer(L5)       │
  │                          └─ TryGetSnapshot → UpdateAnimations │
  │                                                                │
  │ (Animation Phase)                                             │
  │   Animator.Update → OnAnimatorMove()                          │
  │     └─ motor.ApplyDeltaPosition(animator.deltaPosition)       │
  │          └─ ⚠ 可能覆盖地面锁定 Y 轴                           │
  └──────────────────────────────────────────────────────────────┘
```

---

## 2. 帧0: 启动引导

### 2.1 GameManager.Awake() [ExecutionOrder -500]

```
GameManager.Awake()
  ├─ Instance = this; DontDestroyOnLoad(gameObject)
  └─ Bootstrap()
```

### 2.2 Bootstrap Step 1: 初始化 GameContext

```
gameContext = GetComponentInChildren<GameContext>()
gameContext.Initialize()
  → Instance = this
  → isInitialized = true
```

### 2.3 Bootstrap Step 2: 注册所有 Service

```
发现顺序 (按照 BaseService 子节点顺序):
  #1 EventDispatcher
       → service.Register(gameContext)
         → OnRegister(context) → context.RegisterService(this) → IsRegistered=true

  #2 GameState (发现自 GetComponentsInChildren)
       → service.Register(gameContext)
         → OnRegister(context)
           ├─ context.RegisterService(this)
           ├─ 初始化状态: currentState = initialState = MainMenu
           └─ PushSnapshot(SGameState(MainMenu))

  #3 InputManager
       → service.Register(gameContext)
         → OnRegister(context) → context.RegisterService(this)

  #4 CameraManager
       → service.Register(gameContext)
         → OnRegister(context)
           ├─ ValidateConfiguration()
           ├─ EnsureCinemachineBrain()
           ├─ EnsureDefaultVirtualCamera()
           ├─ InitializeDefaultRig()
           └─ context.RegisterService(this)

  #5 PlayerManager
       → service.Register(gameContext)
         → OnRegister(context) → context.RegisterService(this)

  #6 TimeScaleManager
       → service.Register(gameContext)
         → OnRegister(context) → context.RegisterService(this)

  #7 UIManager
       → service.Register(gameContext)
         → OnRegister(context)
           ├─ BuildScreenLookup() → Instantiate 所有 UI Screen prefab
           └─ BuildOverlayLookup() → Instantiate 所有 UI Overlay prefab
```

### 2.4 Bootstrap Step 3: AttachDispatcher

```
AttachDispatcherToServices()  [顺序: registeredServices list]
  │
  ├─ EventDispatcher.AttachDispatcher(eventDispatcher)
  │    → Dispatcher = eventDispatcher
  │    → OnDispatcherAttached() [virtual, empty]
  │
  ├─ GameState.AttachDispatcher(eventDispatcher)
  │    → Dispatcher = eventDispatcher
  │    → OnDispatcherAttached()
  │      ├─ hasInitialized = true
  │      └─ ApplyState(MainMenu, force=true)
  │           ├─ Cursor.visible = true, Cursor.lockState = None
  │           ├─ PushSnapshot(SGameState(MainMenu)) → GameContext.UpdateSnapshot
  │           └─ Dispatcher.Publish(SGameState(MainMenu))
  │                → ⚠ 此时 InputManager 尚未订阅 (Phase 3 才订阅)
  │
  ├─ InputManager.AttachDispatcher(eventDispatcher)
  │    → Dispatcher = eventDispatcher
  │    → OnDispatcherAttached()
  │      ├─ InitializeInputHandlers()
  │      │    └─ foreach handler: handler.InitializeHandler(Dispatcher)
  │      │         → 绑定 inputAction.performed += Execute
  │      │         → 绑定 inputAction.canceled += Execute
  │      ├─ EnableActions()
  │      │    └─ foreach handler: handler.Enable() → runtimeAction.Enable()
  │      │    └─ EnforceHandlerStatePermissions()
  │      │         → currentGameState = Initializing [尚未同步]
  │      │         → 每个 handler 检查 SupportsState(Initializing)
  │      └─ SyncInitialGameState()
  │           └─ GameContext.TryGetSnapshot(out SGameState)
  │                → 拿到 MainMenu [GameState 在 Phase 2 已发布]
  │                → currentGameState = MainMenu
  │                → EnforceHandlerStatePermissions()
  │                     → handler.SupportsState(MainMenu) → 只有 Playing 状态的 handler 被禁用
  │
  ├─ CameraManager.AttachDispatcher(eventDispatcher)
  │    → Dispatcher = eventDispatcher
  │    → OnDispatcherAttached() [virtual, empty]
  │
  ├─ PlayerManager.AttachDispatcher(eventDispatcher)
  │    → OnDispatcherAttached() [virtual, empty]
  │
  ├─ TimeScaleManager.AttachDispatcher(eventDispatcher)
  │    → OnDispatcherAttached() [virtual, empty]
  │
  └─ UIManager.AttachDispatcher(eventDispatcher)
       → OnDispatcherAttached() [virtual, empty]
```

### 2.5 Bootstrap Step 4: ActivateSubscriptions

```
ActivateServiceSubscriptions()
  │
  ├─ EventDispatcher.ActivateSubscriptions() [virtual, empty]
  │
  ├─ GameState.ActivateSubscriptions()
  │    → OnSubscriptionsActivated()
  │      └─ Dispatcher.Subscribe<SUIEscapeIAction>(HandleEscapeIntent)
  │           → 注册监听: 按 Escape → MainMenu ↔ Playing
  │
  ├─ InputManager.ActivateSubscriptions()
  │    → OnSubscriptionsActivated()
  │      └─ Dispatcher.Subscribe<SGameState>(HandleGameStateChanged)
  │
  ├─ CameraManager.ActivateSubscriptions()
  │    → OnSubscriptionsActivated()
  │      ├─ Dispatcher.Subscribe<SLocomotion>(HandleLocomotionSnapshot)
  │      └─ Dispatcher.Subscribe<SLookIAction>(HandleLook)
  │
  ├─ PlayerManager.ActivateSubscriptions() [virtual, empty]
  │
  ├─ TimeScaleManager.ActivateSubscriptions()
  │    → OnSubscriptionsActivated()
  │      └─ Dispatcher.Subscribe<STimeScaleIAction>(HandleTimeScaleRequested)
  │
  └─ UIManager.ActivateSubscriptions() [virtual, empty]
```

### 2.6 Bootstrap Step 5: NotifyInitialized (OnServicesReady)

```
InitializeServices()
  │
  ├─ EventDispatcher.NotifyInitialized() → OnServicesReady() [empty]
  │
  ├─ GameState.NotifyInitialized() → OnServicesReady() [empty]
  │
  ├─ InputManager.NotifyInitialized() → OnServicesReady() [empty]
  │
  ├─ CameraManager.NotifyInitialized()
  │    → OnServicesReady()
  │      ├─ PushCameraSnapshotToContext() → GameContext.UpdateSnapshot(SCameraContext)
  │      ├─ localPlayerAnchor = GameObject.Find("Anchor")?.transform
  │      │    ⚠ 场景中必须存在名为 "Anchor" 的 GameObject
  │      ├─ defaultVirtualCamera.Follow = localPlayerAnchor
  │      └─ defaultVirtualCamera.LookAt = localPlayerAnchor
  │
  ├─ PlayerManager.NotifyInitialized()
  │    → OnServicesReady()
  │      └─ CreatePlayer() ───→ [触发 Player 实例化, 见第3节]
  │
  ├─ TimeScaleManager.NotifyInitialized() → OnServicesReady() [empty]
  │
  └─ UIManager.NotifyInitialized()
       → OnServicesReady()
         ├─ 显示第一个 visible=true 的 Screen
         └─ 显示所有 visible=true 的 Overlay
```

---

## 3. 帧0: Player 实例化

`PlayerManager.CreatePlayer()` 在 GameManager.Awake() 中同步执行。此时游戏尚未进入 Update 循环。

### 3.1 Prefab 实例化

```
Instantiate(PlayerPrefab) → Player (GameObject)
  ├─ [Component] Character
  ├─ [Component] Rigidbody
  ├─ [Component] CapsuleCollider
  └─ Model (嵌套 prefab, Animancer Humanoid)
       ├─ [Component] LocomotionAgent
       ├─ [Component] CharacterAnimationController
       ├─ [Component] LocomotionAnimancerPresenter
       ├─ [Component] Animator
       └─ [Component] NamedAnimancerComponent
```

### 3.2 Awake 阶段 (同步, Unity 保证确定性顺序)

```
Player 根节点:
  Character.Awake()
    ├─ characterAnimation  = GetComponentInChildren<CharacterAnimationController>()  [Model 上]
    ├─ locomotion          = GetComponentInChildren<LocomotionAgent>()                 [Model 上]
    └─ locomotionPresenter = GetComponentInChildren<LocomotionAnimancerPresenter>()   [Model 上]

Model 子节点:
  CharacterAnimationController.Awake()
    ├─ animancer = GetComponentInChildren<NamedAnimancerComponent>()  [同 GO]
    ├─ animator  = GetComponentInChildren<Animator>()                 [同 GO]
    └─ ConfigureRuntimeLayers()
         ├─ animancer.Layers.SetMinCount(6)
         ├─ Layer 0: FullBody  → fullBodyLayer,  fullBodyArbiter
         ├─ Layer 1: UpperBody → upperBodyLayer, upperBodyArbiter
         ├─ Layer 2: Additive  → additiveLayer,  additiveArbiter
         ├─ Layer 3: Facial    → facialLayer,    facialArbiter
         ├─ Layer 4: HeadLook  → headLookLayer   [常驻]
         └─ Layer 5: Footstep  → footstepLayer   [常驻]

  LocomotionAgent.Awake()
    ├─ ResolveRigReferencesIfNeeded()
    │    └─ modelRoot = transform.Find("Model")  [同 GO]
    └─ animancerPresenter = GetComponentInChildren<LocomotionAnimancerPresenter>()
```

### 3.3 OnEnable 阶段 (Awake 之后, 同步)

```
Character.OnEnable()  [默认实现, nothing]

LocomotionAgent.OnEnable()
  ├─ EnsureMotorCreated()
  │    └─ new LocomotionMotor(transform, modelRoot, locomotionProfile)
  │         ├─ 获取 Rigidbody (GetComponent<Rigidbody>())
  │         ├─ detectVerticalOffset = locomotionProfile.groundDetectVerticalOffset
  │         ├─ standBoxHalfExtents  = locomotionProfile.groundStandBoxHalfExtents
  │         └─ enableGroundLocking  = locomotionProfile.enableGroundLocking
  ├─ EnsureLocomotionControllerCreated()
  │    └─ new LocomotionCoordinatorHuman()
  │         └─ base: new LocomotionCoordinatorBase(
  │              new LocomotionGraph(...),
  │              new LocomotionTraversalGraph(),
  │              new LocomotionTurningGraph())
  └─ EnsureInputModuleCreated()
       └─ new LocomotionInputModule(this)
            ├─ 缓存 owner, dispatcher, GameContext
            └─ (尚未 Subscribe, 见下文)
  └─ if (autoSubscribeInput) → inputModule.Subscribe()
       → 向 Dispatcher 注册 9 个输入事件 + SCameraContext 的监听

CharacterAnimationController.OnEnable()  [默认实现, nothing]

LocomotionAnimancerPresenter.OnEnable()  [默认实现, nothing]
```

### 3.4 LocomotionInputModule.Subscribe() — 输入订阅

```
LocomotionInputModule.Subscribe()
  → 从 EventDispatcher 订阅以下类型的监听 Handler:
  
  ┌──────────────────────┬──────────────────────────────────────────┐
  │ 事件类型              │ 处理方式                                 │
  ├──────────────────────┼──────────────────────────────────────────┤
  │ SMoveIAction          │ 存入 moveAction, lastMoveAction          │
  │ SLookIAction          │ 仅 isPlayer=true 时存入 lookAction       │
  │ SCrouchIAction        │ 存入 crouchAction                        │
  │ SProneIAction         │ 存入 proneAction                         │
  │ SWalkIAction          │ 存入 walkAction                          │
  │ SRunIAction           │ 存入 runAction                           │
  │ SSprintIAction        │ 存入 sprintAction                        │
  │ SJumpIAction          │ 存入 jumpAction                          │
  │ SStandIAction         │ 存入 standAction                         │
  │ SCameraContext        │ 存入 cameraControl (仅 isPlayer=true)    │
  └──────────────────────┴──────────────────────────────────────────┘
```

### 3.5 Start 阶段 (OnEnable 之后, 同步)

```
Character.Start()
  ├─ locoDriver = new LocomotionDriver(locomotion, alias, animProfile)
  │     → Channel=FullBody, Priority=Locomotion(0), Mode=Continuous
  │     → 存储 agent, alias, animationProfile
  │
  ├─ traversalDriver = new TraversalDriver(alias)
  │     → Channel=FullBody, Priority=Traversal(4), Mode=OneShot
  │     → 存储 alias
  │
  ├─ characterAnimation.RegisterDriver(locoDriver)
  │    → locoDriver.Initialize(controller)       [仅存储 controller 引用]
  │    → fullBodyArbiter.RegisterDriver(locoDriver)
  │         → driverBuffer[0] = locoDriver
  │         → 按 Priority 排序
  │         → Mode=Continuous → defaultDriver = locoDriver
  │
  └─ characterAnimation.RegisterDriver(traversalDriver)
       → traversalDriver.Initialize(controller)  [空实现]
       → fullBodyArbiter.RegisterDriver(traversalDriver)
            → driverBuffer[1] = traversalDriver
            → 按 Priority 排序 [traversal(4) > locomotion(0)]
            → Mode=OneShot → 不设 defaultDriver

LocomotionAnimancerPresenter.Start()
  ├─ agent    = GetComponentInParent<LocomotionAgent>()      [Model 上]
  └─ animator = GetComponent<Animator>()                     [同 GO]
```

### 3.6 PlayerManager 发布事件

```
PlayerManager.CreatePlayer() [继续]
  ├─ GameContext.UpdateSnapshot(SPlayer)
  └─ Dispatcher.Publish(SPlayerSpawnedEvent)
       → ⚠ 无订阅者 (当前无系统消费此事件)
```

### 3.7 Bootstrap 完成

```
GameManager:
  → isBootstrapped = true
  → Logger: "GameManager bootstrap completed."
```

---

## 4. 帧0: 第一帧 Update

Bootstrap 全部在 Awake 中完成。第一帧 Update 在此之后执行。

### 4.1 CameraManager.Update() [ExecutionOrder -400]

```
CameraManager.Update() [-400]
  └─ TickLocalPlayerAnchor()
       ├─ localPlayerAnchor == null? → RETURN
       │    ⚠ 若场景无 "Anchor" GO → 此后每帧静默返回
       ├─ hasLocomotionPosition == false → RETURN
       │    ⚠ 首次发布尚未发生 (LocomotionAgent.Update 在本帧后面)
       └─ 本帧不会移动 Anchor
```

### 4.2 (ExecutionOrder 0) 竞态窗口

```
⚠ LocomotionAgent 和 CharacterAnimationController 均位于 ExecutionOrder 0
   执行顺序取决于 Unity GameObject 的顺序, 不可预测

场景 A: LocomotionAgent.Update() 先执行 [理想情况]
场景 B: CharacterAnimationController.Update() 先执行
```

#### 场景 A: LocomotionAgent 先执行

```
LocomotionAgent.Update()
  └─ Simulate(deltaTime)
       ├─ inputModule.ReadActions(out inputActions)
       │    → 读取已缓存的输入事件
       │    → 清空单帧信号 (IsRequested/IsReleased)
       │
       ├─ inputModule.ReadCameraControl(out hasCameraControl, out cameraControl)
       │    → ⚠ 首次帧: CameraManager.TickLocalPlayerAnchor 尚未发布 SCameraContext
       │    → hasCameraControl = false
       │    → viewForward = Vector3.zero
       │
       ├─ motor.Evaluate(profile, inputActions, viewForward, deltaTime)
       │    ├─ UpdateKinematics()
       │    │    → 计算 desiredLocalVelocity (基于输入)
       │    │    → SmoothVelocity()
       │    │    → 世界空间转换 (基于 locomotionHeading)
       │    ├─ LocomotionHeadLook.Evaluate()
       │    │    → 计算 local yaw/pitch
       │    ├─ LocomotionKinematics.ComputeSignedPlanarTurnAngle()
       │    │    → 身体朝向 vs 运动朝向的夹角
       │    ├─ EvaluateGroundContactAndApplyConstraints()
       │    │    ├─ LocomotionGroundDetection.EvaluateGroundContact()
       │    │    │    ├─ TrySampleStandingByBox() → 判落地
       │    │    │    └─ TrySampleDistanceByRay() → 测距
       │    │    ├─ AccumulateGroundContactStateDuration()
       │    │    ├─ StabilizeGroundContact() → 防抖
       │    │    ├─ if (grounded): actorTransform.y = contactPoint.y   ← 地面锁定
       │    │    └─ UpdateFreezePositionY()
       │    │         → rigidbody.constraints | FreezePositionY
       │    ├─ LocomotionObstacleDetection.TryDetectForwardObstacle()
       │    │    → 前方射线 + 高度探针
       │    └─ 返回 SLocomotionMotor (position, velocity, ground, obstacle, look, turn)
       │
       ├─ locomotionCoordinator.Evaluate(motor, profile, actions, deltaTime)
       │    ├─ Graph.Evaluate() → Phase/Gait/Posture Aspects
       │    ├─ TraversalGraph.Evaluate() → SLocomotionTraversal
       │    │    └─ (首次帧: Stage=Idle, 无障碍物 + 无跳跃 → 保持 Idle)
       │    └─ 组装 SLocomotionDiscrete (含 isTurning)
       │
       ├─ snapshot = new SLocomotion(motorOutput, mode, traversal)
       │    → Animation = default (不再填充)
       │
       └─ PushSnapshot()
            ├─ GameContext.UpdateSnapshot(snapshot)
            │    → contextSnapshots[typeof(SLocomotion)] = snapshot
            └─ Dispatcher.Publish(snapshot)
                 └─ [同步调用所有订阅者]
                      ├─ CameraManager.HandleLocomotionSnapshot()
                      │    ├─ lastLocomotionPosition = payload.Motor.Position
                      │    └─ hasLocomotionPosition = true
                      └─ (DebugOverlay 等其他消费者...)

CharacterAnimationController.Update()
  └─ fullBodyArbiter.Update(deltaTime)
       ├─ playbackState == None → EvaluatePending()
       │    ├─ 遍历 OneShot drivers (按优先级降序):
       │    │    └─ TraversalDriver.BuildRequest()
       │    │         ├─ GameContext.TryGetSnapshot(out SLocomotion)
       │    │         │    → ✓ 拿到刚发布的快照
       │    │         ├─ traversal.Stage != Requested → return null
       │    │         │    → 首次帧: Idle, 无请求
       │    │         └─ return null
       │    │
       │    └─ ActivateDefault()
       │         ├─ activeDriver = defaultDriver = LocomotionDriver
       │         └─ LocomotionDriver.OnResumed() → isActive = true
       │
       └─ ActiveDriver?.Update(deltaTime)
            └─ LocomotionDriver.Update(deltaTime)
                 ├─ isActive == true → 继续
                 ├─ EnsureInitialized()
                 │    ├─ agent.Motor ≠ null ✓ (OnEnable 中创建)
                 │    ├─ agent.Profile ≠ null ✓ (serialized field)
                 │    ├─ controller.GetFullBodyLayer() ≠ null ✓
                 │    └─ new LocomotionAnimationController(
                 │         controller.Animancer,
                 │         alias,
                 │         locoProfile,
                 │         animationProfile,
                 │         motor,
                 │         new BaseLayer(fullBodyLayer),      ← Layer 0
                 │         new HeadLookLayer(headLookLayer),  ← Layer 4
                 │         new FootLayer(footLayer))          ← Layer 5
                 │         → BaseLayer.EnsureInitialized()
                 │              → stateMachine.ForceSetState(Idle, idleState)
                 │              → BaseIdleState.OnEnter()
                 │                   → Play(idleL)  [播放待机动画]
                 │
                 ├─ GameContext.TryGetSnapshot(out SLocomotion)
                 │    → ✓ 拿到刚发布的快照
                 │
                 └─ locomotionAnimCtrl.UpdateAnimations(snapshot, deltaTime)
                      ├─ BaseLayer.Update(context)  [FullBody Layer 0]
                      │    → stateMachine.CurrentState.Tick()
                      │    → BaseIdleState.Tick()
                      │         → 根据 snapshot 检查条件:
                      │              Phase==GroundedIdle, !IsTurning → 保持 Idle
                      │              !IsGrounded → TrySetState(AirLoop)
                      ├─ HeadLookLayer.Update(context)  [HeadLook Layer 4]
                      │    → TryPlay(lookMixer) as Vector2MixerState
                      │    → 平滑 yaw/pitch 参数
                      └─ FootLayer.Update(context)  [Footstep Layer 5]
                           → 存根实现, 无实际动画
```

#### 场景 B: CharacterAnimationController 先执行

```
CharacterAnimationController.Update()
  └─ fullBodyArbiter.Update(deltaTime)
       └─ LocomotionDriver.Update(deltaTime)
            ├─ EnsureInitialized()
            │    → ✓ 全部依赖满足, 创建 LocomotionAnimationController
            │    → BaseIdleState.OnEnter() → Play(idleL)
            ├─ GameContext.TryGetSnapshot(out SLocomotion)
            │    → ✗ 首次帧尚无快照 (LocomotionAgent 尚未运行)
            │    → return [不驱动 FSM]
            └─ ⚠ FSM 已在 EnsureInitialized 中进入 Idle 状态,
                 但未基于当前快照验证条件

LocomotionAgent.Update()
  └─ Simulate()
       └─ PushSnapshot()
            └─ GameContext.UpdateSnapshot(SLocomotion)  ← 快照现已就绪
            └─ Dispatcher.Publish(SLocomotion)
                 → CameraManager.HandleLocomotionSnapshot()
                      → hasLocomotionPosition = true
```

### 4.3 动画阶段: OnAnimatorMove

```
Animator.Update (Animation 阶段, Update 之后)
  └─ OnAnimatorMove() [在 LocomotionAnimancerPresenter 中]
       ├─ forwardRootMotion == true
       ├─ animator != null, agent != null
       ├─ motor = agent.Motor
       ├─ deltaPosition = animator.deltaPosition
       │    → 包含动画的根运动位移 (X, Y, Z)
       ├─ deltaRotation = animator.deltaRotation
       │
       ├─ motor.ApplyDeltaPosition(deltaPosition)
       │    → actorTransform.position += deltaPosition
       │    → ⚠ 如果 deltaPosition.y ≠ 0, 覆盖地面锁定
       │
       └─ motor.ApplyDeltaRotation(deltaRotation)
            → actorTransform.rotation *= deltaRotation
```

### 4.4 CameraManager.LateUpdate() [ExecutionOrder -400]

```
CameraManager.LateUpdate()
  └─ PushCameraSnapshotToContext()
       ├─ outputCamera = cameraBrain.OutputCamera
       ├─ localPlayerAnchor?.position / rotation
       └─ GameContext.UpdateSnapshot(SCameraContext)
```

---

## 5. 帧1+: 稳态循环

从第 1 帧开始，所有系统已初始化，快照链路完整。

### 5.1 完整的输入 → 动画链路

```
┌─────────────────────────────────────────────────────────────────────┐
│ 输入阶段 (EventDispatcher + InputModule)                            │
│                                                                     │
│  Unity Input System 触发 callback                                   │
│    → InputActionHandler.Execute()                                   │
│      → IAPlayerMove: Dispatcher.Publish(SMoveIAction)               │
│      → IAPlayerLook: Dispatcher.Publish(SLookIAction)               │
│          → CameraManager.HandleLook() → lastLookAction              │
│      → IAPlayerJump: Dispatcher.Publish(SJumpIAction)               │
│      → ... (其他按钮)                                                │
│                                                                     │
│  LocomotionInputModule 的订阅 handler 触发:                          │
│    → PutAction<SMoveIAction>(payload) → moveAction                  │
│    → PutAction<SJumpIAction>(payload) → jumpAction                  │
│    → ...                                                            │
├─────────────────────────────────────────────────────────────────────┤
│ 运动阶段 (LocomotionAgent.Update())                                  │
│                                                                     │
│  ReadActions() → SLocomotionInputActions (聚合 + 清空单帧)           │
│  ReadCameraControl() → SCameraContext (从 CameraManager 发布)        │
│                                                                     │
│  motor.Evaluate()                                                    │
│    → 速度运动学 (期望/平滑/转换)                                     │
│    → 地面检测 (BoxCast + Raycast, 稳定化, 锁定)                     │
│    → 障碍物检测 (前方射线 + 高度探针)                                │
│    → 头部朝向 (yaw/pitch)                                           │
│    → 转向角度                                                        │
│                                                                     │
│  coordinator.Evaluate()                                              │
│    → PhaseAspect: GroundedIdle / GroundedMoving / Airborne           │
│    → GaitAspect: Idle / Walk / Run / Sprint                          │
│    → PostureAspect: Standing / Crouching / Prone                     │
│    → TraversalGraph: Idle → (检测到 Requested?)                      │
│    → TurningGraph: IsTurning                                         │
│                                                                     │
│  PushSnapshot() → GameContext.UpdateSnapshot(SLocomotion)            │
│                 → Dispatcher.Publish(SLocomotion)                   │
├─────────────────────────────────────────────────────────────────────┤
│ 动画阶段 (CharacterAnimationController.Update())                     │
│                                                                     │
│  FullBodyArbiter.Update()                                            │
│    ├─ EvaluatePending(): TraversalDriver.BuildRequest()              │
│    │   → TryGetSnapshot → traversal.Stage == Requested?              │
│    │   → 否 → return null                                            │
│    │                                                                 │
│    └─ LocomotionDriver.Update()                                      │
│        → TryGetSnapshot → LocomotionAnimationController              │
│          ├─ BaseLayer.Update (context)                               │
│          │   → FSM.Tick() → 根据 Phase/Gait/Posture/IsTurning       │
│          │   → PlayIfChanged(correspondingAlias)                    │
│          │   → TurnAngleStepRotationApplier.TryApply()              │
│          ├─ HeadLookLayer.Update (context)                           │
│          │   → Vector2Mixer 参数: yaw/pitch                         │
│          └─ FootLayer.Update (context)                               │
│              → 存根, 无动画                                          │
├─────────────────────────────────────────────────────────────────────┤
│ Root Motion 阶段 (Animation Update)                                  │
│                                                                     │
│  OnAnimatorMove() [LocomotionAnimancerPresenter]                     │
│    → motor.ApplyDeltaPosition(animator.deltaPosition)               │
│    → motor.ApplyDeltaRotation(animator.deltaRotation)               │
├─────────────────────────────────────────────────────────────────────┤
│ 相机阶段                                                             │
│                                                                     │
│  CameraManager.Update() [-400]                                       │
│    → TickLocalPlayerAnchor()                                        │
│      → anchor.position = lastLocomotionPosition + verticalOffset    │
│      → ApplyLookRotationToAnchor() → anchor.rotation                 │
│      → Dispatcher.Publish(SCameraContext) ← 供 LocomotionInputModule │
│                                                                     │
│  CameraManager.LateUpdate() [-400]                                   │
│    → GameContext.UpdateSnapshot(SCameraContext)                      │
└─────────────────────────────────────────────────────────────────────┘
```

### 5.2 相机 Anchor 时序

```
帧 N:
  CameraManager.Update() [-400]
    → anchor.position = lastLocomotionPosition[N-1] + offset  [1帧滞后]
  
  LocomotionAgent.Update() [0]
    → PushSnapshot() → Dispatcher.Publish(SLocomotion)
    → CameraManager.HandleLocomotionSnapshot()
      → lastLocomotionPosition[N] = payload.Motor.Position

帧 N+1:
  CameraManager.Update() [-400]
    → anchor.position = lastLocomotionPosition[N] + offset
```

---

## 6. 攀爬中断链路

当角色遇到可攀爬障碍物并按下跳跃键时：

```
帧 N:
  LocomotionAgent.Update()
    → motor.Evaluate()
      → obstacleDetection.CanClimb = true, ObstacleHeight = 1.2
    → coordinator.Evaluate()
      → TraversalGraph.Evaluate()
        → Idle + JumpButton.IsRequested + CanClimb
        → Traversal = SLocomotionTraversal(Climb, Requested, 1.2, ...)
    → PushSnapshot() → SLocomotion 包含 Requested 遍历

帧 N+1:
  CharacterAnimationController.Update()
    → FullBodyArbiter.Update()
      → EvaluatePending()
        → TraversalDriver.BuildRequest()
          → TryGetSnapshot(SLocomotion) → traversal.Stage == Requested + Type == Climb
          → ObstacleHeight = 1.2 → ResolveClimbAlias(1.2) → ClimbUp2meter
          → return CharacterAnimationRequest.CreateAlias(
              "Traversal_Climb", Traversal, ClimbUp2meter, FullBody, Traversal, 0.1, 0.15)

        → AcceptRequest(traversalDriver, request)
          → InterruptActive()
            → LocomotionDriver.OnInterrupted() → isActive = false
          → activeDriver = traversalDriver
          → layer.Play(ClimbUp2meter)       ← 播放攀爬动画
          → playbackState = Playing

  ⚠ 同帧: LocomotionAgent.Update()
    → TraversalGraph.Evaluate()
      → Requested → EvaluateRequested()
        → 条件仍满足 → 转为 Committed
    → coordinator 发现 Committed:
      → discreteState = CreateActionControlled()
        → Phase = GroundedIdle, Gait = Idle, IsTurning = false


帧 N+2 ~ N+K (攀爬动画播放中):

  CharacterAnimationController.Update()
    → FullBodyArbiter.Update()
      → EvaluatePlaying(deltaTime)
        → layer.CurrentState.NormalizedTime < 0.99 → 返回, 继续播放
        ⚠ ActiveDriver (TraversalDriver) 的 Update 不被调用

  LocomotionAgent.Update()
    → TraversalGraph.Evaluate()
      → Committed: committedTimer += deltaTime
        → timer < 0.45s → 保持 Committed
        → timer >= 0.45s → Completed → 下一帧 Idle


帧 K (攀爬动画播完):

  CharacterAnimationController.Update()
    → FullBodyArbiter.Update()
      → EvaluatePlaying(deltaTime)
        → layer.CurrentState.NormalizedTime >= 0.99 → CompleteActive()
          → playbackState = Completed

帧 K+1:

  CharacterAnimationController.Update()
    → FullBodyArbiter.Update()
      → playbackState == Completed → TransitionToDefault()
        → playbackState = None
        → ActivateDefault()
          → LocomotionDriver.OnResumed() → isActive = true
        → EvaluatePending() → 无 OneShot 请求
        → LocomotionDriver.Update()
          → TryGetSnapshot → FSM 基于当前 Phase (GroundedIdle) 自行恢复到 Idle 状态
```

---

## 7. 事件订阅清单

### Dispatcher.Publish → 订阅关系

| 发布方 | 事件类型 | 订阅方 | 订阅阶段 |
|---|---|---|---|
| InputActionHandler | `SMoveIAction` | LocomotionInputModule | Agent.OnEnable |
| InputActionHandler | `SLookIAction` | LocomotionInputModule | Agent.OnEnable |
| InputActionHandler | `SLookIAction` | CameraManager | Phase 3 |
| InputActionHandler | `SJumpIAction` | LocomotionInputModule | Agent.OnEnable |
| InputActionHandler | `SCrouchIAction` | LocomotionInputModule | Agent.OnEnable |
| InputActionHandler | `SProneIAction` | LocomotionInputModule | Agent.OnEnable |
| InputActionHandler | `SWalkIAction` | LocomotionInputModule | Agent.OnEnable |
| InputActionHandler | `SRunIAction` | LocomotionInputModule | Agent.OnEnable |
| InputActionHandler | `SSprintIAction` | LocomotionInputModule | Agent.OnEnable |
| InputActionHandler | `SStandIAction` | LocomotionInputModule | Agent.OnEnable |
| InputActionHandler | `STimeScaleIAction` | TimeScaleManager | Phase 3 |
| InputActionHandler | `SUIEscapeIAction` | GameState | Phase 3 |
| CameraManager | `SCameraContext` | LocomotionInputModule | Agent.OnEnable |
| GameState | `SGameState` | InputManager | Phase 3 |
| LocomotionAgent | `SLocomotion` | CameraManager | Phase 3 |
| PlayerManager | `SPlayerSpawnedEvent` | (无订阅者) | — |

### GameContext.UpdateSnapshot → 读取关系

| 写入方 | 快照类型 | 读取方 | 方式 |
|---|---|---|---|
| LocomotionAgent | `SLocomotion` | LocomotionDriver | `TryGetSnapshot` |
| LocomotionAgent | `SLocomotion` | TraversalDriver | `TryGetSnapshot` (BuildRequest) |
| LocomotionAgent | `SLocomotion` | LocomotionDebugOverlay | `TryGetSnapshot` |
| CameraManager | `SCameraContext` | (仅 EventDispatcher) | — |
| GameState | `SGameState` | InputManager | `TryGetSnapshot` (SyncInitial) |
| PlayerManager | `SPlayer` | (无消费者) | — |

---

## 8. 已知问题与检查点

### 检查点 1: Anchor 是否存在

```
位置: CameraManager.OnServicesReady()
代码: localPlayerAnchor = GameObject.Find("Anchor")?.transform;
检查: 场景中是否存在名为 "Anchor" 的 GameObject
症状: 若不存在 → localPlayerAnchor 始终为 null → TickLocalPlayerAnchor() 每帧 return → Follow/LookAt 为空
修复: 在场景中创建空 GameObject, 命名为 "Anchor"
```

### 检查点 2: 相机首帧不动

```
原因: CameraManager [-400] 早于 LocomotionAgent [0]
  Frame 0: CameraManager.Update → hasLocomotionPosition=false → RETURN
  Frame 0: LocomotionAgent.Update → PushSnapshot → HandleLocomotionSnapshot → hasLocomotionPosition=true
  Frame 1: CameraManager.Update → hasLocomotionPosition=true → Anchor 开始跟随 (滞后1帧)

影响: Anchor 从第2帧开始跟随, 视觉上可接受
```

### 检查点 3: 角色浮空

```
原因: OnAnimatorMove() 在 Animation Update 阶段 (Update 之后) 覆盖地面锁定 Y 轴
  Update: motor.Evaluate() → actorTransform.position.y = contactPoint.y
  Animation: OnAnimatorMove() → motor.ApplyDeltaPosition(deltaPosition)
           → actorTransform.position += deltaPosition (可能含 Y 偏移)

检查项:
  - idle 动画是否有 root motion Y 偏移? (检查 AnimationClip 的 Root Transform Position (Y) 曲线)
  - applyRootMotionPlanarPositionOnly = true 是否生效? 
    (当前 Presenter 未使用此字段 — ApplyDeltaPosition 总是应用完整 deltaPosition)
  - 落地检测 BoxCast 距离是否足够? (standBoxHalfExtents.y + detectVerticalOffset = 0.03 + 0.01 = 0.04)
```

### 检查点 4: 竞技条件 (Race Condition)

```
问题: LocomotionAgent 和 CharacterAnimationController 都位于 ExecutionOrder 0
  Unity 不保证同 Order 组件之间的执行顺序

场景 B (CharacterAnimationController 先执行):
  - LocomotionDriver.Update() 时快照为空 → 不驱动 FSM
  - 但 EnsureInitialized() 仍在运行 → 创建控制器 + 播放 Idle 动画
  - 缺少基于快照的条件检查, FSM 始终在 Idle

影响: 每帧 FSM 都会重新检查条件, 第2帧拿到快照后恢复正常
建议: 给 CharacterAnimationController 设置 ExecutionOrder > 0
```

### 检查点 5: GameState 转换

```
启动状态: MainMenu (或 Initializing)
  → 所有 Plays 类 InputActionHandler.SupportsState(MainMenu) → 禁用输入
  → 角色无法移动/跳跃/蹲伏

切换到 Playing:
  → 按 Escape → GameState.HandleEscapeIntent → MainMenu→Playing
  → InputManager 收到 SGameState → ApplyGameState(Playing)
  → EnforceHandlerStatePermissions() → 启用 Playing 支持的 handler
  → 角色开始接收输入

检查: 初始化后是否手动触发 MainMenu→Playing 转换
```

### 检查点 6: TraversalDriver 触发验证

```
条件链路:
  1. 场景中有可攀爬障碍物 (Collider + 高度 ≤ 2m)
  2. 角色靠近障碍物 (< 0.75m, LocomotionProfile.obstacleProbeDistance)
  3. obstacleDetection.CanClimb = true
  4. 角色在地面 (GroundedIdle 或 GroundedMoving)
  5. 按下 Jump 按钮

  满足全部条件:
    → TraversalGraph: Idle → Requested
    → TraversalDriver.BuildRequest: 检测到 Requested → 构建 climb 请求
    → Arbiter: AcceptRequest → 中断 LocomotionDriver → 播放 ClimbUp* 动画
    → TraversalGraph: Requested → Committed → Completed

  ⚠ 当前 ClimbUp0_5meter / ClimbUp1meter / ClimbUp2meter 别名需有对应的 AnimationClip 资源
```

### 检查点 7: LocomotionDriver 延迟初始化

```
时序: LocomotionDriver 构造 (Character.Start) → 首次 Update (同帧或下帧)
  EnsureInitialized() 要求:
    - agent.Motor ≠ null  [OnEnable 中创建, ✓]
    - agent.Profile ≠ null  [serialized field, 需手动赋值]
    - controller.Animancer ≠ null  [Awake 中 auto-resolve, ✓]
    - controller.GetFullBodyLayer() ≠ null  [Awake 中创建, ✓]
    - alias ≠ null  [构造参数, 来自 Presenter → 需手动赋值]
    - animationProfile ≠ null  [构造参数, 来自 Presenter → 需手动赋值]

  任一为 null → EnsureInitialized 每帧 return, FSM 永不创建, 动画不播放
```

### 检查点 8: Scene GameObject 检查清单

```
☐ 场景中有 "Anchor" GameObject (CameraManager.OnServicesReady 查找)
☐ 场景中有 "PlayerStart" GameObject (PlayerManager.OnServicesReady 备选)
☐ GameManager prefab 已拖入场景
☐ Player prefab 的 SO 引用已赋值 (见 scene-setup.md 第 6 节)
☐ Player.Model.CharacterAnimationController 的 5 个 AvatarMask 已赋值
☐ Player.Model.LocomotionAgent.isPlayer = true
☐ Cinemachine 已安装 (Package Manager)
```
