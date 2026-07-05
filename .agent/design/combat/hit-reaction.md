# Hit Reaction — 受击反应系统

> **Status**: A测开发中 · Last Updated: 2026-07-05 · v0.36.10

## System Positioning

### Placement
受击反应是战斗系统的反馈层，位于 Combat 域下。当角色受到攻击时，根据伤害携带的冲击数据，播放对应的受击动画。与死亡系统独立——死亡的视觉表现（Ragdoll）是后续系统。

### Purpose
解决"攻击命中后角色无反应"的问题。没有受击反应时，攻击命中只产生数值变化，玩家和 AI 都没有视觉反馈——这导致战斗缺乏打击感和因果关系。受击动画让每一次命中都有可见的身体反应。

### Scope
- IN: Flinch（轻受击）、Stagger（重受击）、Knockdown（击倒→起身）、HP=0 倒地不起
- OUT: 霸体/护盾免疫受击（PropertyTable 接入后）、死亡 Ragdoll（独立系统）、击退位移

### Inputs & Outputs
- INPUT: `SDamageInfo`（携带 `ImpactEffectSO`）→ CharacterCombat.OnReaction()
- OUTPUT: AnimationBrain → DriverArbiter → HitReactionDriver → 播放受击动画
- SIDE EFFECT: 受击动画播放期间，角色不可操作（FullBody 被 HitReactionDriver 占用）

## Gameplay Mechanics

### 受击判定流程
```
攻击命中 → ExecutionState 提取 ImpactEffectSO → SDamageInfo.ImpactEffect
  → AbilityReactor.Resolve()
  → CharacterCombat.OnReaction()
    → reactionLevel 决定受击等级
    → HitDirection 转本地空间 blend parameter
    → 提交 AnimationRequest(DriverType=HitReaction)
      → DriverArbiter 仲裁（HitReaction 抢占一切）
      → HitReactionDriver 播放 MixerTransition2D
        → Knockdown: OnCompleted → ChainGetUp()
```

### 受击等级
| 等级 | 动画 | 后续 | 触发者 |
|------|------|------|--------|
| **Flinch** | hitReactionFlinch | 播放完毕后归还 Locomotion | `reactionLevel=Flinch` |
| **Stagger** | hitReactionStagger | 播放完毕后归还 Locomotion | `reactionLevel=Stagger` |
| **Knockdown** | hitReactionKnockdown → hitReactionGetUp | 起身后归还 Locomotion | `reactionLevel=Knockdown` (Impact) |
| **Death** | hitReactionKnockdown | 停在倒地 pose | HP ≤ 0 |

### 方向系统
受击动画是 4 方向 Blend Tree（X=左右, Y=前后）。`HitDirection`（伤害飞行方向）取反后转为本地空间 blend parameter：
- 从背后被攻击 → 冲击力向前 → 角色向前倒
- 从右侧被攻击 → 冲击力向左 → 角色向左倒

### 抢占规则
| 活跃请求 | 新请求 | 结果 |
|----------|--------|------|
| Idle / Locomotion | 任意 | 接受 |
| Ability / Traversal | HitReaction | **抢占**（受击打断技能） |
| HitReaction | Ability / Traversal | 拒绝（受击不能被技能打断） |
| HitReaction | HitReaction | **抢占**（连续受击互打断） |
| Traversal ↔ Ability | | 互斥（拒绝） |

## Numeric Design

| 参数 | 类型 | 当前值 | 说明 |
|------|------|--------|------|
| `reactionLevel` | enum | 资产配置 | Flinch / Stagger / Knockdown。策划在 ImpactEffectSO 上直接配 |
| `staggerValue` | float | 资产配置 | 冲击值。当前仅用于 Resistance=Ceil()，后续用于霸体比较 |
| Knockdown Resistance | int | `int.MaxValue`（死亡）/ `Ceil(staggerValue)`（Impact） | 保证死亡不可打断 |
| GetUp Resistance | int | 0 | 起身最弱，任意受击可打断 |
| FadeIn | float | 0.1s (Flinch/Stagger/Knockdown), 0.2s (GetUp) | 当前硬编码，后续可移至资产 |

### TBD — 待设计
- **霸体阈值**：staggerValue vs 角色霸体值的判定公式。设计原则：重甲角色应对轻攻击有霸体，重攻击仍应触发受击。
- **Stagger→Knockdown 阈值**：当前由 `reactionLevel` 资产显式指定，无需阈值。如果后续改为 staggerValue 自动判定，需定义阈值。

## Player Experience

### 反馈
- **命中瞬间**：角色身体播放受击动画（0.1s fade-in），方向与攻击来源一致
- **轻受击**：短暂的身体晃动，不影响战斗节奏
- **重受击**：明显的身体后仰，玩家感受到"被打中了"
- **击倒**：角色倒地 → 短暂停顿 → 起身。玩家感到脆弱
- **死亡**：角色倒地后不再起身，明确信号"战斗结束"

### 清晰度
- 受击方向与攻击方向一致（背后被攻击 → 向前倒），玩家可直觉判断攻击来源
- 起身动画被攻击可打断——玩家会学到"倒地时最脆弱"
- 死亡倒地不起——明确的失败信号

### 失败状态
- 连续受击可能产生"锁死"效果（受击→起身→受击→起身），设计上此为有意——倒地被围攻是危险的
- 起身 Resistance=0 确保起身时被攻击会重新倒地

## Edge Cases

| 边界情况 | 预期行为 |
|----------|----------|
| 受击同时死亡（伤害杀死 + ImpactEffect 有反应） | OnReaction 先触发受击动画 → OnDamaged 下一帧触发死亡倒地（抢占） |
| 连续多帧命中同一角色 | HitReaction 互打断，最后命中的受击覆盖前面的（DriverArbiter H2） |
| ImpactEffect 缺失（纯伤害无冲击） | OnReaction 直接 return，不触发任何受击动画 |
| LocomotionAnimationSetSO 为 null | OnReaction return，不触发受击（配置错误，log 由基础设施层处理） |
| 起身过程中被攻击 | ChainGetUp 的 Resistance=0 被新受击的 >0 Resistance 抢占 |
| MixerTransition2D 未配置动画 | HitReactionDriver 正常调用 Play，Animancer 内部处理空 mixer |
| HP 刚好归零（0） | ≤0 判定触发死亡倒地 |
| 角色在开启技能动画时死亡 | HitReaction 抢占 Ability，技能被 OnInterrupted，死亡动画播放 |

## A测 Scope

### A测 交付
- Flinch/Stagger/Knockdown 三级受击动画（资产驱动）
- Impact Knockdown → 起身链
- HP=0 倒地不起
- HitReaction DriverType 级别抢占规则

### 推迟
- 霸体/护盾系统（PropertyTable 接入后）
- 死亡系统：Ragdoll + EventHub 广播
- 受击音效集成
- 击退位移（knockbackForce 字段已预留）
- 受击 VFX（屏幕震动/闪红/粒子）
