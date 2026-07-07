# ItemImportExport

- **菜单**: `RedDust/Item Import-Export`
- **源文件**: `Assets/Scripts/Services/Modules/L3_Item/Editor/ItemImportExport.cs`

> **Last Verified**: 2026-07-07 | **Verification**: PLANNED — 代码尚未创建

## UI 结构

```
┌─────────────────────────────────────────────────────┐
│  ItemImportWindow                                    │
│                                                      │
│  ┌─────────────────────────────────────────────────┐ │
│  │ [1] HEADER                                      │ │
│  │  "Item Import-Export"  "L3_Item · JSON ↔ .asset"│ │
│  └─────────────────────────────────────────────────┘ │
│                                                      │
│  ┌─────────────────────────────────────────────────┐ │
│  │ [2] FILE SELECTOR                               │ │
│  │  JSON File: [____________] [...]                │ │
│  └─────────────────────────────────────────────────┘ │
│                                                      │
│  ┌─────────────────────────────────────────────────┐ │
│  │ [3] PREVIEW                                     │ │
│  │  "N items (M Melee / R Ranged / I Item)"         │ │
│  └─────────────────────────────────────────────────┘ │
│                                                      │
│  ┌─────────────────────────────────────────────────┐ │
│  │ [4] BUTTONS                                     │ │
│  │  [Import]                [Export]                │ │
│  └─────────────────────────────────────────────────┘ │
│                                                      │
│  ┌─────────────────────────────────────────────────┐ │
│  │ [5] RESULT                                      │ │
│  │  Created: N · Updated: M · Skipped: K             │ │
│  │  Errors: ... (scrollable)                        │ │
│  └─────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

## DTO

```csharp
[Serializable]
public class ItemEntry
{
    public string itemType;      // "Item" | "MeleeWeapon" | "RangedWeapon"
    public string name;          // asset name
    public string templateName;  // PropertyTreeSO asset name — 跨机器可移植
    public string overridesJson; // 覆写 JSON (含 Slots)
    public string prefabGuid;    // AssetDatabase.AssetPathToGUID
}

[Serializable]
public class ItemExportFile
{
    public string version = "1.0";
    public string description;
    public ItemEntry[] items;
}
```

## 导出流程

```
AssetDatabase.FindAssets("t:ItemDefSO")
  → 遍历每项
  → itemType: GetType() switch (ItemDefSO→"Item", MeleeWeaponSO→"MeleeWeapon", RangedWeaponSO→"RangedWeapon")
  → templateName: Template?.name
  → overridesJson: OverridesJson 直接拷贝
  → prefabGuid: Prefab ? AssetDatabase.AssetPathToGUID(Prefab) : null
  → 按 itemType+name 排序
  → JsonUtility.ToJson(export, true)
```

## 导入流程

```
Phase 1 Deserialize:
  JsonUtility.FromJson<ItemExportFile>(jsonText)

Phase 2 Validate:
  name 非空, itemType ∈ ["Item","MeleeWeapon","RangedWeapon"]

Phase 3 Resolve:
  templateName → AssetDatabase.FindAssets("t:PropertyTreeSO") 按 name 匹配
  prefabGuid → AssetDatabase.GUIDToAssetPath → LoadAssetAtPath<GameObject>

Phase 4 Create/Update:
  已存在 → 类型匹配检查 → ApplyFields → EditorUtility.SetDirty → updated++
  不存在 → ScriptableObject.CreateInstance<T> → ApplyFields → AssetDatabase.CreateAsset → EnsureBootLabel → created++

Phase 5 Persist:
  AssetDatabase.SaveAssets() + Refresh()
```

## 复用组件

| 组件 | 用途 |
|------|------|
| `EditorImportExport.Draw()` | 窗口骨架 — header + 文件选择 + 预览 + 按钮 + 结果 |
| `DataLabelTools.EnsureBootLabel()` | 新资产标记 Addressables boot label |
