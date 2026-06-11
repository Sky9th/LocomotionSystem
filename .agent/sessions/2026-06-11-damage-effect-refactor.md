# 2026-06-11 — DamageEffectSO 重构 + Effect/Tag 全量重建

## 做了什么

- **DamageEffectSO** 彻底重构：删除 `baseDamage`/`armorPenetration`/`shieldPenetration`/`minDamage`/`maxDamage`，替换为 `baseValue`/`modAdd`/`modMult`/`priority`。作为装备和 Ability 的共享契约。
- **EffectSO** 和 **GameplayTagDefinitionSO** 新增 `description` 字段
- **EffectImportExport** DTO 和 ApplyFields 适配新字段
- **TagImportExport** 修复 GetDepth（改用 dot 计数替代 FullTag 匹配），支持 description 读写
- **旧 Stats 系统**（`Assets/Data/Stats/`）全部删除，被 Properties 替代
- **Tags** 从 JSON 全量重建（228 个），新增 Damage/Impact/Execute 三个树
- **Effects** 从 JSON 全量重建（51 个：29 Damage + 5 Impact + 3 Execute + 14 Cost）
- **AbilityExecutor** 旧 baseDamage 代码注释，后续重写
- **PropertyImportExport** 默认路径改为 `properties_all.json`

## 已知问题

- AbilityExecutor 中 Damage 相关代码已注释，装备→Ability 接口尚未在管线中落地
- Properties 的 ATK 属性（AssetRefList）存储 DamageEffectSO 引用，但 baseValue 每 Effect 不同的存储方案尚未最终确定
- DamageEffect 的 modAdd/modMult/priority 字段在 Ability 侧的实际使用尚未实现

## 设计结论

- 装备填 baseValue，Ability 填 modAdd/modMult，管线按 effectTag 匹配后计算
- 同 effectTag 多个 Effect 按 priority 有序叠加
- StatsTree 不按伤害类型平铺 stat——伤害类型是 DamageEffectSO 的事
- 复杂修正走 IEffectModifier 钩子
- 单值属性（硬直/攻速/精度等）走 StatsTree，多通道伤害走 outputEffects
