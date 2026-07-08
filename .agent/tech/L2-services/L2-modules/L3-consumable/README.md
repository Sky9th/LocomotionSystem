# L3_Consumable — 消耗品定义模块

**Last Verified:** 2026-07-08

## 模块定位

L3_Consumable 是 Entity 系统中消耗品类实体的定义层。管理 Consumable / Material 两类 ScriptableObject 预设。

## 文件清单

| 文件 | 角色 |
|------|------|
| `ConsumableDefSO.cs` | 消耗品中间抽象基类，继承 `PropertyPresetSO` |
| `ConsumableSO.cs` | 消耗品预设（Food + Medical 共用），零 C# 字段 |
| `MaterialSO.cs` | 材料预设，零 C# 字段 |
| `Editor/ConsumableEditorWindow.cs` | EntityEditorWindow 子类，编辑 2 种子类型 |
| `Editor/ConsumableImportExport.cs` | JSON ↔ .asset 导入导出 |

## SO 继承链

```
PropertyPresetSO
  └── ConsumableDefSO
        ├── ConsumableSO
        └── MaterialSO
```

## PropertyTree 映射

| SO 类型 | PropertyTree 模板族 |
|---------|-------------------|
| ConsumableSO | ConsumableBase → {Food, Medical} |
| MaterialSO | ConsumableBase → {Material, Seed} |

## EntityImportConfig

| 字段 | 值 |
|------|-----|
| Category | `"Consumable"` |
| DataRoot | `Assets/Data/Entities/Consumable` |
| AssetFilter | `t:ConsumableDefSO` |
| TypeMap | Consumable, Material |

## 耦合模块

- **L3_Properties** — 基类 `PropertyPresetSO` 所在
- **L2_EntityService** — `EntityImporter` / `EntityEditorWindow` 基类
