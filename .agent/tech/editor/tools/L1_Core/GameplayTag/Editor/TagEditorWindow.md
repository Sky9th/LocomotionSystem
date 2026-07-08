# TagEditorWindow

> **Last Verified**: 2026-07-08 | **Verification**: All referenced files exist, signatures match code

- **菜单**: `RedDust/Tag Editor` (priority 5)
- **源文件**: `Assets/Scripts/L1_Core/RdTag/Editor/TagEditorWindow.cs`
- **命名空间**: `RedDust.Core.Editor`
- **相关**: [RdTagImportWindow](TagImportExport.md) / [Ability Editor](../../../../../L2-services/L2-modules/L3-ability/ability-editor.md)

## 概述

RdTag 可视化编辑器。管理 `RdTagDefSO` 资产树，支持创建、搜索、查看详情、删除 Tag。

数据模型：`TagTreeModel` 扫描 `AssetDatabase` 中所有 `RdTagDefSO`，按 `Parent` 引用构建多叉树（`EditorTreeNode`），提供按 `FullPath` 查找和关键词搜索。

## UI 布局

```
Header Card: 标题 "Tag Editor" + 副标题 "L1_Core · RdTag"
Toolbar Card: ＋ Create Tag / 🔄 Refresh / ▼ Expand All / ▲ Collapse All
Search Bar:   文本框 + ✕ 清除
Main Content (Horizontal):
  左 Tree Panel:      EditorTreeView (searchString 联动搜索)
  右 Inspector Panel: Tag Details / Create Form / 空态提示
Status Bar:   "N tags" + 当前选中 FullPath
```

## 核心功能

### 创建 Tag

- **根标签**: 点击 `＋ Create Tag` → Inspector 切换为 Create Form → 输入 LeafName → 点击 `Create Tag`
- **子标签**: Tag Details 面板中（预留入口，当前由 `StartCreateChild` 方法支持）
- 创建底层调用 `TagCreator.CreateTagChain(fullTag)` — 事务式（失败回滚），自动创建缺失的中间祖先标签
- 创建表单显示缺失祖先提示和投影 FullTag

### 删除 Tag

- 选中标签 → Inspector 显示 `Delete` 按钮
- **引用检查**: `FindReferencers` 扫描 `Assets/Data` 和 `Assets/Scripts` 中所有非 .cs 资产，检查依赖关系
- 确认对话框列出子孙标签和外部引用者
- 删除时级联删除所有子孙 `RdTagDefSO`

### 搜索

- 搜索文本联动 `EditorTreeView.searchString`，按 `FullPath` 和 `LeafName` 匹配
- 搜索结果按相关性排序（精确匹配 > 前缀匹配 > LeafName 匹配）

## 依赖

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | `TagTreeModel` | 数据模型：扫描 AssetDatabase，构建 EditorTreeNode 树 |
| 依赖 | `EditorTreeView` | 共享树渲染器 (RedDust.Shared.EditorUI) |
| 依赖 | `TagCreator` | 标签工厂：CreateTagChain 事务式创建 |
| 依赖 | `EditorButton`, `EditorTokens` | 共享 Editor UI 组件 |
| 消费 | `TagPicker` | 标签选择弹出窗 |

## 已知限制

- Inspector 宽度固定 300px（`InspectorWidth = 300f`）
- 创建子标签入口（`StartCreateChild`）方法已定义但当前 Inspector 未暴露 UI 按钮
- 循环引用检测（`HasCycle`）已实现但未在 UI 中展示警告
