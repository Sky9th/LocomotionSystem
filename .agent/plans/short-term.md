# 短期开发计划 — 战斗闭环

> 日期: 2026-06-02
> 范围: Phase 4 战斗基础架构 + 敌人 AI
> 原则: 每步有可玩增量，聚焦战斗闭环，其余系统延后
> 架构: [L4_Combat README](../tech/L2-services/L2-modules/L3-character/L4-combat/README.md)

---

## 前置工程: 俯视角切换 + A* 寻路

> 当前 Locomotion 按第三人称开发（相机在身后、移动相对于相机朝向、HeadLook 3D）。
> 最终游戏是俯视角，寻路使用 Aron Granberg 的 A\* Pathfinding Project（Asset Store）。
> 不在算法层造轮子，只做集成适配。

### 0.1 俯视角切换 ✅

| 子项 | 说明 | 实现 |
|------|------|------|
| Camera | 第三人称跟随 → 俯视角透视俯拍 | Cinemachine Transposer(WorldSpace) + HardLookAt，代码只设 pivot.position/rotation |
| 鼠标→世界 | Y=0 平面求交 | CameraService.ComputeMouseGroundPosition → Plane.Raycast |
| 移动输入 | WASD 固定屏幕方向 | Motor.ConvertToWorld → Vector3(local.x, 0f, local.y) |
| 角色朝向 | 鼠标地面位置 | CharacterActor Update 中 (mouseWorldPos - position).XZ.normalized |
| LocomotionHeading | 来源改为 mouseWorldPos | CharacterKinematic.Evaluate 参数 viewForward → heading |
| HeadLook | 保留退化模式 | Phase 4 接入感知系统，当前看 actorTransform.forward |
| TurnInPlace 判定 | 删除 lookStability | Stance.EvaluateTurning 直接用 TurnAngle |
| 光标 | Playing 状态 Confined + visible | GameStateService |
| 数据结构 | SCameraContext → SCameraSnapshot | 加 MouseGroundPosition/IsMouseGroundValid |
| 输入模块 | CharacterInputModule → CharacterEventReceiver | Camera 与 Input Actions 分离 |

**附带架构改动**:
- BaseService.PublishState<T>() — GameContext + Dispatcher 统一写入
- Component 禁止直接 GameContext.Instance.UpdateSnapshot()
- CameraService 通过 GameContext.TryGetSnapshot<SPlayer>() 读玩家位置
- SCharacterSnapshot + SLocomotionState 删除，Animation 管线使用 CharacterFrameContext

### 0.2 A\* Pathfinding Project 集成 ✅

| 子项 | 说明 | 实现 |
|------|------|------|
| 适配脚本 | `PathfindingAgent` — 持有 `Seeker`（查询）+ `AIPath`（移动），统一代理入口 | ✅ |
| 路径查询 | `Seeker.StartPath()` 异步算路 | ✅ |
| 移动驱动 | AIPath `desiredVelocity` → `SCharacterIntent.ExternalMovementVelocity` → Motor override 分支 | ✅ |
| 移动集成 | 走现有动画/root motion 管道，AIPath velocity 直接透传 | ✅ |
| Click-to-Move | 右键 → `agent.SetDestination(mousePos)`，heading 使用 `desiredVelocity.normalized` | ✅ |

**延后（到对应 Phase）**:
- GridGraph bake + 障碍标记（Phase 4 战斗/AI 时逐步完善）
- 动态障碍更新（Phase 6 建造时做）
- Flow Field / 多 Agent 优化（Phase 10 尸潮时做）

---

## Phase 4: 战斗基础架构 + 敌人 AI

> 设计: [L4_Combat README](../tech/L2-services/L2-modules/L3-character/L4-combat/README.md) / `injury-system.md` / `noise-system.md`

两个子阶段。4.1 建战斗系统骨架，4.2 建敌人 AI（含噪声感知消费端）。

### 4.1 近战基础架构

> 架构设计: [L4_Combat README](../tech/L2-services/L2-modules/L3-character/L4-combat/README.md)

三层架构，Q/E/R/F 四槽技能栏，GameplayTag 门控+冷却。

**闭环链路**：按键 → CombatComponent.TryActivate()（门控/消耗/冷却）→ CombatDriver.SubmitRequest → AnimationBrain 播放动画 → 阶段机 Windup→Fire→Recovery → CombatPipeline 扇形命中检测 → stats.DamageRule 扣血 → SHitEvent + SNoiseEvent 发布。

| 子项 | 说明 | 依赖 |
|------|------|------|
| GameplayTag + GameplayTagContainer | 层级标签，门控/冷却/状态标记统一管道 | — |
| ECombatSearchType + SkillPhase + SkillAnimationLayer 枚举 | Cone/RayLine/Circle；六阶段；FullBody/UpperBody | — |
| SkillDefSO + WeaponSkillSetSO | SO 配置层：searchType/searchRange/searchAngle/maxTargets/requiresLoS/噪音等级 | GameplayTag |
| 战斗数据结构 | DamageInfo、SHitEvent、SSkillEvent、SNoiseEvent struct | — |
| CombatPipeline | static：SearchCandidates(Cone+RayLine)+FilterByLoS+RollHit(跳过)+CalculateDamage(简化) | SkillDefSO |
| ActiveGameplayEffect + SkillBar + SkillSlot | 冷却 Effect 池化 + 四槽冷却管理 | GameplayTag |
| CombatComponent | 纯类中枢：Tick/TryActivate/门控/冷却/ApplyDamage/HasTag | SkillBar, CombatPipeline |
| CombatDriver | 继承 BaseCharacterAnimationDriver，阶段机，Resistance=15 | CombatComponent |
| SCharacterIntent 扩展 | +ActiveSkillSlot / SkillConfirm / SkillCancel | — |
| PlayerInput + PlayerDirector 扩展 | +4 skill button (Q/E/R/F)，透传至 Intent | SCharacterIntent |
| CharacterActor 集成 | 构造 CombatComponent，Tick 插入 Stats→Animation 之间 | CombatComponent |
| 动画配置 | AnimationAliasProfile +combat alias 字段（横斩+手枪射击） | CombatDriver |
| 场景验证 | 两技能全链路：Q 横斩(Cone) + R 手枪(RayLine) | 上述全部 |

