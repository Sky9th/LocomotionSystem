# Session: Entity 分类四层架构对齐

**Date:** 2026-07-08
**Version:** 0.41.0

## Background

Tag 树、PropertyTree、SO 类层级、Data/Entities 目录四层对 Entity 的分类互不一致。Tag 树把战斗机制和背包槽位两个正交维度揉进同一层（`Entity.Weapon.*` vs `Entity.Item.*`），SO 类层级把 Armor 和 Material 塞进同一个 `PropDefSO`，Data 目录把 Weapon 独立但 Armor 归入 Props。PropertyTree 的分类（Actor/Equipment/Ammo/Consumable/Building/Environment）是唯一正确的设计，其余三层需要追上。

更深层的问题是：SlotDef AcceptTags 已经使用了目标命名（`Entity.Armor.Head`），但这些 Tag 在旧树中根本不存在（实际是 `Entity.Item.Armor.Head`），导致 `ContainerSlot.AcceptsTag()` 的前缀匹配永远不命中——所有护甲槽位过滤完全不工作。

## Changes

### Tag 树
- `Entity.Weapon/*` + `Entity.Item/*` → `Entity.Equipment/Weapon/*` + `Entity.Equipment/Armor/*` + `Entity.Equipment/Tool` + `Entity.Equipment/Container`
- 新增 `Entity.Ammo`、`Entity.Consumable`（含 Food/Medical/Material）、`Entity.Actor`（含 Human/Zombie）、`Entity.Building`、`Entity.SceneItem`
- 新增 `Entity.Equipment.Armor.Shield` tag（原 SlotDef 引用但 Tag 树中不存在）
- 删除 `Entity.Item`（维度合并）、`Entity.Item.Weapon`（双挂标签）、`Entity.Item.Component`（无消费者）
- `tags_all.json` v2.0 → v2.1

### PropertyTree SlotDef AcceptTags
- 9 个 StructDef 的 AcceptTags 对齐新 Tag 路径：`Entity.Armor.Head` → `Entity.Equipment.Armor.Head`，`Entity.Armor.Body` → `Entity.Equipment.Armor.Chest`，`Entity.Armor.Leg` → `Entity.Equipment.Armor.Legs`，`Entity.Armor.Foot` → `Entity.Equipment.Armor.Feet`，`Entity.Equipment.Backpack` → `Entity.Equipment.Container`
- `properties_all.json` 更新并重新导入

### SO 类层级
- 删除 `PropDefSO`（L3_Prop 的中间抽象类）
- 新建 `EquipmentDefSO`、`AmmoDefSO`、`ConsumableDefSO` 三个 intermediate abstract class
- `WeaponDefSO` 改继承 `EquipmentDefSO`（原继承 `PropertyPresetSO`）
- `ArmorSO`/`ToolSO`/`ContainerSO` 改继承 `EquipmentDefSO`
- `AmmoSO` 改继承 `AmmoDefSO`
- `ConsumableSO`/`MaterialSO` 改继承 `ConsumableDefSO`

### L3 模块
- 删除 `L3_Weapon/`（5 cs）、`L3_Prop/`（9 cs）、`L3_Item/`（1 cs，已废弃）
- 新建 `L3_Equipment/`（8 cs）、`L3_Ammo/`（4 cs）、`L3_Consumable/`（5 cs）
- 每个模块含 EditorWindow + ImportWindow 各一

### Data/Entities 目录
- 6 个顶级目录统一单数：`Character`/`Equipment`/`Ammo`/`Consumable`/`Building`/`SceneItem`
- `Weapons/` → `Equipment/Weapon/`，`Props/` 拆入 `Equipment/` + `Ammo/` + `Consumable/`
- 6 个 `_all.json` 文件 category 字段更新，移到新目录
- 删除旧 `items_all.json`、`props_all.json`

### Namespace
- `RedDust.Weapon` → `RedDust.Equipment`
- `RedDust.Prop` → `RedDust.Equipment` / `RedDust.Ammo` / `RedDust.Consumable`
- `AssetService.cs`、`PlayerService.cs` 的 using 和类型引用更新

### Editor
- `TagDomainFilter.cs` 常量更新（`ENTITY_WEAPON`→`ENTITY_EQUIPMENT_WEAPON`）
- 5 个 EntityEditorWindow 子类全部更新（DataRoot 改单数、AssetFilter 改新类型名）
- 6 个 Entity ImportExport + 1 个 Animation ImportExport 添加默认文件路径

### 配置数据
- `abilityTrees_all.json`：compatibleWeaponTags 字符串更新
- `animation_all.json`：weaponTypeTag 字符串更新
- `Blade.asset`、`Pistol.asset`：Tag overrides 更新

## Decisions

| 决策 | 选择 | 被拒绝的方案 |
|------|------|------------|
| Entity 分类标准 | 以 PropertyTree 分类为唯一真相来源 | 以 Tag 树或 SO 层级为准 |
| L3_Character 改名 L3_Actor？ | 不改 — 62 文件太大，Character 是系统名 | 改名对齐 Tag 树 |
| Equipment 是一个 EditorWindow 还是拆分？ | 合为一个 EquipmentEditorWindow（5 种子类型） | 拆 Weapon + Armor 两个窗口 |
| Data 目录名单数还是复数？ | 统一单数（Character/Equipment/...） | 沿用旧复数名 |
| 中间 SO 类要写行为吗？ | 空 marker class，纯类型区分 | 在基类加 shared logic |
| 旧 Entity 数据 Tag 更新 | .asset 由 Unity Editor 重新导入时自然更新 | 手动改 YAML 转义 |

## Known Issues

- AbilityTree `.asset` 文件的 compatibleWeaponTags 存的是 RdTagDefSO GUID 引用——旧 Tag 的 `.asset` 文件若未删除，GUID 不会断但 FullTag 已变，运行时 `HasTag()` 匹配可能失败。需在 Editor 中重新指定或通过 JSON 重新导入 AbilityTree。
- `EntityImportConfig.cs` 和 `EntityEditorWindow.cs` 中 4 处注释仍引用旧示例字符串（`"L3_Weapon"`、`"t:WeaponDefSO"`）。
- `GripAnimationTableSO.cs` 的 tooltip/doc 仍引用 `Entity.Weapon.*`，需更新为 `Entity.Equipment.Weapon.*`。
- Props/ 下旧的 Tag `.asset` 文件（`Entity/Item/*`、`Entity/Weapon/*`）未删除——RdTagImporter 不支持删除，需手动清理。

## Cross-References

- Plan: `plans/tag-tag-iridescent-hopcroft.md`
- Tech: `tech/L1-core/gameplay-tag-entity.md`
- Tech: `tech/L2-services/L2-modules/L3-equipment/`
- Tech: `tech/L2-services/L2-modules/L3-ammo/`
- Tech: `tech/L2-services/L2-modules/L3-consumable/`
- Version: `versions/v0.41.0.md`

### Flag for Design Doc Creation
- [x] No design doc needed — internal architecture refactoring, no player-facing behavior change.
