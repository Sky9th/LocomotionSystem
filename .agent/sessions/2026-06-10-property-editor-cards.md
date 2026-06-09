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

## 经验教训

- `EditorStyles.toolbarSearchField` 的 `fixedHeight` 覆盖 `GUILayout.Height()`
- `GUIStyle.none` 基样式缺少布局属性，破坏卡片循环
- 卡片高度问题：不要 `ExpandHeight(true)`，保持自然行高
