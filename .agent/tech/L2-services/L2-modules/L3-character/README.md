# 02-character · 角色系统

> L3 独立模块 — 角色运动、动画、数值、音频。被 PlayerService（玩家）和未来的 AIService（敌人）使用。

## 分层

角色系统内部按 L4(子模块) → L5(细分) 组织：

| L4 目录 | 职责 |
|---------|------|
| `L4-actor/` | 顶层组装 — CharacterActor 串联全链路，CharacterRig 统一物理写入 |
| `L4-director/` | 意图控制 — ICharacterDirector 接口 + PlayerDirector 实现（输入+寻路→Intent） |
| `L4-config/` | 配置与枚举 — CharacterProfile SO、LocomotionEnums |
| `L4-kinematic/` | 运动学评估 — 地面检测、障碍检测、头部注视 |
| `L4-locomotion/` | 移动仿真 — Motor(速度/转角) + Stance(Phase/Gait/Turning) |
| `L4-animation/` | 动画表现 — Brain + Arbiter + Driver + FSM 7 状态机 |
| `L4-stats/` | 角色数值 — CharacterStats + Rule 模式驱动 |
| `L4-audio/` | 角色音频 — 脚步/受击/状态音效 |
| `L4-pathfinding/` | 寻路代理 — PathfindingAgent(Seeker+AIPath) + PathfindingTester |
| — | 能力系统已独立为 [`L3-ability/`](../L3-ability/README.md) |
| `L4-input/` | 角色输入 — EventDispatcher 桥接 + 输入聚合 |

## 调用链

