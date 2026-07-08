# 2026-07-08 — S5 道具经济数据落地 + 伤害管道接通

## Background

Phase 5 需要完成 S5 闭环基础道具数据。之前 `equipment_all.json`、`ammo_all.json`、`consumable_all.json` 仅有空壳测试数据（8 武器 + 3 弹药 + 4 消耗品），防具完全空白，武器无具体型号和属性覆写，弹药仅口径基型无 AP/HP/Subsonic 弹种，消耗品匮乏。同时伤害管道未接通——近战武器 `Weapon/ATK` 无 DamageEffectSO 引用导致 `MeleeWeaponSO.GetDamageEffects()` 返回 null；`RangedWeaponSO.GetDamageEffects()` 返回硬编码 `baseValue=10` 假数据，弹药属性不生效。

这是 S5 道具经济的首次全量落地，见 plan `squishy-jumping-sketch.md`。

## Changes

### 道具数据落地
- 新增 **49 件成品道具**：防具 10 + 容器/背包 3 + 近战武器 6 + 热武器 7 + 弹药变种 12 + 消耗品 11
- 3 个 JSON 数据文件全量重构：`equipment_all.json`(26)、`ammo_all.json`(12)、`consumable_all.json`(11)
- 删除旧测试数据：8 个通用武器条目 + 3 个弹药口径空壳 + 2 个 Material
- 两套全身防具：拾荒者轻甲套装 + 战术重甲套装（各 Head/Chest/Legs/Feet/Hands 五件）
- 弹药 4 口径 × 3 弹种：9mm(FMJ/JHP/Subsonic)、5.56(FMJ/AP/HP)、7.62(FMJ/AP/Subsonic)、12ga(Buck/Slug/Breach)

### 伤害管道接通
- 新建 **4 个 Ballistic DamageEffectSO**：`Dmg_Ballistic_9mm`(22)、`Dmg_Ballistic_556`(45)、`Dmg_Ballistic_762`(55)、`Dmg_Ballistic_12ga`(80)
- `AmmoBase.asset` PropertyTree 加 `Weapon/ATK` 节点（AssetRefList 类型）
- `AmmoSO.cs` — 覆写 `GetDamageEffects()` 读 `Weapon/ATK`（与 MeleeWeaponSO 同逻辑）
- `RangedWeaponSO.cs` — 修复 `GetDamageEffects()`：沿 `NestedContainer` 链递归查找弹药 Entity，返回其 DamageEffectSO
- 近战武器 `Weapon/ATK` 复用现有 Slash(15)/Blunt(12) DamageEffectSO

### 编辑器改进
- `EntityImporter.cs` — 新增 `GetSubDirectory()`：多类型按 entityType 分子目录，单类型按 templateName 分子目录；自动创建子目录
- `PropertyImportExport.cs` — 已有 Tree 支持 update（原只创建不更新），通过对比 treeJson 判断是否需要写入
- `tags_all.json` — 补全 379 个 Tag 的 `fullTag` 字段（原为空，依赖运行时 RefreshCache）

### Prefab 资产映射
- 全部 Prefab GUID 改为 `.prefab` GUID（初版误用 `.fbx` 模型文件 GUID）
- 19 个 PolygonApocalypse Prefab 映射：武器 13 + 背包 3 + 弹药 4（口径盒子）+ 消耗品 10
- 10 件防具 Prefab 留空（PolygonApocalypse 无成套盔甲）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 配件系统延后，武器自带内建配件 | A: 立即实现独立弹匣/瞄具/消音器物品+装卸 UI → 实现复杂度高，需新增 Attachment Entity 类型和 PropertyTree。B: 完全忽略配件 → 丢失枪械定制玩法。 | 内建配件折中——枪身属性覆盖全部配件效果，玩法完整但实现简洁。完整系统延后到增量阶段。 |
| 子弹按口径独立建 DamageEffectSO，不复用通用 Pierce(12) | A: 复用 `Damage_Physical_Pierce`(12) → 各口径伤害无差异。B: 每弹种建独立 DamageEffectSO → 资产过多。 | 按口径建 4 个，弹种差异通过 PropertyTree `Combat/BaseDamage` 覆写。平衡了精度和资产数量。 |
| 防具全部 prefabGuid 留空 | A: 映射 PolygonApocalypse 护肩/护膝等零散部位件 → 不匹配完整防具概念。B: 不创建防具 → 玩法缺口。 | 属性生效无视觉模型，等模块化角色系统上线后统一处理 Prefab。 |
| 旧测试数据全删 | A: 保留与新增共存 → 名称冲突（HuntingRifle），冗余条目。 | 新 26 条目已覆盖全部类型，旧数据无保留价值。 |
| EntityImporter 按 entityType 自动分子目录 | A: 全部平铺在 DataRoot → 查找困难。B: JSON 的 name 字段支持路径分隔符 → 侵入数据层。 | 在 Importer 层处理，entityType/templateName 自动映射子目录，数据层保持简洁。 |

## Known Issues

- [ ] 防具模型缺失（10 件无 Prefab）— P1 — 等模块化角色系统上线后填入
- [ ] `RangedWeaponSO` 弹药查找依赖 `NestedContainer` 链，当前弹药直接装填到武器容器，未测试含独立弹匣 Item 的嵌套场景 — P2
- [ ] 7.62 和 5.56 共用 `RifleAmmo` PropertyTree 模板，结构相同但 DamageEffectSO 不同 — P3 — 远期可按口径拆模板

## Cross-References

### Related Plans
- [../plans/squishy-jumping-sketch.md](../plans/squishy-jumping-sketch.md) — S5 道具经济实施计划

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-character/L4-equipment/character-equipment.md](../tech/L2-services/L2-modules/L3-character/L4-equipment/character-equipment.md) — CharacterEquipment 装备 GO 生命周期
- [../tech/L2-services/L2-modules/L3-stats/tree/equipment-tree-design.md](../tech/L2-services/L2-modules/L3-stats/tree/equipment-tree-design.md) — StatsTree 装备/弹药/防具层级
- [../tech/L2-services/L2-modules/L3-properties/property-tree-equipment.md](../tech/L2-services/L2-modules/L3-properties/property-tree-equipment.md) — PropertyTree Equipment 子树

### Flag for Design Doc Creation
- [ ] No design doc needed — this session was data implementation of already-designed item economy system as documented in equipment-tree-design.md and property-tree-equipment.md. New item economy design doc planned for future session.
