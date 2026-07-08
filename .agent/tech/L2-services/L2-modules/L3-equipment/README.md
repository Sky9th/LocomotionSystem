# L3_Equipment — 装备定义模块

**Last Verified:** 2026-07-08

## 模块定位

L3_Equipment 是 Entity 系统中装备类实体的定义层。管理 Weapon / Armor / Tool / Container 四类装备的 ScriptableObject 预设。

## 文件清单

| 文件 | 角色 |
|------|------|
| `EquipmentDefSO.cs` | 装备中间抽象基类，继承 `PropertyPresetSO` |
| `WeaponDefSO.cs` | 武器抽象基类，继承 `EquipmentDefSO`。定义 `GetDamageEffects()` 虚方法 |
| `MeleeWeaponSO.cs` | 近战武器，覆写 `GetDamageEffects()`，从 `Weapon/ATK` 直读 `DamageEffectSO[]` |
| `RangedWeaponSO.cs` | 远程武器，覆写 `GetDamageEffects()`，沿容器链查弹药 Entity |
| `ArmorSO.cs` | 防具预设，零 C# 字段 |
| `ToolSO.cs` | 工具预设，零 C# 字段 |
| `ContainerSO.cs` | 容器物品预设，零 C# 字段 |
| `Editor/EquipmentEditorWindow.cs` | EntityEditorWindow 子类，编辑 5 种子类型 |
| `Editor/EquipmentImportExport.cs` | JSON ↔ .asset 导入导出 |

## SO 继承链

```
PropertyPresetSO
  └── EquipmentDefSO
        ├── WeaponDefSO (abstract, 有 GetDamageEffects)
        │     ├── MeleeWeaponSO
        │     └── RangedWeaponSO
        ├── ArmorSO
        ├── ToolSO
        └── ContainerSO
```

## PropertyTree 映射

| SO 类型 | PropertyTree 模板族 |
|---------|-------------------|
| MeleeWeaponSO | Equipment → WeaponBase → MeleeWeapon → {Blade, Axe, Blunt, Polearm} |
| RangedWeaponSO | Equipment → WeaponBase → RangedWeapon → Firearm → {Pistol, Rifle, Shotgun} / Bow |
| ArmorSO | Equipment → ArmorBase → {HeadArmor, BodyArmor, LegArmor} |
| ToolSO | Equipment → ToolBase |
| ContainerSO | Equipment → Backpack |

## EntityImportConfig

| 字段 | 值 |
|------|-----|
| Category | `"Equipment"` |
| DataRoot | `Assets/Data/Entities/Equipment` |
| AssetFilter | `t:EquipmentDefSO` |
| TypeMap | MeleeWeapon, RangedWeapon, Armor, Tool, Container |

## 耦合模块

- **L3_Properties** — 基类 `PropertyPresetSO` 所在
- **L3_Ability** — `DamageEffectSO` 被 `GetDamageEffects()` 返回
- **L3_Container** — `ContainerSO` 与 `CharacterEquipment` 通过 Slots 交互
- **L2_EntityService** — `EntityImporter` / `EntityEditorWindow` 基类
