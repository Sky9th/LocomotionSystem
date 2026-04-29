# Character 模块深度分析

> 分析日期：2025-04-27
> 总文件数：73+ .cs 文件

---

## 1. 当前目录结构

```
Character/
├── Components/
│   └── CharacterActor.cs                    [MonoBehaviour] 组合根
├── Animation/                              ─── 动画子系统
│   ├── Components/
│   │   └── CharacterAnimationController.cs [MonoBehaviour] 动画入口
│   ├── DriverArbiter.cs                    [pure C#] 仲裁逻辑
│   ├── Drivers/
│   │   ├── ICharacterAnimationDriver.cs    [interface]
│   │   ├── LocomotionDriver.cs             [pure C#] Locomotion 动画
│   │   ├── TraversalDriver.cs              [pure C#] Traversal 动画
│   │   └── FullBodyCharacterAnimationDriver.cs [pure C#] 存根
│   └── Requests/
│       ├── CharacterAnimationRequest.cs    [class]
│       ├── EAnimationInterruption.cs       [enum]
│       ├── ECharacterAnimationChannel.cs   [enum]
│       ├── ECharacterAnimationSource.cs    [enum]
│       ├── ECharacterAnimationRequestMode.cs  [enum]
│       ├── ECharacterAnimationPlaybackState.cs [enum]
│       ├── ICharacterAnimationSource.cs    [interface]
│       └── ICharacterAnimationDefaultSource.cs [interface]
├── Input/
│   ├── CharacterInputModule.cs             [pure C#] 输入聚合
│   └── SCharacterInputActions.cs           [struct, Game.Character.Input]
├── Kinematic/
│   ├── CharacterKinematic.cs               [pure C#, Game.Character.Kinematic]
│   ├── SCharacterKinematic.cs              [struct, global]
│   ├── SGroundContact.cs                   [struct, global]
│   ├── SForwardObstacleDetection.cs        [struct, global]
│   ├── CharacterGroundDetection.cs         [static, Game.Character.Probes]
│   ├── CharacterObstacleDetection.cs       [static, Game.Character.Probes]
│   └── CharacterHeadLook.cs                [static, Game.Character.Probes]
└── Locomotion/                             ─── 运动模拟子系统
    ├── Agent/
    │   ├── CharacterLocomotion.cs           [MonoBehaviour]
    │   ├── CharacterLocomotion.Debug.cs     [partial MonoBehaviour]
    │   └── SLocomotion.cs                   [struct, global]
    ├── Motor/
    │   ├── LocomotionMotor.cs               [public class]
    │   └── SLocomotionMotor.cs              [struct, global]
    ├── Computation/
    │   ├── LocomotionKinematics.cs           [static, Game.Locomotion.Computation]
    │   └── LocomotionFootPlacement.cs        [static, Game.Locomotion.Computation]
    ├── Discrete/
    │   ├── Core/
    │   │   ├── LocomotionCoordinatorBase.cs
    │   │   ├── LocomotionCoordinatorHuman.cs
    │   │   ├── LocomotionGraph.cs
    │   │   ├── LocomotionTraversalGraph.cs
    │   │   └── LocomotionTurningGraph.cs
    │   ├── Aspects/
    │   │   ├── PhaseAspect.cs
    │   │   ├── GaitAspect.cs
    │   │   └── PostureAspect.cs
    │   ├── Interface/
    │   │   ├── ILocomotionCoordinator.cs
    │   │   └── ILocomotionAspect.cs
    │   ├── Structs/
    │   │   ├── SLocomotionDiscrete.cs
    │   │   └── SLocomotionTraversal.cs
    │   └── Enums/ (6 enums: Phase, Gait, Posture, Condition, TraversalStage, TraversalType)
    ├── Config/
    │   └── LocomotionProfile.cs             [ScriptableObject]
    └── Animation/                           ── Locomotion 专属动画层
        ├── Core/
        │   ├── LocomotionAnimationController.cs
        │   ├── LocomotionAnimationContext.cs
        │   └── ILocomotionAnimationLayer.cs
        ├── Config/
        │   ├── LocomotionAliasProfile.cs    [ScriptableObject]
        │   ├── LocomotionAnimationProfile.cs [ScriptableObject]
        │   └── LocomotionModeProfile.cs     [ScriptableObject]
        ├── Layers/
        │   ├── HeadLookLayer.cs
        │   ├── FootLayer.cs
        │   └── Base/
        │       ├── BaseLayer.cs             [7-state FSM]
        │       ├── States/ (7 state files)
        │       ├── Conditions/ (8 condition files)
        │       ├── Appliers/TurnAngleStepRotationApplier.cs
        │       └── Core/LocomotionLayerFsmState.cs
        ├── Conditions/
        │   ├── ICheck.cs
        │   ├── AndCondition.cs / OrCondition.cs / NotCondition.cs
        │   └── CheckExtensions.cs
        └── Structs/
            ├── SLocomotionAnimation.cs
            └── SLocomotionAnimationLayerSnapshot.cs
```

