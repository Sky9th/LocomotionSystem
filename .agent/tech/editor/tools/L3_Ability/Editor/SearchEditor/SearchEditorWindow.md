# SearchEditorWindow

- **菜单**: `RedDust/Search Editor`
- **源文件**: `Assets/Scripts/Services/Modules/L3_Ability/Editor/SearchEditor/SearchEditorWindow.cs`
- **最后验证**: 2026-07-08

## UI 结构全图

> 完整枚举 SearchEditorWindow 的所有 UI 区域，为修 UI 提供一致的部位命名。

### Window 总览

```
┌──────────────────────────────────────────────────────────────────────┐
│  SearchEditorWindow                                                   │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [1] HEADER BAR                                                  │ │
│  │  "Search Editor" (large)         "L3_Ability · Editor"          │ │
│  │  [Refresh] [Import/Export] [+Create] [Save */Saved] [Ping]      │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [2] TWO-COLUMN BODY                                            │ │
│  │                                                                  │ │
│  │  ┌──────────────────────┐  ┌──────────────────────────────────┐ │ │
│  │  │ [2a] LEFT PANEL      │  │ [2b] RIGHT PANEL                  │ │ │
│  │  │  (300px)             │  │  (expand)                         │ │ │
│  │  │                      │  │                                   │ │ │
│  │  │ [2a-1] Filter Bar    │  │  [2b-title] "Edit: {name}"       │ │ │
│  │  │  [All][Cone][Ray]    │  │                                   │ │ │
│  │  │  [Circle]            │  │  [2b-1] BASE SECTION             │ │ │
│  │  │                      │  │  ┌ EditorCard "Base" ──────────┐ │ │ │
│  │  │ [2a-2] Search Row    │  │  │ Name, searchType, range,      │ │ │ │
│  │  │  [🔍__________]      │  │  │ targetMask, maxTargets,       │ │ │ │
│  │  │                      │  │  │ targetFilter                  │ │ │ │
│  │  │ [2a-3] Tree          │  │  └──────────────────────────────┘ │ │ │
│  │  │  📁 Cone             │  │                                   │ │ │
│  │  │    📄 Search_Cone_A  │  │  [2b-2] TYPE SECTION             │ │ │
│  │  │    📄 Search_Cone_B ◀│──│──│─ (selected → right)           │ │ │
│  │  │  📁 Ray              │  │  ┌ EditorCard "Cone" ──────────┐ │ │ │
│  │  │    📄 Search_Ray_A   │  │  │ angle [0────●────360]        │ │ │ │
│  │  │  📁 Circle           │  │  │ ── or ──                     │ │ │ │
│  │  │    📄 Search_Circle_A│  │  │ "Ray": requiresLineOfSight   │ │ │ │
│  │  └──────────────────────┘  │  │ "Circle": (no extra fields)  │ │ │ │
│  │                            │  └──────────────────────────────┘ │ │ │
│  │                            └────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [3] STATUS BAR                                                  │ │
│  │  "42 searches · 15 Cone · 20 Ray · 7 Circle"                    │ │
│  │                                       "Search_Cone_2m (Cone)"   │ │
│  └────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘

POPUPS:
  - CreateNewSearch GenericMenu  (Cone / Ray / Circle)
  - SearchImportWindow           (separate EditorWindow for import/export)
```

### OnGUI 调用链

