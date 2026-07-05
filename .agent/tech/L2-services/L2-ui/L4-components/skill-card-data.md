# SkillCardData — 技能卡展示数据结构

> **Source**: `Assets/Scripts/Services/L2_UI/Components/SkillCardData.cs`
> **Last Verified**: 2026-07-06 | **Verification**: All referenced files exist, signatures match code

从 `ActiveAbilitySO` 提取纯展示数据的 struct。效果文本在工厂方法中预格式化为字符串数组，UI 组件不接触 EffectSO 子类。

## Call Chain

```
ActiveAbilitySO (ability data)
  → SkillCardData.FromActiveAbility(def)  // 提取 + 预格式化
  → SkillCard.SetData(data)               // UI 渲染
```

## Coupled Modules

| Direction | Module | Relationship |
|-----------|--------|-------------|
| Consumed by | `SkillCard` | SetData() 接收 |
| Reads from | `ActiveAbilitySO`, `AbilityActivationSO`, `AbilitySearchSO`, `NoiseEventSO` | 数据源（均为 SO 引用） |
| Reads from | `EffectSO` subclasses: `DamageModifierEffectSO`, `ImpactEffectSO`, `CostEffectSO`, `BuffEffectSO` | 效果文本格式化 |

## Public Properties

All fields are public readonly (struct).

| Group | Fields | Type |
|-------|--------|------|
| Identity | `icon`, `displayName`, `description` | `Sprite`, `string`, `string` |
| Cooldown | `cooldownDuration` | `float` |
| Activation | `activationTypeLabel`, `animationLayerLabel`, `windupDuration`, `fireWindowDuration`, `recoveryDuration`, `animationSpeed`, `canCancelWindup`, `canCancelRecovery` | `string`×2, `float`×4, `bool`×2 |
| Search | `searchTypeLabel`, `searchRange` | `string`, `float` |
| Effects | `damageModifiers[]`, `impactText`, `costs[]`, `buffs[]` | pre-formatted `string[]` |
| Combo | `comboLinks[]` | pre-formatted `string[]` |
| Noise | `noiseLevel`, `noiseDecayRadius` | `int`, `float` |
| Queries | `HasEffects`, `HasCombo`, `HasNoise` | `bool` |

## Methods

### FromActiveAbility()
```csharp
public static SkillCardData FromActiveAbility(ActiveAbilitySO def)
```
- **Purpose**: 工厂方法。从 ActiveAbilitySO 递归提取所有子资产数据，预格式化效果文本
- **Params**: `def` — 技能定义 SO，null 返回 default
- **Returns**: 填充完整的 SkillCardData
- **Callers**: `AbilityBarOverlay.OnSlotHover()`
- **Internal**: 调用 `ActivationTypeLabel()`, `AnimationLayerLabel()`, `SearchTypeLabel()`, `KnockbackLabel()`, `ExtractEffects()` 等 private helper

## Internal Mechanics

纯数据 struct + static factory，无状态、无 MonoBehaviour、无 Unity 生命周期。效果提取通过 `switch (effect)` 模式匹配 EffectSO 子类，各子类格式化规则内联在 `ExtractEffects()` 中。

## Future Plans

| Plan | Status | Source |
|------|--------|--------|
| PassiveAbilitySO 支持 | 待做 | PassiveAbilitySO 无 activation/search/combo 字段，需单独处理 |
| 运行时冷却剩余覆盖 | 待做 | 当前只传 cooldownDuration，不含 remaining |
