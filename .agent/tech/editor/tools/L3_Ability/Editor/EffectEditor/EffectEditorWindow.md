# EffectEditorWindow

- **菜单**: `RedDust/Effect Editor`
- **源文件**: `Assets/Scripts/Services/Modules/L3_Ability/Editor/EffectEditor/EffectEditorWindow.cs`
- **最后验证**: 2026-07-08

## UI 结构全图

> 完整枚举 EffectEditorWindow 的所有 UI 区域，为修 UI 提供一致的部位命名。

### Window 总览

```
┌──────────────────────────────────────────────────────────────────────┐
│  EffectEditorWindow                                                  │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [1] HEADER BAR                                                  │ │
│  │  "Effect Editor" (large)          "L3_Ability · Editor"         │ │
│  │  [Refresh] [Import/Export] [+Create] [Save */Saved] [Ping]      │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [2] TWO-COLUMN BODY                                            │ │
│  │                                                                  │ │
│  │  ┌──────────────────────┐  ┌──────────────────────────────────┐ │ │
│  │  │ [2a] LEFT PANEL      │  │ [2b] RIGHT PANEL                  │ │ │
│  │  │  (450px)             │  │  (expand)                         │ │ │
│  │  │                      │  │                                   │ │ │
│  │  │ [2a-1] Filter TabBar │  │  [2b-title] "Edit: {name}"       │ │ │
│  │  │  [All][Dmg][Imp]     │  │                                   │ │ │
│  │  │  [Exe][Cost][Buf]    │  │  [2b-1] BASE SECTION             │ │ │
│  │  │                      │  │  ┌ EditorCard "Base" ──────────┐ │ │ │
│  │  │ [2a-2] Search Row    │  │  │ Name, effectTag, description, │ │ │ │
│  │  │  [🔍__________]      │  │  │ duration, stackable,          │ │ │ │
│  │  │                      │  │  │ maxStacks                     │ │ │ │
│  │  │ [2a-3] Tree          │  │  │ ── [2b-1a] Blocked Tags ──── │ │ │ │
│  │  │  📁 root1            │  │  │ Blocked Tags array    │ │ │ │
│  │  │    📄 Effect_A       │  │  │ [tag][Tag btn] per row        │ │ │ │
│  │  │    📄 Effect_B ◀─────│──│──│─ (selected → right)          │ │ │ │
│  │  │  📁 Uncategorized    │  │  └──────────────────────────────┘ │ │ │
│  │  │    📄 Effect_C       │  │                                   │ │ │
│  │  └──────────────────────┘  │  [2b-2] TYPE SECTION             │ │ │
│  │                            │  ┌ EditorCard "Damage" ─────────┐ │ │ │
│  │                            │  │ (or Impact/Execute/Cost/Buff) │ │ │ │
│  │                            │  │ baseValue, modAdd, modMult,   │ │ │ │
│  │                            │  │ priority / staggerValue etc.  │ │ │ │
│  │                            │  │ / grantedTags + adjuncts      │ │ │ │
│  │                            │  └──────────────────────────────┘ │ │ │
│  │                            └────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [3] STATUS BAR                                                  │ │
│  │  "42 effects · 15 Dmg · 12 Imp · 8 Exe · 5 Cost · 2 Buf"       │ │
│  │                                       "FireBlade (Damage)"      │ │
│  └────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘

POPUPS:
  - CreateNewEffect GenericMenu  (Damage / Impact / Execute / Cost / Buff)
  - TagPicker                    (tag selection popup, 3 处: effectTag, blocked tags, granted tags)
  - EffectImportWindow           (separate EditorWindow for import/export)
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
│   ├── DrawLeftColumn()                [2a] Left Panel (450px)
│   │   └── EditorCard.Draw
│   │       ├── EditorCard.Draw
│   │       │   └── DrawFilterCard()    [2a-1] Filter — EditorButtonGroup.Draw
│   │       ├── EditorCard.GapTight
│   │       ├── EditorCard.Draw
│   │       │   └── DrawSearchCard()    [2a-2] Search — EditorSearchBar.Draw
│   │       └── EditorTreeView.OnGUI()  [2a-3] Tree (lazy-created in DrawLeftColumn)
│   │
│   └── DrawRightColumn()               [2b] Right Panel
│       └── EditorCard.Draw
│           ├── Empty placeholder (if no selection)
│           ├── "Edit: {name}" (title)
│           ├── DrawBaseFields()        [2b-1] Base Section
│           │   └── EditorCard.Draw("Base")
│           │       └── EditorForm.Draw (inline, not stored as field)
│           │           ├── Name (RawField → TextField, rename via RenameEffect)
│           │           ├── effectTag (ObjectFieldWithTag<RdTagDefSO> + TagPicker btn)
│           │           ├── description (RawField → TextArea)
│           │           ├── duration (Float)
│           │           ├── stackable (Toggle)
│           │           ├── maxStacks (Int, visibleWhen: stackable)
│           │           └── Blocked Tags  [2b-1a] ArrayField<RdTagDefSO>
│           │
│           └── DrawTypeSpecificFields() [2b-2] Type Section
│               └── EditorCard.Draw("Damage"|"Impact"|"Execute"|"Cost"|"Buff")
│                   └── EditorForm.Draw (inline, 子类专属字段)
│
├── EditorCard.Gap(Pad)
│
└── DrawStatusBar()                     [3] Status Bar
    ├── stats-summary (总数 + 各类型计数, 含 Buff)
    └── selected-indicator (当前选中资产)
```

