# L3_Ammo — 弹药定义模块

**Last Verified:** 2026-07-08

## 模块定位

L3_Ammo 是 Entity 系统中弹药类实体的定义层。管理 `AmmoSO` 的 ScriptableObject 预设。

## 文件清单

| 文件 | 角色 |
|------|------|
| `AmmoDefSO.cs` | 弹药中间抽象基类，继承 `PropertyPresetSO` |
| `AmmoSO.cs` | 弹药预设，零 C# 字段 |
| `Editor/AmmoEditorWindow.cs` | EntityEditorWindow 子类，编辑 AmmoSO |
| `Editor/AmmoImportExport.cs` | JSON ↔ .asset 导入导出 |

## SO 继承链

```
PropertyPresetSO
  └── AmmoDefSO
        └── AmmoSO
```

## PropertyTree 映射

| SO 类型 | PropertyTree 模板族 |
|---------|-------------------|
| AmmoSO | AmmoBase → {PistolAmmo, RifleAmmo, ShotgunShell} |

## EntityImportConfig

| 字段 | 值 |
|------|-----|
| Category | `"Ammo"` |
| DataRoot | `Assets/Data/Entities/Ammo` |
| AssetFilter | `t:AmmoDefSO` |
| TypeMap | null（单类型） |

## 耦合模块

- **L3_Properties** — 基类 `PropertyPresetSO` 所在
- **L3_Equipment** — `RangedWeaponSO.GetDamageEffects()` 沿容器链查弹药 Entity
- **L2_EntityService** — `EntityImporter` / `EntityEditorWindow` 基类
