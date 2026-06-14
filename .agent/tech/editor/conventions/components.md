# EUI Components — 编辑器 UI 组件库

> `Shared/Editor/Components/` · namespace `RedDust.Shared.EditorUI`
>
> 所有 Editor Window 和 Inspector 共用的标准 IMGUI 组件。基于 Unity `EditorGUILayout` 封装，对标 Element UI，提供一致的外观和交互。

## 核心原则

1. **卡片即容器** — 所有逻辑区块用 `EditorCard.Draw(pad, ...)` 包裹，禁止裸 `EditorStyles.helpBox`
2. **按钮走组件** — 所有按钮走 `EditorButton.Draw`，禁止裸 `GUILayout.Button`
3. **间距统一推导** — 全部间距从 `pad=6` 出发，不出现魔术数字
4. **宽高外置** — 卡片本身不控制宽高（100% 宽），由外层 `BeginHorizontal(options)` 控制

---

## 组件总览

| 组件 | 类型 | 用途 | 对标 |
|------|------|------|------|
| `EditorCard` | static | 卡片容器 — 圆角边框 + 内边距 | `el-card` |
| `EditorButton` | static | 按钮 — 大小 + 颜色风格 | `el-button` |
| `EditorButtonGroup` | static | 按钮组 — 多按钮水平排列，单选高亮 | `el-button-group` |
| `EditorSearchBar` | static | 搜索栏 — Label + TextField + 清除按钮 | `el-input` search |
| `EditorUIUtility` | static | 布局工具 — Header / 搜索 / 筛选委托 / 删除按钮 | — |
| `EditorForm` | class | 声明式表单 — 绑定 SO，自动 SetDirty | `el-form` |
| `EditorImportExport` | static | 导入/导出面板 — 文件选择 + 预览 + 按钮 + 结果 | — |

---

## 1. EditorCard — 卡片容器

**文件**: `Card.cs`

最基础的布局单元。所有编辑器界面都应包裹在 `EditorCard` 中。

### API

```csharp
// 空白卡片
EditorCard.Draw(float pad, Action drawContent);

// 带选中高亮（#2C5D87 蓝色背景）
EditorCard.Draw(float pad, Action drawContent, bool selected);

// 带标题
EditorCard.Draw(float pad, string title, Action drawBody);

// 轻量卡片（半内边距，淡化边框）— 嵌套时使用
EditorCard.DrawLight(float pad, Action drawContent);

// 折叠卡片（▸/▾箭头 + 可展开内容）
EditorCard.DrawFoldout(float pad, string title, ref bool folded, Action drawContent);

// 列表项（扁平内边距 + 选中高亮 + 点击回调）
EditorCard.DrawItem(float pad, Action drawContent, bool selected = false, Action onClick = null);

// 间距
EditorCard.Gap(float pad);       // 标准间距
EditorCard.GapTight();           // 紧凑间距 (3px)
```

### 使用范例

```csharp
EditorCard.Draw(6f, "技能设置", () =>
{
    EditorGUILayout.LabelField("cooldownDuration", EditorStyles.label);
});

EditorCard.DrawFoldout(6f, "高级选项", ref _folded, () =>
{
    EditorCard.DrawLight(4f, () => { /* 嵌套内容 */ });
});
```

### 卡片内部结构

```
BeginVertical(helpBox)
  Space(pad)              ← 上内边距
  BeginHorizontal
    Space(pad)            ← 左内边距
    BeginVertical
      [content]
    EndVertical
    Space(pad)            ← 右内边距
  EndHorizontal
  Space(pad)              ← 下内边距
EndVertical
```

---

## 2. EditorButton — 按钮

**文件**: `Button.cs`

### 风格枚举

| 枚举 | 颜色 | 用途 |
|------|------|------|
| `Default` | 默认灰 | 普通按钮、清除按钮 |
| `Primary` | 绿色 | 保存、选中态 |
| `Success` | 深绿 | 创建、导入 |
| `Danger` | 红色 | 删除、破坏性操作 |

### 尺寸枚举

