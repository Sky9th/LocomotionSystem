# Character 动画子模块 — 现状分析与改进方案

---

## 1. 当前架构定位

### 1.1 角色层级关系

```
Character (MonoBehaviour, 组合根)
  ├── CharacterAnimationController (动画的唯一入口)
  │     ├── Layer 0: FullBody  ── DriverArbiter 仲裁
  │     │     ├── LocomotionDriver (P0, Continuous, 默认)
  │     │     └── TraversalDriver (P4, OneShot)
  │     ├── Layer 1: UpperBody ── DriverArbiter (空)
  │     ├── Layer 2: Additive  ── DriverArbiter (空)
  │     ├── Layer 3: Facial    ── DriverArbiter (空)
  │     ├── Layer 4: HeadLook  ── HeadLookLayer (常驻, 非仲裁)
  │     └── Layer 5: Footstep  ── FootLayer (常驻, 非仲裁)
  │
  ├── LocomotionAgent (运动子系统)
  │     ├── LocomotionMotor (运动学)
  │     ├── LocomotionCoordinator (离散状态 + Traversal 图)
  │     ├── LocomotionInputModule (输入聚合)
  │     └── PushSnapshot() → GameContext + EventDispatcher
  │
  ├── LocomotionAnimancerPresenter (残留: 仅根运动转发)
  │     └── OnAnimatorMove() → motor.ApplyDeltaPosition/Rotation
  │
  └── 未来子系统: AbilitySystem, CombatSystem ...
```

### 1.2 当前状态：迁移完成 ✓

已从 **Locomotion 直管 Animancer** 迁移到 **CharacterAnimationController 统一调度**：

| | 旧架构（已废弃） | 新架构（当前） |
|---|---|---|
| 层管理者 | `LocomotionAnimancerPresenter`（Start 中直接分配 Layer） | `CharacterAnimationController`（Awake 中分配 6 层） |
| 动画驱动 | `animancerPresenter.Evaluate()` 每帧被 Agent 调用 | `LocomotionDriver` 注册到 FullBodyArbiter，独立 Update |
| 攀爬动画 | 无 | `TraversalDriver` OneShot 请求，中断 Locomotion |
| 根运动 | Presenter.OnAnimatorMove() | Presenter.OnAnimatorMove()（待迁移到 Controller） |
| 数据解耦 | Agent 直管动画层 | Agent 仅 PushSnapshot，Driver 独立读取快照 |

### 1.3 已完成的基础设施

| 设施 | 状态 | 实际使用者 |
|---|---|---|
| `ICharacterAnimationDriver` | ✓ | LocomotionDriver, TraversalDriver |
| `DriverArbiter` (仲裁器) | ✓ | FullBody Channel 已投入使用 |
| `CharacterAnimationRequest` | ✓ | TraversalDriver.BuildRequest() |
| `ECharacterAnimationPlaybackState` | ✓ | DriverArbiter 内部状态流转 |
| `CharacterAnimationController` | ✓ | 管理 6 层 + 4 个 Arbiter |
| `Character.cs` (组合根) | ✓ | 注册所有 Driver，持有子系统引用 |

---

## 2. 已拆解掉的旧功能

### 2.1 LocomotionAnimancerPresenter 的职责削减

| 旧行为 | 新状态 |
|---|---|
| `animancer.Layers.SetMinCount(3)` | ✗ 移除。由 `CharacterAnimationController` 管理 6 层 |
| `new BaseLayer(baseLayer)` 创建层 | ✗ 移除。由 `LocomotionDriver.EnsureInitialized()` 创建 |
| `Evaluate()` 每帧驱动动画 | ✗ 移除。由 `LocomotionDriver.Update()` 替代 |
| `OnAnimatorMove()` 根运动转发 | ✓ 保留。计划迁移到 `CharacterAnimationController` |
| 持有 `LocomotionAliasProfile` / `LocomotionAnimationProfile` | ✓ 保留。`Character.cs` 通过 Presenter 读取 |

### 2.2 已迁移的功能对照

