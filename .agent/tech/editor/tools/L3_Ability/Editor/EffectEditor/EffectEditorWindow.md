# EffectEditorWindow

- **菜单**: `RedDust/Effect Editor`
- **源文件**: `Assets/Scripts/Services/Modules/L3_Ability/Editor/EffectEditor/EffectEditorWindow.cs`

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
│  │  │  (300px)             │  │  (expand)                         │ │ │
│  │  │                      │  │                                   │ │ │
│  │  │ [2a-1] Filter TabBar │  │  [2b-title] "Edit: {name}"       │ │ │
│  │  │  [All][Dmg][Imp]     │  │                                   │ │ │
│  │  │  [Exe][Cost]         │  │  [2b-1] BASE SECTION             │ │ │
│  │  │                      │  │  ┌ EditorCard "Base" ──────────┐ │ │ │
│  │  │ [2a-2] Search Row    │  │  │ Name, effectTag, duration,    │ │ │ │
│  │  │  [🔍__________]      │  │  │ stackable, maxStacks          │ │ │ │
│  │  │                      │  │  │ ── [2b-1a] Blocked Tags ──── │ │ │ │
│  │  │ [2a-3] Ability Tree  │  │  │ applicationBlockedTags[N]    │ │ │ │
│  │  │  📁 root1            │  │  │ [tag][Tag btn][×] per row    │ │ │ │
│  │  │    📄 Effect_A       │  │  │ [+ Add Blocked Tag]          │ │ │ │
│  │  │    📄 Effect_B ◀─────│──│──│─ (selected → right)          │ │ │ │
│  │  │  📁 Uncategorized    │  │  └──────────────────────────────┘ │ │ │
│  │  │    📄 Effect_C       │  │                                   │ │ │
│  │  └──────────────────────┘  │  [2b-2] TYPE SECTION             │ │ │
│  │                            │  ┌ EditorCard "Damage" ─────────┐ │ │ │
│  │                            │  │ (or Impact/Execute/Cost)      │ │ │ │
│  │                            │  │ baseValue, modAdd, modMult,   │ │ │ │
│  │                            │  │ priority / staggerValue etc.  │ │ │ │
│  │                            │  └──────────────────────────────┘ │ │ │
│  │                            └────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [3] STATUS BAR                                                  │ │
│  │  "42 effects · 15 Dmg · 12 Imp · 8 Exe · 7 Cost"               │ │
│  │                                       "FireBlade (Damage)"      │ │
│  └────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘

POPUPS:
  - CreateNewEffect GenericMenu  (Damage / Impact / Execute / Cost)
  - TagPicker                    (tag selection popup)
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
│   ├── DrawLeftColumn()                [2a] Left Panel (300px)
│   │   └── EditorCard.Draw
│   │       ├── DrawFilterCard()        [2a-1] Filter Tab Bar
│   │       ├── DrawSearchCard()        [2a-2] Search Row
│   │       └── AbilityTreeView.DrawTree()  [2a-3] Ability Tree
│   │
│   └── DrawRightColumn()               [2b] Right Panel
│       └── EditorCard.Draw
│           ├── Empty placeholder (if no selection)
│           ├── "Edit: {name}" (title)
│           ├── DrawBaseFields()        [2b-1] Base Section
│           │   └── EditorCard.Draw("Base")
│           │       ├── EditorForm _baseForm (Name, effectTag, duration, stackable, maxStacks)
│           │       └── DrawBlockedTags()  [2b-1a] Blocked Tags Sub-section
│           │
│           └── DrawTypeSpecificFields() [2b-2] Type Section
│               └── EditorCard.Draw("Damage"|"Impact"|"Execute"|"Cost")
│                   └── EditorForm _typeForm (子类专属字段)
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
| `title-label` | `EditorStyles.largeLabel` | "Effect Editor" |
| `breadcrumb-label` | `EditorStyles.label` (right-aligned) | "L3_Ability · Editor" |
| `toolbar-row` | `EditorGUILayout.BeginHorizontal` | 按钮容器行 |
| `refresh-btn` | `EditorButton.Draw("Refresh")` | 重建树 + 清除选中 |
| `import-export-btn` | `EditorButton.Draw("Import/Export")` | 打开 EffectImportWindow |
| `create-btn` | `EditorButton.Draw("+ Create", Success)` | 弹出 GenericMenu (4 种子类) |
| `save-btn` | `EditorButton.Draw("Save *"/"Saved", Primary/Default)` | dirty 时变 Primary 样式 + 显示 * |
| `ping-btn` | `EditorButton.Draw("Ping")` | PingObject，仅 `_selectedEffect != null` 时显示 |

