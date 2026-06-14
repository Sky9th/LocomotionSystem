# 2025-06-14 EUI 组件标准化

## 背景

上次提交 (`cceee6b7`) 完善了 EUI 组件架构（Slot/回调模式 + 令牌体系）。本次对 `Shared/Editor/Components/` 全部 13 个组件进行四轮审核，逐文件对照 EUI 约定，修复所有偏离。

## 改了什么

### EditorTokens 令牌体系
- 新增 5 个语义色：`ColorPrimary(#4C7EFF) / ColorSuccess / ColorWarning / ColorDanger / ColorInfo(#A8B2BF)`
- 新增 4 个辅助色：`ColorDivider / ColorDim / ColorResultOk / ColorResultErr`
- 新增 3 个控件尺寸：`SizeSm(16) / SizeMd(20) / SizeLg(26)` — 对齐 design-tokens.md §4 L/M/S 三档
- 修正 3 个 Padding 值：Small(4,4,1,1)→(6,6,1,1) / Medium(14,14,5,5)→(10,10,3,3) / Large(18,18,7,7)→(14,14,5,5)

### Button.cs — 最大改动 (-148 行)
- 删除 `EditorButtonStyle` 枚举 + 旧 API 全部代码（~90 行）
- 删除死代码：`GetSolidTexture() / Lighten() / Darken() / _texCache / Delete(Rect)`
- 5 个颜色改用 EditorTokens 语义令牌：Primary #409EFF→#4C7EFF, Info #909399→#A8B2BF
- `Draw(type)` 参数加默认值 `EditorButtonType.Default`，兼容跳过 type 的旧调用

### 其余组件 (6 个违规)
- **Card.cs**: 硬编码 Color→ColorDim, GapTight 3f→PadTight, fontSize 11→FontSm, 裸 GUILayout.Button→EditorButton
- **Form.cs**: 删 2 个未使用 using, RowSpacing 6f→EditorTokens.Pad, 注释修正
- **ImportExport.cs**: Pad 常量→EditorTokens.Pad, DrawHeader→DrawCardHeader, 硬编码 Color→令牌
- **EditorDivider.cs**: Color→ColorDivider, Space(8f)→Pad, 18f→singleLineHeight
- **FormItem.cs**: Space(2)→Pad/3
- **UIUtility.cs**: fontSize 12→FontBase, 删未使用 using
- **ButtonGroup.cs/EditorInput.cs**: EditorButtonStyle→EditorButtonType, 删未使用 using

### 外部 (14 文件)
- 全部 `private const float Pad = 6f` → `EditorTokens.Pad`
- 全部 `EditorButtonStyle` → `EditorButtonType` (35 处)

### 文档
- components.md: 规则 6.→7., DrawWithHeader→DrawCardHeader, Primary 绿→蓝, 语义色表, 尺寸表, EditorInput API 补充
- design-tokens.md: 5.1→5.2 重复节号修正

## 已知未修复

- 外部 45 处违规 (17 裸 Button + 14 硬编码 Color + 14 硬编码 Space) — 集中在 PropertyTreeEditorWindow 等窗口文件，属于独立任务
- 5 个死令牌 (ColorGreen/ColorGreenDark/ColorBlue/ColorButtonText/SizeLg) — 保留作为设计系统储备