---

## 2. 组件清单（按类型）

### MonoBehaviour (4 个)

| 类 | 文件 | GameObject 位置 |
|---|---|---|
| `CharacterActor` | `Components/CharacterActor.cs` | Player 根节点 |
| `CharacterAnimationController` | `Animation/Components/CharacterAnimationController.cs` | Model 子节点 |
| `CharacterLocomotion` | `Locomotion/Agent/CharacterLocomotion.cs` | Model 子节点 |
| ~~`LocomotionAnimancerPresenter`~~ | 已删除 | — |

### ScriptableObject (4 个)

| 类 | 用途 |
|---|---|
| `LocomotionProfile` | 运动参数 (速度/地面/障碍物/转向) |
| `LocomotionAliasProfile` | 动画别名映射 (StringAsset 引用) |
| `LocomotionAnimationProfile` | 动画参数 (头部平滑/转向速度/落地阈值) |
| `LocomotionModeProfile` | 特定模式下的转向速度 |

### Pure C# 类 (运行时构造)

| 类 | 创建位置 | 职责 |
|---|---|---|
| `CharacterInputModule` | `CharacterActor.Awake()` | 输入聚合 + 事件订阅 |
| `CharacterKinematic` | `CharacterActor.Awake()` | 地面/障碍物/朝向计算 |
| `LocomotionMotor` | `CharacterLocomotion.OnEnable()` | 速度计算 + 根运动 |
| `LocomotionCoordinatorHuman` | `CharacterLocomotion.OnEnable()` | 离散运动状态机 |
| `LocomotionDriver` | `CharacterActor.Start()` | Locomotion 动画 Driver |
| `TraversalDriver` | `CharacterActor.Start()` | Traversal 动画 Driver |
| `DriverArbiter` (×4) | `CharacterAnimationController.Awake()` | 层仲裁器 |
| `LocomotionAnimationController` | `LocomotionDriver.EnsureInitialized()` | 动画层编排 |

---

## 3. 数据流分析

### 3.1 当前推送至 GameContext 的快照

| 快照类型 | 推送方 | 时机 | 读取方 |
|---|---|---|---|
| `SCharacterKinematic` | `CharacterActor.Update()` | 每帧 | `CameraManager`, `CharacterLocomotion` |
| `SLocomotion` | `CharacterLocomotion.PushSnapshot()` | 每帧 | `LocomotionDriver`, `DebugOverlay` |
| `SCameraContext` | `CameraManager.Update()` + `LateUpdate()` | 每帧 | `LocomotionInputModule` (via Event) |

### 3.2 Character 内部数据传递

```
CharacterActor.Update()
  ├── inputModule.ReadActions() → SCharacterInputActions
  ├── characterLocomotion.SetInput(↓)           ← 向下推送
  ├── characterKinematic.Evaluate()
  └── GameContext.UpdateSnapshot(SCharacterKinematic)

CharacterLocomotion.Update()
  ├── GameContext.TryGetSnapshot<SCharacterKinematic>() ← 从 Context 拉取
  ├── motor.Evaluate(in kinematic)
  ├── coordinator.Evaluate(in kinematic, in motorOutput)
  └── PushSnapshot(SLocomotion) → GameContext + EventDispatcher

CameraManager.Update() [-400]
  └── GameContext.TryGetSnapshot<SCharacterKinematic>() ← 拉取位置

LocomotionDriver.Update() (via CharacterAnimationController)
  └── GameContext.TryGetSnapshot<SLocomotion>() ← 拉取动画数据
```

