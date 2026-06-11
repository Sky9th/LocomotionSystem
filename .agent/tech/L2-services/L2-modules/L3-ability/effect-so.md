# EffectSO — 效果抽象基类

> `L3_Ability/Config/Effect/EffectSO.cs` · 抽象基类 · 2026-06-11

## 定位

所有游戏效果（Damage/Impact/Execute/Cost/Buff）的统一基类。对标 UE GAS `UGameplayEffect`。

## 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `effectTag` | `GameplayTagDefinitionSO` | 效果身份标签。路由防�公式/AI/VFX |
| `description` | `string` | 策划可读说明文本 |
| `duration` | `float` | ≤0=瞬时，>0=持续 tick |
| `stackable` | `bool` | 是否可叠加 |
| `maxStacks` | `int` | 最大叠加层数 |
| `applicationBlockedTags` | `GameplayTagDefinitionSO[]` | 施加条件。任意匹配则拒绝 |

## 子类

| 子类 | 效果类型 | JSON type |
|------|---------|-----------|
| `DamageEffectSO` | 伤害（瞬时/DoT） | `"Damage"` |
| `ImpactEffectSO` | 硬直+击退 | `"Impact"` |
| `ExecuteEffectSO` | 斩杀 | `"Execute"` |
| `CostEffectSO` | 资源消耗/恢复 | `"Cost"` |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| BuffEffectSO | Phase 5+ | effect-inventory.md §5.3 |
| CleanseEffectSO | Phase 5+ | effect-inventory.md §5.2 |
| HealEffectSO | Phase 5+ | effect-inventory.md §5.1 |