**纳入 4.1 的预留接口**（一次定义，行为先简化）：SkillPhase 完整六阶段枚举、ECombatSearchType 完整三类型、CombatPipeline 四阶段方法签名（SearchCandidates/FilterByLoS/RollHit/CalculateDamage）、SkillConfirm/Cancel 字段、AnimationLayer 枚举、ActiveGameplayEffect 过期回调、OnInterrupted 清理逻辑、ComboWindow 相关字段。

**不纳入 4.1**（依赖 Phase 5 或后续系统的内容移到长期计划）：Circle 搜索类型（旋风斩/战吼）、完整投骰管道 RollHit/CalculateDamage、连招系统、远程武器 SkillConfirm/Cancel（瞄准）、Buff/Debuff、多武器切换、熟练度、HitReactDriver、投射物系统。

### 4.2 敌人 AI 基础

> 复用 4.1 的 CombatComponent（纯类，不绑 Player）和 SNoiseEvent。SNoiseEvent 发布端已在 4.1 完成，本节做感知消费端。

| 子项 | 说明 |
|------|------|
| 丧尸生成 | 网格可行走节点随机选点 Spawn，基础属性（HP/伤害/移速） |
| 行为 FSM | Idle → Alerted → Chase → Attack → Dead |
| 空闲 | 随机选可行走节点作为巡逻目标 |
| 听觉感知 | 订阅 4.1 的 SNoiseEvent → `distance <= radius[level]` → Alerted → agent.SetDestination(声源) |
| 视觉感知 | 2D 地面扇形（前方角度 + 距离）→ 看到玩家 → 直接 Chase |
| 攻击 | 靠近玩家 → CombatComponent.TryActivate() → 伤害判定 |
| 死亡 | HP 归零 → 死亡动画 → 移除 + 基础掉落 |

**不纳入 4.2**：噪音连锁反应/障碍物衰减/昼夜倍率/环境噪音（→ Phase 12+ 噪音扩展）、视觉感知光线影响/尸群协调/特殊感染者（→ Phase 12+ 敌人扩展）、丧尸化过程（→ Phase 12+ 伤病扩展）。

---
**当前 2 个子阶段的关系**：

```
4.1 战斗基础架构
  ├── Q/R 按键 → SkillDefSO → CombatComponent.TryActivate()
  ├── CombatDriver 阶段机(Windup→Fire→Recovery) → 动画播放
  ├── CombatPipeline.SearchCandidates(Cone Q / RayLine R) + FilterByLoS(R)
  ├── 命中 → stats.DamageRule → HP -
  └── GameEvent<SHitEvent>.Raise() + GameEvent<SNoiseEvent>.Raise()
          │                          │
          └──────────┬───────────────┘
                     ▼
4.2 敌人 AI ──── SNoiseEvent 听觉感知 + SHitEvent 受击反馈
  ├── 行为 FSM: Idle → Alerted → Chase → Attack → Dead
  ├── CombatComponent.TryActivate() 复用 4.1 的命中判定管线
  └── 死亡 → HasTag("State.Dead") → 掉落
```

**可玩增量**: 俯视角地图 → 丧尸在附近闲逛 → 你跑过去发出声音 → 丧尸警觉追来 → 你用 Q 横斩 / R 手枪攻击 → 它掉血死亡 → 掉落物品。

**不纳入短期计划**（已移入长期计划）:
- 复杂远程 RangedDriver（瞄准 Active 阶段/SkillConfirm）、Circle 搜索类型、投射物系统 — Phase 5 / Phase 12+
- 连招系统、HitReactDriver、完整投骰管道 RollHit/CalculateDamage — Phase 4.1b / Phase 12+
- Buff/Debuff、多武器切换、熟练度 — Phase 5 资源/物品系统
- 噪音连锁反应/障碍物衰减/昼夜倍率 — Phase 12+ 噪音扩展
- 视觉感知光线影响/尸群协调/特殊感染者/丧尸化过程 — Phase 12+ 敌人/伤病扩展

---

## 已完成（Phase 1 ~ 4 前置）

| Phase | 内容 | 状态 |
|-------|------|------|
| 1 | LocomotionSystem | ✅ |
| 1.5 | 音效系统骨架 | ✅ |
| 2 | 通用数值系统 | ✅ |
| 2.5 | Character Stats 管理 | ✅ |
| 3 | HUD UI + MainMenu | ✅ |
| 3.5 | PauseMenu + Loading | ✅ |
| 3.6 | Service 架构加固 | ✅ |
| 3.7 | 数据流架构重构 (PublishState + Component 解耦) | ✅ |
| 4 前置 | 俯视角切换 | ✅ |
| 4 前置 | A\* Pathfinding 集成 | ✅ |

---

## 已移出短期计划

以下从旧短期计划移入 [长期计划](long-term.md)：

- Phase 5（资源系统 + 负重 + 存档）
- Phase 6（建造基础）
- Phase 7（时间与日夜）
- Phase 8+（农业/烹饪、NPC、尸潮、科技树、全生态联通）
