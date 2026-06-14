# EUI — 编辑器 UI 设计体系

> **EUI = Editor UI (IMGUI)** — Unity Editor 中的 `EditorWindow` / `Editor` / `EditorGUILayout` 界面。
> 与游戏运行时 UGUI (`gui-conventions`) 完全分离，仅在 `#if UNITY_EDITOR` 下编译。

## 铁律：改 UI 前先画图

**任何 EUI 界面的修改，必须先画 UI 结构图落地为 md 文件，再动手改代码。**

1. 阅读目标 EditorWindow 完整代码
2. 画 ASCII UI 结构图，为每个部位命名（参考 [effect-editor-ui.md](../../L2-modules/L3-ability/effect-editor-ui.md)）
3. 写入文档，确认每个部位是否使用 EUI 组件还是裸 Unity GUI
4. 按图改代码

## 快速查找

| 需要什么 | 看哪里 |
|----------|--------|
| 有哪些组件、怎么用 | [components.md](components.md) |
| 颜色色值 / 字号 / 圆角 | [design-tokens.md](design-tokens.md) |
| 对标 Element UI 的源参考 | [element-ui-design-tokens.md](../../../references/element-ui-design-tokens.md) |
| 对标 Unity Editor 的源参考 | [unity-editor-design-tokens.md](../../../references/unity-editor-design-tokens.md) |

## 组件速查

| 需要什么 | 用什么 | 禁止 |
|----------|--------|------|
| 区块容器 | `EditorCard.Draw(pad, ...)` | 裸 `EditorStyles.helpBox` |
| 按钮 | `EditorButton.Draw(text, style, size)` | 裸 `GUILayout.Button` |
| 按钮组（单选） | `EditorButtonGroup.Draw(current, values, labels)` | |
| 搜索栏 | `EditorSearchBar.Draw(current, labelWidth)` | |
| 表单（绑定 SO） | `new EditorForm(so)` + fluent API | |
| 间距 | `EditorCard.Gap(pad)` / `EditorCard.GapTight()` | 魔术数字 `Space(2)` |
| 删除按钮 | `EditorUIUtility.DeleteButton()` | |
| 空状态文字 | `EditorUIUtility.GreyPlaceholder` | |

## 与 gui-conventions 的关系

| | EUI | GUI |
|------|-----|------|
| 技术 | IMGUI (`EditorGUILayout`) | UGUI (Canvas/Prefab) |
| 运行环境 | `#if UNITY_EDITOR` | Runtime |
| 组件位置 | `Shared/Editor/Components/` | Prefab + `L2_UI/` |
| 约定文件 | `eui-conventions` | `gui-conventions` |