| 原有功能 | 当前位置 | 文件 |
|---|---|---|
| BaseLayer 7状态 FSM | `LocomotionDriver` 内部包装 → `LocomotionAnimationController` | `LocomotionDriver.cs` |
| HeadLookLayer | `LocomotionDriver` 内部包装 → Layer 4 | `LocomotionDriver.cs` |
| FootLayer | `LocomotionDriver` 内部包装 → Layer 5 | `LocomotionDriver.cs` |
| TurnAngleStepRotationApplier | 仍在 BaseLayer FSM 内部 | `BaseLayer/Appliers/` |
| RootMotion 转发 | 仍在 Presenter | `LocomotionAnimancerPresenter.cs` |
| 动画快照读取 | `LocomotionDriver.Update()` → `GameContext.TryGetSnapshot` | `LocomotionDriver.cs` |

---

## 3. 目标架构 → 当前实现

### 3.1 组件关系（实际实现）

```
┌──────────────────────────────────────────────────────────────────────┐
│ Character (MonoBehaviour)                                            │
│                                                                      │
│  Start():                                                            │
│    new LocomotionDriver(agent, alias, animProfile)                   │
│      → controller.RegisterDriver → FullBodyArbiter (P0, Continuous) │
│    new TraversalDriver(alias)                                        │
│      → controller.RegisterDriver → FullBodyArbiter (P4, OneShot)    │
│                                                                      │
│  ┌─ CharacterAnimationController ────────────────────────────────┐  │
│  │  Awake(): ConfigureRuntimeLayers (6 layers)                   │  │
│  │    Layer 0: FullBody  → FullBodyArbiter                       │  │
│  │    Layer 1-3: 空 Arbtier (未注册 Driver)                       │  │
│  │    Layer 4: HeadLook  → 常驻层 (由 LocomotionDriver 驱动)     │  │
│  │    Layer 5: Footstep  → 常驻层 (由 LocomotionDriver 驱动)     │  │
│  │                                                                │  │
│  │  Update(): fullBodyArbiter.Update()                            │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─ LocomotionAgent ─────────────────────────────────────────────┐  │
│  │  Update() → Simulate():                                       │  │
│  │    Motor → Coordinator → TraversalGraph                       │  │
│  │    → snapshot = SLocomotion(motor, discrete, traversal)       │  │
│  │    → PushSnapshot() → GameContext + EventDispatcher           │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─ LocomotionDriver (ICharacterAnimationDriver) ─────────────────┐  │
│  │  Update():                                                     │  │
│  │    EnsureInitialized() → new LocomotionAnimationController(... │  │
│  │    → GameContext.TryGetSnapshot(out SLocomotion snapshot)      │  │
│  │    → locomotionAnimCtrl.UpdateAnimations(snapshot, deltaTime)  │  │
│  │      ├─ BaseLayer.Update()      [FullBody Layer]              │  │
│  │      ├─ HeadLookLayer.Update()  [HeadLook Layer]              │  │
│  │      └─ FootLayer.Update()      [Footstep Layer]              │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌─ TraversalDriver (ICharacterAnimationDriver) ──────────────────┐  │
│  │  BuildRequest():                                               │  │
│  │    → GameContext.TryGetSnapshot(out SLocomotion snapshot)      │  │
│  │    → traversal.Stage == Requested && Type == Climb?            │  │
│  │    → 选择 ClimbUp* 别名 → 构建 CharacterAnimationRequest       │  │
│  │    → 返回给 Arbiter → 中断 LocomotionDriver                    │  │
│  └────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

### 3.2 Driver 仲裁机制（已实现）

```
FullBody Channel 仲裁:
  Driver 列表 (按优先级):
    ┌──────────────────────┐
    │ TraversalDriver (P4) │  ← OneShot, 更高优先级
    │ LocomotionDriver(P0) │  ← Continuous, 默认 Driver
    └──────────────────────┘

  每帧流程:
    Arbiter.Update()
      ├─ EvaluatePending(): 遍历 OneShot drivers → BuildRequest()
      │   └─ TraversalDriver → 检测到 Requested 遍历 → AcceptRequest
      │       ├─ InterruptActive() → LocomotionDriver.OnInterrupted()
      │       └─ layer.Play(climbClip)
      │
      └─ ActiveDriver?.Update(): 
          └─ LocomotionDriver.Update() → 读快照 → 驱动 FSM
```

### 3.3 请求状态机（实际行为）

```
                      TraversalDriver.BuildRequest()
                               │
                               ▼
                        (返回非null请求)
                               │
                               ▼
                          AcceptRequest
                               │
                    LocomotionDriver.OnInterrupted()
                               │
                       layer.Play(climbClip)
                               │
                               ▼
                          Playing
                               │
                    (NormalizedTime >= 0.99)
                               │
                               ▼
                          CompleteActive()
                               │
                    TransitionToDefault()
                               │
                    LocomotionDriver.OnResumed()
