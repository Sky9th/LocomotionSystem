# EntityEditorWindow — Entity 编辑器抽象基类

- **源文件**: `Assets/Scripts/Services/L2_EntityService/Editor/EntityEditorWindow.cs`
- **配套**: `EntityImporter.cs` (共享导入引擎) + `EntityImportConfig.cs` (配置对象)
- **命名空间**: `RedDust.Entities.Editor`

> **Last Verified**: 2026-07-10 | **Verification**: Content Id preview + auto-Id init + performance caches added; all referenced files exist, signatures match code

## 架构

`EntityEditorWindow` 是所有 Entity 编辑器的抽象基类。6 个子类（Character / Equipment / Ammo / Consumable / Building / SceneItem）继承此类，只覆写配置方法（类型过滤、模板列表、创建菜单等），UI 布局完全由基类提供。

## UI 结构（三栏布局）

```
┌─────────────────────────────────────────────────────────────────┐
│ [1] HEADER                                                       │
│  {Category} Editor  |  {Breadcrumb}                              │
│  [Refresh] [Import/Export]  ←flex→  [+ Create ▼] [Save *] [Del] │
├─────────────────────────────────────────────────────────────────┤
│ [2a] LEFT (280px)  | [2b] CENTER (expand)    | [2c] RIGHT (200) │
│  [search-bar]       |  [basic-card]           |  [preview]       │
│  [tree-view]        |    Template / Prefab    |    Icon thumbnail│
│   📁 TemplateA (N)  |  [category-section]     |    Prefab preview│
│     📄 Entity1      |  [{folder}-card]        |                  │
│     📄 Entity2      |    Property rows         |                  │
│   📁 TemplateB (N)  |                         |                  │
│   📄 Entity3 (无模板)│                         |                  │
├─────────────────────────────────────────────────────────────────┤
│ [3] STATUS BAR                                                   │
│  Type: XxxSO · Template: Yyy · N props (M overrides)             │
└─────────────────────────────────────────────────────────────────┘
```

## 子类覆写点

| 覆写点 | 类型 | 必须 | 示例 (EquipmentEditorWindow) |
|--------|------|------|------------------------------|
| `GetTargetType()` | abstract | ✅ | `typeof(EquipmentDefSO)` |
| `GetEditorTitle()` | abstract | ✅ | `"Equipment Editor"` |
| `GetBreadcrumb()` | abstract | ✅ | `"L3_Equipment · Editor"` |
| `GetCreateMenuItems()` | abstract | ✅ | `[("Melee Weapon", typeof(MeleeWeaponSO)), ...]` |
| `GetDefaultAssetDir()` | abstract | ✅ | `"Assets/Data/Entities/Equipment"` |
| `GetAssetFilter()` | abstract | ✅ | `"t:EquipmentDefSO"` |
| `GetAssetDirForType(Type)` | virtual | 否 | Equipment/Consumable 覆写以按子类型路由到子目录 |
| `GetTemplatePresets(Type)` | virtual | 否 | 返回 `(label, assetName)[]`。null = 回退 ObjectField |
| `OpenImportWindow()` | virtual | 否 | 返回 `Action`，基类据此显示 Import/Export 按钮 |
| `DrawExtraToolbarButtons()` | virtual | 否 | 工具栏额外按钮 |
| `DrawCategorySpecificSection()` | virtual | 否 | 插入 Basic Card 和 Properties 之间的类别特定 UI |
| `GetStatusSummary()` | virtual | 否 | 状态栏摘要文本 |

## 生命周期

| 方法 | 触发时机 | 行为 |
|------|---------|------|
| `OnEnable()` | 窗口打开 | 设置 minSize (900×500)，创建 EditorTreeView，RefreshAssetList |
| `OnDisable()` | 窗口关闭 | 如有未保存修改 → 弹窗确认 |
| `OnGUI()` | 每帧 | Ctrl+S 保存快捷键，渲染三栏布局 (DrawHeader → DrawThreeColumnBody → DrawStatusBar) |

## 核心操作

