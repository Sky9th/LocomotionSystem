# 2026-06-15 — EUI FormItem 架构重构

## 改了什么
- `EditorFormItem` 统一为单一渲染入口 `Draw(label, drawSlot)`
- `ArrayField` 改为走 `Draw`，不再独立布局
- `ObjectFieldWithTag` 改为走 `Draw`（ref → local 桥接）
- 删除 `DrawReflected`、`DrawInput`、`FieldType` enum
- `EditorLabel` 新增 `DefaultStyle`（左右 padding/margin 清零），去掉高度约束
- `EditorButton.GetStyle` margin 左右清零
- `EffectEditorWindow` 删除 `DrawBlockedTags`，统一用 `ArrayField`，label 中文化并加 tooltip
- 移除所有 `form.DefaultLabelWidth = xxx`，统一默认值 90f
- 水平布局的 FormItem：`MaxWidth` 限制 + 跳过 Divider

## 为什么
- 原来 FormItem 有多个渲染入口（DrawReflected/RawField/ObjectFieldWithTag/ArrayField）各自重复布局逻辑
- ArrayField 不在 form 内，左边距不一致
- label 的 `EditorStyles.label` 左右 margin/padding 导致额外间距

## 已知问题
- wordWrap 已启用但受限于 `EditorStyles.label` 的 `alignment: MiddleLeft`
- 水平组内 FormItem 宽度 `MaxWidth(w * 2.5)` 可能需要按实际内容调整
