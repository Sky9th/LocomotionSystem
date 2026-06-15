# NoiseEditorWindow

- **菜单**: `RedDust/Noise Editor`
- **源文件**: `Assets/Scripts/Services/Modules/L3_Ability/Editor/NoiseEditor/NoiseEditorWindow.cs`
- **相关文档**: [NoiseImportExport](NoiseImportExport.md)

## UI 结构全图

> 完整枚举 NoiseEditorWindow 的所有 UI 区域，为修 UI 提供一致的部位命名。

### Window 总览

```
┌──────────────────────────────────────────────────────────────────────┐
│  NoiseEditorWindow                                                    │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [1] HEADER BAR                                                  │ │
│  │  "Noise Editor" (large)          "L3_Ability · Editor"          │ │
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
│  │  │ [2a-1] Search Row    │  │  [2b-title] "Edit: {name}"       │ │ │
│  │  │  [🔍__________]      │  │                                   │ │ │
│  │  │                      │  │  ┌ EditorCard (no title) ───────┐ │ │ │
│  │  │ [2a-2] Ability Tree  │  │  │ [2b-1] Name                  │ │ │ │
│  │  │  📁 Noise            │  │  │ [2b-2] Noise Type + Tag btn  │ │ │ │
│  │  │    📁 Combat         │  │  │ [2b-3] level                 │ │ │ │
│  │  │      📄 Noise_Lv5_A │  │  │ [2b-4] decayRadius           │ │ │ │
│  │  │      📄 Noise_Lv5_B ◀│──│──│─ (selected → right)          │ │ │ │
│  │  │    📁 World           │  │  └──────────────────────────────┘ │ │ │
│  │  │  📁 Uncategorized    │  │                                   │ │ │
│  │  │    📄 Noise_NoTag    │  │                                   │ │ │
│  │  └──────────────────────┘  │                                   │ │ │
│  │                            └────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ [3] STATUS BAR                                                  │ │
│  │  "44 noises"                                                    │ │
│  │                            "Noise_Lv5_Pistol (level:5)"          │ │
│  └────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────┘

POPUPS:
  - TagPicker (noiseType 选择)
  - NoiseImportWindow (separate EditorWindow for import/export)
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
│   │       ├── EditorCard.DrawLight → EditorSearchBar.Draw()  [2a-1] Search Row
│   │       └── AbilityTreeView.DrawTree()                      [2a-2] Ability Tree
│   │
│   └── DrawRightColumn()               [2b] Right Panel
│       └── EditorCard.Draw
│           ├── Empty placeholder (if no selection)
│           ├── "Edit: {name}" (title)
│           └── DrawEditForm()           [2b-1~4] Single Form
│               └── EditorForm (Name, noiseType + Tag btn, level, decayRadius)
│
├── EditorCard.Gap(Pad)
│
└── DrawStatusBar()                     [3] Status Bar
    ├── stats-summary ("N noises")
    └── selected-indicator ("{name} (level:{F0})")
```

### 部位命名表

#### [1] Header Bar

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `title-label` | `EditorStyles.largeLabel` | "Noise Editor" |
| `breadcrumb-label` | `EditorStyles.label` (right-aligned) | "L3_Ability · Editor" |
| `toolbar-row` | `EditorGUILayout.BeginHorizontal` | 按钮容器行 |
| `refresh-btn` | `EditorButton.Draw("Refresh")` | 重建树 + 清除选中 |
| `import-export-btn` | `EditorButton.Draw("Import/Export")` | 打开 NoiseImportWindow |
| `create-btn` | `EditorButton.Draw("+ Create", Success)` | 直接创建 NoiseEventSO（单类型，无 GenericMenu） |
| `save-btn` | `EditorButton.Draw("Save *"/"Saved", Primary/Default)` | dirty 时变 Primary 样式 + 显示 * |
| `ping-btn` | `EditorButton.Draw("Ping")` | PingObject，仅 `_selectedNoise != null` 时显示 |