### 3.3 ⚠ 问题

1. **双重数据路径**: `SCharacterKinematic` 同时通过 `GameContext` (pull) 和局部调用 (Character → Locomotion) 传递。GameContext 是全局中心，用于跨系统通信；Character 内部数据应通过局部 Context 向下传递。

2. **`SCharacterInputActions` 不推 GameContext**: 当前通过 `SetInput()` 向下传递（正确），但 CharacterLocomotion 仍通过 `GameContext` 读取 `SCharacterKinematic`，应统一为顶部推送。

3. **Locomotion 子系统内部仍用 GameContext**: `LocomotionDriver` 读取 `SLocomotion` 通过 GameContext。应通过局部调用传递。

---

## 4. 跨模块依赖分析

### 4.1 CharacterActor 的直接依赖

| 依赖 | 模块 | 是否合理 |
|---|---|---|
| `CharacterAnimationController` | Animation | ✓ 父模块调用子模块 |
| `CharacterInputModule` | Input | ✓ |
| `CharacterKinematic` | Kinematic | ✓ |
| `CharacterLocomotion` | Locomotion | ✓ |
| `LocomotionDriver` | Animation.Drivers | ⚠ 应委托给 Animation |
| `TraversalDriver` | Animation.Drivers | ⚠ 应委托给 Animation |
| `LocomotionMotor` | Locomotion.Motor | ⚠ 通过 CharacterLocomotion 间接访问 |
| `LocomotionProfile` / `LocomotionAliasProfile` / `LocomotionAnimationProfile` | Config | ⚠ 配置应统一注入 |

### 4.2 CharacterLocomotion 的依赖

| 依赖 | 模块 | 是否合理 |
|---|---|---|
| `LocomotionMotor` | 自身子模块 | ✓ |
| `ILocomotionCoordinator` | 自身子模块 | ✓ |
| `GameContext` | 全局 | ⚠ 应通过 CharacterActor 获取数据 |
| `EventDispatcher` | 全局 | ⚠ 应由 CharacterActor 管理发布 |
| `SCharacterKinematic` (from GameContext) | Kinematic | ⚠ 应通过参数传递 |

### 4.3 问题：CharacterActor 跨级调用

```
CharacterActor.Start()
  → new LocomotionDriver(motor, ...)    ← 接触 Animation 的子类
  → new TraversalDriver(alias)          ← 接触 Animation 的子类
  → characterAnimation.RegisterDriver() ← 通过 Animation 入口（正确）
```

**问题**: CharacterActor 直接实例化 `LocomotionDriver` 和 `TraversalDriver`，破坏了逐级调用原则。应委托给 `CharacterAnimationController` 或其工厂。

---

## 5. 命名问题

| 当前名称 | 问题 | 建议 |
|---|---|---|
| `CharacterLocomotion` | 在 `Game.Locomotion.Agent` 命名空间下，但类名含 "Character" | 命名空间改为 `Game.Character.Locomotion` 或类名改 `LocomotionAgent` |
| `LocomotionDriver` | 在 `Animation/Drivers/` 下，是 Animation 子系统的一部分 | 保持，命名合理 |
| `TraversalDriver` | 同上 | 保持 |
| `LocomotionAnimationController` | 在 `Locomotion/Animation/Core/` 下，是 Locomotion 内部动画实现 | 保持 |
| `BaseLayer` / `HeadLookLayer` / `FootLayer` | Locomotion 专属动画层 | 保持 |
| `LocomotionMotor` | 速度计算器，"Motor" 合理但可更好 | `LocomotionVelocity` 或保持 |
| `LocomotionProfile` | 配置 | `LocomotionConfig` 更准确 |
| `LocomotionAliasProfile` | 动画别名 | 已纳入 Animation/Config，命名合理 |
| `CharacterKinematic` / `SCharacterKinematic` | Kinematic 含义偏物理运动 | 保持，后续扩展属性用独立模块 |
| `LocomotionCoordinatorHuman` | 人类角色的运动协调器 | 保持 |

---

## 6. 目录与命名空间不一致