```

---

## 4. 数据流

### 4.1 输入 → 动画的完整链路

```
LocomotionAgent.Update()                     CharacterAnimationController.Update()
        │                                               │
        ├─ inputModule.ReadActions()                    ├─ FullBodyArbiter.Update()
        ├─ motor.Evaluate()                             │   │
        │   └─ 地面检测 + 运动学 + 障碍物探测           │   ├─ EvaluatePending()
        ├─ coordinator.Evaluate()                       │   │   └─ TraversalDriver.BuildRequest()
        │   └─ TraversalGraph.Evaluate()                │   │       └─ 读快照 → Requested+Climb?
        │       └─ Idle→Requested→Committed→Completed   │   │          → 返回 ClimbUp* 请求
        │                                               │   │
        └─ PushSnapshot() ──────────────────────┐       │   └─ (无请求) → ActivateDefault()
                         快照缓存 (GameContext)  │       │       └─ LocomotionDriver.OnResumed()
                     + 事件发布 (EventDispatcher)│       │
                                                 │       └─ ActiveDriver.Update()
                                                 │           └─ LocomotionDriver.Update()
                                                 │               ├─ GameContext.TryGetSnapshot()
                                                 │               └─ locomotionAnimCtrl.UpdateAnimations()
                                                 │                   ├─ BaseLayer.Update (Layer 0)
                                                 │                   ├─ HeadLookLayer.Update (Layer 4)
                                                 │                   └─ FootLayer.Update (Layer 5)
                                                 │
                    ┌────────────────────────────┘
                    ▼
              GameContext.UpdateSnapshot(SLocomotion)
              EventDispatcher.Publish(SLocomotion)
                    │
                    ├──→ CameraManager.HandleLocomotionSnapshot()
                    │       → 更新 Anchor 位置 (滞后1帧, Bug-1)
                    │
                    └──→ LocomotionDebugOverlay (快照显示)
```

### 4.2 根运动

```
每帧执行顺序:
  Update(): motor.Evaluate()
    → EvaluateGroundContactAndApplyConstraints()
    → actorTransform.position.y = contactPoint.y (地面锁定)
  
  Animation Update 阶段:
    OnAnimatorMove() [Presenter]
    → motor.ApplyDeltaPosition(animator.deltaPosition)
    → actorTransform.position += deltaPosition (可能覆盖地面锁定, Bug-2)
```

---

## 5. 实施状态

### Phase 1 ✓ 清理并统一层布局
- [x] Presenter 不再自己分配层 → 由 Controller 的 `GetFullBodyLayer()` 等获取
- [x] `CharacterAnimationController` 扩展为 6 层 (FullBody/UpperBody/Additive/Facial/HeadLook/Footstep)
- [x] HeadLook/Footstep 作为常驻层 (Layer 4/5)，不参与仲裁

### Phase 2 ✓ 实现仲裁框架
- [x] `DriverArbiter.cs` — 每 Channel 一个仲裁器，优先级排序 + 状态流转
- [x] `ICharacterAnimationDriver` 扩展 Priority / Mode / BuildRequest
- [x] `CharacterAnimationController.Update()` 驱动 4 个 Arbiter

### Phase 3 ✓ 将 Locomotion 动画接入 Driver 模式
- [x] `LocomotionDriver.cs` — 包装 BaseLayer FSM，Continuous 模式，Priority=0
- [x] `LocomotionAgent.Simulate()` — 移除 `animancerPresenter.Evaluate()`，仅 PushSnapshot
- [x] `Character.Start()` — 创建并注册 LocomotionDriver

### Phase 4 ✓ 实现攀爬动画中断
- [x] `TraversalDriver.cs` — OneShot 模式，FullBody Channel，Priority=4
- [x] `BuildRequest()` 直接从 `GameContext` 读快照 → 检测 Requested+Climb
- [x] 根据 `ObstacleHeight` 选择 ClimbUp0_5meter / ClimbUp1meter / ClimbUp2meter
- [x] 中断/恢复流程: LocomotionDriver.OnInterrupted() → 动画播放 → OnResumed()
- [ ] 未验证：需要实际场景中触发攀爬逻辑测试中断链

### Phase 5 ✓ 清理废弃代码
- [x] Presenter 移除 `Start()` 中的 `LocomotionAnimationController` 创建
- [x] Presenter 移除 `Evaluate()` / `BuildAnimationSnapshot()` / `LastAnimation` / `AnimationSnapshots`
- [x] Presenter 精简为 68 行：仅保留根运动转发 + 配置持有
- [ ] `OnAnimatorMove()` 未迁移到 `CharacterAnimationController`（按计划第 4.3 节待完成）

---

## 6. 当前已知问题

### Bug-1: Camera Anchor 不跟随角色（首帧滞后）

**根因**: `CameraManager` 的 `[DefaultExecutionOrder(-400)]` 早于 `LocomotionAgent`(0)。首帧 `TickLocalPlayerAnchor()` 时 `hasLocomotionPosition=false`，anchor 不移动。

**链路**:
```
CameraManager.Update() [-400] → TickLocalPlayerAnchor()
  → hasLocomotionPosition=false → return (不移动 anchor)

