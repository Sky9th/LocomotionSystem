# Character 模块——当前调用链分析

> 分析日期: 2025-04-27
> 基于实际代码（存在部分文件损坏）

---

## 1. GameObject 层级

```
Player (根节点, Tag=Player, Layer=6)
  ├── CharacterActor           ← Character/Components
  ├── Rigidbody
  ├── CapsuleCollider
  └── Model (子节点)
       ├── CharacterAnimationController  ← Character/Animation/Components
       ├── CharacterLocomotion           ← Character/Locomotion/Agent
       ├── Animator
       └── NamedAnimancerComponent
```

**Component 关系**:
- `CharacterActor` 在根节点
- `CharacterAnimationController` 和 `CharacterLocomotion` 在 Model 子节点
- 三者都是 MonoBehaviour，有独立的 Unity 生命周期
- `CharacterActor` 通过 `GetComponentInChildren<>` 查找 Model 上的组件

---

## 2. 初始化链路 (第一帧之前)

### 时序

```
GameManager.Awake() [-500]
  └── Bootstrap Phase 4 → PlayerManager.CreatePlayer()
       └── Instantiate(Player.prefab)
            │
            ├── CharacterActor.Awake()
            │     ├── characterAnimation = GetComponent<CharacterAnimationController>()
            │     ├── characterLocomotion = GetComponentInChildren<CharacterLocomotion>()
            │     ├── inputModule = new CharacterInputModule(this)
            │     └── characterKinematic = new CharacterKinematic(transform, ...)
            │
            ├── CharacterAnimationController.Awake()
            │     ├── animancer = GetComponentInChildren<NamedAnimancerComponent>()
            │     ├── animator  = GetComponentInChildren<Animator>()
            │     └── ConfigureRuntimeLayers()
            │           ├── animancer.Layers.SetMinCount(6)
            │           ├── Layer 0: FullBody  → fullBodyArbiter  (无 Mask)
            │           ├── Layer 1: UpperBody → upperBodyArbiter (upperBodyMask)
            │           ├── Layer 2: Additive  → additiveArbiter  (additiveMask)
            │           ├── Layer 3: Facial    → facialArbiter    (facialMask)
            │           ├── Layer 4: HeadLook  → headLookLayer    (headMask, 常驻)
            │           └── Layer 5: Footstep  → footstepLayer    (footMask, 常驻)
            │
            └── CharacterLocomotion.Awake()
                  └── ResolveRigReferencesIfNeeded() → modelRoot

            ├── CharacterActor.OnEnable()
            │     └── inputModule.Subscribe()
            │           └── 向 Dispatcher 注册 9 个输入事件 + SCameraContext
            │
            ├── CharacterLocomotion.OnEnable()
            │     ├── motor = new LocomotionMotor(transform)
            │     └── coordinator = new LocomotionCoordinatorHuman()
            │           ├── LocomotionGraph { PhaseAspect, GaitAspect, PostureAspect }
            │           ├── LocomotionTraversalGraph
            │           └── LocomotionTurningGraph
            │
            └── CharacterActor.Start()
                  ├── motor = characterLocomotion?.Motor         ❌ 违规: 访问 Child 内部
                  ├── characterAnimation.SetMotor(motor)
                  ├── locoDriver = new LocomotionDriver(motor, profile, alias, animProfile)   ❌ 违规
                  ├── traversalDriver = new TraversalDriver(alias)                           ❌ 违规
                  ├── characterAnimation.RegisterDriver(locoDriver)
                  │     ├── locoDriver.Initialize(controller)
                  │     └── fullBodyArbiter.RegisterDriver(locoDriver)
                  │           └── 排序: [LocomotionDriver(P0), TraversalDriver(P4)]
                  └── characterAnimation.RegisterDriver(traversalDriver)
                        └── fullBodyArbiter.RegisterDriver(traversalDriver)
```

### 问题

1. **CharacterActor.Start() 在 Awake 之后、Update 之前执行**
2. Motor 在 `CharacterLocomotion.OnEnable()` 创建 → Start 时可访问 ✓
3. 但 `characterLocomotion` 在 `CharacterActor.Awake()` 中赋值 → Start 时可用 ✓
4. **然而 `CharacterActor.Awake()` 访问 `characterLocomotion.ModelRoot`** → 此时 `CharacterLocomotion.Awake()` 已执行（同帧），所以 `modelRoot` 可能已解析 ✓
5. 时序风险: `CharacterLocomotion.OnEnable()` 中创建 Motor，但 `CharacterActor.Start()` 通过 `characterLocomotion.Motor` 访问 → Start 在 OnEnable 之后 ✓