### 部位命名表

#### [1] Header Bar

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `title-label` | `EditorStyles.largeLabel` | "Effect Editor" |
| `breadcrumb-label` | `EditorTokens.BreadcrumbStyle` (right-aligned) | "L3_Ability · Editor" |
| `toolbar-row` | `EditorGUILayout.BeginHorizontal` | 按钮容器行 |
| `refresh-btn` | `EditorButton.Draw("Refresh")` | 重建 model + RebuildTree |
| `import-export-btn` | `EditorButton.Draw("Import/Export")` | 打开 EffectImportWindow |
| `create-btn` | `EditorButton.Draw("+ Create", Success)` | 弹出 GenericMenu (5 种子类) |
| `save-btn` | `EditorButton.Draw("Save *"/"Saved", Primary/Default)` | dirty 时变 Primary 样式 + 显示 * |
| `ping-btn` | `EditorButton.Draw("Ping")` | PingObject，仅 `_selectedEffect != null` 时显示 |

#### [2a] Left Panel

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `filter-tab-bar` | `EditorButtonGroup.Draw(_filter, ...)` | 6 个 tab: All / Dmg / Imp / Exe / Cost / Buf |
| `search-row` | `EditorSearchBar.Draw(_searchText, labelWidth: 42f)` | 单行文本框，过滤树节点 |
| `effect-tree` | `EditorTreeView.OnGUI(rect)` | effectTag 层级树 + Uncategorized 兜底 |

#### [2b] Right Panel

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `empty-placeholder` | `EditorUIUtility.GreyPlaceholder` | "Select an effect from the left panel." |
| `right-title` | `EditorCard.Draw($"Edit: {name}", ...)` | 卡片标题含当前编辑资产名 |

#### [2b-1] Base Section

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `base-section-card` | `EditorCard.Draw("Base")` | 整个 Base 卡片 |
| `base-form` | `EditorForm.Draw` (inline, not field) | 每次 OnGUI 即时创建，OnChange → MarkDirty |
| `name-field` | `RawField("Name")` → `TextField` | 编辑 .name，change 触发 RenameEffect |
| `effectTag-field` | `ObjectFieldWithTag<RdTagDefSO>("effectTag")` | Tag SO 对象字段 + "Tag" 按钮弹出 TagPicker；rootFilter 按子类（Damage/Impact/Effect）不同 |
| `description-field` | `RawField("description")` → `TextArea` | 描述文本 |
| `duration-field` | `Float("duration")` | |
| `stackable-toggle` | `Toggle("stackable")` | |
| `maxStacks-field` | `Int("maxStacks")` | `visibleWhen: stackable`, `onBeforeSet: Max(1,v)` |

