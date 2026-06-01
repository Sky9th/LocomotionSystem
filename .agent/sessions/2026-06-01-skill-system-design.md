# 2026-06-01 — 技能系统架构设计

## 目标

为 RedDust 设计完整的 L4_Combat 技能子系统架构，作为 Phase 4.1（近战攻击）的施工蓝图。

## 调研范围

| 项目 | 类型 | 借鉴什么 | 舍弃什么 |
|------|------|---------|---------|
| **UE GAS** | C++ 原生技能框架 | ASC 中枢、Tag 门控、Effect 统一修改属性、AbilityTask 异步编排 | 网络复制、客户端预测、AnimMontage 任务 |
| **MaiKuraki / CycloneGames** | Unity GAS 复刻 | ScriptableObject 配置层、连招冷却豁免（RemoveTag）、三层架构 | CharacterAttributeSet（与 Stats 冲突）、CrossFade 动画（与 Animancer 冲突） |
| **EX-GAS (No78Vino)** | Unity GAS 移植 | TimelineAbility 编辑器概念、施加/激活分离 | 依赖 Odin Inspector、稳定性不足 |
| **gas-unity (Rangerz132)** | 模块化技能系统 | 策略块组装设计（Targeting/Cooldown/Effect Strategy） | Phase 4.1 不需要这种灵活性 |
| **V Rising** | Top-down 生存 | 武器+法术双池、独立 CD | 多人 PvP 平衡设计 |
| **Lost Ark** | Top-down ARPG | Tripod 技能升级、Stagger/Counter 机制 | MMO 复杂度 |
| **Hades** | 动作 Roguelike | 5 槽固定 + 祝福修改、Dash 万能取消 | 局内随机构建 |
| **Albion Online** | 沙盒 MMO | 武器=技能组、装备驱动 | 全 loot PvP 设计 |
| **Project Zomboid** | 生存（参考游戏） | UpperBody 蒙版动画、Early Out 取消 | — |
| **Elden Ring** | 动作 RPG | 体力耗尽仍可攻击但弱、战灰可拆卸 | 锁定目标系统 |

## 关键决策

| 决策 | 原因 |
|------|------|
| **借鉴 GAS 三层理念（Config→ASC→Driver），但完全定制实现** | UE GAS 和 MaiKuraki 都太重，且与 RedDust 现有 Animancer/Stats/EventChannel 体系冲突 |
| **CombatComponent 为纯类，非 MonoBehaviour** | 与 PlayerDirector、CharacterKinematic、GroundLocomotion 同级，保持一致性 |
| **冷却用 Duration Effect + GameplayTag，不用计时器** | 冷却与 Buff/Debuff 统一处理；标签可被外部查询；连招时可直接 RemoveTag 豁免 |
| **CombatDriver 走现有 DriverArbiter 模式** | 复用 Resistance 优先级协商，与 LocomotionDriver/TraversalDriver 同级注册 |
| **伤害/消耗/Buff 统一用 Effect 管道，经 CombatComponent → Stats** | Effect 是属性唯一修改者，不分散在技能代码里 |
| **GameplayTag 用 string 层级标签，不替换 Locomotion 枚举** | Tag 管战斗状态，Locomotion 枚举管移动状态。两者并行不互替 |
| **Director 只设 ActiveSkillSlot，不检查冷却/体力** | 分离意图与执行。冷却/体力检查在 CombatDriver.TryActivate 中完成，避免吞输入 bug |

## 舍弃的路径

| 路径 | 原因 |
|------|------|
| UE GAS 直译 | 网络同步/预测是负担；AnimMontage 与 Animancer 不兼容 |
| 直接引入 MaiKuraki CycloneGames | CharacterAttributeSet 与 Stats 体系冲突；CrossFade 动画管线冲突；引入 VContainer/HybridCLR 额外依赖 |
| 直接引入 EX-GAS | 依赖 Odin Inspector（付费）；WIP 稳定性不足 |
| 左键普攻 | 已否决 — 所有攻击都是技能，走统一管道 |

## 架构概要

```
配置层:  SkillDefSO + WeaponSkillSetSO (ScriptableObject 纯数据)
管理层:  CombatComponent (纯类) — SkillBar + ActiveEffects + OwnedTags
执行层:  CombatDriver (ICharacterAnimationDriver) + CombatPipeline (static 纯函数)
```

详见 tech 文档：`L4-combat/README.md`

## 场景验证

通过 8 个代表性场景端到端模拟验证架构完备性：

| # | 场景 | 验证了 | Phase |
|---|------|--------|-------|
| 1 | 横斩 | 基础链路、Tag 门控、OverlapSphere 检测 | 4.1 |
| 2 | 瞄准射击 | 多阶段(W→A→F→R)、二次输入(SkillConfirm)、Stay 动画、射线检测 | 4.1+ |
| 3 | 蓄力重击 | Active 阶段自身计时、运行时参数注入管道 | 4.1+ |
| 4 | 连招链 | 冷却豁免(RemoveTag)、ComboWindow 标签、技能中途切换 | 4.1b |
| 5 | 位移打击 | CombatDriver 访问 CharacterRig 写位移 | 4.1+ |
| 6 | 弹道投射物 | Projectile 独立发布事件 | 4.2 |
| 7 | 被中断 | HitReactDriver + Resistance 协商、OnInterrupted 清理 | 4.1b |
| 8 | 自身增益 | ApplyEffect Buff 施加(Duration+Modifier+Tag)、过期移除 | 4.2 |

详见 tech 文档 §代表性场景验证。

## 关联

- Tech: `L4-combat/README.md`
- Plan: `short-term.md` Phase 4.1
- Design: `injury-system.md`, `noise-system.md`, `game-overview.md` §4
- Previous session: `2026-06-01-pathfinding-motor-integration.md`, `2026-06-01-so-event-channel.md`