---

## 3. 稳态帧调用链

```
每帧执行顺序:

CameraManager.Update() [-400]
  └── TickLocalPlayerAnchor()
       ├── localPlayerAnchor == null? → 需场景中有 "Anchor" GO
       └── GameContext.TryGetSnapshot<SCharacterKinematic>()
            → kinematic.Position + verticalOffset → anchor.position

CharacterActor.Update() [0]
  ├── inputModule.ReadActions(out inputActions)
  ├── characterLocomotion.SetInput(in inputActions)       ← 向下推输入
  ├── GameContext.TryGetSnapshot<SCameraContext>() → viewForward
  ├── kinematic = characterKinematic.Evaluate(profile, viewForward, dt)
  │     ├── CharacterGroundDetection.EvaluateGroundContact()
  │     ├── → EvaluateStableGroundContact → 地面锁定 → actorTransform.position.y 修正
  │     ├── CharacterHeadLook.Evaluate()
  │     └── CharacterObstacleDetection.TryDetectForwardObstacle()
  └── GameContext.UpdateSnapshot(kinematic)               ← 对外推快照

CharacterAnimationController.Update() [0]   ⚠ 同 ExecutionOrder, 顺序不确定
  ├── fullBodyArbiter.Update(dt)
  │     ├── EvaluatePending()
  │     │    ├── TraversalDriver.BuildRequest()
  │     │    │    └── GameContext.TryGetSnapshot<SLocomotion>()
  │     │    │         → traversal.Stage == Requested + Type == Climb?
  │     │    │         → 否 → return null
  │     │    └── ActivateDefault() → LocomotionDriver.OnResumed()
  │     │
  │     └── ActiveDriver.Update(dt)
  │          └── LocomotionDriver.Update(dt)
  │               ├── EnsureInitialized()
  │               │    └── new LocomotionAnimationController(...)
  │               │         ├── new BaseLayer(fullBodyLayer)   ← FSM 初始化, Play(idleL)
  │               │         ├── new HeadLookLayer(headLookLayer)
  │               │         └── new FootLayer(footLayer)
  │               ├── GameContext.TryGetSnapshot<SLocomotion>()
  │               │    → ⚠ 若 CharacterLocomotion 还未运行 → 读到上帧数据
  │               └── locomotionAnimCtrl.UpdateAnimations(snapshot, dt)
  │                    ├── BaseLayer.Update(context)     [Layer 0]
  │                    │    └── FSM.Tick() → 根据 snapshot 切换状态 → Play(alias)
  │                    ├── HeadLookLayer.Update(context)  [Layer 4]
  │                    └── FootLayer.Update(context)      [Layer 5]
  └── (upperBody/ additive/ facial Arbiter → 无 Driver → 空转)

CharacterLocomotion.Update() [0]        ⚠ 同 ExecutionOrder, 顺序不确定
  ├── GameContext.TryGetSnapshot<SCharacterKinematic>()   ⚠ 从 Context 拉取
  ├── ⚠ 文件损坏: Simulate() 内容不完整

[Animation Phase]
  CharacterAnimationController.OnAnimatorMove()
    ├── forwardRootMotion == true
    └── motor.ApplyDeltaPosition(animator.deltaPosition)
         motor.ApplyDeltaRotation(animator.deltaRotation)
         → actorTransform.position += deltaPosition   ← 可能覆盖地面锁定
```

---

## 4. 调用关系图（当前状态）

