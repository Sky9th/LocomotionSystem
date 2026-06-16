# L3_Character 数据传递树

> 以类为节点，构造参数为数据入口。
> `Registry` 是 Module 基类自动注册管道（`base(registry)` → `parent.Register(this)`），不列入数据流。
> ⚠ = 缺失 ctx 或跨级调用。

---

```
CharacterActor : ModuleBehaviour
│
│  Awake: ResolveComponents() 收集自身 GameObject 上组件:
│    AnimationBrain, EventHub, PropertyAgent,
│    AbilityExecutor, AbilityReactor, PathfindingAgent
│
│  OnAssemble:
│
├── new CharacterRig(transform, modelRoot)
│      入: Transform root, Transform model
│      出: 物理写入 API
│
├── new CharacterBuildContext(
│       root, eventHub, agent, ability, reactor, pathfinding,
│       modelRoot, characterRig, skillSlot1, skillSlot2
│    )
│      出: 静态引用容器 → 向下分发
│      ⚠ 未包含: AnimationAliasProfile, LocomotionAnimationProfile,
│               AudioConfig, 5×AvatarMask, LocomotionProfile,
│               ForwardRootMotion, ApplyRootMotionRotation, AutoMatchAnimationSpeed
│
│   ── 以下 ctx 均指 CharacterBuildContext ──
│
├── [Player] new PlayerDirector(ctx)
│     入: ctx (EventHub, Ability, SkillSlot1/2, Pathfinding, ModelRoot)
│     出: SCharacterIntent
│     │
│     └── new PlayerInput(ctx.EventHub)
│           入: EventHub
│           ⚠ BindEvents 内 GameContext.Instance (L3→L1)
│           出: per-frame input flags, mouseGroundPosition
│
├── [NPC]  new NpcDirector()
│     ⚠ 空壳, 没收 ctx
│     出: SCharacterIntent.None
│
├── new CharacterKinematic(ctx)
│     入: ctx (Rig, Root, ModelRoot)
│     出: SCharacterKinematic
│     │
│     └── (static 工具类，无构造，纯计算)
│           CharacterGroundDetection.EvaluateGroundContact(pos, probe, radius, mask, angle)
│           CharacterHeadLook.Evaluate(viewFwd, modelRoot, root, profile)
│           CharacterObstacleDetection.TryDetectForwardObstacle(pos, …)
│
├── new GroundLocomotion()
│     ⚠ 没收 ctx (当前不需要——数据全从 Simulate(ref FrameCtx) 来)
│     │
│     ├── new Motor()
│     │     Evaluate(kin, intent, profile, dt) → SCharacterMotor
│     │
│     └── new Stance()
│           Evaluate(motor, kin, intent, profile, kProfile, animProfile, dt)
│             → SCharacterDiscrete
│
├── new CharacterCombat(ctx)
│     入: ctx (Agent, Ability, Reactor, EventHub)
│     出: 桥接 Ability ↔ Properties
│
├── [Model 子节点, Awake 触发]
│   AnimationBrain : ModuleBehaviour
│     ⚠ Awake: GetComponentInParent<CharacterActor>() → _actor
│     ⚠ 从 _actor 读: Masks×5, AnimationAliasProfile,
│                      LocomotionAnimationProfile, ForwardRootMotion …
│     入: Apply(in FrameCtx) ← 每帧消费
│     │
│     ├── new DriverArbiter(fullBodyLayer)
│     │     入: AnimancerLayer
│     │     Resolve(in FrameCtx, dt) → 驱动调度
│     │
│     ├── [AddComponent] LocomotionDriver : BaseAnimationDriver
│     │     ⚠ OnWire: GetComponent<CharacterActor>() 取:
│     │         AnimationAliasProfile, LocomotionAnimationProfile,
│     │         LocomotionProfile, ctx
│     │     Drive(in FrameCtx, dt) → baseLayer.Update(...)
│     │     │
│     │     └── new BaseLayer(
│     │             brain.FullBodyLayer,
│     │             actor.AnimationAliasProfile,
│     │             actor.LocomotionAnimationProfile,
│     │             actor.LocomotionProfile,
│     │             actor.Context
│     │         )
│     │           入: AnimancerLayer, 3×SO, BuildCtx
│     │           ctx 只用: buildContext.Rig
│     │           Update(FrameCtx, dt) → FSM tick
│     │           │
│     │           ├── new BaseIdleState(this)
│     │           ├── new BaseMovingState(this)
│     │           ├── new BaseTurnInPlaceState(this)
│     │           ├── new BaseIdleToMovingState(this)
│     │           ├── new BaseTurnInMovingState(this)
│     │           ├── new BaseAirLoopState(this)
│     │           └── new BaseAirLandState(this)
│     │                 入: BaseLayer (通过 Owner)
│     │                 出: Play/PlayIfChanged 驱动动画
│     │
│     └── [AddComponent] TraversalDriver : BaseAnimationDriver
│           ⚠ OnEnable: GetComponent<CharacterActor>()
│                        ?.AnimationAliasProfile → _aliasProfile
│           Evaluate(in FrameCtx, dt) → 障碍检测→爬越请求
│           OnStarted/OnCompleted → 控制 Rig 物理
│
├── [Model 子节点]
│   CharacterAudio : ModuleComponent
│     ⚠ Config getter: GetComponentInParent<CharacterActor>()
│                      ?.CharacterAudioConfig
│     OnWire → 订阅 AnimationBrain.OnFootstep
│     HandleFootstep → AudioChannel.Play
│
└── [根 GameObject]
    PathfindingAgent : ModuleComponent
      OnAssemble → GetComponent<Seeker>(), GetComponent<AIPath>()
      SyncLocomotion(in SCharacterDiscrete) → 同步速度
      SetDestination(Vector3) → 寻路
      出: HasPath, DesiredVelocity, PathDirection
```

