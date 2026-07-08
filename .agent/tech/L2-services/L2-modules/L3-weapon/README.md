# L3_Weapon · 武器系统

> `Assets/Scripts/Services/Modules/L3_Weapon/` · L3 独立模块
> **Last Verified**: 2026-07-08 | **Verification**: All referenced files exist, signatures match code

## 层级定位

L3 独立模块，位于 `L2-modules/L3-weapon/`。从原 `L3_Item` 拆分而来，与 `L3_Prop`、`L3_SceneItem` 平级。

武器预设继承 `PropertyPresetSO`，所有数据全进 PropertyTree。

## 架构

```
L3_Weapon/
├── WeaponDefSO.cs           # [SO] 武器抽象基类 — 继承 PropertyPresetSO
├── MeleeWeaponSO.cs         # [SO] 近战武器 — GetDamageEffects 从 Weapon/ATK 读 DamageEffectSO[]
├── RangedWeaponSO.cs        # [SO] 远程武器 — 伤害来自弹药 Entity
└── Editor/
    ├── WeaponEditorWindow.cs    # EntityEditorWindow 子类 — 编辑武器预设
    └── WeaponImportExport.cs    # WeaponImportWindow — JSON 导入/导出
```

## 调用链

```
定义时:
  WeaponEditorWindow → PropertyPresetSO.Template/OverridesJson/Prefab
  WeaponImportWindow → EntityImporter(EntityImportConfig)

运行时:
  EntityService.Spawn → Instantiate(Prefab) → Identity.BindEntity
  Ability Pipeline → MeleeWeaponSO.GetDamageEffects(entity)
                   → RangedWeaponSO.GetDamageEffects(entity)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | PropertyPresetSO, PropertyTreeSO | 属性定义 + 结构 |
| 依赖 | Entity | GetDamageEffects 入参 |
| 被消费 | L2_EntityService | Spawn → Instantiate Prefab |
| 被消费 | Ability Pipeline | 读取 DamageEffectSO[] |
| 被消费 | AssetService | Boot 加载 WeaponDefSO |

## 设计决策

| 决策 | 原因 |
|------|------|
| WeaponDefSO 为抽象中间类 | 未来提取武器共性（如 GetDamageEffects 通用逻辑），当前作为类型标记 |
| 从 ItemDefSO 独立继承 PropertyPresetSO | 武器不是"物品"的子类——是独立领域概念 |
| MeleeWeaponSO/RangedWeaponSO 在 L3_Weapon/ | 武器领域内聚，不与道具/场景物品混杂 |

## 编辑器工具

| 工具 | 菜单 | 说明 |
|------|------|------|
| WeaponEditorWindow | `RedDust/Weapon Editor` | EntityEditorWindow 子类，编辑 MeleeWeaponSO / RangedWeaponSO。覆写 `GetAssetDirForType` — MeleeWeaponSO → `Weapons/Melee/`，RangedWeaponSO → `Weapons/Ranged/` |
| WeaponImportWindow | `RedDust/Weapon Import-Export` | JSON 导入/导出，EntityImporter 引擎驱动 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| GetDamageEffects 通用逻辑上提到 WeaponDefSO | 待做 | MeleeWeaponSO / RangedWeaponSO 共存稳定后 |
| RangedWeaponSO 弹药容器链实现 | 待做 | Container 系统运行时实现 |
| Weapon PropertyTree 结构细化 | 待做 | PropertyTree 系统落地 |