#### [2b-1a] Blocked Tags Sub-section

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `blocked-tags-array` | `ArrayField<RdTagDefSO>("Blocked Tags")` | 阻止效果施加的标签列表 |
| `blocked-tag-row` | inline `drawRow` delegate | 单行 = ObjectField + Tag btn（Inline 绘制，无独立 Delete 按钮） |
| `blocked-tag-object` | `EditorGUILayout.ObjectField(tag, typeof(RdTagDefSO), false)` | 可拖入 RdTagDefSO |
| `blocked-tag-picker-btn` | `EditorButton.Default("Tag", Small, width: 35)` | 弹出 TagPicker (`APPLICATION_BLOCKED_TAGS`) |

#### [2b-2] Type Section

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `type-section-card` | `EditorCard.Draw(title)` | title 为 "Damage"/"Impact"/"Execute"/"Cost"/"Buff" |
| `type-form` | `EditorForm.Draw` (inline) | OnChange → MarkDirty |

**Damage 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `baseValue` | `Float` | |
| `modAdd` | `Float` | |
| `modMult` | `Float` | |
| `priority` | `Int` | |

**Impact 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `staggerValue` | `Float` | |
| `knockbackForce` | `Float` | |
| `knockbackDir` | `Enum<EKnockbackDirection>` | |

**Execute 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `hpThreshold` | `Slider(0, 1)` | |

**Cost 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `def` | `ObjectField<PropertyDefSO>` | 属性定义引用 |
| `amount` | `Float` | |

**Buff 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `grantedTags` | `ArrayField<RdTagDefSO>("Granted Tags")` | Buff 激活期间授予目标的标签，含 TagPicker 按钮 (`GRANTED_TAGS`) |
| `adjuncts` | `ArrayField<SBuffAdjunct>("Float Adjuncts")` | 属性修正列表，每行含 Property (ObjectField) + valueAdd (Float) + valueMultiply (Float) |

### [3] Status Bar

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `stats-summary` | `EditorStyles.miniLabel` | "N effects · X Dmg · Y Imp · Z Exe · W Cost · V Buf" |
| `selected-indicator` | `EditorStyles.miniLabel` | "{name} ({TypeName 去掉 EffectSO 后缀})"，仅选中时显示 |

### Popups

| 部件名 | 类型 | 触发位置 | 说明 |
|--------|------|----------|------|
| `create-menu` | `GenericMenu` | `create-btn` | 5 项: Damage / Impact / Execute / Cost / Buff |
| `effect-tag-picker` | `TagPicker.Show()` | `effectTag` 的 Tag 按钮 | rootFilter 按子类不同：DAMAGE / IMPACT / EFFECT |
| `blocked-tag-picker` | `TagPicker.Show()` | blocked-tag-row 的 Tag 按钮 | rootFilter: `APPLICATION_BLOCKED_TAGS` |
| `granted-tag-picker` | `TagPicker.Show()` | Buff granted-tags 的 Tag 按钮 | rootFilter: `GRANTED_TAGS` |
| `import-export-window` | `EffectImportWindow` | `import-export-btn` | 独立 EditorWindow |

## 数据模型（内存状态）

| 字段 | 类型 | 说明 |
|------|------|------|
| `_allEffects` | `List<EffectSO>` | 全量扫描结果 (`AssetDatabase.FindAssets("t:EffectSO")`) |
| `_treeRoots` | `List<EditorTreeNode>` | 树根节点（文件夹） |
| `_treeNodeIndex` | `Dictionary<string, EditorTreeNode>` | FullPath → Node，建树用 |
| `_treeView` | `EditorTreeView` | 树组件实例，`OnEnable` 初始化；左栏 lazy-created |
| `_selectedEffect` | `EffectSO` | 当前选中（右栏编辑目标） |
| `_searchText` | `string` | 搜索文本 |
| `_filter` | `EffectTypeFilter` | All / Damage / Impact / Execute / Cost / Buff |
| `_hasChanges` | `bool` | 脏标记，控制 Save 按钮 |
| `_rightScroll` | `Vector2` | 右栏 ScrollView 位置 |
| `_effectTagButtonRect` | `Rect` | effectTag 的 TagPicker 弹出定位 |
| `_blockedTagButtonRect` | `Rect` | blocked tag 的 TagPicker 弹出定位 |
| `_grantedTagButtonRect` | `Rect` | granted tag (Buff) 的 TagPicker 弹出定位 |