LocomotionAgent.Update() [0] → PushSnapshot()
  → EventDispatcher.Publish(SLocomotion)
  → CameraManager.HandleLocomotionSnapshot()
  → hasLocomotionPosition=true
```

从第 2 帧开始 anchor 会跟随，但始终滞后 1 帧。另外需确认场景中存在名为 `"Anchor"` 的 GameObject（`OnServicesReady` 中通过 `GameObject.Find` 查找）。

### Bug-2: 角色浮空

**根因**: `OnAnimatorMove()` (Animation Update 阶段) 应用 root motion delta 覆盖了 `Update()` 中的地面锁定。

**链路**:
```
Update(): motor.Evaluate()
  → EvaluateGroundContactAndApplyConstraints()
  → actorTransform.position.y = contactPoint.y (地面锁定)

Animation Update 阶段 (之后):
  → OnAnimatorMove() 
  → motor.ApplyDeltaPosition(animator.deltaPosition)
  → actorTransform.position += deltaPosition (Y 轴被覆盖)
```

缓解措施（Phase 5 已做）：移除 Presenter.Start() 的第二套控制器创建，消除双重动画控制。但 `OnAnimatorMove()` 本身仍然存在覆盖风险→ 取决于动画资源是否有 root motion Y 偏移。

### 待处理项

| 项 | 优先级 | 说明 |
|---|---|---|
| Anchor 跟随修复 | 高 | 考虑 CameraManager 不依赖 EventDispatcher 首次事件，或改为直接引用 Agent |
| 地面锁定保护 | 高 | `OnAnimatorMove` 中忽略 deltaPosition.y，或 `applyRootMotionPlanarPositionOnly` 生效 |
| 根运动迁移到 Controller | 中 | 按计划 4.3 节，将 OnAnimatorMove 从 Presenter 移到 CharacterAnimationController |
| Presenter 彻底移除 | 低 | 将配置 Profile 移到 Character.cs，移除 Presenter |
| TraversalDriver 测试 | 中 | 需要实际场景中放置可攀爬障碍物验证中断/恢复链路 |
| UpperBody/Additive/Facial Arbiter | 低 | 当前为空，预留扩展 |

---

## 7. 关键设计决策理由

| 决策 | 理由 |
|---|---|
| Driver 模式而非 Layer 直接继承 | 允许多个子系统共享同一个 Animancer Layer，通过优先级动态切换 |
| 默认 Driver (LocomotionDriver) 不通过 Request 机制 | Locomotion 是常态，不需要每次 Request/Accept 的开销 |
| HeadLook/Footstep 作为常驻层不参与仲裁 | 它们始终叠加在全身上（通过 AvatarMask），不会被中断 |
| 数据通过 GameContext 快照而非直接引用 | LocomotionAgent 和 AnimationController 解耦，各自独立 Update |
| 仲裁器在 Channel 粒度而非全局 | 不同 Channel (FullBody vs UpperBody) 可以同时有不同的 Active Driver |
| TraversalDriver.BuildRequest() 直接读快照 | 避免 `Update()`/`BuildRequest()` 之间的一帧延迟问题 |
| LocomotionDriver 延迟初始化 | 避免 Motor 在构造时尚未创建（OnEnable 中创建）的时序问题 |
| Presenter 保留为根运动栈 | 避免过早引入 CharacterAnimationController ↔ Motor 的双向耦合 |
