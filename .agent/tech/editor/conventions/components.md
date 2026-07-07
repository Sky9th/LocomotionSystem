# EUI Components — 编辑器 UI 组件库

> `Shared/Editor/Components/` · namespace `RedDust.Shared.EditorUI`
>
> 所有 Editor Window 和 Inspector 共用的标准 IMGUI 组件。基于 Unity `EditorGUILayout` 封装，对标 Element UI。

---

## 设计理念

**组件 = 布局 Slot，回调 = 内容。组件之间零耦合。**

每个组件只提供一个"洞"（`Action drawSlot` / `Action<T> onSelected` / `event Action OnChange`），调用方往洞里填内容。组件不知道、不持有、不关心洞里放的什么。

| 原则 | 说明 |
|------|------|
| **组件纯静态、不持仓** | `FormItemGroup.Draw(Horizontal, () => ...)` 不存 FormItem；`EditorForm` 不存字段实例。回调里现画现走 |
| **组件只提供 Slot，不管 Slot 内容** | `DrawCardHeader` 右侧是 `drawRight` 回调；`ObjectFieldWithTagPicker` 弹窗是 `onTagSelected` 回调。组件定布局框架，调用方定内容 |
| **Slot vs 变体：内容可预知用变体** | 内容不可预知 → `Action drawSlot`；内容可预知（按钮、Picker）→ 命名变体 `TextFieldWithClear`、`ObjectFieldWithTagPicker` |
| **组合组件只编排子组件** | 上层组件只做布局编排，渲染全委托子组件。声明式列举内部依赖（如 `SearchBar = Label + Input.TextFieldWithClear`），不裸调 `GUILayout` |
| **组件不跨层调用** | `EditorFormItem` 不直接 `SetDirty`，走 `f.NotifyFieldChanged`；`EditorInput` 不画 Label；`EditorButton` 不托管弹窗逻辑 |
| **禁止薄委托** | `EditorUIUtility` 不放 `DrawSearchRow` 等空壳——调用方直接调底层组件 |

---

## 代码规范

1. **卡片即容器** — 所有逻辑区块用 `EditorCard.Draw(pad, ...)` 包裹
2. **按钮走组件** — 所有按钮走 `EditorButton.Primary()`/`Danger()` 等入口
3. **间距令牌集中** — IMGUI 布局令牌在 `EditorTokens.Pad` / `PadTight`（详见 [design-tokens.md §5.0](design-tokens.md)），组件引用令牌，禁止各自 `const float Pad = 6f`
4. **间距必须推导** — 所有 gap 从 `Pad` 令牌计算（`Pad / 2`、`Pad * 2`），禁止硬编码 `3f`/`4f` 等魔术数字
5. **风格令牌与 Helper 分离** — 设计令牌（`Pad`/`Font`/`Color`/`Padding`）放 `EditorTokens`，纯数据；Helper（`GreyPlaceholder`等）放 `EditorUIUtility`。两者不混装
6. **GUI.Button 优于 GUILayout.Button** — 禁止裸 `GUILayout.Button`；禁止裸 `EditorStyles.helpBox`
7. **回调写内容** — 所有组件通过 `Action` 回调接收内容，对标 `EditorCard.Draw(pad, () => { ... })`

---

> 设计令牌（颜色/字体/间距/圆角）见 [design-tokens.md](design-tokens.md)。IMGUI 布局令牌（`Pad`/`PadTight`）定义在 `EditorTokens`。

## 组件总览

| 组件 | 角色 | 说明 |
|------|------|------|
| `EditorCard` | 布局容器 | 纯容器 + 带标题 + 间距 |
| `EditorForm` | 表单布局 | BeginGroup/EndGroup、OnChange/OnSubmit、统一 SetDirty |
| `EditorFormItem` | 字段渲染 | 拼装 EditorLabel + EditorInput + 变更检测 |
| `EditorLabel` | Label | 纯标签文字 |
| `EditorInput` | Input + 侧边按钮 | ObjectField/FloatField/... + TagButton 等 |
| `EditorButton` | 按钮 | 类型入口 Primary()/Danger()/... + Delete() |
| `EditorButtonGroup` | 按钮组 | 多按钮水平排列，单选高亮 |
| `FormItemGroup` | 布局包装 | `Draw(Horizontal, () => {...})` 水平/垂直排列 |
| `EditorSearchBar` | 搜索栏 | Label + TextField + 清除按钮 |
| `EditorDivider` | 分隔线 | 细线 + 可选标题 |
| `DataLabelTools` | Addressables 标记 | 批量/单资产 boot label 注册 |
| `EditorImportExport` | 导入/导出 | 文件选择 + 预览 + 按钮 + 结果 |
| `EditorTokens` | 设计令牌 | 布局/字号/内边距/颜色常量 |
| `EditorUIUtility` | Helper | GreyPlaceholder 等工具样式 |