#### [2a] Left Panel

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `search-row` | `EditorSearchBar.Draw` | 单行文本框 (`labelWidth: 42f`)，过滤树节点 |
| `ability-tree` | `AbilityTreeView.DrawTree` | noiseType 标签层级树 + Uncategorized 兜底 |

#### [2b] Right Panel

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `empty-placeholder` | `EditorUIUtility.GreyPlaceholder` | "Select a noise from the left panel." |
| `right-title` | `EditorStyles.boldLabel` | "Edit: {name}" |

#### [2b-1~4] Single Form（无 Base/Type 分区）

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `name-field` | `RawField("Name")` → `TextField` | 编辑 `.name`，change 触发 `RenameNoise` |
| `noiseType-field` | `ObjectField<GameplayTagDefinitionSO>("noiseType")` | SO 对象字段 |
| `noiseType-tag-btn` | `EditorButton.Draw("Tag")` | 触发 `TagPicker.Show(_noiseTagButtonRect)` |
| `level-field` | `Float("level")` | 噪音等级，0=无声 |
| `decayRadius-field` | `Float("decayRadius")` | 衰减半径（米） |

### [3] Status Bar

| 部件名 | 控件 | 说明 |
|--------|------|------|
| `stats-summary` | `EditorStyles.miniLabel` | "N noises" |
| `selected-indicator` | `EditorStyles.miniLabel` | "{name} (level:{level:F0})"，仅选中时显示 |

### Popups

| 部件名 | 类型 | 触发位置 | 说明 |
|--------|------|----------|------|
| `noise-type-picker` | `TagPicker.Show()` | `noiseType-tag-btn` | 选择 noiseType 标签，选中后触发 `_needsRefresh = true`（重建树） |
| `import-export-window` | `NoiseImportWindow` | `import-export-btn` | 独立 EditorWindow |

## 数据模型（内存状态）

| 字段 | 类型 | 说明 |
|------|------|------|
| `_allNoises` | `List<NoiseEventSO>` | 全量扫描结果（`AssetDatabase.FindAssets("t:NoiseEventSO")`） |
| `_treeRoots` | `List<AbilityTreeNode>` | 树根节点（按 noiseType 的 parent 链构建多级文件夹） |
| `_treeNodeIndex` | `Dictionary<string, AbilityTreeNode>` | FullPath → Node，建树用 |
| `_selectedNoise` | `NoiseEventSO` | 当前选中（右栏编辑目标） |
| `_searchText` | `string` | 搜索文本 |
| `_foldouts` | `Dictionary<string, bool>` | 树节点展开/折叠状态 |
| `_hasChanges` | `bool` | 脏标记，控制 Save 按钮 |
| `_needsRefresh` | `bool` | 需要重建模型标记 |
| `_noiseTagButtonRect` | `Rect` | TagPicker 弹出定位（Repaint 时回写） |

> **无 EditorForm 字段**：与 SearchEditor 相同，`DrawEditForm` 中局部创建 `EditorForm.Draw`，每次 OnGUI 重建表单。

## 关键交互

1. **选中 Noise**：点击左栏树的叶子 → `SelectNoise()` → `_selectedNoise = noise` → `Repaint()`
2. **改字段**：EditorForm.OnChange → `MarkDirty()` → `SetDirty + _hasChanges = true`
3. **改名**：RawField change → `RenameNoise()` → `AssetDatabase.RenameAsset()` → `_needsRefresh = true`
4. **改 noiseType 标签**：Tag 按钮 → TagPicker 选择 → `n.noiseType = t` → `_needsRefresh = true`（需重建树，因为树按标签 parent 链分组）
5. **删 Noise**：树的 Delete 回调 → `DeleteNoise()` → `AbilityEditorUtility.DeleteAssetWithConfirm()`
6. **建 Noise**：Create btn → `CreateNewNoise()` → 直接 `ScriptableObject.CreateInstance<NoiseEventSO>()`（单类型，无菜单）
7. **搜索**：EditorSearchBar 输入 → `_searchText` 变化（无 debounce）
8. **Save**：Save btn → `AssetDatabase.SaveAssets()` → `_hasChanges = false`
9. **Refresh**：Refresh btn → `RefreshAll()` → `_needsRefresh = true + 清除选中 + foldouts`