| 文件位置 | 当前 namespace | 应 |
|---|---|---|
| `Character/Animation/Drivers/*` | `Game.Character.Animation.Drivers` | ✓ |
| `Character/Animation/DriverArbiter.cs` | `Game.Character.Animation` | ✓ |
| `Character/Input/*` | `Game.Character.Input` | ✓ |
| `Character/Kinematic/*` | `Game.Character.Kinematic` / `Game.Character.Probes` | ⚠ 两个命名空间混在同一目录 |
| `Character/Locomotion/Agent/*` | `Game.Locomotion.Agent` | ⚠ 应在 `Game.Character.Locomotion` |
| `Character/Locomotion/*` | `Game.Locomotion.*` | ⚠ 应在 `Game.Character.Locomotion.*` |
| `Character/Locomotion/Animation/*` | `Game.Locomotion.Animation.*` | ⚠ 应在 `Game.Character.Locomotion.Animation` |
| `Character/Locomotion/Discrete/*` | `Game.Locomotion.Discrete.*` | ⚠ |
| `Character/Locomotion/Computation/*` | `Game.Locomotion.Computation` | ⚠ |

根本问题：**所有 Locomotion 文件的命名空间以 `Game.Locomotion` 开头，但文件在 `Character/Locomotion/` 下**。既然 Locomotion 是 Character 的子模块，命名空间应为 `Game.Character.Locomotion`。

---

## 7. 仿真 Locomotion 与动画 Locomotion 的区分

"Locomotion" 这个词在代码中同时指两件事，是当前目录混乱的根本原因：

### 仿真 Locomotion (What)
**决定角色应该处于什么运动状态**

| 模块 | 位置 | 职责 |
|---|---|---|
| `LocomotionMotor` | `Character/Locomotion/Motor/` | 速度计算、平滑、朝向、转向角 |
| `LocomotionCoordinator` | `Character/Locomotion/Coordination/` | Phase/Gait/Posture 状态机 + Traversal + Turning |
| `CharacterLocomotion` | `Character/Locomotion/Agent/` | 协调 Motor + Coordinator，每个角色一个实例 |

**输入**: `SCharacterInputActions` + `SCharacterKinematic`  
**输出**: `SLocomotionMotor` + `SLocomotionDiscrete` + `SLocomotionTraversal`  
**运行**: 始终运行（CharacterActor 每帧驱动）

### 动画 Locomotion (How)
**决定角色当前状态应该播放什么动画**

| 模块 | 位置 | 职责 |
|---|---|---|
| `LocomotionDriver` | `Character/Animation/Drivers/` | 实现 `ICharacterAnimationDriver`，驱动 FullBody 层 |
| `BaseLayer` + FSM | `Character/Animation/Layers/Locomotion/` | 7 状态 FSM，根据仿真输出选择动画 |
| `HeadLookLayer` | `Character/Animation/Layers/` | 头部朝向混合 |
| `FootLayer` | `Character/Animation/Layers/` | 脚步动画（存根） |

**输入**: `SLocomotion`（仿真输出）  
**输出**: 驱动 Animancer Layer 播放对应 clip  
**运行**: 通过 `CharacterAnimationController` → `DriverArbiter` → Driver.Update() 驱动

### 区分示例：攀爬

```
仿真: 检测障碍物 → Traversal=Requested → Committed → Phase 锁定 Idle
  → 角色运动停止（位置不前进）

动画: Arbiter 检测 Requested → 中断 LocomotionDriver → 播放 ClimbUp
  → 视觉上角色在爬（位置可能由根运动推进）
```

---

## 8. 目标架构

### 8.1 目录结构

