# EntityEditor 子类 — 6 模块配置速查

> **基类**: `EntityEditorWindow` — 提供完整三栏 UI，子类只覆写配置方法

> **Last Verified**: 2026-07-08

## 子类对照表

| 子类 | 文件 | 命名空间 | AssetFilter | TargetType |
|------|------|---------|-------------|------------|
| `EquipmentEditorWindow` | `L3_Equipment/Editor/EquipmentEditorWindow.cs` | `RedDust.Equipment.Editor` | `t:EquipmentDefSO` | `EquipmentDefSO` |
| `AmmoEditorWindow` | `L3_Ammo/Editor/AmmoEditorWindow.cs` | `RedDust.Ammo.Editor` | `t:AmmoDefSO` | `AmmoDefSO` |
| `ConsumableEditorWindow` | `L3_Consumable/Editor/ConsumableEditorWindow.cs` | `RedDust.Consumable.Editor` | `t:ConsumableDefSO` | `ConsumableDefSO` |
| `BuildingEditorWindow` | `L3_Building/Editor/BuildingEditorWindow.cs` | `RedDust.Building.Editor` | `t:BuildingDefSO` | `BuildingDefSO` |
| `CharacterEditorWindow` | `L3_Character/Editor/CharacterEditorWindow.cs` | `RedDust.Character.Editor` | `t:CharacterDefSO` | `CharacterDefSO` |
| `SceneItemEditorWindow` | `L3_SceneItem/Editor/SceneItemEditorWindow.cs` | `RedDust.SceneItem.Editor` | `t:SceneItemDefSO` | `SceneItemDefSO` |

## Menuitem 清单

| 窗口 | 菜单路径 | Priority |
|------|---------|----------|
| CharacterEditorWindow | `RedDust/Character Editor` | 6 |
| EquipmentEditorWindow | `RedDust/Equipment Editor` | 7 |
| ConsumableEditorWindow | `RedDust/Consumable Editor` | 8 |
| AmmoEditorWindow | `RedDust/Ammo Editor` | 9 |
| BuildingEditorWindow | `RedDust/Building Editor` | 9 |
| SceneItemEditorWindow | `RedDust/Scene Item Editor` | 10 |

## Create 菜单项 (GetCreateMenuItems)

| 模块 | 创建项 |
|------|--------|
| **Equipment** | Melee Weapon (`MeleeWeaponSO`), Ranged Weapon (`RangedWeaponSO`), Armor (`ArmorSO`), Tool (`ToolSO`), Container (`ContainerSO`) |
| **Ammo** | Ammo (`AmmoSO`) |
| **Consumable** | Consumable (`ConsumableSO`), Material (`MaterialSO`) |
| **Building** | Building (`BuildingDefSO`) |
| **Character** | Character (`CharacterDefSO`) |
| **SceneItem** | Scene Item (`SceneItemDefSO`) |

## 资产目录路由

| 模块 | DefaultAssetDir | GetAssetDirForType 覆写? |
|------|----------------|-------------------------|
| Equipment | `Assets/Data/Entities/Equipment` | ✅ 按子类型: MeleeWeapon→`Weapon/Melee`, RangedWeapon→`Weapon/Ranged`, Armor→`Armor`, Tool→`Tool`, Container→`Container` |
| Consumable | `Assets/Data/Entities/Consumable` | ✅ ConsumableSO/MaterialSO 均回退到 Default (平级目录) |
| Ammo | `Assets/Data/Entities/Ammo` | 无 |
| Building | `Assets/Data/Entities/Building` | 无 |
| Character | `Assets/Data/Entities/Character` | 无 |
| SceneItem | `Assets/Data/Entities/SceneItem` | 无 |

## Template 预设 (GetTemplatePresets)

| 模块 | selectedType | 预设列表 (label, assetName) |
|------|-------------|---------------------------|
| **Equipment** | `MeleeWeaponSO` | WeaponBase, MeleeWeapon, Axe, Blade, Blunt, Polearm |
| | `RangedWeaponSO` | WeaponBase, RangedWeapon, Firearm, Pistol, Rifle, Shotgun, Bow, Throwable |
| | `ArmorSO` | ArmorBase, BodyArmor, HeadArmor, LegArmor |
| | `ToolSO` | ToolBase, RepairKit |
| | `ContainerSO` | Backpack |
| **Ammo** | `AmmoSO` (不区分) | AmmoBase, PistolAmmo, RifleAmmo, ShotgunShell |
| **Consumable** | `ConsumableSO` | ConsumableBase, Food, Medical |
| | `MaterialSO` | Material, Seed |
| **Building** | (不区分) | Building |
| **Character** | (不区分) | Actor, Human, Zombie |
| **SceneItem** | (不区分) | Entity, Environment, Equipment |

## 覆写方法速查

所有 6 个子类都覆写了 5 个 abstract 方法 + `OpenImportWindow()` + `GetTemplatePresets()`。仅 Equipment 和 Consumable 额外覆写了 `GetAssetDirForType()`。

| 方法 | Equipment | Ammo | Consumable | Building | Character | SceneItem |
|------|-----------|------|------------|----------|-----------|-----------|
| `GetTargetType()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `GetEditorTitle()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `GetBreadcrumb()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `GetCreateMenuItems()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `GetDefaultAssetDir()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `GetAssetFilter()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `GetAssetDirForType()` | ✅ | — | ✅ | — | — | — |
| `GetTemplatePresets()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `OpenImportWindow()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

## 交叉引用

- [EntityEditorWindow.md](L2_EntityService/EntityEditorWindow.md) — 抽象基类文档
- [EntityImporter.md](L2_EntityService/EntityImporter.md) — JSON 导入/导出引擎