---

## Update 数据流 (每帧)

```
CharacterActor.Update(dt)
│
│  new FrameCtx { LocomotionProfile, LocomotionAnimationProfile, KinematicProfile }
│
├─① director.Evaluate()
│     PlayerInput → input flags
│     ctx.Pathfinding → HasPath, DesiredVelocity
│     → FrameCtx.Intent
│
├─② characterKinematic.Evaluate(profile, heading, aim, dt)
│     ctx.Rig → ground lock
│     static 工具类 → ground/head/obstacle 检测
│     → FrameCtx.Kinematic
│
├─③ locomotionSimulator.Simulate(ref FrameCtx, intent, profile, dt)
│     Motor.Evaluate → FrameCtx.Motor
│     Stance.Evaluate → FrameCtx.Discrete
│
├─④ characterAnimation.Apply(in FrameCtx)
│     DriverArbiter.Resolve →
│       LocomotionDriver.Drive → BaseLayer.Update → FSM → Play
│       TraversalDriver.Evaluate → 障碍检测
│     UpdateHeadLook / ApplySpeedMultiplier
│
└─⑤ pathfindingAgent.SyncLocomotion(in ctx.Discrete)
```

---

## 缺口: ctx 注入不完整

| # | 类 | 当前取数据方式 | 应改为 |
|---|-----|--------------|--------|
| 1 | `NpcDirector` | 没有任何数据 | 收 ctx |
| 2 | `AnimationBrain` | `GetComponentInParent<CharacterActor>().AnimationAliasProfile/Masks/…` | `SetBuildContext(ctx)` |
| 3 | `LocomotionDriver` | `GetComponent<CharacterActor>().AnimationAliasProfile/LocomotionAnimationProfile/LocomotionProfile/Context` | AnimationBrain 下传 ctx |
| 4 | `TraversalDriver` | `GetComponent<CharacterActor>().AnimationAliasProfile` | AnimationBrain 下传 ctx |
| 5 | `CharacterAudio` | `GetComponentInParent<CharacterActor>().CharacterAudioConfig` | AnimationBrain 下传 ctx |

## 缺口: BuildCtx 字段不全

当前 BuildCtx 包含 10 个字段，但子模块还在通过 `GetComponent<CharacterActor>()` 往上爬取这些：

| 缺失字段 | 谁需要 |
|---------|--------|
| `AnimationAliasProfile` | AnimationBrain, LocomotionDriver, TraversalDriver |
| `LocomotionAnimationProfile` | AnimationBrain, LocomotionDriver |
| `LocomotionProfile` | LocomotionDriver (传 BaseLayer) |
| `CharacterAudioConfig` | CharacterAudio |
| `UpperBodyMask, AdditiveMask, FacialMask, HeadMask, FootMask` | AnimationBrain |
| `ForwardRootMotion, ApplyRootMotionRotation, AutoMatchAnimationSpeed` | AnimationBrain |

## 跨级调用

| # | 位置 | 调用 | 消除方式 |
|---|------|------|---------|
| 1 | `PlayerInput.BindEvents` | `GameContext.Instance.TryResolveService(out dispatcher)` | EventDispatcher→EventHub 迁移后消除 |