> **无 `_foldouts` 字段**：折叠状态由 `EditorTreeView` 内部管理，不暴露给 Window。
>
> **无 `_baseForm` / `_typeForm` 字段**：与 NoiseEditor/SearchEditor 一致，`EditorForm.Draw` 在 `DrawBaseFields`/`DrawTypeSpecificFields` 中局部创建，每次 OnGUI 即时构建。
>
> **无 `_needsRefresh` 字段**：`RefreshModel()` 在 `OnEnable` 调用一次，后续通过 `RebuildTree()` 局部更新。

## 关键交互

1. **选中 Effect**：点击左栏树的叶子 → `onSelect` 回调 → `SelectEffect(node.UserData as EffectSO)` → `_selectedEffect = effect` → `Repaint()`
2. **改字段**：EditorForm.OnChange → `MarkDirty()` → `EditorUtility.SetDirty + _hasChanges = true`
3. **改名**：RawField Name change → `RenameEffect()` → `AssetDatabase.RenameAsset()` → `RefreshModel() + RebuildTree()`
4. **删 Effect**：树的 Delete 回调 → `DeleteEffect()` → `AbilityEditorUtility.DeleteAssetWithConfirm()`
5. **建 Effect**：Create btn → `CreateNewEffect()` → GenericMenu → `CreateEffect<T>()` → `ScriptableObject.CreateInstance<T>()` → `RefreshModel() + RebuildTree()`
6. **过滤**：EditorButtonGroup 切换 → `_filter` 变化 → `RebuildTree()` → `_treeView.SetData(...)`
7. **搜索**：EditorSearchBar 输入 → `_searchText` 变化 → `RebuildTree()` → `_treeView.SetData(...)`
8. **Save**：Save btn → `AssetDatabase.SaveAssets()` → `_hasChanges = false`
9. **Refresh**：Refresh btn → `RefreshModel()` → `RebuildTree()`

## 依赖的共享组件

| 组件 | 用途 |
|------|------|
| `EditorCard.Draw` | 卡片容器 |
| `EditorCard.Gap / GapTight` | 间距 |
| `EditorButton.Draw` | 统一按钮（支持 Style/Size/enabled） |
| `EditorButton.Default` | 小号标签按钮（Tag 按钮） |
| `EditorButtonGroup.Draw` | Filter 标签栏 |
| `EditorSearchBar.Draw` | 搜索行 |
| `EditorForm.Draw` | 自动表单（反射 + `EditorFormItem.*` 字段定义） |
| `EditorFormItem.RawField` | 自定义绘制字段（Name rename、description TextArea） |
| `EditorFormItem.ObjectFieldWithTag<T>` | Tag SO 对象字段 + TagPicker 按钮合一 |
| `EditorFormItem.Float` | 浮点字段 |
| `EditorFormItem.Int` | 整数字段 |
| `EditorFormItem.Toggle` | 开关字段 |
| `EditorFormItem.Slider` | 滑条字段（Execute.hpThreshold） |
| `EditorFormItem.Enum` | 枚举下拉字段 |
| `EditorFormItem.ArrayField<T>` | 数组字段（Blocked Tags, Granted Tags, Adjuncts） |
| `EditorFormItem.ObjectField<T>` | SO 对象字段 |
| `EditorUIUtility.GreyPlaceholder` | 灰色占位文字样式 |
| `EditorTokens` | 间距/样式/Pad 常量 |
| `EditorTreeView` | 通用树组件（搜索过滤、选中高亮、删除回调、折叠状态内部管理） |
| `EditorTreeNode` | 树节点数据结构（UserData 承载 EffectSO） |
| `EditorTree.SortTreeRecursive` | 树排序 |
| `EditorTree.ComputeTreeCounts` | 树节点计数 |
| `TagPicker.Show` | 标签选择弹窗 |
| `AbilityEditorUtility.DeleteAssetWithConfirm` | 删除确认 + 删除资产 |
| `EffectImportWindow` | 导入导出窗口 |
| `EffectImporter.ExportToJson` | 导出到 JSON |