### 资产列表 (RefreshAssetList / BuildTree)
- `GetAssetFilter()` 查找资产 → 按 TargetType 过滤
- 按 `Template.name` 分组为文件夹树节点 (EditorTreeNode)
- 无模板的资产直接放根层级
- 搜索过滤 (FilterTreeNodes) 递归匹配 DisplayName

### 选中实体 (SelectPreset)
- 保存前一个实体的修改 → 加载新 Template → 解析 OverridesJson 到 `_overrideValues` / `_minOverrides` / `_maxOverrides`
- 调用 `ResolveStructureEditor(template)` 展开 PropertyTree 为 `Dictionary<string, PropertyDefSO>`

### 保存 (Save)
- 将 `_overrideValues` / `_minOverrides` / `_maxOverrides` 序列化为 `{"Overrides":[{Path,Value,Min,Max}]}` JSON
- 写入 `_selectedPreset.OverridesJson` → SetDirty → SaveAssets

### 创建资产 (CreateAsset)
- `GetAssetDirForType(soType)` 确定目录 → 必要时创建子文件夹
- `ScriptableObject.CreateInstance` → 自动生成唯一路径 → `DataLabelTools.EnsureBootLabel`
- 创建后自动选中

### 删除 (DeleteSelectedPreset / OnTreeDelete)
- 弹窗确认 → `AssetDatabase.DeleteAsset` → 刷新列表

## 属性表单类型分发

`DrawPropertyRow()` 通过 `switch (def)` 分发到 9 种 PropertyDefSO 渲染器：

| PropertyDefSO 子类 | 渲染控件 | 特殊行为 |
|-------------------|---------|---------|
| `FloatPropertyDefSO` | 值 / Min / Max 三字段 | 支持 Min/Max 覆写 |
| `IntPropertyDefSO` | 值 / Min / Max 三字段 | 同上 |
| `BoolPropertyDefSO` | Toggle | — |
| `StringPropertyDefSO` | TextField (Description 用 TextArea) | 自动识别 Description 路径 |
| `RdTagPropertyDefSO` | TextField + TagPicker 按钮 | 弹出 TagPicker 选择 |
| `RdTagListPropertyDefSO` | TagChips (标签粒) + 添加/删除 | 弹出 TagPicker 多选 |
| `AssetRefPropertyDefSO` | ObjectField | 按 AssetTypeConstraint 过滤 |
| `AssetRefListPropertyDefSO` | 列表 ObjectField + Add/Remove | GUID 数组存储 |
| `StructPropertyDefSO` | SlotDef 编辑器 (若 structTypeName 为 SlotDef) | AcceptTags / Capacity / WeightLimit |

## Template 字段

`DrawTemplateField()` 有两种模式：
1. **预设列表模式**（`GetTemplatePresets` 返回非 null）：下拉按钮 + GenericMenu，按 label 显示，按 assetName 解析 SO
2. **ObjectField 模式**（回退）：直接拖拽 PropertyTreeSO

切换模板时：确认清除已有 overrides → 重新 SelectPreset

## 复用 API

| API | 来源 | 用途 |
|-----|------|------|
| `PropertyTreeSO.ResolveAllNodes()` | L3_Properties | 获取全部节点 |
| `PropertyDefSO` (9 子类) | L3_Properties | 控件类型和约束 |
| `EditorTreeView` / `EditorTreeNode` | Shared/EditorUI | 左栏树列表 |
| `EditorCard` / `EditorButton` / `EditorLabel` / `EditorInput` | Shared/EditorUI | EUI 组件 |
| `EditorSearchBar` | Shared/EditorUI | 搜索栏 |
| `TagPicker` | L1_Core/RdTag | 标签选择弹窗 |
| `EditorImportExport.Draw()` | Shared/EditorUI | Import/Export 面板骨架 |

## 内部 Helper 类型

| 类型 | 用途 |
|------|------|
| `OverrideEntry` | 序列化 Path / Value / Min / Max |
| `OverrideContainer` | `List<OverrideEntry>` 的 JSON 包装 |
| `TagListWrap` | `string[]` JSON 反序列化 (RdTagList) |
| `SlotListWrap` | `SlotDef[]` JSON 反序列化 |
| `GuidListWrap` | `string[]` JSON 反序列化 (AssetRefList) |
