# Tech 文档 — 模块总领

> 按架构层级组织：L1(GameManager) → L2(Service) → L3(Module) → L4(Component) → L5(子结构)
> 旧文档备份在 `../tech/`，不纳入日常查询。

## 文档目录

```
tech-v2/
├── README.md               # 本文件
├── L1-core/                # GameManager 层 — 根节点、上下文、Service 基类
├── L2-services/            # Service 层 — 场景、时间、状态、玩家、摄像机、事件
├── L3-character/           # Module — 角色系统 (actor/kinematic/locomotion/animation/stats/audio/input)
├── L3-input/               # Module — 输入系统
├── L3-ui/                  # Module — UI 系统
├── L3-stats/               # Module — Stat 数值框架
├── L3-audio/               # Module — 音频系统
├── L3-logging/             # Module — 日志系统
├── L3-pathfinding/         # Module — 寻路系统
├── L3-editor/              # Module — 编辑器工具
└── L3-utility/             # Module — 通用工具
```

---

## 架构层级

数据流方向：**L1→L2→L3→L4→L5，逐级传递，逐级返回。严禁跨级调用。**

```
L0  Unity Engine (不文档化)

L1  GameManager           Core 根 — GameContext / GameService / BaseService
│     │
│     └── 持有并管理所有 L2 Service
│
L2  GameService           服务层 — 每个 Service 继承 BaseService
│     ├── EventDispatcherService    # 事件总线 (L1→L2→L3 通信通道)
│     ├── SceneService              # 场景加载/卸载
│     ├── TimeService               # 时间控制
│     ├── GameStateService          # 全局状态机
│     ├── PlayerService             # 玩家 Spawn/Despawn
│     └── CameraService             # 摄像机 + 鼠标地面坐标
│     │
│     └── 使用 L3 模块完成具体功能
│
L3  GameModule            独立模块 — 被 Service 使用，不依赖特定 Service
│     ├── Character       角色系统 (PlayerService 用，未来 AIService 也用)
│     ├── Input           输入抽象
│     ├── UI              UI 面板/组件
│     ├── Stats           Stat 数值框架
│     ├── Audio           音频播放
│     ├── Logging         日志管道
│     ├── Pathfinding     A* 寻路
│     ├── Editor          编辑器工具
│     └── Utility         通用工具
│     │
│     └── 内部按 L4 组件 / L5 子结构组织
│
L4  GameComponent         模块内子组件
│     Character: actor / kinematic / locomotion / animation / stats / audio / input
│     Input:     service / actions / structs
│     UI:        service / core / components / config / hud / main-menu
│     Stats:     definition / tree / instance / modifier / interfaces / editor
│     ...
│
L5  子结构                复杂逻辑的进一步拆分
      Character: animation/states/ / animation/drivers/ / stats/rules/ / kinematic/structs/
```

## 代码索引（按目标层级）

### L1 — GameManager

```
Core/
├── GameContext.cs               # 全局上下文 — Service Registry + Snapshot Store
├── GameService.cs               # 服务根节点 — Bootstrap 五步启动 + TeardownSession
└── BaseService.cs               # Service 基类 — 四阶段生命周期 + PublishState
```

### L2 — Service

```
Core/
├── EventDispatcherService.cs    # 事件总线 — Subscribe/Publish/Unsubscribe + MetaStruct
├── SceneService.cs              # 场景管理 — Core+Additive Loading
├── TimeService.cs               # 时间控制 — Gameplay/UI 时间线分离
├── GameStateService.cs          # 状态机 — MainMenu⇄Playing⇄Paused
├── PlayerService.cs             # 玩家管理 — Spawn/Despawn/位置追踪
├── CameraService.cs             # 摄像机 — Cinemachine + 鼠标地面坐标
├── GameProfile.cs               # 项目级 SO 配置入口
├── IGameplaySessionHandler.cs   # 会话接口 — OnGameplaySessionEnd
├── Scene/
│   ├── SLoadSceneRequest.cs
│   ├── SSceneLoadStart.cs
│   ├── SSceneLoadComplete.cs
│   ├── SSceneTransition.cs
│   └── SUnloadSceneRequest.cs
├── Structs/
│   ├── MetaStruct.cs
│   └── Contexts/
│       ├── SCameraSnapshot.cs
│       ├── SCharacter.cs
│       ├── SGameState.cs
│       ├── SPlayer.cs
│       └── SPlayerSpawnedEvent.cs
└── Time/
    ├── TimeService.cs
    └── SIActionWorldSpeed.cs
```

### L3 — Character