---

## 1. EditorCard — 卡片容器

**文件**: `Card.cs`

最基础的布局单元。所有编辑器界面都应包裹在 `EditorCard` 中。

### API

```csharp
// 纯容器卡片（样式全内置，继承 EditorStyles.helpBox）
EditorCard.Draw(Action drawContent);

// 带标题卡片
EditorCard.Draw(string title, Action drawBody);

// 间距
EditorCard.Gap(float px);
EditorCard.GapTight();  // 3px
```

> `DrawLight` / `DrawFoldout` / `DrawItem` / `DrawCardHeader` 已移除（v0.14.3）。Card 只是容器，选中/折叠/列表项应由上层组合实现。

### 使用范例

```csharp
// Header
EditorCard.Draw(Pad, () =>
    EditorCard.DrawCardHeader("Ability Editor", "L3_Ability · Editor",
        drawRight: () =>
        {
            EditorButton.Primary("Save *", enabled: _hasChanges);
            EditorButton.Default("Refresh");
        }));
```

---

## 2. EditorButton — 按钮

**文件**: `Button.cs`

### 类型入口（高频）

```csharp
EditorButton.Default("Refresh", size: Medium);
EditorButton.Primary("Save *", size: Medium, enabled: _hasChanges);
EditorButton.Success("+ Create", size: Medium);
EditorButton.Danger("✕", size: Small, width: 20);
EditorButton.Warning("Warn", size: Small);
EditorButton.Info("Info", size: Small);

// 删除
EditorButton.Delete();         // ✕ Danger Small w:20
EditorButton.Delete(rect);     // Rect 版
```

### 通用入口（低频）

```csharp
EditorButton.Draw(text, type, size, width?, height?, tooltip?, enabled);
EditorButton.Draw(rect, text, type, tooltip?);
```

### 类型 & 尺寸

| EditorButtonType | 颜色 |
|-----------------|------|
| `Default` | 系统默认 |
| `Primary` | 蓝 #4C7EFF |
| `Success` | 深绿 |
| `Warning` | 橙 |
| `Danger` | 红 #D32222 |
| `Info` | 灰蓝 #A8B2BF |

| EditorButtonSize | font | padding | fixedHeight | 说明 |
|-----------------|------|---------|-------------|------|
| `Small` | 11 | (6,6,1,1) | 0 (auto) | 原生 miniButton |
| `Medium` | 12 | (10,10,3,3) | 24 | 默认 |
| `Large` | 14 | (14,14,5,5) | 28 | 大按钮 |

---

## 3. EditorButtonGroup — 按钮组

**文件**: `ButtonGroup.cs`

```csharp
// Enum 模式
var next = EditorButtonGroup.Draw(current, values, labels, size);

// 索引模式
int idx = EditorButtonGroup.Draw(labels, selectedIndex, size);
```

---

## 4. EditorSearchBar — 搜索栏

**文件**: `SearchBar.cs`

结构: `[Label] [TextField(flex)] [✕ ClearBtn]`

```csharp
string next = EditorSearchBar.Draw(_searchText, labelWidth: 45f);
```

---

## 5. EditorLabel — Label

**文件**: `EditorLabel.cs`

```csharp
EditorLabel.Draw("Name", 80, tooltip: "效果的显示名称");
```

### 默认样式

`EditorLabel.DefaultStyle` — 基于 `EditorStyles.label`，左右 padding/margin 清零，上下保留原值。`Draw` 的 `style` 参数不传时默认使用此样式。

---

## 6. EditorInput — Input + 侧边按钮