```
Character/
├── Components/                            ← 仅 MonoBehaviour
│   └── CharacterActor.cs
│
├── Animation/                             ← 动画子系统
│   ├── Components/
│   │   └── CharacterAnimationController.cs
│   ├── DriverArbiter.cs
│   ├── Drivers/
│   │   ├── ICharacterAnimationDriver.cs
│   │   ├── LocomotionDriver.cs            ← 动画 Locomotion
│   │   ├── TraversalDriver.cs
│   │   └── FullBodyCharacterAnimationDriver.cs (存根)
│   ├── Layers/
│   │   ├── HeadLookLayer.cs
│   │   ├── FootLayer.cs
│   │   └── Locomotion/                    ← Locomotion 专属动画层
│   │       ├── BaseLayer.cs               ← 7 状态 FSM
│   │       ├── States/                    ← Idle, Moving, TurnInPlace...
│   │       ├── Conditions/                ← CanEnter*, CanExit*
│   │       └── Appliers/                  ← TurnAngleStepRotation
│   ├── Requests/
│   │   └── (enums, DTOs, interfaces)
│   └── Config/
│       ├── LocomotionAliasProfile.cs
│       ├── LocomotionAnimationProfile.cs
│       └── LocomotionModeProfile.cs
│
├── Input/                                 ← 输入子系统
│   ├── CharacterInputModule.cs
│   └── SCharacterInputActions.cs
│
├── Kinematic/                             ← 物理运动数据
│   ├── CharacterKinematic.cs
│   ├── SCharacterKinematic.cs
│   ├── SGroundContact.cs
│   ├── SForwardObstacleDetection.cs
│   └── Probes/
│       ├── CharacterGroundDetection.cs
│       ├── CharacterObstacleDetection.cs
│       └── CharacterHeadLook.cs
│
└── Locomotion/                            ← 仿真 Locomotion 子系统
    ├── Agent/
    │   ├── CharacterLocomotion.cs
    │   └── SCharacterSnapshot.cs          ← 统一对外快照 (原 SLocomotion)
    ├── Motor/
    │   ├── LocomotionMotor.cs
    │   └── SLocomotionMotor.cs
    ├── Coordination/
    │   ├── LocomotionCoordinatorBase.cs
    │   ├── LocomotionCoordinatorHuman.cs
    │   ├── LocomotionGraph.cs
    │   ├── LocomotionTraversalGraph.cs
    │   ├── LocomotionTurningGraph.cs
    │   ├── Aspects/
    │   ├── Interfaces/
    │   ├── Structs/
    │   └── Enums/
    ├── Computation/
    │   ├── LocomotionKinematics.cs
    │   └── LocomotionFootPlacement.cs
    └── Config/
        └── LocomotionProfile.cs
```

### 8.2 区分：仿真 vs 动画

| | 仿真 Locomotion | 动画 Locomotion |
|---|---|---|
| 目录 | `Character/Locomotion/` | `Character/Animation/Drivers/` + `Layers/Locomotion/` |
| 入口 | `CharacterLocomotion` | `LocomotionDriver` |
| 做什么 | 计算速度、状态、转向角 | 选择动画 clip、驱动 FSM |
| 可暂停 | 攀爬时锁定 Phase=Idle | 攀爬时被 Arbiter 中断 |
| 对外输出 | `SCharacterSnapshot` | 无 (驱动 Animancer) |

### 8.3 命名空间规则

| 目录 | 命名空间 |
|---|---|
| `Character/Components/` | `Game.Character.Components` |
| `Character/Animation/*` | `Game.Character.Animation.*` |
| `Character/Input/` | `Game.Character.Input` |
| `Character/Kinematic/` | `Game.Character.Kinematic` |
| `Character/Locomotion/*` | `Game.Character.Locomotion.*` |

### 8.4 对外快照——有且仅有一个

**原则**: Character 模块对外暴露唯一快照，所有子系统数据统一打包，不分散推送。

**`SCharacterSnapshot`** (当前 `SLocomotion` 重命名):

```csharp
public struct SCharacterSnapshot
{
    public SCharacterKinematic Kinematic { get; }     // 位置/朝向/地面/障碍物
    public SLocomotionMotor Motor { get; }             // 速度/Heading/TurnAngle
    public SLocomotionDiscrete DiscreteState { get; }  // Phase/Gait/Posture
    public SLocomotionTraversal Traversal { get; }     // 穿越状态
}
```

| 旧 | 新 |
|---|---|
| `SCharacterKinematic` 单独推 GameContext | 仅作为 `SCharacterSnapshot.Kinematic` |
| `SLocomotion` 推 GameContext + Dispatcher | 重命名为 `SCharacterSnapshot`，CharacterActor 统一推送 |
| CameraManager 读 `SCharacterKinematic` | CameraManager 读 `SCharacterSnapshot.Kinematic` |

### 8.5 逐级调用原则验证