## NoiseEventSO 数据模型

| 字段 | 类型 | 说明 |
|------|------|------|
| `noiseType` | `GameplayTagDefinitionSO` | 噪音类型标签（`Noise.Combat.WeaponFire` 等），AI 行为路由 |
| `level` | `float` | 噪音等级。0=无声，越大传播越远 |
| `decayRadius` | `float` | 衰减半径（米），超出此距离 AI 听不到 |

> NoiseEventSO 是单一密封类（`sealed`），无子类。因此右栏没有子类分支——单一表单即可覆盖全部字段。
>
> 噪音消费方为 AI 听觉系统（`①②⑧` 维度中的 ⑧ Broadcast）。实际音效由动画事件驱动，不由此 SO 播放。

### 树分组方式

左栏树按 `noiseType` GameplayTag 的 **parent 链** 构建多级文件夹（类似 EffectEditor 的 `effectTag` 层级）。无 noiseType 的资产放入 `"Uncategorized"` 兜底文件夹。

```
Noise.Combat.WeaponFire  →   📁 Combat / 📁 WeaponFire / 📄 Noise_Lv5_Pistol
Noise.World.Footstep     →   📁 World / 📁 Footstep / 📄 Noise_Lv2_Step
null (无标签)            →   📁 Uncategorized / 📄 Noise_NoTag
```

## 依赖的共享组件

| 组件 | 用途 |
|------|------|
| `EditorCard.Draw/DrawLight` | 卡片容器（默认/浅色） |
| `EditorCard.Gap` | 间距 |
| `EditorButton.Draw` | 统一按钮（支持 Style/Size/enabled） |
| `EditorSearchBar.Draw` | 搜索行 |
| `EditorForm.Draw` | 自动表单 |
| `EditorFormItem.RawField` | 自定义 Name 字段（rename 回调） |
| `EditorFormItem.ObjectField` | noiseType SO 对象字段 |
| `EditorFormItem.Float` | 浮点字段（level, decayRadius） |
| `EditorUIUtility.GreyPlaceholder` | 灰色占位文字样式 |
| `AbilityTreeView.DrawTree` | 通用 Ability 树组件 |
| `AbilityTreeNode` | 树节点数据结构（Noise 属性承载 `NoiseEventSO`） |
| `AbilityEditorUtility.SortTreeRecursive` | 树排序 |
| `AbilityEditorUtility.ComputeTreeCounts` | 树节点计数 |
| `AbilityEditorUtility.DeleteAssetWithConfirm` | 删除确认 + 删除资产 |
| `TagPicker.Show` | 标签选择弹窗 |
| `NoiseImportWindow` | 导入导出窗口 |
| `NoiseImporter.ExportToJson` | 导出到 JSON |

## 与其他编辑器的设计差异

| 维度 | EffectEditor | SearchEditor | NoiseEditor |
|------|-------------|-------------|-------------|
| 树分组 | `effectTag` parent chain（多级层级） | `searchType` 枚举（单层文件夹） | `noiseType` parent chain（多级层级） |
| 筛选 | TabBar (All/Dmg/Imp/Exe/Cost) | EditorButtonGroup (All/Cone/Ray/Circle) | 无筛选（仅搜索） |
| 右栏结构 | Base + Type 两卡片分区 | Base + Type 两卡片分区 | 单一表单（无分区） |
| 子类分支 | 4 种（Damage/Impact/Execute/Cost） | 3 种（Cone/Ray/Circle） | 无（单一 sealed 类） |
| TagPicker | effectTag + Blocked Tags（多处） | 无 | noiseType（1 处） |
| Create 菜单 | GenericMenu (4 项) | GenericMenu (3 项) | 直接创建（无菜单） |
| StatusBar | 4 分类计数 | 3 分类计数 | 仅总数 + level 显示 |