| 枚举 | padding | fontSize | 说明 |
|------|---------|----------|------|
| `Auto` | — | 默认 | 自适应（GUILayout 默认） |
| `Small` | 7×15 | 12 | miniButton |
| `Medium` | 10×16 | 12 | 自定义 style |
| `Large` | 14×22 | 13 | 自定义 style |

### API

```csharp
// 标准按钮
bool clicked = EditorButton.Draw("Import",
    EditorButtonStyle.Success,
    EditorButtonSize.Large,
    120f);                         // 可选固定宽度

// 手动 Rect 版本
bool clicked = EditorButton.Draw(rect, "x", EditorButtonStyle.Danger);
```

### 典型用法

| 用途 | API |
|------|-----|
| 主操作（Save） | `EditorButton.Draw("Save *", Primary, Medium)` |
| 创建 | `EditorButton.Draw("+ Create", Success, Medium)` |
| 导入 | `EditorButton.Draw("Import", Success, Large, 120f)` |
| 导出 | `EditorButton.Draw("Export", Primary, Large, 120f)` |
| 删除 | `EditorButton.Draw("x", Danger, Small, 20)` — 见 `EditorUIUtility.DeleteButton()` |
| 清除 | `EditorButton.Draw("x", Default, Small, 20)` — SearchBar 内 |

---

## 3. EditorButtonGroup — 按钮组

**文件**: `ButtonGroup.cs`

多个按钮横向排列，仅一个高亮选中（Primary 风格）。对标 `el-button-group`。

### API

```csharp
// Enum 模式
T next = EditorButtonGroup.Draw(
    T current, T[] values, string[] labels,
    EditorButtonSize size = EditorButtonSize.Small);

// 索引模式
int idx = EditorButtonGroup.Draw(
    string[] labels, int selectedIndex = -1,
    EditorButtonSize size = EditorButtonSize.Small);
```

- 点击返回新值/索引，未点击返回原值
- 选中 → `Primary`，未选中 → `Default`

---

## 4. EditorSearchBar — 搜索栏

**文件**: `SearchBar.cs`

结构: `[Label(w:固定)] [TextField(flex)] [× ClearBtn(w:20)]`。对标 `el-input` search。

### API

```csharp
string next = EditorSearchBar.Draw(_searchText, labelWidth: 45f);
```

- 有文字 → 清除按钮可点击（`Default` 风格）
- 空文字 → 清除按钮 `GUI.enabled = false`
- 布局参照 `EditorFormItem`：`BeginHorizontal` + `singleLineHeight`

---

## 5. EditorUIUtility — 布局工具

**文件**: `UIUtility.cs`

`DrawFilterTabBar` 和 `DrawSearchRow` 已委托给 `EditorButtonGroup` / `EditorSearchBar`，此处为便捷入口。

### API

```csharp
// Header
EditorUIUtility.DrawHeaderCard(6f, "Ability Editor", "L3_Ability",
    hasChanges: true, onSave: () => Save());

// Tooltip Label
EditorUIUtility.LabelWithTooltip(so, "cooldownDuration", 90f);

// 搜索行 → EditorSearchBar.Draw
string search = EditorUIUtility.DrawSearchRow(_searchText, labelWidth: 45f);

// 筛选标签栏 → EditorButtonGroup.Draw
EAbilityType type = EditorUIUtility.DrawFilterTabBar(_type, tabs, labels);

// 删除按钮 → EditorButton.Draw("x", Danger)
bool del = EditorUIUtility.DeleteButton();
```

### 颜色常量

| 常量 | 值 | 用途 |
|------|-----|------|
| `ColorGreen` | (0.4, 0.8, 0.4) | 保存按钮 |
| `ColorGreenDark` | (0.4, 0.7, 0.4) | 创建/导入按钮 |
| `ColorBlue` | #4C7EFF | 选中高亮 |
| `ColorRed` | #D32222 | 错误 / 删除 |
| `ColorButtonText` | #EEEEEE | 有色按钮上的白字 |

### 预置样式

```csharp
EditorUIUtility.GreyPlaceholder  // 灰色居中空状态文字
```

---

## 6. EditorForm — 声明式表单

**文件**: `Form.cs` + `FormItem.cs`（internal）

绑定 `ScriptableObject`，fluent API 定义字段，`Draw()` 自动渲染 Label + Input + SetDirty。

