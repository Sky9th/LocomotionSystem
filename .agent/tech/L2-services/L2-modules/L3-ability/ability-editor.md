# Ability Editor — 编辑器架构

> **Last Verified**: 2026-07-05 | **Verification**: All referenced files exist, signatures match code
> 代码路径: `Assets/Scripts/Services/Modules/L3_Ability/Editor/`
> 层级: Editor（非运行时 L 体系）

## 调用链

```
MenuItem "RedDust/Ability Editor"
  └── AbilityEditorWindow
        ├── AbilityEditorModel      ← 扫描 AssetDatabase，构建树
        ├── AbilityListView         ← 左栏：筛选 + 搜索 + Create New
        ├── AbilityEditorMiddlePanel ← 中栏：EditorForm 编辑 Ability
        └── SubAssetPickerView      ← 右栏：子资产选择器

MenuItem "RedDust/{Type} Editor"   ← 独立编辑窗口
  ├── ActivationEditorWindow
  ├── SearchEditorWindow
  ├── EffectEditorWindow
  └── NoiseEditorWindow

MenuItem "RedDust/{Type} Import-Export"  ← 导入/导出
  ├── ImportExport (共享面板，类名 EditorImportExport)
  ├── EffectImportWindow
  ├── SearchImportWindow
  ├── ActivationImportWindow
  ├── NoiseImportWindow
  └── AbilityImportWindow

共享组件 (Assets/Scripts/Shared/Editor/Components/)
  ├── EditorCard       ← 卡片容器
  ├── EditorButton     ← 按钮
  ├── EditorForm       ← 表单数据绑定
  └── EditorUIUtility  ← 搜索/筛选/空状态

Tree 渲染:
  ├── AbilityTreeView  ← 通用树渲染器 (EditorCard 卡片)
  └── AbilityTreeNode  ← 树节点数据 (Ability/Effect/Search/Activation/Noise)
```

## 架构

### AbilityEditorWindow — 三栏布局

| 栏 | 组件 | 职责 |
|----|------|------|
| 左 | `AbilityListView` | 筛选 (All/Active/Passive) + 搜索 + Create New + `AbilityTreeView` 树 |
| 中 | `AbilityEditorMiddlePanel` | `EditorForm` 表单编辑：Identity → Activation → Search → Effects → Noise → Tags → Cooldown → Combo |
| 右 | `SubAssetPickerView` | 根据 `SubAssetSlot` 展示对应类型列表，Select 后赋给中栏 |

数据流：`AbilityEditorModel.Refresh()` → 扫描 AssetDatabase → 构建树根 → `AbilityTreeView` 渲染 → 用户选中叶子 → 中栏编辑 → 右栏挑选子资产。

### 独立编辑器窗口

四个子资产编辑器遵循统一模式（对标 `ActivationEditorWindow`）：

```
Header: 标题 + Refresh/Import-Export/+Create/Save/Ping
TwoColumns:
  左: 搜索 + AbilityTreeView
  右: EditorForm 编辑（空态 → 占位提示，选中 → 表单）
StatusBar: 总数 + 分类统计
```

| 窗口 | 资产类型 | 树分组方式 | 特有字段 |
|------|---------|-----------|---------|
| `ActivationEditorWindow` | `AbilityActivationSO` | `activationType` enum | animationAsset, 动画参数 |
| `SearchEditorWindow` | `AbilitySearchSO` 子类 | `searchType` enum | Cone.angle, Ray.requiresLineOfSight |
| `EffectEditorWindow` | `EffectSO` 子类 | `effectTag` parent chain | 按 Damage/Impact/Execute/Cost 分支 |
| `NoiseEditorWindow` | `NoiseEventSO` | `noiseType` parent chain | level, decayRadius |

### Import/Export 共享组件

`EditorImportExport.Draw()` 渲染完整面板：

```
Header Card: 标题 + 副标题
File Card: JSON 文件路径 + "…" 浏览
Preview Card: 富文本预览（buildPreview 回调解析 DTO）
Buttons: Import (Success/Disabled) + Export (Primary)
Result Card: Created/Skipped/Errors 彩色输出
```

每个 ImportWindow 提供：`buildPreview` (filePath→富文本)、`onImport` (filePath→统计元组)、`onExport` (filePath→无)。

**AbilityImportExport 路径规则**：
- Active: `abilityTag` 字段 → `ResolveActiveDir()` 解析为 `Definition/Actives/{Melee|Ranged}/...`
- Passive: 固定 `Definition/Passives/`（与 Actives 同级目录约定一致）
- Import 时自动创建目录，`ApplyFields()` 填充 Active/Passive 各自的专有字段

**AbilityTreeImportExport 更新策略**：
- 新树：`CreateInstance<AbilityTreeSO>` → `ApplyTreeFields` → `AssetDatabase.CreateAsset`
- 已有树：`ApplyTreeFields` 覆盖更新（displayName / description / tags / nodes），不再 skip
- `ApplyTreeFields` 是共享方法，解析 `treeTags` / `compatibleWeaponTags` / `compatibleGripTags` / `nodes`（含 ability 和 passive 引用）

### AbilityTreeView

递归树渲染器——每个节点一个 `EditorCard.Draw(Pad, ..., isSelected)` 卡片：

```
EditorCard (isSelected → 蓝色背景)
  invisible button  ← 整行点击（排除 foldout/删除按钮区域）
  foldout / "-"
  Label
  DeleteButton (仅叶子)
  Children (递归 EditorCard)
```

节点数据用 `AbilityTreeNode`：`Ability`/`Effect`/`Search`/`Activation`/`Noise` 五选一 + `IsFolder`/`Depth`/`Children`。

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 编辑 | `AbilityDefSO`, `PassiveAbilitySO` 等 | Editor 读写 SO 字段 |
| 依赖 | `EditorCard`, `EditorButton`, `EditorForm` | Shared/Editor 组件 |
| 依赖 | `TagPicker` | Tag 选择弹出窗 |
| 依赖 | `AbilityTreeView`, `AbilityTreeNode` | 树渲染 |
| 依赖 | `AbilityEditorUtility` | 排序/摘要/删除 |
| 消费 | `EditorImportExport` | 共享导入导出面板 |

## 已知问题

- AbilityTreeView 卡片点击范围：invisible button 仅覆盖 rowRect，card padding (6px) 未覆盖
- PopupWindow 定位：TagPicker.Show 的 rect 坐标空间未完全对齐按钮位置

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| Ability Creation Wizard | 方案设计中 | TagEditor, TagPicker |
| 修复卡片点击范围 | 待处理 | — |
| 修复 Popup 定位 | 待处理 | Unity PopupWindow API 研究 |
| AbilityTreeView 空状态 GreyPlaceholder 化 | 待合并 | — |
