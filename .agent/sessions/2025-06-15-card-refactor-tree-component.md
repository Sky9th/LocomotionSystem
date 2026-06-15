# 2025-06-15 Card 重构 + EditorTreeView 组件

## 改动范围

23 个文件，893+ 行增，468- 行删。

## Card API 简化

**前**：`Draw`/`DrawLight`/`DrawFoldout`/`DrawItem`，带 `selected`/`borderless`/`pad` 参数。
**后**：仅 `Draw(Action)` 和 `Draw(string title, Action)`。样式全内置，`CardStyle` 继承 `EditorStyles.helpBox`。

- `DrawLight` → `Draw`（所有调用方适配）
- `DrawFoldout`/`DrawItem` → `[Obsolete]`
- `selected`/`borderless`/`pad` 参数全部移除
- `DrawCardHeader` 被移除，调用方自行内联 header 渲染

## EditorTreeView 组件

基于 `UnityEditor.IMGUI.Controls.TreeView`：
- `EditorTreeView` — `SetData(roots, onSelect, onDelete)` 绑定 `EditorTreeNode`
- 折叠/选中/右键删除全部内置
- `EditorTree` 保留为工具类（`CreateDemoData`, `SortTreeRecursive`, `ComputeTreeCounts`）

## EffectEditorWindow 迁移

- 左栏切到 `EditorTreeView`
- 恢复 Filter（ButtonGroup）+ Search（SearchBar）+ 外层 Card + 内层树 Card
- `TreeView` 隐藏底色问题通过 `showBorder=false` + Card `borderless` 缓解，未彻底解决

## Tag 系统迁移（已回滚）

尝试将 TagEditorWindow + TagPicker 切到 EditorTreeView，因 TreeView 初始化问题失败，已 `git checkout` 回滚。保留旧 `TagTreeView`/`TagNode` 不变。

## 其他

- `EditorTokens` 新增 `ColorCardBg`/`ColorCardBorder`
- `EditorLabel` 新增自适应宽度 `Draw(string, tooltip, style)` 重载
- 所有 16+ 调用方适配新 Card API