### 构建 API

```csharp
var form = new EditorForm(targetSO);

form
    .Float("cooldownDuration", "冷却时间", labelWidth: 90f)
    .Toggle("overrideExclusion", "无视互斥")
    .Enum<EActivationType>("activationType", "激活方式")
    .TextField("internalName", "内部名")
    .TextArea("description", "描述")
    .ObjectField<AbilityActivationSO>("activation", "激活方式")
    .Slider("animationSpeed", 0.1f, 3f, "动画速度")
    .ReadOnly()
    .HelpText("超过 1.0 为加速")
    .Divider("高级选项")
    .CustomOnChange((old, @new) => { /* 自定义变更处理 */ return true; });
```

### 字段类型

| 方法 | Unity 控件 | 值类型 |
|------|-----------|--------|
| `.Float()` | `EditorGUILayout.FloatField` | float |
| `.Int()` | `EditorGUILayout.IntField` | int |
| `.Slider()` | `EditorGUILayout.Slider` | float |
| `.Toggle()` | `EditorGUILayout.Toggle` | bool |
| `.Enum<T>()` | `EditorGUILayout.EnumPopup` | enum |
| `.TextField()` | `EditorGUILayout.TextField` | string |
| `.TextArea()` | `EditorGUILayout.TextArea` | string (多行) |
| `.ObjectField<T>()` | `EditorGUILayout.ObjectField` | UnityEngine.Object |
| `.RawField()` | 自定义 | 任意 |

### 修饰方法

| 方法 | 作用 |
|------|------|
| `.ReadOnly()` | 禁用输入 |
| `.HelpText(text)` | 输入下方灰色说明 |
| `.PostInput(drawExtra)` | 输入后追加自定义控件 |
| `.CustomDraw(drawFunc)` | 完全替换输入控件 |
| `.CustomOnChange(onChange)` | 替换变更处理（需返回 bool） |
| `.CustomEquals(equals)` | 替换等值比较 |
| `.Divider(title)` | 分组分隔线 |

### 生命周期

```csharp
if (EditorForm.NeedsRebuild(_form, _selectedSO))
    _form = BuildForm(_selectedSO);
_form?.Draw();
```

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `DefaultLabelWidth` | 90f | Label 列宽度 |
| `RowSpacing` | 6f | 行间距 |
| `OnAnyChange` | null | 任一字段变更事件 |

---

## 7. EditorImportExport — 导入/导出面板

**文件**: `ImportExport.cs`

所有 `*ImportExport` 窗口的共享骨架。

### API

```csharp
EditorImportExport.Draw(
    title: "Ability Import-Export",
    subtitle: "L3_Ability · JSON ↔ .asset",
    defaultDir: "Assets/Data/Ability",
    fileExtension: "json",
    defaultFileName: "abilities_export",
    filePath: ref _filePath,
    previewText: ref _previewText,
    result: ref _result,
    buildPreview: path => BuildPreview(path),
    onImport: path => { /* ... */ },
    onExport: path => { /* ... */ }
);
```

### 窗口模板

```csharp
public class MyImportWindow : EditorWindow
{
    private string _filePath;
    private string _previewText;
    private (int created, int skipped, List<string> errors) _result;

    [MenuItem("RedDust/My Import-Export", priority = 99)]
    public static void Open()
    {
        var window = GetWindow<MyImportWindow>("My Import-Export");
        window.minSize = new Vector2(520, 420);
        window.Show();
    }

    private void OnGUI()
    {
        EditorImportExport.Draw(
            title: "My Import-Export",
            /* ... */
        );
    }
}
```

---

## 布局约定

| 约定 | 值 | 说明 |
|------|-----|------|
| 默认内边距 `pad` | 6f | 卡片内边距 = 卡片间隙 = 窗口边缘间距 |
| 卡片间距 | `EditorCard.Gap(6f)` | 同级卡片间 |
| 紧凑间距 | `EditorCard.GapTight()` | 关联紧密元素间 (3px) |
| 默认 Label 宽度 | 90f | `EditorForm.DefaultLabelWidth` |
| 折叠三角宽度 | 14px | 叶节点保留占位对齐 |
| 树节点缩进 | 18px/层 | `Space(depth * 18)`，在文字层缩进 |
| 删除按钮 | `EditorUIUtility.DeleteButton()` | 红色 "x" |
| 空状态文本 | `EditorUIUtility.GreyPlaceholder` | 灰色居中 |
| 导入/导出窗口 | 520×420 | 最小尺寸 |

