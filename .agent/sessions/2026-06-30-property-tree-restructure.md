# 2026-06-30 — PropertyTree 全族重构

## Background

PropertyTree 继承结构此前存在多处问题：WeaponBase 仅有 Durability/MaxDurability 两个属性而大量战斗属性分散在 MeleeWeapon/RangedWeapon 叶子层；Presentation/Behavior 在 6 个叶子树重复定义；ContainerBase 作为独立分支不合理（槽位是 Entity 通用能力）；rTag 命名不理想。同时 PropertyDefSO 基类臃肿承载所有类型的字段，PropertyTable.DoWrite 存在巨型 switch-case。本次会话系统性地解决了这些问题。

触发：用户审查 WeaponBase 属性树，发现结构问题后逐层展开至全族。

## Changes

### L3_Properties 代码层
- PropertyDefSO 基类瘦身，按 PropertyType 拆分为 9 个子类（FloatPropertyDefSO/IntPropertyDefSO/.../StructPropertyDefSO）
- PropertyTable.DoWrite 瘦身：ComputeWriteValue 由 Def 子类多态分发，简单类型走 WriteSimpleTyped 统一路径
- OverrideEntry 新增 Min/Max 字段，ParseOverrides 填入 _minOverrides/_maxOverrides 字典
- PropertyType.rTag/rTagList → RdTag/RdTagList 全量改名
- RTagPropertyDefSO/RTagListPropertyDefSO → RdTagPropertyDefSO/RdTagListPropertyDefSO
- Class `PropertyDefSO`, `PropertyTable`, `PropertyDefSOEditor`, `PropertyImportExport`, `PropertyTreeEditorPopups` 全部同步更新

### L3_Item 代码层
- ItemDefSO.cs 清理过期注释（MaxDurability/DamageType 引用）

### Properties 设计层（7 个分支文档）
- `property-tree-structure.md` — 完整继承树（Entity→Equipment→WeaponBase/ArmorBase/ToolBase + Actor/Ammo/Consumable/Building/Environment）
- `property-tree-equipment.md` — Equipment/WpnBase/ArmorBase/ToolBase 逐层详述
- `property-tree-actor.md` — Human 16 抗性 + 21 熟练度 + Hygiene，含设计决策/远期规划
- `property-tree-ammo.md` — AmmoBase 弹药物理特性，ShotgunShell +PelletCount +Spread
- `property-tree-consumable.md` — Food/Medical/Material + Seed/RepairKit 新子树
- `property-tree-building.md` — Insulation/MaterialType/Flamability 等修正
- `property-tree-environment.md` — Radiation/Wind/Precipitation/Season 等新增

### Properties 数据层
- `properties_all.json` v2.1 全族对齐：159 Float / 36 Trees / 零孤立引用
- 所有树 `.asset` 文件同步更新（Entity/Equipment/WeaponBase/ArmorBase/ToolBase + 全部叶子）
- NightVision/FlashResist DefaultFloat 100→0（越界修复）
- MaterialTier Max 100→6，MaterialType_Building Max 100→4
- BodyArmor StanceStability DefId CarryWeight→CarryWeightBonus
- Backpack 树恢复（Equipment 叶子，Slots/ContainerSlot）
- ContainerBase 树删除

