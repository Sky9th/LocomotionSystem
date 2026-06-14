---
name: eui-conventions
description: RedDust 编辑器 UI (IMGUI) 开发约定——EUI 组件库/视觉令牌/布局模式。制作 EditorWindow、Inspector、新建 EUI 组件时必须遵循。
when_to_use: 编写 Editor GUI 代码、修改 EditorWindow、新增 EUI 组件、调整编辑器界面时遵守
---

## 什么是 EUI

**EUI = Editor UI**，即 Unity Editor 中的 IMGUI 界面（`EditorWindow`、`Editor`、`EditorGUILayout`），与游戏运行时 UGUI（`gui-conventions`）完全不同。

- EUI 只在 `#if UNITY_EDITOR` 下运行，不进入 Runtime 构建
- EUI 组件位于 `Assets/Scripts/Shared/Editor/Components/`，namespace `RedDust.Shared.EditorUI`
- 对标 Element UI 组件命名：`el-button` → `EditorButton`，`el-card` → `EditorCard`

## 铁律：改 UI 前先画图

**任何 EUI 界面的修改（新增/调整布局/重构），必须先画 UI 结构图落地为 md 文件，再动手改代码。**

流程：
1. 阅读目标 EditorWindow 的完整代码
2. 画出 UI 结构图（ASCII 树形图），为每个部位命名
3. 写入文档（如 `.agent/tech/L2-modules/L3-ability/effect-editor-ui.md`）
4. 确认每个部位是否使用了 EUI 组件还是裸 Unity GUI
5. 按图改代码

原因：IMGUI 的 `OnGUI` 是纯方法调用链，没有可视化结构。不画图直接改，必然错漏。

## 文档索引

| 文档 | 内容 | 何时查阅 |
|------|------|---------|
| [components.md](.agent/tech/editor/conventions/components.md) | EUI 完整参考 — 组件 API + 布局模式 + 常见陷阱 | 写任何 EUI 面板前 |
| [design-tokens.md](.agent/tech/editor/conventions/design-tokens.md) | 视觉令牌 — 颜色色值 / 字号 / 间距 / 控件尺寸 / 圆角 | 设计新面板外观时 |
| [element-ui-design-tokens.md](.agent/references/element-ui-design-tokens.md) | Element Plus 源参考 | 理解对标来源 |
| [unity-editor-design-tokens.md](.agent/references/unity-editor-design-tokens.md) | Unity Editor 源参考 | 理解基底色/字号来源 |

## 组件速查

| 需要什么 | 用什么 |
|----------|--------|
| 区块容器 | `EditorCard.Draw(pad, ...)` 或 `EditorCard.Draw(pad, title, ...)` |
| 轻量嵌套容器 | `EditorCard.DrawLight(pad, ...)` |
| 折叠面板 | `EditorCard.DrawFoldout(pad, title, ref folded, ...)` |
| 按钮 | `EditorButton.Draw(text, style, size)` |
| 按钮组（单选） | `EditorButtonGroup.Draw(current, values, labels)` |
| 搜索栏 | `EditorSearchBar.Draw(current, labelWidth)` |
| 表单（绑定 SO） | `new EditorForm(so)` + fluent API |
| 间距 | `EditorCard.Gap(pad)` / `EditorCard.GapTight()` |
| 删除按钮 | `EditorUIUtility.DeleteButton()` |
| 空状态文字 | `EditorUIUtility.GreyPlaceholder` |

## 新增组件 Checklist

1. 在 `Assets/Scripts/Shared/Editor/Components/` 新建 `.cs` 文件
2. namespace `RedDust.Shared.EditorUI`，`#if UNITY_EDITOR` 包裹
3. 对标 Element UI 命名（`el-xxx` → `EditorXxx`）
4. 内部调用现有组件（`EditorButton.Draw` 而非裸 `GUILayout.Button`）
5. 参考 `EditorFormItem` 的单行布局模式
6. 更新 `editor/conventions/components.md`：总览表 + 新章节 + 依赖图
