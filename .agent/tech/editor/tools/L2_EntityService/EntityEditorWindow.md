# EntityEditorWindow — Entity 编辑器抽象基类

- **源文件**: `Assets/Scripts/Services/L2_EntityService/Editor/EntityEditorWindow.cs`
- **配套**: `EntityImporter.cs` (共享导入引擎) + `EntityImportConfig.cs` (配置对象)

> **Last Verified**: 2026-07-08 | **Verification**: All referenced files exist, signatures match code

## UI 结构（三栏布局）

```
┌─────────────────────────────────────────────────────────────┐
│ [1] HEADER                                                   │
│  {Category} Editor  |  {Breadcrumb}                          │
│  [Refresh] [Import/Export]  ←flex→  [+Create ▼] [Save] [Del]│
├─────────────────────────────────────────────────────────────┤
│ [2a] LEFT (280px)  | [2b] CENTER (expand)  | [2c] RIGHT(200)│
│  [search-bar]       |  [basic-card]         |  [preview]     │
│  [tree-view]        |    Template / Prefab  |    Icon        │
│   📁 TemplateA (N)  |  [extra-sections]     |    Prefab      │
│     📄 Entity1      |  [{folder}-card]      |                │
│                     |    Property rows       |                │
├─────────────────────────────────────────────────────────────┤
│ [3] STATUS BAR                                               │
│  Type: XxxSO · Template: Yyy · N props (M overrides)         │
└─────────────────────────────────────────────────────────────┘
```

## 子类覆写点

| 覆写点 | 类型 | 必须 | 示例 (WeaponEditorWindow) |
|--------|------|------|--------------------------|
| `GetTargetType()` | abstract | ✅ | `typeof(WeaponDefSO)` |
| `GetEditorTitle()` | abstract | ✅ | `"Weapon Editor"` |
| `GetBreadcrumb()` | abstract | ✅ | `"L3_Weapon · Editor"` |
| `GetAssetFilter()` | abstract | ✅ | `"t:WeaponDefSO"` |
| `GetCreateMenuItems()` | abstract | ✅ | `[("Melee Weapon", typeof(MeleeWeaponSO)), ...]` |
| `GetDefaultAssetDir()` | abstract | ✅ | `"Assets/Data/Entities/Weapons"` |
| `GetTemplatePresets(Type)` | virtual | 否 | 返回 `(label, assetName)[]`。null=回退 ObjectField。selectedType 为当前实体 C# 类型 |
| `OpenImportWindow()` | virtual | 否 | `WeaponImportWindow.Open` |
| `DrawExtraToolbarButtons()` | virtual | 否 | — |
| `DrawCategorySpecificSection()` | virtual | 否 | — |
| `GetStatusSummary()` | virtual | 否 | — |

## 属性表单类型分发

9 种 PropertyDefSO 子类 → 对应 DrawXxxRow 渲染器：
Float(Min/Max), Int(Min/Max), Bool, String(TextArea for Description), RdTag(TagPicker), RdTagList(TagChips), AssetRef(ObjectField), AssetRefList(Add/Remove), Struct(SlotDef editor)

## 复用 API

| API | 来源 | 用途 |
|-----|------|------|
| `PropertyTreeSO.ResolveAllNodes()` | L3_Properties | 获取全部节点 |
| `PropertyDefSO.Type/Min/Max/DefaultValue` | L3_Properties | 控件类型和约束 |
| `EditorTreeView` / `EditorTreeNode` | Shared/Editor | 左栏树列表 |
| `EditorCard` / `EditorButton` / `EditorLabel` / `EditorInput` | Shared/Editor | EUI 组件 |
| `EditorSearchBar` | Shared/Editor | 搜索栏 |
| `TagPicker` | L1_Core/RdTag | 标签选择弹窗 |
| `EditorImportExport.Draw()` | Shared/Editor | Import/Export 面板骨架 |
