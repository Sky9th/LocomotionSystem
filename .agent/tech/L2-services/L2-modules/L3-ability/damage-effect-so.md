# DamageEffectSO — 伤害效果契约

> `L3_Ability/Config/Effect/DamageEffectSO.cs` · 2026-06-11

## 定位

伤害效果的**共享契约资产**。装备和 Ability 共用同一份 `.asset`。装备负责填 `baseValue`（物理地基），Ability 负责填 `modAdd`/`modMult`/`priority`（动作修正）。

## 字段

| 字段 | 类型 | 默认值 | 谁填 | 语义 |
|------|------|:---:|------|------|
| `effectTag` | GameplayTag | — | 设计 | 继承自 EffectSO。伤害类型路由键（如 `Damage.Physical.Slash`） |
| `description` | string | — | 设计 | 继承自 EffectSO。策划可读说明 |
| `duration` | float | — | 设计 | 继承自 EffectSO。≤0=瞬时，>0=每 tick DoT |
| `baseValue` | float | 0 | **装备** | 伤害基底。刀=8，子弹=12 |
| `modAdd` | float | 0 | **Ability** | 加法修正。优先级内先执行 |
| `modMult` | float | 1.0 | **Ability** | 乘法修正。加法后执行 |
| `priority` | int | 0 | **Ability** | 同 effectTag 多 Effect 时的执行顺序 |

## 管道计算

```
⑤ Effects:
  foreach effect in ability.effects (sorted by priority):
    baseVal  = equipment.GetBaseValue(effect.effectTag)   // 装备填
    addenda  = effect.modAdd                                // 技能填
    multiply = effect.modMult                               // 技能填
    hit.IncomingDamage = (baseVal + addenda) × multiply
```

## 资产数量

29 个 DamageEffectSO，按 damageType 分 4 组：

| 分组 | 数量 | effectTag 示例 |
|------|:---:|------|
| Physical | 9 | Slash, Blunt, Pierce, Bite, Explosive, Crush（+Heavy 变体） |
| Elemental | 12 | Fire, Cold, Shock, Acid, Poison, Radiation（+Strong 变体） |
| Biological | 5 | Bleed, Disease, Suffocation（+Strong/Virulent 变体） |
| Special | 3 | True, True_Strong, Fall |

## JSON 导入

`effects_all.json` → `EffectImportWindow` → `.asset`。DTO 字段：`baseValue`/`modAdd`/`modMult`/`priority`。

## 与旧版对比

| | 旧 (v0.10.x) | 新 (v0.11.1+) |
|---|---|---|
| 伤害值 | `baseDamage`（写在 asset 上） | `baseValue`（装备运行时填入） |
| 技能修正 | 无 | `modAdd` + `modMult` + `priority` |
| 穿甲 | `armorPenetration` | 移到装备侧 StatsTree |
| 护盾穿透 | `shieldPenetration` | 暂移除 |
| 伤害界限 | `minDamage`/`maxDamage` | 暂移除 |

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被引用 | GearDefSO（outputEffects） | 装备拖拽引用，填 baseValue |
| 被引用 | AbilityDefSO（effects） | Ability 拖拽引用，填 modAdd/modMult |
| 消费 | Ability Pipeline ⑤ Effects | 读取后计算 hit.IncomingDamage |
| 导入 | EffectImportExport | JSON ↔ .asset |
