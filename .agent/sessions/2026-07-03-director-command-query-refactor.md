# Session: Director → Command/Query 架构重构

**Date**: 2026-07-03
**Version**: v0.35.0
**Branch**: feature/ability-pipeline

## Background

当前 Director 模块设计不合理：PlayerDirector 超载（同时处理输入、装备、技能、移动意图），NpcDirector 是空壳，UI 直接穿透访问 L3 CharacterBuildContext，无 AI 系统，无 GO 时的实体查询（如"远处未加载 NPC 是否存活"）走不通。

引入全新设计：Entity 作为对外交互唯一入口，Command 模块桥接外部系统→角色命令，Query 模块提供无需 GO 的数据查询。Character 收敛为纯角色模块，暴露 internal 接口供 Command 调用。

## Changes

### 架构 — Entity.Command / Entity.Query
- **新建 7 个文件**: EntityQueryModule、VitalsQuery、InventoryQuery、EquipmentQuery、EntityCommandModule、SCharacterInputState、AIService
- Entity 新增 `Command` 和 `Query` 属性，构造时初始化
- Query 分层设计：L0 Identity → L1 Vitals → L2 Inventory → L3 Equipment → L5 State
- Command 直接调用 CharacterActor 暴露的子模块（Pathfinding/Ability/Container），Actor 不代理

### 消除
- 删除 Director/ 目录（ICharacterDirector、PlayerDirector、PlayerInput、NpcDirector、SCharacterIntent）共 4 个 .cs + 4 个 .meta
- 删除 CameraSnapshotEvent — 连续帧数据改走 GameContext
- SCharacterIntent 拆解分布到 CharacterKinematic（heading/aim）、GroundLocomotion（gait）、Motor（velocity）、Stance（posture）
- CharacterFrameContext 删除 Intent 字段

### 重构 — PlayerService
- 新增 BindInput\<T\> 泛型辅助，输入事件直接调 Entity.Command
- 新增 TryGetMouseGround 从 GameContext 读取鼠标位置
- 删除 OnCameraSnapshot 事件订阅，改为 WriteInputState → CharacterActor.InputState
- 字段从 10+ 帧标志缩减为 4 个持久状态变量

### 重构 — CharacterActor
- 字段重组：公开属性区 / 私有字段区分离，移除 director
- 新增 internal 属性：Pathfinding、Ability、Container、InputState
- Update 简化为直接调用 characterKinematic.Evaluate(InputState, dt)、locomotionSimulator.Simulate(InputState, ...)

### Namespace 重组
- CharacterRig 从 Actor/ 迁移到 Kinematic/，namespace → RedDust.Character.Kinematic
- Structs 统一到 L3_Character/Structs/（SCharacterFrameContext、SCharacterInputState、CharacterBuildContext、CharacterConst）
- Director namespace 完全消除
- Animation 驱动类 public → internal（BaseAnimationDriver 及 3 个子类）

### 简化
- PathfindingAgent 新增 HasActivePath 属性，消除 3 处重复
- UnsubscribeInputEvents 死代码删除
- null-conditional 优化：animSet?.GetNativeSpeed(gait) ?? 0f
- Stance 删除未使用的缓存字段
- Motor speed 变量内联

## Decisions

| 决策 | 选择 | 替代方案（被拒绝） |
|------|------|-------------------|
| Command 模块如何找到 CharacterActor | Entity.View.GetComponent\<CharacterActor\>() | CharacterActor 主动注册（L3→L2 反向依赖） |
| Query 与 CharacterActor 的关系 | Query 是纯数据层，不依赖 Actor | Actor 暴露状态属性给 Query 读（职责边界模糊） |
| 连续输入（AimPoint/Posture/Sprint）的处理 | SCharacterInputState 由外部写入，Actor 读取 | 走 Command 每帧调用（语义错误、开销大） |
| SCharacterIntent 处理 | 拆解到各子模块内部推算 | 重命名保留（仍在 Actor 内不合适）；新建 CharacterMotionEvaluator（多此一举） |
| CameraSnapshot 连续帧数据 | 走 GameContext 共享数据源 | 走事件推送（语义错误、开销大） |
| Animation 驱动类可见性 | public → internal | 保持 public（无外部消费者，暴露多余） |
| 无接口抽象（ICharacterCommandReceiver 等） | 只有一个实现者时不抽接口 | 定义接口（过度设计） |

## Known Issues

- CharacterActor.EvaluateCharacterIntent() 已删除，但 pipeline 的每帧意图推算（gait/posture/heading/aim）分布到 4 个子模块；后续可能需要一个统一的 integration test
- PlayerService.CycleEquip 硬编码装备映射（test_blade/test_pistol）仍在 EntityCommandModule 中，待装备系统完成后迁移
- CameraSnapshot 仍通过事件推送到 PlayerService（WriteInputState 调用链）；TODO 标记后续改为 Camera 写 GameContext + CharacterActor 自取
- 4 个陈旧注释残留（CharacterBuildContext、AbilityForest、CharacterEquipment、AIService 中提到已删除的 PlayerDirector/NpcDirector）

## Cross-References

- Tech: `tech/L2-EntityService/entity-command-query.md` (new)
- Tech: `tech/L3-Character/actor.md` (updated)
- Tech: `tech/L2-PlayerService/player-service.md` (updated)
- Tech: `tech/L2-AIService/ai-service.md` (new)
- Plan: `.agent/../plans/director-command-mossy-elephant.md`
- Previous: `sessions/2026-06-xx-properties-refactor-v2.md`

### Flag for Design Doc Creation
- [x] No design doc needed — internal architectural refactor, no player-facing behavior change.
