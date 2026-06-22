# Character 模块 · 生命周期审计

> **Last Verified**: 2026-06-22 | **Verification**: 所有方法内容与代码一致

## 完整生命周期表

| 模块 | Awake | OnAssemble | OnEnable | Start | OnWire | OnDisable | OnDestroy | Update |
|------|-------|-----------|----------|-------|--------|-----------|-----------|--------|
| **CharacterActor** `Hub` | `SetupModel()` 实例化Model+AddComponent AnimationBrain → `ResolveComponents()` GetComponent×6收集引用 → `new CharacterRig` → `new CharacterBuildContext(...)` 传入14个参数 → `new PlayerDirector/NpcDirector(Registry)` → `new CharacterKinematic(Registry)` → `new GroundLocomotion(Registry)` → `new CharacterCombat(Registry)` → `base.Awake()` 扫描MB子+OnAssembleAll | — | 空 | `base.Start()` → OnWireAll → `agent.AddModifier(hunger)` | — | `characterKinematic?.Reset()` | `combat?.UnsubscribeEvents()` → `characterKinematic?.Reset()` | director.Evaluate → kinematic.Evaluate → locomotion.Simulate → animation.Apply → pathfinding.Sync |
| **AnimationBrain** `Hub` | `AddComponent<LocomotionDriver>()` → `AddComponent<TraversalDriver>()` → `base.Awake()` 扫描+OnAssembleAll | — | — | `GetComponentInParent<CharacterActor>()?.BuildContext` → 找Animancer/Animator → `SetMinCount(7)` → `new DriverArbiter` → `BindLayer(0..6)`×7 → `base.Start()` OnWireAll → `locoDriver.BaseLayer.FootstepCallback = () => OnFootstep` | — | — | — | Apply: `fullBodyArbiter.Resolve` + UpdateHeadLook + ApplySpeedMultiplier |
| **BaseAnimationDriver** `ChildMono` | — | — | 空 | — | `brain = GetComponent<AnimationBrain>` → `brain?.RegisterDriver(this)` | 空 | `brain?.UnregisterDriver(this)` | — |
| **LocomotionDriver** `ChildMono` | — | — | — | — | `base.OnWire()` 继承注册 → 取BuildContext→DefaultLocomotionSet → `new BaseLayer(brain.FullBodyLayer, set, config, ctx)` | 空 | 继承`brain?.UnregisterDriver` | Evaluate(grip切换逻辑+Arm层控制) + Drive(baseLayer.Update) |
| **TraversalDriver** `ChildMono` | — | — | — | — | 继承注册 | 空 | 继承`brain?.UnregisterDriver` | Evaluate(空/TODO) + Drive(空) |
| **CharacterAudio** `ChildMono` | — | `brain = GetComponent<AnimationBrain>()` | — | — | `brain.OnFootstep += HandleFootstep` | — | `brain.OnFootstep -= HandleFootstep` | — |
| **PathfindingAgent** `ChildMono` | — | `GetComponent×2` → 配置参数 → `Teleport(transform.position)` | — | — | — | — | — | — |
| **PlayerDirector** `Child` | — | `input = new PlayerInput(ctx.EventHub)` | — | — | `input.BindEvents()` | — | — | — |
| **NpcDirector** `Child` | — | — | — | — | — | — | — | — |
| **CharacterKinematic** `Child` | — | — | — | — | — | — | — | — |
| **GroundLocomotion** `Child` | — | — | — | — | — | — | — | — |
| **CharacterCombat** `Child` | — | — | — | — | `ctx.Ability`×3 回调 + `ctx.Reactor`×4 回调 + `EventHub.Register(Hit)` | — | 父代调`UnsubscribeEvents()` | — |

**图例**：`—` 无此方法

## 问题清单（已全部修复）

| # | 模块 | 阶段 | 问题 | 修复 |
|---|------|------|------|------|
| 1 | ~~BaseAnimationDriver~~ | ~~OnEnable~~ | ~~RegisterDriver~~ | ✅ 删除，OnEnable 清空 |
| 2 | ~~BaseAnimationDriver~~ | ~~OnDisable~~ | ~~UnregisterDriver~~ | ✅ 删除，OnDisable 清空 |
| 3 | ~~BaseAnimationDriver~~ | ~~OnDestroy~~ | ~~未实现~~ | ✅ 新增 `brain?.UnregisterDriver` |
| 4 | ~~CharacterAudio~~ | ~~OnWire~~ | ~~GetComponent~~ | ✅ 移到 OnAssemble |
| 5 | ~~CharacterAudio~~ | ~~OnDestroy~~ | ~~未取消订阅~~ | ✅ 新增 OnDestroy |
| 6 | ~~CharacterCombat~~ | ~~OnAssemble~~ | ~~回调赋值~~ | ✅ 移到 OnWire |
| 7 | ~~LocomotionDriver~~ | ~~OnWire~~ | ~~重复取 brain~~ | ✅ 删除 |
| 8 | ~~PathfindingAgent~~ | ~~OnWire~~ | ~~Teleport~~ | ✅ 移到 OnAssemble |

## 相关文档

- [module-lifecycle.md](../../../L1-core/module-lifecycle.md) — 生命周期标准
- [module-system.md](../../../L1-core/module-system.md) — ModuleHub / ModuleChildMono / ModuleChild 定义
