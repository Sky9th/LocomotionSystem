# EntityImporter — Entity JSON 导入/导出引擎

- **源文件**: `Assets/Scripts/Services/L2_EntityService/Editor/EntityImporter.cs`
- **配套**: `EntityImportConfig.cs` (配置对象) + `EntityImportExport.cs` (各模块 ImportWindow)
- **命名空间**: `RedDust.Entities.Editor`

> **Last Verified**: 2026-07-08

## 概述

`EntityImporter` 是所有 Entity `*_all.json` 文件的统一导入/导出引擎。替代了旧的 5 份独立 `XxxImporter` 类，差异全部由 `EntityImportConfig` 参数化。

## 数据模型

### EntityEntry (JSON 实体条目)
```csharp
public class EntityEntry
{
    public string entityType;     // 类型标签，单类型模块为 null
    public string name;           // 实体名称（必填，禁止路径分隔符）
    public string templateName;   // PropertyTreeSO 资源名
    public string overridesJson;  // JSON 覆写字符串
    public string prefabGuid;     // Prefab GUID
}
```

### EntityExportFile (JSON 文件格式)
```csharp
public class EntityExportFile
{
    public string version = "1.0";
    public string description;
    public string category;       // Character|Equipment|Ammo|Consumable|Building|SceneItem
    public EntityEntry[] entities;
}
```

### EntityImportConfig (模块配置)
```csharp
public class EntityImportConfig
{
    public string Category;              // 类别显示名
    public string Breadcrumb;            // 面包屑
    public string DataRoot;              // 资产根目录
    public string AssetFilter;           // AssetDatabase 过滤器，如 "t:EquipmentDefSO"
    public string DefaultFileName;       // 默认导出文件名（不含扩展名）
    public Dictionary<string, Type> TypeMap;   // entityType → C# Type。单类型为 null
    public Type DefaultType;             // TypeMap 为空时的默认类型
    public Func<string, string> BuildPreview;  // Preview 委托
}
```

## 各模块配置

| 模块 | Category | AssetFilter | TypeMap | DefaultType |
|------|----------|-------------|---------|-------------|
| Character | Character | `t:CharacterDefSO` | null | CharacterDefSO |
| Equipment | Equipment | `t:EquipmentDefSO` | MeleeWeapon/RangedWeapon/Armor/Tool/Container | MeleeWeaponSO |
| Ammo | Ammo | `t:AmmoDefSO` | null | AmmoSO |
| Consumable | Consumable | `t:ConsumableDefSO` | Consumable/Material | ConsumableSO |
| Building | Building | `t:BuildingDefSO` | null | BuildingDefSO |
| SceneItem | SceneItem | `t:SceneItemDefSO` | null | SceneItemDefSO |

## Export 流程

```
ExportToJson(config)
  ├── AssetDatabase.FindAssets(AssetFilter) → 获取所有匹配 GUID
  ├── 逐个加载 PropertyPresetSO
  │     ├── entityType = TypeToLabel(preset.GetType())   // Type→Label 反查
  │     ├── name = preset.name
  │     ├── templateName = preset.Template?.name
  │     ├── overridesJson = preset.OverridesJson
  │     └── prefabGuid = AssetPathToGUID(preset.Prefab)
  ├── 排序: 按 entityType → name
  └── JsonUtility.ToJson(export, prettyPrint: true)
```

## Import 流程（5 Phase）

```
ImportFromJson(jsonText, config)
  Phase 1: JsonUtility.FromJson<EntityExportFile> — 反序列化
  Phase 2: BuildLookups
           ├── BuildAssetLookup<PropertyTreeSO>("t:PropertyTreeSO") — 模板名→SO
           └── BuildExistingLookup(config) — 已存在实体名→SO
  Phase 3: Validate per entry
           ├── name 为空 → skip + error
           ├── name 含 / 或 \ → skip + error
           └── entityType 不在 TypeMap → skip + error
  Phase 4: Create or Update
           ├── existingByName.TryGetValue(name) → 找到?
           │     ├── 类型不匹配 → skip + error
           │     └── 类型匹配 → ApplyFields + SetDirty (updated++)
           └── 未找到 → CreateInstance + ApplyFields + CreateAsset (created++)
  Phase 5: SaveAssets + Refresh
```

### 关键规则

| 校验 | 行为 |
|------|------|
| `name` 为空 | 跳过 + "Skipping: empty name." |
| `name` 含 `/` 或 `\` | 跳过 + "Path separator in name." |
| `entityType` 未知 (TypeMap != null) | 跳过 + "Unknown entityType." |
| 类型不匹配 (已存在 .asset 类型 ≠ 配置解析类型) | 跳过 + "Type mismatch." |
| `templateName` 未找到 | template 置 null (不报错，运行时可能出问题) |
| `prefabGuid` 未找到 | prefab 置 null (不报错) |

## 共享 Helper

| 方法 | 用途 |
|------|------|
| `BuildAssetLookup<T>(filter)` | 构建 name→SO 字典 (public，供其他 Importer 复用) |
| `ResolvePrefab(guid)` | GUID → GameObject |
| `ApplyFields(target, entry, template, prefab)` | 统一写入 Template / OverridesJson / Prefab |

## 6 个 ImportWindow 对照

每个模块有一个 EditorWindow 子类包装 Import/Export UI，全部调用 `EntityImporter`：

| 窗口类 | 文件 | MenuItem |
|--------|------|----------|
| `EquipmentImportWindow` | `L3_Equipment/Editor/EquipmentImportExport.cs` | `RedDust/Equipment Import-Export` |
| `AmmoImportWindow` | `L3_Ammo/Editor/AmmoImportExport.cs` | `RedDust/Ammo Import-Export` |
| `ConsumableImportWindow` | `L3_Consumable/Editor/ConsumableImportExport.cs` | `RedDust/Consumable Import-Export` |
| `CharacterImportWindow` | `L3_Character/Editor/CharacterImportExport.cs` | `RedDust/Character Import-Export` |
| `BuildingImportWindow` | `L3_Building/Editor/BuildingImportExport.cs` | `RedDust/Building Import-Export` |
| `SceneItemImportWindow` | `L3_SceneItem/Editor/SceneItemImportExport.cs` | `RedDust/Scene Item Import-Export` |

每个 ImportWindow 共用一个 `EditorImportExport.Draw()` 静态方法渲染 Import/Export 面板。