### Entity Preset 资产层
- Zombie.asset 模板 GUID 修正（Human 树→Zombie 树）
- Backpack.asset 删除（树已不存在）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Equipment 层引入（Entity→Equipment） | A: 不做，Durability+表现层分别在 WeaponBase/ArmorBase/ToolBase 重复定义 | Equipment 统一消除三处重复，表达"所有装备物品"的 is-a 关系 |
| WeaponBase 属性压缩至 5 个（ATK/AttackSpeed/AttackRange/NoiseRadius/IsTwoHanded） | A: 保留 StaminaCost/CritChance/CritMulti/StunChance/Knockback 共 10 个 | EffectSO 体系（CostEffectSO/ImpactEffectSO/DamageEffectSO.modMult）已覆盖伤害/消耗/冲击，不应平铺为叶子属性 |
| MaxDurability 删除 | A: 保留独立属性支持 Buff 修改上限 | OverrideEntry 已支持 Min/Max 覆写，Durability.Max 替代 |
| ContainerBase 删除 | A: 保留独立分支 | Slots/ 是 Entity 通用能力，万物都可有槽位。背包是 Equipment 叶子 |
| rTag→RdTag 全量改名 | A: 保持 rTag | Rd 前缀防止与 Unity 内置类型冲突，命名惯例 |
| DamageType 删除 | A: 保留为武器独立属性 | DamageEffectSO.effectTag 已覆盖伤害类型路由，Def 本身也不存在（僵尸节点） |
| Recoil 重构为 Ammo.RecoilFactor + Firearm.RecoilModifier | A: 保留 RangedWeapon 单一 Recoil | 后座力由弹药冲量和枪械设计共同决定，分拆符合物理模型 |
| 6 分支各配子 Agent 交叉审核 | A: 手动逐文件审 | 5 个并行 Agent + 两轮审核，共发现 ~88 条问题，覆盖率远超人工 |
| No design doc — all changes are internal data model restructuring, no player-facing mechanic changes | — | PropertyTree 是数据结构层，策划不可见 |

## Known Issues

- [ ] 运行时 PropertyTable 报 `Movement/Acceleration`、`Vitals/Hunger` 等路径不存在 — 根因可能是 PropertyDefinitionRegistry 缓存未刷新（Import 跳过已存在的树 ? 还是注册表提前初始化）。临时方案：Unity Editor 中调用 `PropertyDefinitionRegistry.Invalidate()` 后重新 Play。P0
- [ ] NoiseRadius 最终层级（WeaponBase vs RangedWeapon）延后决策
- [ ] ArmorBase: Insulation/NoiseLevel/WaterResist 延后
- [ ] ToolBase: WearRate/RepairMaterialTier/StaminaCostPerUse vs CostEffectSO 矛盾 延后
- [ ] 箭矢弹药 (ArrowBase) 延后
- [ ] MeleeWeapon 层空壳化—保留为分类层但无独有属性，未来加 BlockRate/ParryWindow
- [ ] Blunt/Polearm/Spear 空叶子—保留为分类层，纯数值差异走 Spawn Config

## Cross-References

### Related Sessions
- [2026-06-10-property-inventory-design.md](2026-06-10-property-inventory-design.md) — 早期的 Property Inventory 设计，本次重构的基础

### Related Tech Docs
- [tech/.../property-tree-structure.md](../tech/L2-services/L2-modules/L3-properties/property-tree-structure.md) — 全族继承枝干
- [tech/.../property-tree-equipment.md](../tech/L2-services/L2-modules/L3-properties/property-tree-equipment.md) — Equipment 子树详述
- [tech/.../property-tree-actor.md](../tech/L2-services/L2-modules/L3-properties/property-tree-actor.md) — Actor 子树
- [tech/.../property-tree-ammo.md](../tech/L2-services/L2-modules/L3-properties/property-tree-ammo.md) — Ammo 子树
- [tech/.../property-tree-consumable.md](../tech/L2-services/L2-modules/L3-properties/property-tree-consumable.md) — Consumable 子树
- [tech/.../property-tree-building.md](../tech/L2-services/L2-modules/L3-properties/property-tree-building.md) — Building 子树
- [tech/.../property-tree-environment.md](../tech/L2-services/L2-modules/L3-properties/property-tree-environment.md) — Environment 子树
- [tech/.../property-inventory.md](../tech/L2-services/L2-modules/L3-properties/property-inventory.md) — 原始 Property Inventory（待同步更新）
- [tech/.../property-def-so-subclasses.md](../tech/L2-services/L2-modules/L3-properties/property-def-so-subclasses.md) — PropertyDefSO 子类化文档

### Related Design Docs
- None — all changes are internal data model restructuring, no player-facing mechanic changes.

### Flag for Design Doc Creation
- [ ] No design doc needed — PropertyTree 是数据结构层重构，不涉及玩家可见行为变化。
