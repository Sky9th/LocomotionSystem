# Tech 文档

> 按 L1→L5 架构层级组织。数据流：L1→L2→L3→L4→L5，逐级传递返回，严禁跨级调用。

### 命名规则

| 位置 | 命名格式 | 示例 |
|------|---------|------|
| **代码目录** (`Assets/Scripts/`) | `L{N}_{PascalCase}`，占位容器不带 L | `L1_Core/`, `L2_Audio/`, `Services/`, `Modules/` |
| **文档目录** (`.agent/tech/`) | `L{N}-{kebab-case}`，纯文档不随 Unity 风格 | `L1-core/`, `L2-audio/`, `L4-animation/`, `L5-drivers/` |

> **为什么不同？** 代码目录跟 Unity 项目惯例（PascalCase），文档目录保持 kebab-case 便于阅读。两者表示同一架构层级，仅命名风格不同。

### L 层级定义

| 层级 | 定义 | 判断标准 | 示例 |
|------|------|---------|------|
| **L1** | 根管理层 | 持有所有 Service，无业务逻辑 | `L1_Core/` |
| **L2** | 系统服务 | 继承 BaseService，协调 L1↔L3 | `L2_Audio/`, `L2_Input/`, `L2_UI/` |
| **L3** | 领域模块 | 独立领域，不隶属单一 L2，可被多个 Service 共用 | `L3_Character/`, `L3_Stats/` |
| **L4** | 领域子系统 | L3 内部的**不同领域子系统**，承担独立功能 | `L4_Animation/`, `L4_Kinematic/`, `L4_Locomotion/`, `L4_Stats/`, `L4_Audio/` |
| **L5** | 子系统的子系统 | L4 内部的**附属子系统**，承担其下一级独立功能 | `L5_Drivers/`, `L5_Locomotion/` |

> **不是 L4 的**：模块自身组件代码（`Actor/`、`Actions/`、`Core/`、`Components/`、`HUD/`）——这是代码分组，不是子系统。
> **不是 L5 的**：按文件类型分组（`Config/`、`Structs/`、`Data/`、`Rules/`、`States/`、`Requests/`）——这是代码结构，不是子系统。

