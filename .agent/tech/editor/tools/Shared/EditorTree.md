# EditorTree 完整迁移清单

> **状态**: EffectEditorWindow ✅ 已迁移。AbilityTreeNode / AbilityTreeView 已删除，统一使用 `EditorTreeNode` / `EditorTreeView` (Shared/EditorUI)

> 所有跟 Tree 渲染相关的文件，按影响范围排序。

## 树渲染组件（3 个自定义 + 1 个内联）

| # | 组件 | 文件 | 节点类型 | 调用方 |
|---|---|---|---|---|
| 1 | `AbilityTreeView` | `L3_Ability/Editor/_Shared/AbilityTreeView.cs` | `AbilityTreeNode` | AbilityEditorWindow, SubAssetPickerView, ActivationEditorWindow, NoiseEditorWindow, SearchEditorWindow (5 处) |
| 2 | `PropertyTreeListView` | `L3_Properties/Editor/PropertyTreeListView.cs` | `PropertyTreeListItem` | PropertyTreeEditorWindow (1 处) |
| 3 | `TagTreeView` | `L1_Core/GameplayTag/Editor/TagTreeView.cs` | `TagNode` | TagEditorWindow, TagPicker (2 处) |
| 4 | 内联 `DrawCenterNode` | `L3_Properties/Editor/PropertyTreeEditorWindow.cs` | 内部 `CenterTreeNode` | 自身 (拖拽/折叠/搜索) |

## 自定义节点类型（4 种）

| # | 类型 | 文件 | 何时删除 |
|---|---|---|---|
| 1 | `AbilityTreeNode` | `L3_Ability/Editor/_Shared/AbilityTreeNode.cs` | 全部迁移后 |
| 2 | `PropertyTreeListItem` | `L3_Properties/Editor/PropertyTreeListItem.cs` | 全部迁移后 |
| 3 | `TagNode` | `L1_Core/GameplayTag/Editor/TagNode.cs` | 全部迁移后 |
| 4 | `CenterTreeNode` (内部类) | `PropertyTreeEditorWindow.cs` | 全部迁移后 |

## 建树方法（6 处 BuildTree/RefreshModel）

| # | 文件 | 方法 | 构建节点类型 |
|---|---|---|---|
| 1 | `AbilityEditorModel.cs` | `BuildTree` | `AbilityTreeNode` |
| 2 | `ActivationEditorWindow.cs` | `BuildTree` | `AbilityTreeNode` |
| 3 | `NoiseEditorWindow.cs` | `BuildTree` | `AbilityTreeNode` |
| 4 | `SearchEditorWindow.cs` | `BuildTree` | `AbilityTreeNode` |
| 5 | `TagTreeModel.cs` | 构造函数 | `TagNode` |
| 6 | `PropertyTreeEditorWindow.cs` | `RefreshModel` / `RebuildCenterTree` | `PropertyTreeListItem` / `CenterTreeNode` |

## 工具方法（迁移后清理）

| 文件 | 方法 |
|---|---|
| `AbilityEditorUtility.cs` | `SortTreeRecursive(AbilityTreeNode)`, `ComputeTreeCounts(AbilityTreeNode)` |
| `EditorTree.cs` | `SortTreeRecursive(EditorTreeNode)`, `ComputeTreeCounts(EditorTreeNode)` ✅ 保留 |

## 状态字段（所有窗口的旧 Tree 状态）

| 窗口 | 需清理字段 |
|---|---|
| AbilityEditorWindow | `_foldouts` |
| SubAssetPickerView | `_effectFoldouts` |
| ActivationEditorWindow | `_foldouts`, `_treeRoots(AbilityTreeNode)` |
| NoiseEditorWindow | `_foldouts`, `_treeRoots(AbilityTreeNode)` |
| SearchEditorWindow | `_foldouts`, `_treeRoots(AbilityTreeNode)` |
| TagEditorWindow | `_foldouts`, `_treeScroll` |
| TagPicker | `_foldouts` |
| PropertyTreeEditorWindow | `_treeFoldouts`, `_centerFoldouts`, `_leftTreeRoots`, `_centerTreeRoots` |

## 已迁移

| 文件 | 状态 |
|---|---|
| `EffectEditorWindow` | ✅ `EditorTreeView` + `EditorTreeNode` |