**文件**: `EditorInput.cs`

纯输入控件，不画 Label。内置可选侧边按钮。

```csharp
// 基础 Input
float v = EditorInput.FloatField(oldValue, width: 60);
int   n = EditorInput.IntField(oldValue);
bool  b = EditorInput.Toggle(oldValue);
var   t = EditorInput.ObjectField<T>(oldValue);

// 侧边按钮
EditorInput.TagButton(ref tagBtnRect);  // Tag 按钮，自动捕获 Rect

// ObjectField + TagPicker 组合
var next = EditorInput.ObjectFieldWithTagPicker(val, ref rect,
    onTagSelected: t => { ... });

// 其他 Input
string s = EditorInput.TextField(oldValue, width: 120);
string c = EditorInput.TextFieldWithClear(oldValue);
float  f = EditorInput.Slider(0.5f, 0f, 1f);
var    e = EditorInput.EnumPopup(myEnum);
var    o = EditorInput.ObjectField(obj, typeof(ScriptableObject), false);
```

---

## 7. EditorForm — 表单布局

**文件**: `Form.cs`

回调模式——每帧创建 + 绘制，对标 EditorCard。

### API

```csharp
EditorForm.Draw(target, form =>
{
    form.DefaultLabelWidth = 80;

    // 字段 — 通过 EditorFormItem
    EditorFormItem.Float("duration");
    EditorFormItem.Toggle("stackable");
    EditorFormItem.ObjectField<T>("effectTag");

    // 手动字段
    EditorFormItem.RawField("Name", 80, getValue, setValue, drawFunc, equals);

    // 数组字段
    EditorFormItem.ArrayField<T>("adjuncts", getValue, setValue, drawRow, createDefault);

    // 水平分组
    form.BeginGroup(FormGroupLayout.Horizontal);
    EditorFormItem.Float("a");
    EditorFormItem.Float("b");
    form.EndGroup();

    // ObjectField + TagPicker
    EditorFormItem.ObjectFieldWithTag<T>("effectTag", ref _tagBtnRect);

    // 事件
    form.OnChange += () => _dirty = true;
    form.OnSubmit += () => Save();
});
```

### 事件 & 方法

| 成员 | 说明 |
|------|------|
| `OnChange` | 任一字段变更 |
| `OnSubmit` | `Submit()` 时触发 |
| `Submit()` | 手动提交 |
| `NotifyFieldChanged(field, val)` | Form 统一写字段 + SetDirty + 发 OnChange |

### 布局上下文

通过 `EditorForm.Current`（线程静态）传递。`EditorFormItem` 和 `EditorInput` 通过它拿 `DefaultLabelWidth`、`RowSpacing` 等。

---

## 8. EditorFormItem — 字段渲染

**文件**: `FormItem.cs`

### 架构

**唯一渲染入口** `Draw(label, drawSlot)` — Label 左（固定宽 + wordWrap）+ 右边距 + Slot 右。
Float/Int/Toggle/ObjectField/Enum/TextField/Slider/RawField/ObjectFieldWithTag/ArrayField 全部走 `Draw`。

```
Draw:
  BeginRow()              ← spacing + _itemIndex++
  BeginHorizontal
    BeginVertical(w)      ← 左布局（固定宽度容器）
      EditorLabel.Draw    ← Label 占左布局整行
    EndVertical
    Space(Pad)            ← 右边距
    slot()                ← 右布局（输入控件）
  EndHorizontal
  if (!inGroup) Divider   ← 垂直布局画分隔线
```

### API

```csharp
// 反射字段
EditorFormItem.Float("duration", label: "Duration", visibleWhen: () => ...);
EditorFormItem.Int("maxStacks", onBeforeSet: v => Mathf.Max(1, v));
EditorFormItem.Toggle("stackable");
EditorFormItem.Enum<T>("type");
EditorFormItem.TextField("name");
EditorFormItem.ObjectField<T>("effectTag");
EditorFormItem.Slider("hpThreshold", 0f, 1f);

// 自定义内容
EditorFormItem.RawField(label, labelWidth, getValue, setValue, drawFunc, equals);

// ObjectField + TagPicker（ref → local 桥接）
EditorFormItem.ObjectFieldWithTag<T>(fieldName, ref tagBtnRect);

// 多行数组（Label "[N]" 在左列，数组行在右列）
EditorFormItem.ArrayField<T>(label, getValue, setValue, drawRow, createDefault,
    onChanged: ..., tooltip: ...);
```