```
tech/
├── README.md                           # 本文件
│
├── L1-core/                            # Layer 1: GameManager 根
│   ├── README.md
│   ├── game-context.md                 # GameContext — Service Registry + Snapshot Store
│   ├── game-service.md                 # GameService — Bootstrap 五步启动
│   ├── base-service.md                 # BaseService — 四阶段生命周期
│   ├── structs.md                      # MetaStruct + Core Context Structs
│   └── events/                         # SO Event Channel 基础设施
│       ├── README.md
│       ├── event-channel-base.md       # EventChannelBase — 抽象根
│       ├── game-event.md               # GameEvent<T> — 通用事件通道
│       ├── event-channels.md           # EventChannels — 引用集中 + 驱动 IEventListener
│       └── i-event-listener.md         # IEventListener — 订阅约定接口
│
├── L2-services/                        # 占位容器: 所有 L2 Service
│   ├── README.md
│   │
│   ├── L2-event-dispatcher/            # L2: 简单 Service ×6
│   │   └── event-dispatcher.md
│   ├── L2-scene-service/
│   │   └── scene-service.md
│   ├── L2-time-service/
│   │   └── time-service.md
│   ├── L2-game-state-service/
│   │   └── game-state-service.md
│   ├── L2-player-service/
│   │   └── player-service.md
│   ├── L2-camera-service/
│   │   └── camera-service.md
│   │
│   ├── L2-audio/                       # L2 Service (自身代码: Data/ Structs/)
│   │   ├── README.md
│   │   ├── audio-manager.md
│   │   ├── Data/                       # AudioChannel, AudioSetSO
│   │   └── Structs/                    # AudioRequest, AudioResponse
│   │
│   ├── L2-input/                       # L2 Service (自身代码: Events/ Structs/)
│   │   ├── README.md
│   │   ├── input-service.md
│   │   ├── events/                     # SO Event Channel 输入事件
│   │   │   ├── README.md
│   │   │   ├── i-input-event.md       # IInputEvent — 生命周期接口
│   │   │   ├── input-event.md         # InputEvent<T> — 泛型输入通道
│   │   │   └── button-input-events.md # 具体按钮事件 ×6
│   │   └── Structs/
│   │       ├── SIActionUIEscape.cs
│   │       └── Control/               # SIActionMove, SIActionLook + Button/
│   │
│   ├── L2-ui/                          # L2 Service (自身代码: Core/ Components/ HUD/ Config/ MainMenu/)
│   │   ├── README.md
│   │   ├── ui-service.md
│   │   ├── Core/                       # UIScreen, UIOverlay, UIScreenId 等
│   │   ├── Components/                 # UIButton, UILabel, UIPanel, UIStatBar
│   │   ├── Config/                     # UIPanelConfigSO, UIThemeSO
│   │   ├── HUD/                        # VitalsOverlay, StatusOverlay, LoadingOverlay
│   │   └── MainMenu/                   # MainMenuScreen, PauseMenuScreen
│   │
│   └── L2-modules/                     # 占位容器: L3 独立模块
│       ├── L3-character/               # L3: 角色系统
│       │   ├── README.md
│       │   ├── Actor/                  # CharacterActor, CharacterRig, CharacterFrameContext [自身组件]
│       │   ├── Config/                 # CharacterProfile, LocomotionEnums [代码结构]
│       │   ├── Input/                  # CharacterEventReceiver, SCharacterInputActions [代码结构]
│       │   │
│       │   ├── L4-animation/           # L4: 动画子系统
│       │   │   ├── AnimationBrain.cs
│       │   │   ├── DriverArbiter.cs
│       │   │   ├── Config/             # AnimationAliasProfile, LocomotionAnimationProfile, LocomotionModeProfile
│       │   │   ├── Requests/           # AnimationRequest, OnCompleteBehavior, OnInterruptedBehavior
│       │   │   └── L5-drivers/         # L5: 驱动子系统
│       │   │       ├── i-character-animation-driver.md
│       │   │       ├── base-character-animation-driver.md
│       │   │       ├── TraversalDriver.cs
│       │   │       └── L5-locomotion/  # L5: 移动驱动子系统
│       │   │           ├── LocomotionDriver, BaseLayer, BaseStateKey
│       │   │           └── States/     # 7 个 FSM state [代码结构]
│       │   │
│       │   ├── L4-audio/               # L4: 音效子系统
│       │   │   ├── CharacterAudio.cs
│       │   │   └── Config/             # CharacterAudioConfigSO, FootstepSetSO
│       │   ├── L4-kinematic/           # L4: 运动学子系统
│       │   │   ├── CharacterKinematic, GroundDetection, HeadLook, ObstacleDetection
│       │   │   └── Structs/            # SCharacterKinematic, SGroundContact, SForwardObstacleDetection
│       │   ├── L4-locomotion/          # L4: 移动控制子系统
│       │   │   ├── ILocomotionSimulator, GroundLocomotion, Motor, Stance
│       │   │   ├── LocomotionProfile.cs
│       │   │   └── Structs/            # SCharacterMotor, SCharacterDiscrete
│       │   └── L4-stats/               # L4: 数值子系统
│       │       ├── CharacterStats.cs
│       │       └── Rules/              # 8 个 stat rule
│       │   └── L4-combat/              # L4: 战斗技能子系统
│       │       ├── CombatComponent.cs   # 中枢管理器
│       │       ├── Config/             # SkillDefSO, WeaponSkillSetSO
│       │       ├── Runtime/            # SkillBar, CombatPipeline
│       │       └── L5_Drivers/         # CombatDriver
│       │
│       ├── L3-stats/                   # L3: Stat 数值框架 (自身代码: Definition/ Tree/ Instance/ Modifier/ Interfaces/ Editor/)
│       │   ├── README.md
│       │   ├── Definition/             # StatDefSO
│       │   ├── Tree/                   # StatsNodeSO, StatsTreeSO
│       │   ├── Instance/               # StatInstance
│       │   ├── Modifier/               # StatModifier, ModifierContext
│       │   ├── Interfaces/             # IStatConsumable 等 4 个接口
│       │   └── Editor/                 # StatsTreeWindow
│       │
│       └── L3-pathfinding/             # L3: 寻路系统
│           └── README.md
│
└── shared/                              # 占位容器: 全局 Helper (不限层级)
    ├── README.md
    ├── data-assets.md
    ├── logging/
    │   ├── README.md
    │   ├── log-manager.md, log-channel.md, log-level.md
    │   ├── appender/                   # ILogAppender, ConsoleAppender
    │   └── compat/                     # Logger
    ├── editor/
    │   ├── README.md
    │   ├── editor-core-loader.md, game-context-editor.md
    │   └── prototype/                  # SyntyPrototypeBrowser, SyntyPrototypeMenu
    └── utility/
        ├── README.md
        └── gizmo-debug-utility.md
```

