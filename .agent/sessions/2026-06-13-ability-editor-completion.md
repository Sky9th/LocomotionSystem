# 2026-06-13 Ability 编辑器完整化

## 做了什么
补全 Ability 系统所有附属资产的独立编辑器窗口和导入/导出功能，标准化共享 Editor UI 组件。

## 新增
- **NoiseEditorWindow**: 独立 NoiseEventSO 编辑窗口（对标 ActivationEditorWindow 模式）
- **NoiseImportExport**: Noise JSON 导入/导出
- **AbilityImportExport**: AbilityDefSO/PassiveAbilitySO JSON 导入/导出
- **EditorImportExport**: 从简易面板重构为 EffectImportWindow 风格完整面板
- AbilityTreeNode 新增 NoiseEventSO 节点支持

## 修复
- DamageEffectSO 编辑器补全 modAdd/modMult/priority（之前只暴露 baseValue）
- AbilityDefSOEditor/PassiveAbilitySOEditor 伤害预览更新为七列格式
- AbilityEditorWindow "Create New" 存根改为实际创建功能
- SubAssetPickerView "Create New" 跳转到独立编辑器窗口
- TagPicker 搜索栏改为 EditorUIUtility.DrawSearchRow
- TagPicker 按钮标准化 + Footer 卡片包裹

## 标准化
- EditorForm.RowSpacing 4f→6f（对齐项目 Pad）
- FormItem 行高统一 EditorGUIUtility.singleLineHeight
- 所有 Import/Export 窗口统一使用 EditorCard/EditorButton
- 移除手动 new GUIStyle 空状态 → EditorUIUtility.GreyPlaceholder

## 已知问题
- Popup 定位（PopupWindow.Show rect 坐标空间）
- AbilityTreeView 卡片内边距点击范围

## 关键文件
- `Assets/Scripts/Services/Modules/L3_Ability/Editor/NoiseEditor/` (新增)
- `Assets/Scripts/Services/Modules/L3_Ability/Editor/AbilityEditor/AbilityImportExport.cs` (新增)
- `Assets/Scripts/Shared/Editor/Components/ImportExport.cs`
- `Assets/Scripts/Services/Modules/L3_Ability/Editor/EffectEditor/EffectEditorWindow.cs`
- `Assets/Scripts/Services/Modules/L3_Ability/Editor/EffectEditor/EffectImportExport.cs`
- `Assets/Scripts/L1_Core/GameplayTag/Editor/TagPicker.cs`