#### [2a] Left Panel

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `filter-tab-bar` | `EditorUIUtility.DrawFilterTabBar` | 5 个 tab: All / Dmg / Imp / Exe / Cost |
| `search-row` | `EditorUIUtility.DrawSearchRow` | 单行文本框，过滤树节点 |
| `ability-tree` | `AbilityTreeView.DrawTree` | effectTag 层级树 + Uncategorized 兜底 |

#### [2b] Right Panel

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `empty-placeholder` | `EditorUIUtility.GreyPlaceholder` | "Select an effect from the left panel." |
| `right-title` | `EditorStyles.boldLabel` | "Edit: {name}" |

#### [2b-1] Base Section

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `base-section-card` | `EditorCard.Draw("Base")` | 整个 Base 卡片 |
| `base-form` | `EditorForm _baseForm` | 表单实例，`NeedsRebuild` 控制重建 |
| `name-field` | `RawField("Name")` → `TextField` | 编辑 .name，change 触发 RenameEffect |
| `effectTag-field` | `ObjectField<GameplayTagDefinitionSO>("effectTag")` | SO 对象字段 + "Tag" 按钮弹出 TagPicker |
| `effectTag-picker-btn` | `EditorButton.Draw("Tag")` | 触发 `TagPicker.Show(_effectTagButtonRect)` |
| `duration-field` | `Float("duration")` | |
| `stackable-toggle` | `Toggle("stackable")` | |
| `maxStacks-field` | `Int("maxStacks")` | `visibleWhen: stackable`, `onBeforeSet: Max(1,v)` |

#### [2b-1a] Blocked Tags Sub-section

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `blocked-tags-header` | `EditorStyles.miniBoldLabel` | "applicationBlockedTags [N]" |
| `blocked-tag-row` | `BeginHorizontal` | 单行 = ObjectField + Tag btn + Delete btn |
| `blocked-tag-object` | `EditorGUILayout.ObjectField` | 可拖入 GameplayTagDefinitionSO |
| `blocked-tag-picker-btn` | `EditorButton.Draw("Tag")` | 弹出 TagPicker 选择替换 |
| `blocked-tag-delete-btn` | `EditorUIUtility.DeleteButton()` | × 按钮，标记 `removeAt = i` |
| `add-blocked-tag-btn` | `EditorButton.Draw("+ Add Blocked Tag")` | 追加 null 项到数组末尾 |

#### [2b-2] Type Section

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `type-section-card` | `EditorCard.Draw(title)` | title 为 "Damage"/"Impact"/"Execute"/"Cost" |
| `type-form` | `EditorForm _typeForm` | `NeedsRebuild` 控制，OnAnyChange → MarkDirty |

**Damage 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `baseValue` | `Float` | 带 TooltipAttribute |
| `modAdd` | `Float` | 带 TooltipAttribute |
| `modMult` | `Float` | 带 TooltipAttribute |
| `priority` | `Int` | 带 TooltipAttribute |

**Impact 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `staggerValue` | `Float` | |
| `knockbackForce` | `Float` | |
| `knockbackDir` | `Enum<EKnockbackDirection>` | |

**Execute 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `hpThreshold` | `Slider(0,1)` | |

**Cost 子表单：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `def` | `ObjectField<PropertyDefSO>` | 属性定义引用 |
| `amount` | `Float` | |

### [3] Status Bar

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `stats-summary` | `EditorStyles.miniLabel` | "N effects · X Dmg · Y Imp · Z Exe · W Cost" |
| `selected-indicator` | `EditorStyles.miniLabel` | "{name} ({Type})"，仅选中时显示 |