```
CharacterActor.Update()
  │
  ├── [Ability] ability.Tick(dt)                       ← 冷却倒计时、ActiveEffect 过期
  │
  ├── [Director] PlayerDirector.Evaluate() → SCharacterIntent
  │   ├── PlayerInput → 鼠标/按键帧状态 + Skill1~4
  │   └── PathfindingAgent → SetDestination / DesiredVelocity
  │
  ├── [Kinematic] CharacterKinematic.Evaluate()
  │   ├── CharacterGroundDetection → SGroundContact
  │   ├── CharacterHeadLook → Vector2(yaw, pitch)
  │   └── CharacterObstacleDetection → SForwardObstacleDetection
  │   → 输出 ctx.Kinematic
  │
  ├── [Locomotion] GroundLocomotion.Simulate()
  │   ├── Motor.Evaluate() → SCharacterMotor (速度 + TurnAngle)
  │   └── Stance.Evaluate() → SCharacterDiscrete (Phase/Gait/Posture/IsTurning)
  │   → 输出 ctx.Motor + ctx.Discrete
  │
  ├── [Stats] CharacterStats.Update()
  │   └── Rules: Damage / Deplete / PassiveGain / SprintStamina / ToggleModifier
  │
  ├── [Animation] AnimationBrain.Apply(ctx)
  │   └── DriverArbiter.Resolve()
  │       ├── LocomotionDriver → BaseLayer FSM (7 状态)
  │       │   ├── Idle / Moving / IdleToMoving
  │       │   ├── TurnInPlace / TurnInMoving
  │       │   └── AirLoop / AirLand
  │       ├── TraversalDriver → 攀爬/跨越动画
  │       └── AbilityDriver → 技能动画 + 命中检测 + 伤害施加
  │
  └── [Audio] CharacterAudio (AnimationBrain 脚步事件回调驱动)
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| CharacterActor | Core (PlayerService) | 被 PlayerService 实例化和管理 |
| CharacterActor | Core (CameraService) | 读取 SCameraSnapshot 获取朝向 |
| AbilityComponent | CharacterActor | MonoBehaviour, Update 中 Tick；管理 Tags + Effects |
| AbilityDriver | AnimationBrain (DriverArbiter) | 注册为 Driver，通过 SubmitRequest 播放技能动画 |
| CharacterRig | CharacterKinematic, AnimationBrain, BaseLayer, TraversalDriver | 统一物理写入入口 |
| Motor | CharacterKinematic | 读 BodyForward + LocomotionHeading 算 TurnAngle |
| CharacterStats | Stats 框架 (05-stats) | 依赖 StatsTreeSO / StatInstance |
| CharacterAudio | Audio 系统 (06-audio) | 通过 AudioChannel 播放音效 |
| 整个模块 | 未来 AIService | AI 将使用同一 CharacterActor 生成敌人 |

## 设计决策

| 决策 | 原因 |
|------|------|
| CharacterRig 统一物理写入 | Transform/Rigidbody/Collider 所有修改走单一入口 |
| 父调子不跨级 | CharacterActor → Kinematic → Locomotion → Animation，不跨层 |
| Driver/Arbiter 动画驱动模式 | LocoDriver（连续）+ TraversalDriver（一次性），Arbiter 统一仲裁 |
| Motor + Stance 分离 | 速度计算 (How fast) 与状态判定 (What state) 解耦 |
| Rule 模式驱动 Stats | 每个数值变化规则独立，CharacterStats 只做 Tick 分派 |
| SphereCast 单探头地面检测 | 膝盖高度单探头替代 BoxCast+Raycast，更稳定 |
| Y 轴常态不碰 | 地面锁定处理 Y，攀爬/落地时 SuppressGroundLock 解锁 |
| 动画请求自带 Tags+Resistance | 无全局优先级表，后来者自读 |
| Component Driver 模式 | Inspector 可视化配置，OnEnable 自注册到 Arbiter |
| Evaluate 接口 | Driver 非活跃时也能提交请求（如 TraversalDriver 在 LocoDriver 活跃时预提交） |
| SCharacterSnapshot 已删除 (2026-05-25) | 原打包 Input+Kinematic+Motor+Discrete+Stats 供管线消费，现 Animation 直接消费 `CharacterFrameContext`，外部通过 PlayerService 获取玩家数据 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| AIService 使用 CharacterActor 生成敌人 | 远期 | AI 系统 | 架构设计 |
| L3_Ability 能力子系统 | Phase 4.1 | AbilityDefSO + AbilityComponent + AbilityDriver | skill-system-design |
| Ability/Interaction/Movie 接入 Animation | 远期 | 对应系统的 ActionRequest | 旧 character-animation.md |
| FootLayer 独立实现 | 待做 | — | 旧 animation-design.md |
| UpperBody/Additive 动画层 | 远期 | 战斗/交互系统 | 旧 coverage-analysis.md |
| Vault/StepOver 穿越类型 | 待做 | — | 枚举已定义 |
| Crawl 步态 | 待做 | — | 枚举已定义 |
| Stats 外部事件桥接 (CombatSystem → DamageRule) | 待做 | — | 旧 stats-rule-system.md |
| Stat 规则配置化 (当前硬编码) | 待做 | 05-stats | 代码 TODO |
| 根运动迁移到 AnimationBrain | 待做 | — | 旧 animation-architecture-plan.md |
| EquipmentComponent 装备槽位 | 待做 | L3-equipment (GearDefSO, GearInstance) | [equipment-system.md](../../../../design/equipment-system.md) |
| 受击/死亡/呼吸音效 | 待做 | 06-audio | 代码预留 |

## 子文档索引

### actor/
| 文件 | 内容 |
|------|------|
| [character-actor.md](L4-actor/character-actor.md) | CharacterActor — 组合根，Update 调用链入口 |
| [character-actor-debug.md](L4-actor/character-actor-debug.md) | CharacterActor.Debug — Gizmo 可视化 |
| [character-rig.md](L4-actor/character-rig.md) | CharacterRig — 物理实体统一写入入口 |
| [character-const.md](L4-actor/character-const.md) | CharacterConst — PropertyTree 路径/rTag/槽位 ID 全局常量 |
| [character-frame-context.md](L4-actor/character-frame-context.md) | CharacterFrameContext — 帧内数据总线 |

### config/
| 文件 | 内容 |
|------|------|
| [character-profile.md](L4-config/character-profile.md) | CharacterProfile — 地面/障碍参数 SO |
| [locomotion-enums.md](L4-config/locomotion-enums.md) | LocomotionEnums — Phase/Gait/Posture 枚举 |

### kinematic/
| 文件 | 内容 |
|------|------|
| [character-kinematic.md](L4-kinematic/character-kinematic.md) | CharacterKinematic — 运动学入口 |
| [character-ground-detection.md](L4-kinematic/character-ground-detection.md) | CharacterGroundDetection — SphereCast 地面 |
| [character-head-look.md](L4-kinematic/character-head-look.md) | CharacterHeadLook — 头部 yaw/pitch |
| [character-obstacle-detection.md](L4-kinematic/character-obstacle-detection.md) | CharacterObstacleDetection — 障碍检测 |
| [structs/s-character-kinematic.md](L4-kinematic/structs/s-character-kinematic.md) | SCharacterKinematic struct |
| [structs/s-forward-obstacle-detection.md](L4-kinematic/structs/s-forward-obstacle-detection.md) | SForwardObstacleDetection struct |
| [structs/s-ground-contact.md](L4-kinematic/structs/s-ground-contact.md) | SGroundContact struct |

### locomotion/
| 文件 | 内容 |
|------|------|
| [i-locomotion-simulator.md](L4-locomotion/i-locomotion-simulator.md) | ILocomotionSimulator 接口 |
| [ground-locomotion.md](L4-locomotion/ground-locomotion.md) | GroundLocomotion — 仿真编排器 |
| [motor.md](L4-locomotion/motor.md) | Motor — 速度/转角计算 |
| [stance.md](L4-locomotion/stance.md) | Stance — Phase/Gait/Posture/Turning |
| [structs/s-character-motor.md](L4-locomotion/structs/s-character-motor.md) | SCharacterMotor struct |
| [structs/s-character-discrete.md](L4-locomotion/structs/s-character-discrete.md) | SCharacterDiscrete struct |
| [config/locomotion-profile.md](L4-locomotion/config/locomotion-profile.md) | LocomotionProfile SO |

### animation/
| 文件 | 内容 |
|------|------|
| [animation-brain.md](L4-animation/animation-brain.md) | AnimationBrain — 6 层 Animancer 总控 |
| [driver-arbiter.md](L4-animation/driver-arbiter.md) | DriverArbiter — Driver 优先级仲裁 |
| [drivers/i-character-animation-driver.md](L4-animation/drivers/i-character-animation-driver.md) | ICharacterAnimationDriver 接口 |
| [drivers/base-character-animation-driver.md](L4-animation/drivers/base-character-animation-driver.md) | BaseCharacterAnimationDriver 基类 |
| [drivers/locomotion/locomotion-driver.md](L4-animation/drivers/locomotion/locomotion-driver.md) | LocomotionDriver 入口 |
| [drivers/locomotion/base-layer.md](L4-animation/drivers/locomotion/base-layer.md) | BaseLayer — 7 状态 FSM + ApplyTurnStepRotation |
| [drivers/locomotion/base-state-key.md](L4-animation/drivers/locomotion/base-state-key.md) | BaseStateKey 枚举 |
| [drivers/locomotion/locomotion-layer-fsm-state.md](L4-animation/drivers/locomotion/locomotion-layer-fsm-state.md) | FSM 状态基类 |
| [drivers/locomotion/L5-states/base-idle-state.md](L4-animation/drivers/locomotion/L5-states/base-idle-state.md) | 待机状态 |
| [drivers/locomotion/L5-states/base-moving-state.md](L4-animation/drivers/locomotion/L5-states/base-moving-state.md) | 移动状态 |
| [drivers/locomotion/L5-states/base-idle-to-moving-state.md](L4-animation/drivers/locomotion/L5-states/base-idle-to-moving-state.md) | 启动过渡 |
| [drivers/locomotion/L5-states/base-turn-in-place-state.md](L4-animation/drivers/locomotion/L5-states/base-turn-in-place-state.md) | 原地转身 |
| [drivers/locomotion/L5-states/base-turn-in-moving-state.md](L4-animation/drivers/locomotion/L5-states/base-turn-in-moving-state.md) | 移动中转身 |
| [drivers/locomotion/L5-states/base-air-loop-state.md](L4-animation/drivers/locomotion/L5-states/base-air-loop-state.md) | 空中循环 |
| [drivers/locomotion/L5-states/base-air-land-state.md](L4-animation/drivers/locomotion/L5-states/base-air-land-state.md) | 分级落地 |
| [drivers/traversal/traversal-driver.md](L4-animation/drivers/traversal/traversal-driver.md) | TraversalDriver 攀爬 |
| [config/locomotion-animation-profile.md](L4-animation/config/locomotion-animation-profile.md) | LocomotionAnimationProfile SO |
| [config/locomotion-mode-profile.md](L4-animation/config/locomotion-mode-profile.md) | LocomotionModeProfile SO |
| [requests/animation-request.md](L4-animation/requests/animation-request.md) | AnimationRequest |
| [requests/on-complete-behavior.md](L4-animation/requests/on-complete-behavior.md) | 完成行为枚举 |
| [requests/on-interrupted-behavior.md](L4-animation/requests/on-interrupted-behavior.md) | 中断行为枚举 |

### stats/
| 文件 | 内容 |
|------|------|
| [character-stats.md](L4-stats/character-stats.md) | CharacterStats — StatsTree + Rule 容器 |
| [float-state.md](L4-stats/float-state.md) | FloatState — Float 运行时状态（Tick/Modify/事件），Properties 消费层 |
| [float-modifier.md](L4-stats/float-modifier.md) | FloatModifier + RateContext — 持久帧级修改器，A/B/C 三类 |
| [rules/character-stat-rule.md](L4-stats/L5-rules/character-stat-rule.md) | CharacterStatRule 基类 |
| [rules/deplete-chain-rule.md](L4-stats/L5-rules/deplete-chain-rule.md) | DepleteChainRule 基类 |
| [rules/hunger-deplete-rule.md](L4-stats/L5-rules/hunger-deplete-rule.md) | HungerDepleteRule — 饥饿→HP |
| [rules/passive-gain-rule.md](L4-stats/L5-rules/passive-gain-rule.md) | PassiveGainRule 基类 |
| [rules/sprint-stamina-rule.md](L4-stats/L5-rules/sprint-stamina-rule.md) | SprintStaminaRule — 冲刺体力 3x |
| [rules/toggle-modifier-rule.md](L4-stats/L5-rules/toggle-modifier-rule.md) | ToggleModifierRule 基类 |

### audio/
| 文件 | 内容 |
|------|------|
| [character-audio.md](L4-audio/character-audio.md) | CharacterAudio — 脚步事件音效 |
| [config/character-audio-config-so.md](L4-audio/config/character-audio-config-so.md) | CharacterAudioConfigSO |
| [config/footstep-set-so.md](L4-audio/config/footstep-set-so.md) | FootstepSetSO |

### director/
| 文件 | 内容 |
|------|------|
| [s-character-intent.md](L4-director/s-character-intent.md) | SCharacterIntent — 角色意图结构体，含寻路速度 override |
| [i-character-director.md](L4-director/i-character-director.md) | ICharacterDirector 接口 |
| [player/player-director.md](L4-director/player/player-director.md) | PlayerDirector — 玩家输入+寻路→Intent |
| [player/player-input.md](L4-director/player/player-input.md) | PlayerInput — 输入聚合器 |

### pathfinding/
| 文件 | 内容 |
|------|------|
| [pathfinding-agent.md](L4-pathfinding/pathfinding-agent.md) | PathfindingAgent — Seeker+AIPath 寻路代理 |
| [pathfinding-tester.md](L4-pathfinding/pathfinding-tester.md) | PathfindingTester — 随机目的地测试 |

### ability/ (已迁移至独立模块)
| 文件 | 内容 |
|------|------|
| [README.md](../L3-ability/README.md) | L3_Ability — 能力定义、效果管道、搜索形状 |

### input/
| 文件 | 内容 |
|------|------|
| [character-event-receiver.md](L4-input/character-event-receiver.md) | CharacterEventReceiver — 输入桥接 |
| [s-character-input-actions.md](L4-input/s-character-input-actions.md) | SCharacterInputActions struct |
