# DamageModifierEffectSO — 技能伤害修正器

> `L3_Ability/Config/Effect/DamageModifierEffectSO.cs` · 2026-07-05
> **Last Verified**: 2026-07-05 | **Verification**: File exists, signatures match code

## 定位

伤害修正效果。**只来自技能**，按 `targetTag` 匹配实体伤害通道并修正。

与 `DamageEffectSO` 类型拆分：DamageEffectSO 是实体伤害通道（baseValue），DamageModifierEffectSO 是技能修正（modAdd/modPercent）。

## 字段

| 字段 | 类型 | 默认值 | 语义 |
|------|------|:---:|------|
| `effectTag` | RdTagDefSO | — | 继承自 EffectSO。修正器自身标签 |
| `targetTag` | RdTagDefSO | — | 要修正哪个 tag 的实体通道 |
| `modAdd` | float | 0 | 固定值加成（乘法后叠加） |
| `modPercent` | float | 0 | 百分比加成（0.5=+50%，多 modifier 加法叠加） |
| `priority` | int | 0 | 同 targetTag 多 modifier 时的叠加顺序 |
| `duration` | float | — | 继承自 EffectSO。≤0=瞬时，>0=持续修正 |

## 伤害公式

```
outgoing = baseValue × (1 + ΣmodPercent) + ΣmodAdd
```

- 多 modifier 对同一通道：百分比**加法叠加**，避免 ×3×4 爆炸
- 固定值 `modAdd` 在乘法后叠加，不受百分比加成影响

## Tag 匹配

使用 `RdTag.IsAncestorOf()` 层级匹配：
- modifier `Damage.Physical.Slash` 匹配通道 `Damage.Physical.Slash.Heavy` ✅
- modifier `Damage.Physical` 匹配通道 `Damage.Physical.Pierce` ✅
- modifier `Damage.Elemental.Fire` 不匹配 `Damage.Physical.Slash` ✅

## 管道位置

```
ExecutionState.BuildDamageInfo:
  ability.targetEffects.OfType<DamageModifierEffectSO>() → CollectDamageModifiers()
  → for each 实体通道, MatchTag(mod.targetTag, channel.effectTag)
```

## 资产

19 个 DamageModifierEffectSO（DamageMod_*）：
- Physical Heavy 变体: Slash(+50%), Blunt(+50%), Pierce(+50%)
- Elemental 变体: Fire(+20%/+150%), Cold(+20%/+150%), Shock(+20%/+150%), etc.
- Biological 变体: Bleed(+150%), Disease(+50%/+150%)
- Special: True(+200%)

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被引用 | AbilitySO.targetEffects | EffectSO 多态数组中的一员 |
| 消费 | ExecutionState.CollectDamageModifiers | OfType 过滤后参与 tag 匹配 |
| 导入 | EffectImportExport | JSON ↔ .asset，effectType="DamageMod" |
