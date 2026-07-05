# CharacterCombat · 战斗中枢

> **Last Verified**: 2026-07-05 | **Verification**: All referenced files exist, signatures match code

> `Character/Combat/CharacterCombat.cs` — `internal sealed class ModuleChild`，桥接 Ability 管道与 Character 属性系统。注册到 AbilityReactor 的回调管线，消费 SDamageInfo → 伤害结算 + 受击动画 + 死亡判定。

## 调用链

```
AbilityReactor.Resolve(SDamageInfo)
  → ResolutionCallback → OnResolveDamage()   // 防御公式
  → ApplyDamageCallback → OnApplyDamage()     // HP 落地
  → ReactionCallback → OnReaction()           // 受击动画
  → OnDamagedCallback → OnDamaged()           // 死亡判定

OnReaction():
  → ctx.ResolvedLocoAnimSet 取受击动画
  → impact.reactionLevel switch → Flinch/Stagger/Knockdown mixer
  → WorldToLocalDirection(hit.HitDirection, ctx.Root) → blend parameter
  → 构建 AnimationRequest(DriverType=HitReaction) → ctx.Animation.SubmitRequest()
  → if Knockdown: request.OnCompleted = _ => ChainGetUp()

OnDamaged():
  → HP<=0 → 构建 AnimationRequest(hitReactionKnockdown, Resistance=int.MaxValue)
  → 无 OnCompleted（不起身，停在倒地 pose）

ChainGetUp():
  → 构建 AnimationRequest(hitReactionGetUp, Resistance=0) → SubmitRequest
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | AbilityReactor | 注册回调：Resolution/ApplyDamage/Reaction/OnDamaged |
| 依赖 | CharacterBuildContext | 读取 Properties / ResolvedLocoAnimSet / Animation |
| 依赖 | AnimationBrain | SubmitRequest 提交受击动画 |
| 依赖 | SDamageInfo | 读取 ImpactEffect / HitDirection |
| 依赖 | LocomotionAnimationSetSO | 选择 hitReactionFlinch/Stagger/Knockdown/GetUp |

## 关键设计

| 决策 | 理由 |
|------|------|
| 受击等级由 `ImpactEffectSO.reactionLevel` 决定 | 资产驱动，策划直接控制 |
| 无霸体判定 | 有 ImpactEffect 即触发，TODO: PropertyTable 霸体值 |
| Impact Knockdown → 起身链 | `OnCompleted` 链式提交，复用仲裁 |
| HP≤0 倒地不起 | 停在倒地 pose，死亡系统后续实现 |
| HitDirection 取反 | 伤害飞行方向 → 冲击来向（受击反应正确方向） |

## 方法

### OnReaction(SDamageInfo, float)
- **Purpose**: 受击动画入口。无霸体判定，直接根据 reactionLevel 选动画
- **Callers**: `AbilityReactor.ReactionCallback`

### OnDamaged(SDamageInfo, float)
- **Purpose**: 死亡判定入口。HP≤0 → 倒地不起
- **Notes**: 死亡系统（Ragdoll + EventHub）预留，后续 task

### ChainGetUp()
- **Purpose**: 起身动画请求。Resistance=0，可被任何受击打断
- **Callers**: `OnReaction`（Knockdown.OnCompleted）

### WorldToLocalDirection(Vector3, Transform)
- **Purpose**: 世界 HitDirection → 本地 blend parameter (X=右, Y=前)
- **Notes**: 取反 HitDirection（伤害方向 → 冲击来向）

### OnResolveDamage(SDamageInfo) / OnApplyDamage(SDamageInfo, float)
- 防御公式 + HP 落地。无改动。

## 未来规划

| 计划 | 状态 | 来源 |
|------|------|------|
| 霸体阈值判定 | P1 | session 2026-07-05 |
| 死亡系统（Ragdoll + EventHub） | P0 | session 2026-07-05 |
| Flinch/Stagger 方向动画 | P2 | — |