```
Character/
├── Components/                              # L4: Actor
│   ├── CharacterActor.cs                    # 组合根 — 每帧 Evaluate() 串联全链路
│   ├── CharacterActor.Debug.cs              # Gizmo 调试
│   ├── CharacterRig.cs                      # 物理实体统一写入入口
│   └── CharacterFrameContext.cs             # 帧内数据载体
├── Config/                                  # L4: Config
│   └── CharacterProfile.cs                  # 角色参数 SO
├── Enums/
│   └── LocomotionEnums.cs                   # Phase / Gait / Posture 枚举
├── Kinematic/                               # L4: Kinematic
│   ├── CharacterKinematic.cs                # 运动学入口
│   ├── CharacterGroundDetection.cs          # SphereCast 地面检测
│   ├── CharacterHeadLook.cs                 # 头部注视 yaw/pitch
│   ├── CharacterObstacleDetection.cs        # 前方障碍检测
│   ├── SCharacterKinematic.cs               # 运动学输出 struct
│   ├── SForwardObstacleDetection.cs         # 障碍检测结果 struct
│   └── SGroundContact.cs                    # 地面接触 struct
├── Locomotion/                              # L4: Locomotion
│   ├── GroundLocomotion.cs                  # ILocomotionSimulator 实现
│   ├── ILocomotionSimulator.cs              # 仿真接口
│   ├── Motor.cs                             # 速度/转角计算
│   ├── Stance.cs                            # Phase/Gait/Posture 判定
│   ├── SCharacterMotor.cs                   # 运动输出 struct
│   ├── SCharacterDiscrete.cs                # 离散状态 struct
│   └── Config/LocomotionProfile.cs          # 移动参数 SO
├── Animation/                               # L4: Animation
│   ├── Components/AnimationBrain.cs         # 动画总控
│   ├── DriverArbiter.cs                     # Driver 仲裁
│   ├── Config/
│   │   ├── AnimationAliasProfile.cs
│   │   ├── LocomotionAnimationProfile.cs
│   │   └── LocomotionModeProfile.cs
│   ├── Drivers/
│   │   ├── ICharacterAnimationDriver.cs     # Driver 接口
│   │   ├── BaseCharacterAnimationDriver.cs  # Driver 基类
│   │   ├── Locomotion/LocomotionDriver.cs   # 移动驱动
│   │   ├── Locomotion/BaseLayer.cs          # FSM + 程序化转身
│   │   ├── Locomotion/BaseStateKey.cs       # FSM 状态枚举
│   │   ├── Locomotion/LocomotionLayerFsmState.cs
│   │   ├── Locomotion/States/BaseIdleState.cs
│   │   ├── Locomotion/States/BaseMovingState.cs
│   │   ├── Locomotion/States/BaseIdleToMovingState.cs
│   │   ├── Locomotion/States/BaseTurnInPlaceState.cs
│   │   ├── Locomotion/States/BaseTurnInMovingState.cs
│   │   ├── Locomotion/States/BaseAirLoopState.cs
│   │   ├── Locomotion/States/BaseAirLandState.cs
│   │   └── Traversal/TraversalDriver.cs
│   ├── Layers/                              # 空 — HeadLook/Foot 内联到 AnimationBrain
│   └── Requests/
│       ├── AnimationRequest.cs
│       ├── OnCompleteBehavior.cs
│       └── OnInterruptedBehavior.cs
├── Stats/                                   # L4: Stats
│   ├── CharacterStats.cs
│   └── Rules/
│       ├── CharacterStatRule.cs
│       ├── DamageRule.cs
│       ├── BatchDamageRule.cs
│       ├── DepleteChainRule.cs
│       ├── HungerDepleteRule.cs
│       ├── PassiveGainRule.cs
│       ├── SprintStaminaRule.cs
│       └── ToggleModifierRule.cs
├── Audio/                                   # L4: Audio
│   ├── CharacterAudio.cs
│   └── Config/
│       ├── CharacterAudioConfigSO.cs
│       └── FootstepSetSO.cs
└── Input/                                   # L4: Input
    ├── CharacterEventReceiver.cs            # EventDispatcher 输入桥接
    └── SCharacterInputActions.cs            # 输入聚合 struct
```

### L3 — Input / UI / Stats / Audio / Logging / Pathfinding / Editor / Utility

> 待 L4/L5 结构展开后补充完整代码树。

---

## 关联

- **设计文档** `../design/` — 系统设计意图 (WHY)
- **开发约定** `../tech/conventions/` — 命名规范、资产规范
- **会话归档** `../sessions/` — 历史决策记录
- **开发计划** `../plans/` — 长/短期路线图