```
OnGUI
├── DrawHeader()                        [1] Header Bar
│   ├── Row 1: title-label + breadcrumb-label
│   └── Row 2: toolbar-row (Refresh, Import/Export, Create, Save, Ping)
│
├── EditorCard.Gap(Pad)
│
├── DrawTwoColumns()                    [2] Two-Column Body
│   ├── DrawLeftColumn()                [2a] Left Panel (300px)
│   │   └── EditorCard.Draw
│   │       ├── EditorCard.Draw → EditorButtonGroup.Draw()  [2a-1] Filter Bar
│   │       ├── EditorCard.GapTight
│   │       ├── EditorCard.Draw → EditorSearchBar.Draw()    [2a-2] Search Row
│   │       └── EditorTreeView.OnGUI(rect)                   [2a-3] Tree
│   │
│   └── DrawRightColumn()               [2b] Right Panel
│       └── EditorCard.Draw
│           ├── Empty placeholder (if no selection)
│           ├── "Edit: {name}" (title)
│           ├── DrawBaseFields()        [2b-1] Base Section
│           │   └── EditorCard.Draw("Base")
│           │       └── EditorForm.Draw (inline)
│           │           ├── Name (RawField → TextField, rename via RenameSearch)
│           │           ├── searchType (Enum<ESearchType>)
│           │           ├── range (Float)
│           │           ├── targetMask (RawField → custom DrawLayerMaskField)
│           │           ├── maxTargets (Int)
│           │           └── targetFilter (Enum<ETargetFilter>)
│           │
│           └── DrawTypeSpecificFields() [2b-2] Type Section
│               └── EditorCard.Draw("Cone"|"Ray"|"Circle")
│                   ├── Cone: EditorForm.Draw (angle Slider)
│                   ├── Ray:  EditorForm.Draw (requiresLineOfSight Toggle)
│                   └── Circle: GreyPlaceholder "(no additional fields)"
│
├── EditorCard.Gap(Pad)
│
└── DrawStatusBar()                     [3] Status Bar
    ├── stats-summary (总数 + 各类型计数)
    └── selected-indicator (当前选中资产)
```

### 部位命名表

#### [1] Header Bar

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `title-label` | `EditorStyles.largeLabel` | "Search Editor" |
| `breadcrumb-label` | `EditorTokens.BreadcrumbStyle` (right-aligned) | "L3_Ability · Editor" |
| `toolbar-row` | `EditorGUILayout.BeginHorizontal` | 按钮容器行 |
| `refresh-btn` | `EditorButton.Draw("Refresh")` | 重建模型 + 清除选中 |
| `import-export-btn` | `EditorButton.Draw("Import/Export")` | 打开 SearchImportWindow |
| `create-btn` | `EditorButton.Draw("+ Create", Success)` | 弹出 GenericMenu (3 种子类) |
| `save-btn` | `EditorButton.Draw("Save *"/"Saved", Primary/Default)` | dirty 时变 Primary 样式 + 显示 * |
| `ping-btn` | `EditorButton.Draw("Ping")` | PingObject，仅 `_selectedSearch != null` 时显示 |

#### [2a] Left Panel

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `filter-bar` | `EditorButtonGroup.Draw(_filter, ...)` | 4 个 tab: All / Cone / Ray / Circle |
| `search-row` | `EditorSearchBar.Draw(_searchText, labelWidth: 42f)` | 单行文本框，文本传给 `_treeView.searchString` |
| `search-tree` | `EditorTreeView.OnGUI(rect)` | searchType 虚拟文件夹分组 + 叶子节点 |

#### [2b] Right Panel

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `empty-placeholder` | `EditorUIUtility.GreyPlaceholder` | "Select a search from the left panel." |
| `right-title` | `EditorCard.Draw($"Edit: {name}", ...)` | 卡片标题含当前编辑资产名 |

#### [2b-1] Base Section

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `base-section-card` | `EditorCard.Draw("Base")` | 整个 Base 卡片 |
| `base-form` | `EditorForm.Draw` (inline, not field) | 每次 OnGUI 即时构建，OnChange → MarkDirty |
| `name-field` | `RawField("Name")` → `TextField` | 编辑 `.name`，change 触发 `RenameSearch` |
| `searchType-field` | `Enum<ESearchType>("searchType")` | 只读展示（子类 OnEnable 自设） |
| `range-field` | `Float("range")` | |
| `targetMask-field` | `RawField("Target Mask")` → custom `DrawLayerMaskField` | 使用 `EditorGUILayout.MaskField` 模拟 LayerMask 字段（Unity 无内置 LayerMaskField）；tooltip 通过反射读取 `TooltipAttribute` |
| `maxTargets-field` | `Int("maxTargets")` | |
| `targetFilter-field` | `Enum<ETargetFilter>("targetFilter")` | |

