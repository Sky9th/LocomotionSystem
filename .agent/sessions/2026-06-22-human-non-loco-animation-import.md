# 2026-06-22 — Human 非 Locomotion 动画资产整理

## Background

Zombie 已有 Attack/Reaction/Special 三类非 Locomotion FBX 动画（共 19 个，来自 Zombie Animset），Human 仅有 Locomotion FBX。需要从 PROTOFACTOR 源补全 Human 的 Attack/Reaction/Special。

## Changes

### Human 非 Locomotion FBX 导入
从 External PROTOFACTOR 源按 **角色类型 → 武器类型 → 动画类别** 三层结构组织，复制到 `Assets/Art/Animations/PROTOFACTOR/Human/`：

| 武器类型 | PROTOFACTOR 源 | Attack | Reaction | Special |
|---------|---------------|--------|----------|---------|
| Unarmed | Combat Bare Fists Animset | 38 | 23 | 12 |
| 1H_Blade | 1Handed Melee Weapon Animset | 12 | 21 | 4 |
| 1H_Sidearm | 2Handed Gun Animset | 18 | 14 | 33 |
| **合计** | | **68** | **58** | **49** = **175** |

### 分类规则
- **Attack**: Shoot/Reload/MeleeAttack/Combo/Jab/Hook/Uppercut/Kick/ThrowGrenade 等
- **Reaction**: GetHit(4方向×轻重)/Death(4方向)/Dead/GetBackUp/Blocked/Stunned
- **Special**: Dodge(4方向)/Cover(进出/探头/平移)/Taunt/IdleCombat 变体
- **排除**: Walk/Run/Sprint/CrouchMove/Climb/Jump/Landing/Falling (Locomotion)

### _RM 规则
- 有 `_RM.fbx` → 取 `_RM`，弃非 RM
- 仅存在非 RM → 取非 RM（如 Shoot/Reload——原地动画无位移）
- **⚠ 严禁导入 `2Guns` 后缀**——双持手枪，非项目使用的单持姿态

### 文档
- 新建 `protofactor-fbx-assets.md` — PROTOFACTOR FBX 目录结构、源映射表、命名约定、缺口

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 2Handed Gun Animset 作为 1H_Sidearm 源 | Double Guns Animset → 命名含 `2Guns`，是双持手枪 | 2Handed Gun 中双手持一把手枪的姿态与单手副武器最接近 |
| Attack 无 _RM 也导入 | 等 _RM 版补充 → 攻击动画如 Shoot/Reload 本身不应有位移 | 有位移的攻击动画才需要 _RM，原地攻击用非 RM 正确 |
| 先只做 Human/Male | 同时做 Female → 当前无 Female 骨骼，无法验证 | 男性骨骼验证通过后再做女性 |

## Known Issues

- [ ] `Human/Unarmed/Locomotion/` 仍未导入——空手移动需从 Basic Locomotion Animset 提取
- [ ] `Human/1H_Blade/Locomotion/Relax/` 缺 Walk/Run Mixer 方向动画
- [ ] Attack/Reaction/Special 动画暂无 SO 类型和 Driver——当前处于"待消费"状态
- [ ] TraversalDriver 是 TODO stub，攀爬动画虽有 FBX 但无运行时驱动
- [ ] Zombie TypeB/TypeE FBX 尚未导入

## Cross-References

### Related Sessions
- [2026-06-22-traversal-merge-and-zombie-config.md](2026-06-22-traversal-merge-and-zombie-config.md) — TraversalAnimationSetSO 合并 + Zombie 配置落地

### Related Tech Docs
- [tech/.../protofactor-fbx-assets.md](../tech/L2-services/L2-modules/L3-character/L4-animation/protofactor-fbx-assets.md) — PROTOFACTOR FBX 资产目录（本次新建）
- [tech/.../locomotion-animation-set.md](../tech/L2-services/L2-modules/L3-character/L4-animation/config/locomotion-animation-set.md) — （如存在）LocomotionAnimationSetSO 文档

### Flag for Design Doc Creation
- [x] No design doc needed — asset import + cataloging, no design-facing changes.