```
                 ┌──────────────────────────────────────┐
                 │           CharacterActor              │
                 │  Awake → Start → Update              │
                 └──────┬──────┬────────┬──────────────┘
                        │      │        │
            ┌───────────┘      │        └──────────────────┐
            ▼                  ▼                            ▼
    ┌──────────────┐  ┌──────────────┐         ┌──────────────────────┐
    │ InputModule  │  │ CharacterKin │         │ CharacterAnimation   │
    │ ReadActions  │  │ .Evaluate()  │         │ RegisterDriver() ←──┼─── Start 中 new Driver ❌
    └──────────────┘  └──────┬───────┘         │ SetMotor()           │
                             │                 │ Update() → Arbiter   │
                             │                 └──────────┬───────────┘
                             │                            │
                             │              ┌─────────────┼─────────────┐
                             │              ▼             ▼             ▼
                ┌────────────┴──┐  ┌──────────┐ ┌──────────┐ ┌─────────┐
                │CharacterLocom │  │Arbiter   │ │LocoDriver│ │Traversal│
                │ .SetInput()◄──│  │Evaluate  │ │.Update() │ │Driver   │
                │ .Simulate()───│──│Pend/Play │ │→GameCtx◄─│ │→GameCtx◄│
                │ .Motor ───────│──│Default   │ │→BaseLayer│ │→Request │
                └──────┬────────┘  └──────────┘ └──────────┘ └─────────┘
                       │
            ┌──────────┼──────────┐
            ▼          ▼          ▼
    ┌──────────┐ ┌──────────┐ ┌──────────┐
    │ Motor    │ │Coordinator│ │GameCtx   │ ← PushSnapshot ❌
    │.Evaluate │ │.Evaluate  │ │Publish   │
    └──────────┘ └──────────┘ └──────────┘
```

### 当前致命问题

| 位置 | 问题 |
|---|---|
| `CharacterLocomotion.cs:62-78` | **Simulate() 代码损坏** — 重复的 TryGetSnapshot，缺失 motor/coordinator/snapshot 逻辑 |
| `CharacterActor.cs:103,106,112` | ❌ **跨级调用**: 直接访问 `Motor`、创建 `LocomotionDriver`/`TraversalDriver` |
| `CharacterLocomotion.cs:70` | ⚠ 从 `GameContext` 拉取 `SCharacterKinematic`（应参数化） |
| `CharacterLocomotion.cs:80-94` | ⚠ `PushSnapshot()` 直接操作 `GameContext`（应由 Actor 统一） |
| `CharacterActor.cs:83` | ⚠ 从 `GameContext` 拉取 `SCameraContext`（应通过 `CharacterInputModule.ReadCameraControl`） |
| `LocomotionDriver.cs:63` | ⚠ 从 `GameContext` 拉取 `SLocomotion`（应参数化） |
| `TraversalDriver.cs:44` | ⚠ 从 `GameContext` 拉取 `SLocomotion`（应参数化） |
| `CharacterAnimationController:197` | ⚠ `OnAnimatorMove()` 中 root motion 覆盖地面锁定 |

---

## 5. GameContext 快照流

```
当前 (多个入口推送):
  CharacterActor.Update()
    └── GameContext.UpdateSnapshot(SCharacterKinematic)    ← 入口 1

  CharacterLocomotion.Simulate()  [损坏]
    └── PushSnapshot()
         ├── GameContext.UpdateSnapshot(SLocomotion)        ← 入口 2
         └── Dispatcher.Publish(SLocomotion)                ← 入口 3 (Event)

  CameraManager.Update() [-400]                             
    └── Dispatcher.Publish(SCameraContext)                  ← 入口 4 (Event)
  CameraManager.LateUpdate() [-400]
    └── GameContext.UpdateSnapshot(SCameraContext)          ← 入口 5
```

**快照消费**:
```
CameraManager.TickLocalPlayerAnchor()
  └── GameContext.TryGetSnapshot<SCharacterKinematic>()

CharacterLocomotion.Simulate()
  └── GameContext.TryGetSnapshot<SCharacterKinematic>()

LocomotionDriver.Update()
  └── GameContext.TryGetSnapshot<SLocomotion>()

TraversalDriver.BuildRequest()
  └── GameContext.TryGetSnapshot<SLocomotion>()
```

---

## 6. 与目标架构的差异

| 目标 | 当前 | 差异 |
|---|---|---|
| 唯一对外快照 `SCharacterSnapshot` | 3 条推送路径 | 需合并到 CharacterActor |
| 逐级调用，不跨级 | CharacterActor 访问 Motor + 创建 Driver | 需修正 Driver 创建权责 |
| 子系统参数化，不拉 GameContext | CharacterLocomotion/Driver 到处 TryGetSnapshot | 需改为参数传入 |
| CharacterLocomotion.Simulate() 纯函数 | 损坏 + PushSnapshot 直接操作 Context | 需重写并返回数据</pre> |