#### [2b-2] Type Section

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `type-section-card` | `EditorCard.Draw(title)` | title 为 "Cone"/"Ray"/"Circle" |

**Cone 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `angle` | `Slider(0, 360)` | 扇形全角 |

**Ray 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `requiresLineOfSight` | `Toggle("requiresLineOfSight")` | 是否需要视线 |

**Circle 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| _(无额外字段)_ | -- | 仅使用基类 range / targetMask / maxTargets / targetFilter |

### [3] Status Bar

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `stats-summary` | `EditorStyles.miniLabel` | "N searches · X Cone · Y Ray · Z Circle" |
| `selected-indicator` | `EditorStyles.miniLabel` | "{name} ({typeName})"，仅选中时显示。typeName 由 `AbilityEditorUtility.GetSearchTypeDisplayName` 获取 |

### Popups

| 部件名 | 类型 | 触发位置 | 说明 |
|--------|------|----------|------|
| `create-menu` | `GenericMenu` | `create-btn` | 3 项: Cone / Ray / Circle |
| `import-export-window` | `SearchImportWindow` | `import-export-btn` | 独立 EditorWindow |

## 数据模型（内存状态）

| 字段 | 类型 | 说明 |
|------|------|------|
| `_allSearches` | `List<AbilitySearchSO>` | 全量扫描结果（`AssetDatabase.FindAssets("t:AbilitySearchSO")`） |
| `_treeRoots` | `List<EditorTreeNode>` | 树根节点（按 searchType 虚拟文件夹） |
| `_treeView` | `EditorTreeView` | 树组件实例，`OnEnable` 初始化 |
| `_selectedSearch` | `AbilitySearchSO` | 当前选中（右栏编辑目标） |
| `_searchText` | `string` | 搜索文本（赋值给 `_treeView.searchString`） |
| `_filter` | `SearchTypeFilter` | All / Cone / Ray / Circle |
| `_needsRefresh` | `bool` | 需要重建模型标记 |
| `_hasChanges` | `bool` | 脏标记，控制 Save 按钮 |
| `_rightScroll` | `Vector2` | 右栏 ScrollView 位置 |

> **无 `_treeNodeIndex` 字段**：树节点索引为 `BuildTree()` 内的局部 `Dictionary<string, EditorTreeNode> nodeIndex`，不暴露给 Window。
>
> **无 `_foldouts` 字段**：折叠状态由 `EditorTreeView` 内部管理。
>
> **无 `_baseForm` / `_typeForm` 字段**：与 NoiseEditor 一致，`EditorForm.Draw` 在绘制函数中局部创建，每次 OnGUI 即时构建。EditorForm 内部通过 `NeedsRebuild` 隐式控制渲染。

## 关键交互

1. **选中 Search**：点击左栏树的叶子 → `onSelect` 回调 → `SelectSearch(node.UserData as AbilitySearchSO)` → `_selectedSearch = search` → `Repaint()`
2. **改字段**：EditorForm.OnChange → `MarkDirty()` → `SetDirty + _hasChanges = true`
3. **改名**：RawField Name change → `RenameSearch()` → `AssetDatabase.RenameAsset()` → `_needsRefresh = true`
4. **删 Search**：树的 Delete 回调 → `DeleteSearch()` → `AbilityEditorUtility.DeleteAssetWithConfirm()`
5. **建 Search**：Create btn → `CreateNewSearch()` → GenericMenu → `CreateSearch<T>()` → `ScriptableObject.CreateInstance<T>()`
6. **过滤**：EditorButtonGroup 切换 → `_filter` 变化 → `OnFilterChanged()` → `_needsRefresh = true` + `BuildTree()` + `Repaint()`
7. **搜索**：EditorSearchBar 输入 → `_searchText` 变化 → 赋值给 `_treeView.searchString`（EditorTreeView 内部即时过滤，无 debounce）
8. **Save**：Save btn → `AssetDatabase.SaveAssets()` → `_hasChanges = false`
9. **Refresh**：Refresh btn → `RefreshAll()` → `_needsRefresh = true + 清除选中` → `Repaint()`

## SearchSO 类型体系

