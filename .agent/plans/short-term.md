# 短期开发计划 — 战斗闭环

> 日期: 2026-05-23
> 范围: Phase 4 战斗基础 + 敌人 AI + 噪音
> 原则: 每步有可玩增量，聚焦战斗闭环，其余系统延后

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

## Phase 4: 战斗基础 + 敌人 AI 基础 + 噪音骨架

> 设计: `injury-system.md` / `noise-system.md`

### 4.1 近战攻击

| 子项 | 说明 |
|------|------|
| 攻击触发 | 鼠标左键 → 角色面向光标方向 → 播放攻击动画 |
| 伤害判定 | 动画事件驱动判定窗口 → 前方扇形/球形检测 → 命中敌人 |
| 武器数据 | WeaponDefSO（伤害值/攻速/范围/噪音等级/伤害类型） |
| 命中反馈 | 音效（AudioChannel）+ 受击闪白/特效 |

### 4.2 伤害管道

| 子项 | 说明 |
|------|------|
| 命中→伤害 | 武器伤害值 → 穿透 DamageRule（已有）→ 目标 HP 扣除 |
| 施加伤病 | 根据武器伤害类型施加割伤/钝器伤/咬伤 → 触发持续效果（流血/疼痛） |
| 死亡 | HP 归零 → 播放死亡动画 → 移除实体 + 基础掉落 |

### 4.3 敌人 AI 基础

| 子项 | 说明 |
|------|------|
| 丧尸生成 | 网格可行走节点中随机选点 Spawn，基础属性（HP/伤害/移速） |
| 行为 FSM | Idle → Alerted → Investigating → Chase → Attack |
| 空闲 | 随机选可行走节点作为巡逻目标 |
| 听觉感知 | 订阅 SNoiseEvent → 在噪音半径内 → GridAgent.SetDestination(声源位置) |
| 视觉感知 | 2D 地面扇形（前方角度 + 距离）→ 看到玩家 → 直接 Chase |
| 攻击 | 靠近玩家 → 播放攻击动画 → 伤害判定 → 施加咬伤 |
| 死亡 | HP 归零 → 播放死亡动画 → 移除 + 掉落物品 |

### 4.4 噪音骨架

| 子项 | 说明 |
|------|------|
| SNoiseEvent | struct { Vector3 position, int level, ENoiseType type, GameObject source } |
| 发布者 | 移动（走路/跑步）、近战（挥空/命中）、交互（开门/破窗）行为发布噪音 |
| 丧尸反应 | 订阅 SNoiseEvent → `distance <= radius[level]` → Alerted → 向声源移动 |
| 噪音等级 | 6 级（0 无声 → 6 震耳），阶段 4.1-4.3 只用到 2-5 |

---
**当前 4 个子系统的关系**：

```
鼠标左键 → 武器攻击 → 命中检测 → 伤害管道 → 施加伤病 + 发布噪音
                                              │
                                        ┌─────┘
                                        ▼
                              SNoiseEvent → 丧尸听觉感知 → FSM 状态切换
                                                              │
                                              GridAgent.SetDestination(声源)
```

**可玩增量**: 俯视角地图 → 丧尸在附近闲逛 → 你跑过去发出声音 → 丧尸警觉追来 → 你用武器砍它 → 它流血 → 死亡 → 掉落物品。

**不做的**:
- 远程武器、技能栏、熟练度成长
- 噪音连锁反应（第 2 层）、障碍物衰减
- 丧尸化过程（仅做感染值累积）
- 视觉感知的光线影响
- 尸群协调、特殊感染者

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