## 迁移来源

> **v1 和 v2 已归档至 `.agent/archive/tech-v1/` 和 `.agent/archive/tech-v2/`。**

| 新位置 | 旧来源 | 状态 |
|--------|--------|------|
| L1_Core/ | L1-core/ | 直接迁移 |
| Services/L2_EventDispatcher/ | L2-services/EventDispatcher | 重组 |
| Services/L2_SceneService/ | L2-services/SceneService | 重组 |
| Services/L2_TimeService/ | L2-services/TimeService | 重组 |
| Services/L2_GameStateService/ | L2-services/GameStateService | 重组 |
| Services/L2_PlayerService/ | L2-services/PlayerService | 重组 |
| Services/L2_CameraService/ | L2-services/CameraService | 重组 |
| Services/L2_Audio/ | L3-audio/ | 提升至 L2 |
| Services/L2_Input/ | L3-input/ | 提升至 L2 |
| Services/L2_UI/ | L3-ui/ | 提升至 L2 |
| Services/Modules/L3_Character/ | L3-character/ | 直接迁移 |
| Services/Modules/L3_Stats/ | L3-stats/ | 重组 |
| Services/Modules/L3_Pathfinding/ | L3-pathfinding/ | 直接迁移 |
| Shared/Logging/ | L3-logging/ | 移至 Shared |
| Shared/Editor/ | L3-editor/ | 移至 Shared |
| Shared/Utility/ | L3-utility/ | 移至 Shared |

## 层级规则

| 规则 | 说明 |
|------|------|
| L1 只有一个入口 | GameService 是唯一根 |
| L2 Service 不直接互相引用 | 通过 GameContext 或 EventDispatcher |
| L3 不依赖特定 L2 | Character 不 import PlayerService |
| L3 可被多个 L2 共用 | Character ← PlayerService + AIService |
| L4 是 L3 的领域子系统 | Animation, Kinematic, Locomotion, Stats, Audio 各自承担独立功能 |
| L4 只被同模块调用 | L4_Animation 只被 Character 内部使用 |
| L5 是 L4 的附属子系统 | L5_Drivers 是 Animation 的驱动子系统，L5_Locomotion 是 Drivers 的移动子系统 |
| L5 只被同子系统调用 | L5_Locomotion 只被 L5_Drivers 内部使用 |
| Shared 不限层级 | 任何层可调用 |
| 自身组件不是 L4 | Actor/ (Character 自身)、Actions/ (Input 自身)、Core/ Components/ HUD/ (UI 自身) |
| 代码结构不是 L5 | Config/ Structs/ Data/ States/ Rules/ Requests/ — 按文件类型分组，不构成子系统 |

## 命名规则对照

| 类型 | 代码目录 (`Assets/Scripts/`) | 文档目录 (`.agent/tech/`) |
|------|---------------------------|---------------------------|
| 占位容器 | `Services/`, `Modules/`, `Shared/` | `L2-services/`, `L2-modules/`, `shared/` |
| L1 层级 | `L1_Core/` | `L1-core/` |
| L2 层级 | `L2_{Name}/` | `L2-{name}/` |
| L3 层级 | `L3_{Name}/` | `L3-{name}/` |
| L4 子系统 | `L4_{Name}/` (仅领域子系统) | `L4-{name}/` |
| L5 子子系统 | `L5_{Name}/` (仅附属子系统) | `L5-{name}/` |

> 代码目录跟 Unity PascalCase + 下划线分隔 L 前缀；文档目录用 kebab-case 纯小写，便于阅读。两者 L 数字含义完全相同。
> **L4/L5 不应用于普通代码分组**，仅当目录代表一个真正独立的子系统/子子系统时才带 L 前缀。
