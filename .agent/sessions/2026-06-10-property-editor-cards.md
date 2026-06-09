# 2026-06-10 — PropertyTreeEditor 卡片 UI 实现

## 背景

L3_Properties 系统落地后，需要 Editor 工具来可视化编辑 PropertyTree 的节点结构。

## 完成内容

### PropertyTreeEditorWindow 中间栏卡片化

**Folder 卡片**：
- 布局：`[▶/▼] | [___Name___] | [×]`
- 折叠按钮固定 20px 左列
- TextField 可编辑 Folder 名称（Enter 确认重命名）
- 删除按钮右对齐（红色，带确认对话框）

**Property 卡片**：
- 布局：`Name | Type(右对齐) | [×]`
- 自身属性：正常背景 + 白色字体
- 继承属性：灰色背景 + 灰色字体（`GUILayout.Label` 无交互）

**卡片嵌套**：Folder 卡片内嵌套 Property 卡片列表，统一 `EditorUIUtility.DrawCard(Pad)` 风格。

### 其他修复

- 左侧列表选中 Tree 时折叠按钮消失 → 折叠区域恢复 `GUI.backgroundColor = Color.white`
- `LocalNodeCount` 只统计属性节点（有 DefId），不含 Folder
- 搜索框高度统一 22px

## 设计决策

- 继承属性用 `GUIStyle.none` 基样式 + 全部 8 状态色设为 `ColorInherit` → 废弃，改回 `EditorStyles.label` + `GUI.color` 着色
- 移除 DefId 和 Source 列 → 名称和颜色已区分继承/自身
- 移除 inline 编辑（双击重命名）→ 简化，后续再加

## 2026-06-10 下午 — 拖拽排序

### 实现
- **拖起**：`MouseDrag` → `PrepareStartDrag` + `objectReferences` + `StartDrag`
- **拖中**：浮动卡片跟随鼠标（`GUI.Box` 半透明），原位置跳过渲染
- **放下**：全局最优距离匹配（每个文件夹算距鼠标距离，最近者胜出）→ `ReorderLeaf`
- **排序持久化**：`ReorderLeaf` 先存 `treeJson` 再 `RefreshAfterEdit`，避免被 `LoadOwnNodes` 覆写
- **SortTreeNodes**：改为实例方法，叶子 + 文件夹都按 `_ownNodes` 顺序排，不再字母排序覆盖

## 2026-06-10 后续 — 右侧属性池 + 拖入 + 删除修复 + 文件夹拖拽

### 右侧属性池
- 搜索栏 + 属性卡片列表（字母排序）
- 已使用属性绿色背景标记
- 属性行 `MouseDrag` → `StartDrag("DefDrag")` 带 `PropertyDefSO`
- 拖入文件夹：`HandleDefDrop` 最近匹配 + `AddDefToFolder` 指定插入位置
- `AddDefToFolder` 先存 `treeJson` 再 `RefreshAfterEdit`

### 删除修复
- `DeleteLeaf` / `DeleteFolderByNode` / `AddFolder` / `AddLeafToNode` 全部持久化 treeJson

### 文件夹拖拽排序
- `≡` 锚点拖拽（10px），悬浮高亮
- `HandleFolderReorder` 覆盖层指示线，复用属性排序模式
- `ReorderFolder` 在 `_ownNodes` 中重排根级文件夹
- `IsDraggingFolder()` 检查 `DefId` 为空 → 文件夹/属性拖拽互不干扰
- `GUI.enabled` 拖拽期间禁用文件夹输入框
- `string.IsNullOrEmpty` 防空字符串泄漏

### 搜索优化
- 搜索匹配属性加粗高亮，不再隐藏未匹配属性

### 踩坑
- `_folderDropIndex` 泄漏 → 每帧重置
- `_dragNodeId` 空字符串导致全局禁用 → `string.IsNullOrEmpty` 替换所有判空
- `SortTreeNodes` 文件夹按字母排序 → 改为按 `_ownNodes` 顺序

### 踩坑
- Unity Mono 的 `Dictionary` 不保证迭代顺序，`ResolveAllNodes` 返回的字典顺序不可预测
- `DragUpdated`/`DragPerform` 中 `GUI.Box` 不在 Repaint 时不渲染 → 指示线移到事件外绘制
- 占布局空间的 `GetControlRect` drop zone 导致拖拽时卡片间距变大 → 改为 `EditorGUI.DrawRect` 覆盖层
- 首次尝试的 `HandleDropBetween` 每个文件夹都消费事件 → 改为全局最优匹配

- `EditorStyles.toolbarSearchField` 的 `fixedHeight` 覆盖 `GUILayout.Height()`
- `GUIStyle.none` 基样式缺少布局属性，破坏卡片循环
- 卡片高度问题：不要 `ExpandHeight(true)`，保持自然行高
