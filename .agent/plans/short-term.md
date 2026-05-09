# 短期开发计划 — 围绕 Character

> 日期: 2026-05-09
> 范围: P0 阶段，以 Character 为中心展开
> 原则: 每步有可玩增量，不预建空架子

## 路线总览

```
Phase 1 ──→ Phase 1.5 ──→ Phase 2 ──→ Phase 3 ──→ Phase 4
Loco完结   音效骨架     生存+HUD    战斗基础     动画增强
(1-2周)    (1周)        (3-5周)    (4-6周)     (3-4周)
```

---

## Phase 1: LocomotionSystem 完结

**目标**: 运动系统达到可封装里程碑。

| 任务 | 状态 |
|------|------|
| HeadLook (归一化/平滑/冻结) | ✅ 完成 |
| Footstep (Animancer事件注入 + Debug) | ✅ 代码完成，待配合音效 |

**可玩增量**: 角色动画完整，头部随视线转向

---

## Phase 1.5: 音效系统骨架 + 脚步声落地

**目标**: 搭建音效系统最小骨架，让脚步声真正"听见"。

| 子项 | 说明 |
|------|------|
| AudioManager 骨架 | Channel 音量字典 + SetVolume/Mute |
| AudioChannel 组件 | 挂 AudioSource，注册到 AudioManager |
| CharacterAudio 组件 | 挂 CharacterActor，持有 footstepClips[]，PlayFootstep(isRight) |
| 接线 | BaseLayer 事件回调 → CharacterAudio.PlayFootstep |

**改动范围**: `Assets/Scripts/Audio/` (新目录)，`BaseLayer` 改 1 行（Debug → CharacterAudio）
**可玩增量**: 走路听到脚步声，音效系统可扩展

---

## Phase 2: 角色生存状态

**目标**: CharacterActor 上扩展生存系统，角色有血有肉。

| 指标 | 消耗 | 恢复 | 归零后果 |
|------|------|------|---------|
| 饥饿 | 持续下降 | 吃东西 | HP 下降 |
| 口渴 | 持续下降 | 喝水 | HP 下降 |
| 体力 | 跑/跳消耗 | 休息 | 无法冲刺 |
| HP | 受伤 | 医疗 | 死亡 |

| 配套 | 说明 |
|------|------|
| 状态 HUD | 四指标 + 当前装备，读 GameContext 只刷新 |
| 数据流 | CharacterActor → Snapshot → GameContext → UI |

**优势**: 改动集中在 `Character/Components/`，不外扩
**可玩增量**: 捡到食物要吃，受伤要治，角色有生存压力

---

## Phase 3: 战斗基础

**目标**: 能做最简单的近战攻击。

| 子项 | 说明 |
|------|------|
| 近战攻击 | 鼠标左键触发，射线/碰撞检测 |
| 伤害判定 | 武器伤害 → 目标 HP 扣除 |
| 武器数据 | ScriptableObject，伤害值/攻速 |
| 基础反馈 | 命中音效/特效 |

**依赖**: Phase 2 的 HP 系统
**可玩增量**: 能砍丧尸，战斗循环建立

---

## Phase 4: 角色动画增强

**目标**: 受伤有视觉反馈，上半身动画独立。

| 子项 | 说明 |
|------|------|
| Hit React | 受击时播放受击动画 |
| UpperBody 覆盖 | 利用 Layer 1 + mask，上半身独立于下肢播放 |
| 多层仲裁雏形 | 为 UpperBody/Additive/Facial 铺路 |

**依赖**: Phase 3 + 多层仲裁器（同步实现）
**可玩增量**: 受击有反应，动画更自然

---

## 延后

- Vault / StepOver 障碍物穿越
- 姿势物理联动（crouch/prone）
- Crawl 爬行