变更检测后走 `f.NotifyFieldChanged(field, newVal)`——不直接调 `EditorUtility.SetDirty`。

---

## 9. FormItemGroup — 布局包装

**文件**: `FormItemGroup.cs`

```csharp
FormItemGroup.Draw(FormGroupLayout.Horizontal, () =>
{
    item1.Draw(null);
    item2.Draw(null);
});
```

---

## 10. EditorDivider — 分隔线

**文件**: `EditorDivider.cs`

```csharp
EditorDivider.Draw("Advanced Options");
```

---

## 11. EditorImportExport — 导入/导出

**文件**: `ImportExport.cs`

所有 `*ImportExport` 窗口的共享骨架，保持不变。

---

## 12. EditorTokens — 设计令牌

**文件**: `EditorTokens.cs`

所有组件共用的布局、字号、内边距、颜色常量。纯数据，不掺杂逻辑。

| 类别 | 令牌 |
|------|------|
| 布局 | `Pad`(6f) / `PadTight`(3f) |
| 字号 | `FontSm`(11) / `FontBase`(12) / `FontLg`(14) |
| 控件尺寸 | `SizeSm`(16f) / `SizeMd`(20f) / `SizeLg`(26f) |
| 内边距 | `PaddingSmall` / `PaddingMedium` / `PaddingLarge` |
| 颜色 | `ColorGreen` / `ColorGreenDark` / `ColorBlue` / `ColorRed` / `ColorButtonText` / `ColorSelected` |
| 语义色 | `ColorPrimary`(蓝 #4C7EFF) / `ColorSuccess`(绿 #67C23A) / `ColorWarning`(橙 #E6A23C) / `ColorDanger`(红 #D32222) / `ColorInfo`(灰蓝 #A8B2BF) |
| 辅助色 | `ColorDivider`(分隔线) / `ColorDim`(卡片淡化) / `ColorResultOk`(结果绿) / `ColorResultErr`(结果红) |

---

## 13. EditorUIUtility — Helper

**文件**: `UIUtility.cs`

| 成员 | 说明 |
|------|------|
| `GreyPlaceholder` | 灰色居中空状态 GUIStyle |

---

## 14. DataLabelTools — Addressables 标记

**文件**: `Shared/Editor/DataLabelTools.cs`

Addressables label 注册工具。解决 Importer 创建新资产后 Build 中不可用的问题。

### API

```csharp
// 批量：扫描目录下所有资产，标记指定 label
DataLabelTools.TagAllData();        // "boot" → Assets/Data/
DataLabelTools.TagPrototypeArt();   // "prototype-art" → Assets/Art/

// 单个：Importer 创建资产后调用，确保 Build 不遗漏
DataLabelTools.EnsureBootLabel(assetPath);
```

### 菜单

| 菜单项 | 功能 |
|--------|------|
| `RedDust/Data/Tag All Data as 'boot'` | 批量标记 |
| `RedDust/Data/Tag Prototype Art as 'prototype-art'` | 原型美术标记 |

---

## 依赖关系

```
EditorCard
    └── EditorTokens (ColorSelected)

EditorForm
    ├── EditorLabel
    ├── EditorInput
    └── EditorFormItem
          ├── EditorLabel
          └── EditorInput

EditorFormItem
    ├── EditorLabel
    ├── EditorInput
    └── EditorForm.Current (context)

EditorButton → EditorTokens (Font/Padding/Color)
EditorButtonGroup → EditorButton
EditorSearchBar
    ├── EditorLabel
    ├── EditorInput (TextFieldWithClear)
    └── EditorTokens (PadTight)
EditorDivider → (standalone)
FormItemGroup → (standalone)
EditorImportExport → EditorCard + EditorButton

EditorTokens → (纯数据，无依赖)
EditorUIUtility → (纯 Helper，无依赖)
```

所有组件 `#if UNITY_EDITOR`，不进入运行时构建。