| 类型 | ESearchType | 形状 | 特有字段 | 用途 |
|------|-------------|------|---------|------|
| `ConeSearchSO` | `Cone` | 扇形 | `angle` (0-360) | 横斩、霰弹散射 |
| `RaySearchSO` | `RayLine` | 射线 | `requiresLineOfSight` | 手枪、步枪、刺击 |
| `CircleSearchSO` | `Circle` | 圆形 | _(无)_ | 旋风斩、光环、陷阱触发 |

> 所有子类在 `OnEnable()` 自设 `searchType`，禁止手动修改。

### 树分组方式

左栏树按 `AbilityEditorUtility.GetSearchTypeDisplayName(searchType)` 分组——即 "Cone" / "Ray" / "Circle" 三个虚拟文件夹。不像 EffectEditor 依赖 `effectTag` 的 parent chain 层级——SearchSO 没有标签字段，纯粹按枚举类型分桶。

```
Cone  →  📁 Cone  / 📄 Search_Cone_A / 📄 Search_Cone_B
Ray   →  📁 Ray   / 📄 Search_Ray_A
Circle →  📁 Circle / 📄 Search_Circle_A
```

## 依赖的共享组件

| 组件 | 用途 |
|------|------|
| `EditorCard.Draw` | 卡片容器 |
| `EditorCard.Gap / GapTight` | 间距 |
| `EditorButton.Draw` | 统一按钮（支持 Style/Size/enabled） |
| `EditorButtonGroup.Draw` | Filter 标签栏 |
| `EditorSearchBar.Draw` | 搜索行 |
| `EditorForm.Draw` | 自动表单（反射 + `EditorFormItem.*` 字段定义） |
| `EditorFormItem.RawField` | 自定义绘制字段（Name rename、targetMask LayerMaskField） |
| `EditorFormItem.Enum` | 枚举下拉字段 |
| `EditorFormItem.Float` | 浮点字段 |
| `EditorFormItem.Int` | 整数字段 |
| `EditorFormItem.Slider` | 滑条字段（Cone.angle） |
| `EditorFormItem.Toggle` | 开关字段（Ray.requiresLineOfSight） |
| `EditorUIUtility.GreyPlaceholder` | 灰色占位文字样式 |
| `EditorTokens` | 间距/样式/Pad 常量 |
| `EditorTreeView` | 通用树组件（搜索过滤、选中高亮、删除回调、折叠状态内部管理） |
| `EditorTreeNode` | 树节点数据结构（UserData 承载 `AbilitySearchSO`） |
| `EditorTree.SortTreeRecursive` | 树排序 |
| `EditorTree.ComputeTreeCounts` | 树节点计数 |
| `AbilityEditorUtility.GetSearchTypeDisplayName` | 将 `ESearchType` 转为显示名 |
| `AbilityEditorUtility.SearchMatchesFilter` | 判断 SearchSO 是否匹配 `SearchTypeFilter` |
| `AbilityEditorUtility.DeleteAssetWithConfirm` | 删除确认 + 删除资产 |
| `SearchImportWindow` | 导入导出窗口 |
| `SearchImporter.ExportToJson` | 导出到 JSON |

## 与 EffectEditor 的设计差异

| 维度 | EffectEditor | SearchEditor |
|------|-------------|-------------|
| 树分组 | `effectTag` parent chain（多级层级，跳过根标签） | `searchType` 枚举（单层虚拟文件夹） |
| 筛选组件 | `EditorButtonGroup.Draw` (6 tab) | `EditorButtonGroup.Draw` (4 tab) |
| 搜索组件 | `EditorSearchBar.Draw` | `EditorSearchBar.Draw` |
| 右栏标签 | TagPicker (Base 区 effectTag + Blocked Tags + Buff Granted Tags) | 无标签字段 |
| 子类字段 | 5 种类型各有 2-4 个不同字段 | Cone=1, Ray=1, Circle=0 |
| EditorForm 持有 | inline 局部创建（无实例字段） | inline 局部创建（无实例字段） |
| _needsRefresh | 无（用 RebuildTree 局部更新） | 有 |
| 左栏宽度 | 450px | 300px |
