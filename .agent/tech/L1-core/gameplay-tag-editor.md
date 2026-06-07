# GameplayTag Editor — 标签编辑器

> `L1_Core/GameplayTag/Editor/` · `namespace RedDust.Core.Editor` · `#if UNITY_EDITOR`
>
> 可视化标签管理工具，提供独立管理窗口 + 可嵌入选择器。

## 文件

| 文件 | 职责 |
|------|------|
| `TagEditorWindow.cs` | 独立管理窗口，Header + Toolbar + Search + 左 Tree/右 Inspector + StatusBar |
| `TagTreeView.cs` | 共享树渲染器（静态），折叠/展开/选中/右键菜单 |
| `TagPicker.cs` | 可嵌入 PopupWindowContent，搜索 + 树浏览 + 一键创建 |
| `TagTreeModel.cs` | AssetDatabase 扫描真实标签，parent 引用建树，Search/MissingAncestors |
| `TagCreator.cs` | 事务式链创建，目录/残留清理，失败回滚 |
| `TagNode.cs` | 独立节点类，模型和渲染器共用 |

## 入口

- `Tools/RedDust/Tag Editor` → TagEditorWindow
- `Tools/RedDust/Tag Picker (Test)` → TagPicker 测试弹窗
- `TagPicker.Show(rect, rootFilter, allowCreate, onSelected)` → 代码调用

## UI 风格

严格参照 `StatsTreeEditorWindow`：
- `pad = 6f` 统一间距，`EditorStyles.helpBox` 卡片
- 嵌套 `BeginHorizontal/Vertical` 布局，不手动像素缩进
- 左边折叠区固定 `BeginHorizontal(Width=18f)` 防跳变
- 字体统一 `EditorStyles.label`，废弃 `miniLabel`
- 已有标签正常字重，未创建加粗灰显，选中加粗

## TagPicker 集成

```csharp
TagPicker.Show(
    activatorRect: GUILayoutUtility.GetLastRect(),
    rootFilter: "Damage.",
    allowCreate: true,
    currentFullTag: "Damage.Elemental.Fire",
    onSelected: tag => { /* tag.FullTag */ }
);
```

## 目录规则

目录纯粹为了 Project 窗口可视化管理。标签层级由 `parent` SO 引用定义。标签有子标签时创建同名目录放入子标签。