---

## 布局模式

### 模式 A：多栏布局

```
BeginHorizontal                  ← 栏容器
  BeginHorizontal(Width, ExpandHeight)  ← 栏 1
    EditorCard.Draw(pad, () => { ... })
  EndHorizontal
  Space(pad)                     ← 栏间隙
  BeginHorizontal(ExpandWidth, ExpandHeight)  ← 栏 2
    EditorCard.Draw(pad, () => { ... })
  EndHorizontal
EndHorizontal
```

- 每栏 = `BeginHorizontal(尺寸选项)` → `EditorCard.Draw` → `EndHorizontal`
- 固定宽用 `Width(...)`，自适应用 `ExpandWidth(true)`

### 模式 B：子卡片列表

```
EditorCard.Draw(pad, () =>
{
    EditorCard.Draw(pad, () => { ... });
    EditorCard.Gap(pad);
    EditorCard.Draw(pad, () => { ... });
    EditorCard.Gap(pad);
    EditorCard.Draw(pad, () => { ... });
});
```

每个子区块独立 `EditorCard.Draw`，间隙 `EditorCard.Gap(pad)`。嵌套不影响外层内边距。

### 模式 C：折叠树节点

```
EditorCard.Draw(pad, () =>
{
    BeginHorizontal;
      BeginHorizontal(Width(20));  ← 折叠三角区 (14+6)
        foldRect(14) / dash
        Space(6)
      EndHorizontal;
      BeginVertical(ExpandWidth);
        Space(depth * 18);         ← 文字缩进，非卡片嵌套
        名称 label/button
        [子节点 — 模式 B]
      EndVertical;
    EndHorizontal;
});
```

树节点独立卡片递归嵌套。缩进在文字层（`Space(depth * 18)`），不靠卡片层级。

### 窗口级间距

```
Window edge
│ Space(pad)     ← 上
│ ┌─ BeginHorizontal ────────────────┐
│ │ Space(pad)   ← 左                │
│ │ content (BeginVertical)          │
│ │ Space(pad)   ← 右                │
│ └──────────────────────────────────┘
│ Space(pad)     ← 下
Window edge
```

---

## 常见陷阱

1. **禁止裸 `GUILayout.Button`** → 走 `EditorButton.Draw`
2. **禁止裸 `EditorStyles.helpBox`** → 走 `EditorCard.Draw`
3. **禁止魔术数字** → `Space(2)`, `Space(4)` 等一律用 `pad` 或 `EditorCard.Gap`
4. **禁止 emoji** → Unity IMGUI 默认字体不渲染
5. **卡片嵌套合法** → 内层 `EditorCard.Draw` 不影响外层内边距
6. **`Foldout` 不支持 `GUILayoutOption`** → 用 `GetRect` + `EditorGUI.Foldout` 固定宽度
7. **`ref` 参数不能进 lambda** → 先捕获到本地变量，lambda 结束后回写
8. **命名空间避免 `*.Editor`** → 与 `UnityEditor.Editor` 冲突，用 `*.EditorUI`

---

## 依赖关系

```
EditorImportExport
    ├── EditorCard (Draw, DrawLight, Gap, GapTight)
    ├── EditorButton (Draw)
    └── EditorUIUtility (GreyPlaceholder)

EditorUIUtility
    ├── EditorCard (Draw)
    ├── EditorButton (Draw)
    ├── EditorButtonGroup (Draw)   ← DrawFilterTabBar 委托
    └── EditorSearchBar (Draw)     ← DrawSearchRow 委托

EditorButtonGroup
    └── EditorButton (Draw)

EditorSearchBar
    └── EditorButton (Draw)

EditorForm
    └── EditorFormItem (internal)

EditorFormItem
    └── (standalone)
```

所有组件都在 `#if UNITY_EDITOR` 下，不进入运行时构建。