```
CharacterActor
  ├── new CharacterInputModule()          ✓ 直接子模块 (Input)
  ├── new CharacterKinematic()            ✓ 直接子模块 (Kinematic)
  ├── locomotion.SetInput()               ✓ 直接子模块 (Locomotion)
  ├── locomotion.Simulate(kinematic, input)  ✓ 参数化调用
  ├── characterAnimation.RegisterDriver() ✓ 直接子模块 (Animation)
  │
  ├── new LocomotionDriver()              ❌ 跨级 → 改为 Locomotion.Initialize 内创建
  ├── new TraversalDriver()               ❌ 跨级 → 改为 AnimationController 创建
  └── locomotion.Motor                    ❌ 访问内部 → 消除

修正后:
  CharacterActor.Start()
    → locomotion.Initialize(characterAnimation)          ← Locomotion 自己注册 Driver
    → characterAnimation.CreateTraversalDriver(alias)    ← Animation 内部创建
```

### 8.6 数据流（修正后）

```
CharacterActor.Update()
  │
  ├── Step 1: inputModule.ReadActions(out ctx.Input)
  ├── Step 2: GameContext.TryGetSnapshot<SCameraContext>() → viewForward
  ├── Step 3: ctx.Kinematic = characterKinematic.Evaluate(profile, viewForward, dt)
  ├── Step 4: locomotion.Simulate(ref ctx, profile, viewForward, dt)
  │            ├── ctx.Motor = motor.Evaluate(...)
  │            ├── ctx.Discrete = coordinator.Evaluate(...)
  │            └── ctx.Traversal = coordinator.CurrentTraversal
  │
  └── Step 5: snapshot = new SCharacterSnapshot(ctx.Kinematic, ctx.Motor, ctx.Discrete, ctx.Traversal)
       ├── GameContext.UpdateSnapshot(snapshot)              ← 唯一对外出口
       └── Dispatcher.Publish(snapshot)                      ← 外部 Event

CharacterLocomotion.Simulate(ref ctx, profile, viewForward, dt)
  └── 纯函数, 不访问 GameContext, 不访问 Dispatcher

CharacterAnimationController.Update() (同帧, 读 GameContext)
  └── fullBodyArbiter.Update()
       ├── TraversalDriver.BuildRequest() → 有新请求?
       └── LocomotionDriver.Update(snapshot, dt)             ← 参数传入
            └── BaseLayer FSM.Tick(snapshot)
```

| 变化 | 旧 | 新 |
|---|---|---|
| Locomotion 获取 kinematic/input | `GameContext.TryGetSnapshot()` | `ref ctx` 参数 |
| Locomotion 发布快照 | `PushSnapshot()` 直接操作 Context | 无副作用, 填充 ctx 返回 |
| Driver 获取 snapshot | `GameContext.TryGetSnapshot()` | 参数 `Update(snapshot, dt)` [需改接口] |
| Driver 创建 | CharacterActor `new LocomotionDriver()` | `Locomotion.Initialize()` 自注册 |
| 对外快照数 | 2 个 (`SCharacterKinematic` + `SLocomotion`) | 1 个 (`SCharacterSnapshot`) |

### 8.7 实施步骤

| Phase | 内容 | 复杂度 |
|---|---|---|
| A | 命名空间迁移: `Game.Locomotion.*` → `Game.Character.Locomotion.*` | 中 |
| B | 动画层目录重组: Layers 从 Locomotion 移到 Animation | 中 |
| C | Driver 创建权责: Locomotion.Initialize(controller) 自注册 | 低 |
| D | CharacterLocomotion 参数化: Simulate 改为纯参数，返回数据 | 低 |
| E | 统一快照: `SLocomotion` → `SCharacterSnapshot`，CharacterActor 唯一出口 | 低 |
| F | Kinematic/Probes 子目录整理 | 低 |

### 8.8 Kinematic 模块职责边界

| 属于 Kinematic | 不属于 Kinematic |
|---|---|
| Position, BodyForward, LookDirection | HP, 耐力, 饥饿 |
| GroundContact (着地/距离/斜面) | Buff/Debuff 状态 |
| ForwardObstacleDetection | 装备/背包数据 |
| 物理运动数据 | 生命统计数据 → 另起 `Vitals/` |