### Popups

| 部件名 | 类型 | 触发位置 | 说明 |
|--------|------|----------|------|
| `create-menu` | `GenericMenu` | `create-btn` | 4 项: Damage / Impact / Execute / Cost |
| `effect-tag-picker` | `TagPicker.Show()` | `effectTag-picker-btn` | 选择 GameplayTagDefinitionSO |
| `blocked-tag-picker` | `TagPicker.Show()` | `blocked-tag-picker-btn` | 替换 blocked tag 单项 |
| `import-export-window` | `EffectImportWindow` | `import-export-btn` | 独立 EditorWindow |

## 数据模型（内存状态）

| 字段 | 类型 | 说明 |
|------|------|------|
| `_allEffects` | `List<EffectSO>` | 全量扫描结果 |
| `_treeRoots` | `List<AbilityTreeNode>` | 树根节点（文件夹） |
| `_treeNodeIndex` | `Dictionary<string, AbilityTreeNode>` | FullPath → Node，建树用 |
| `_selectedEffect` | `EffectSO` | 当前选中（右栏编辑目标） |
| `_searchText` | `string` | 搜索文本 |
| `_filter` | `EffectTypeFilter` | All / Damage / Impact / Execute / Cost |
| `_baseForm` | `EditorForm` | 基础字段表单实例 |
| `_typeForm` | `EditorForm` | 子类字段表单实例 |
| `_foldouts` | `Dictionary<string, bool>` | 树节点展开/折叠状态 |
| `_hasChanges` | `bool` | 脏标记，控制 Save 按钮 |
| `_needsRefresh` | `bool` | 需要重建模型标记 |

## 关键交互

1. **选中 Effect**：点击左栏树的叶子 → `SelectEffect()` → `_selectedEffect = effect` → `Repaint()`
2. **改字段**：EditorForm.OnAnyChange → `MarkDirty()` → `SetDirty + _hasChanges = true`
3. **改名**：RawField change → `RenameEffect()` → `AssetDatabase.RenameAsset()` → `_needsRefresh = true`
4. **删 Effect**：树的 Delete 回调 → `DeleteEffect()` → `AbilityEditorUtility.DeleteAssetWithConfirm()`
5. **建 Effect**：Create btn → `CreateNewEffect()` → GenericMenu → `CreateEffect<T>()` → `ScriptableObject.CreateInstance<T>()`
6. **过滤**：TabBar 切换 → `_filter` 变化 → `OnFilterChanged()` → `BuildTree() + 清除 foldouts`
7. **搜索**：SearchRow 输入 → `_searchText` 变化（无 debounce）
8. **Save**：Save btn → `AssetDatabase.SaveAssets()` → `_hasChanges = false`
9. **Refresh**：Refresh btn → `RefreshAll()` → `_needsRefresh = true + 清除选中 + foldouts`

## 依赖的共享组件

| 组件 | 用途 |
|------|------|
| `EditorCard.Draw/DrawLight` | 卡片容器（默认/浅色） |
| `EditorCard.Gap/GapTight` | 间距 |
| `EditorButton.Draw` | 统一按钮（支持 Style/Size/enabled） |
| `EditorForm` | 自动表单（反射 + Builder 模式） |
| `EditorUIUtility.DrawFilterTabBar` | Filter 标签栏 |
| `EditorUIUtility.DrawSearchRow` | 搜索行 |
| `EditorUIUtility.DeleteButton` | 删除按钮（×） |
| `EditorUIUtility.GreyPlaceholder` | 灰色占位文字样式 |
| `AbilityTreeView.DrawTree` | 通用 Ability 树组件 |
| `AbilityTreeNode` | 树节点数据结构 |
| `TagPicker.Show` | 标签选择弹窗 |
| `AbilityEditorUtility` | 共用工具（数组、树排序、摘要、删除确认） |
| `EffectImportWindow` | 导入导出窗口 |
| `EffectImporter.ExportToJson` | 导出到 JSON |
