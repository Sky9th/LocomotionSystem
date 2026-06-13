# EUI Components — 编辑器 UI 组件库

> `Shared/Editor/Components/` · namespace `RedDust.Shared.EditorUI`
>
> 所有 Editor Window 和 Inspector 共用的标准 IMGUI 组件。基于 Unity `EditorGUILayout` 封装，提供一致的外观和交互。

## 组件总览

| 组件 | 类型 | 用途 | 对标 |
|------|------|------|------|
| `EditorCard` | static | 卡片容器 — 圆角边框 + 内边距 | `el-card` |
| `EditorButton` | static | 按钮 — 大小 + 颜色风格 | `el-button` |
| `EditorUIUtility` | static | 布局工具 — Header / 搜索 / 筛选 / Tooltip | — |
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

---

## 2. EditorButton — 按钮

**文件**: `Button.cs`

### 风格枚举

| 枚举 | 值 | 颜色 |
|------|-----|------|
| `EditorButtonStyle.Default` | 0 | 默认灰 |
| `EditorButtonStyle.Primary` | 1 | 绿色 |
| `EditorButtonStyle.Success` | 2 | 深绿 |
| `EditorButtonStyle.Danger` | 3 | 红色 |

### 尺寸枚举

| 枚举 | padding | fontSize |
|------|---------|----------|
| `EditorButtonSize.Auto` | — | 默认 | 自适应（GUILayout 默认） |
| `EditorButtonSize.Small` | 7×15 | 12 | miniButton |
| `EditorButtonSize.Medium` | 10×16 | 12 | 自定义 style |
| `EditorButtonSize.Large` | 14×22 | 13 | 自定义 style |

### API

```csharp
// 主按钮
bool clicked = EditorButton.Draw("Import",
    EditorButtonStyle.Success,     // 深绿色
    EditorButtonSize.Large,        // 大号
    120f);                         // 固定宽度

// 筛选标签
bool clicked = EditorButton.DrawTab("Melee", isSelected);

// 手动 Rect 版本
bool clicked = EditorButton.Draw(rect, "x", EditorButtonStyle.Danger);
```

---

## 3. EditorUIUtility — 布局工具

**文件**: `UIUtility.cs`

### API

```csharp
// 标准 Header：[Title] [Subtitle(右)] [Save*]
EditorUIUtility.DrawHeaderCard(6f, "Ability Editor", "L3_Ability",
    hasChanges: true, onSave: () => Save());

// Tooltip Label（自动从 [Tooltip] attribute 读取）
EditorUIUtility.LabelWithTooltip(so, "cooldownDuration", 90f);
EditorUIUtility.LabelWithTooltip("CD", "冷却时间（秒）", 90f);

// 搜索行：Label + TextField + 清除按钮
string search = EditorUIUtility.DrawSearchRow(_searchText, labelWidth: 45f);

// 枚举筛选标签栏
EAbilityType type = EditorUIUtility.DrawFilterTabBar(_type, tabs, labels);

// 红色删除按钮 "x"
bool del = EditorUIUtility.DeleteButton();       // GUILayout 版
bool del = EditorUIUtility.DeleteButton(rect);   // Rect 版
```

### 颜色常量

| 常量 | 值 | 用途 |
|------|-----|------|
| `ColorGreen` | (0.4, 0.8, 0.4) | 保存按钮 |
| `ColorGreenDark` | (0.4, 0.7, 0.4) | 创建/导入按钮 |
| `ColorBlue` | #4C7EFF | 链接色 / 选中高亮 |
| `ColorRed` | #D32222 | 错误 / 删除按钮 |
| `ColorButtonText` | #EEEEEE | 有色按钮上的白字 |

### 预置样式

```csharp
EditorUIUtility.GreyPlaceholder  // 灰色居中提示文字 ("No items." / "File not found.")
```

---

## 4. EditorForm — 声明式表单

**文件**: `Form.cs` + `FormItem.cs`（internal）

> 绑定一个 `ScriptableObject`，通过 fluent API 定义字段列表。
> `Draw()` 自动渲染 Label + Input + 变更检测 + `EditorUtility.SetDirty`。

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
    .ReadOnly()                    // 只读
    .HelpText("超过 1.0 为加速")    // 灰色帮助文本
    .Divider("高级选项")            // 分组分隔线
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
// 在 OnGUI 中
if (EditorForm.NeedsRebuild(_form, _selectedSO))
    _form = BuildForm(_selectedSO);   // 重建表单
_form?.Draw();                         // 渲染
```

### 属性

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `DefaultLabelWidth` | 90f | Label 列宽度 |
| `RowSpacing` | 6f | 行间距 |
| `OnAnyChange` | null | 任一字段变更事件 |

---

## 5. EditorImportExport — 导入/导出面板

**文件**: `ImportExport.cs`

所有 `*ImportExport` 窗口的共享骨架。一次性配置，提供完整的文件选择 + 预览 + 导入/导出按钮 + 结果展示。

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
    onImport: path => { var (c, s, e) = Importer.ImportFromFile(path); return (c, s, e); },
    onExport: path => File.WriteAllText(path, Importer.ExportToJson())
);
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `title` | string | 窗口标题（largeLabel） |
| `subtitle` | string | 右侧副标题（灰字） |
| `defaultDir` | string | 文件选择器默认目录 |
| `fileExtension` | string | 文件扩展名过滤 |
| `defaultFileName` | string | 导出默认文件名 |
| `filePath` | ref string | 双向绑定的文件路径 |
| `previewText` | ref string | 缓存的预览文本 |
| `result` | ref (int, int, List\<string\>) | 导入结果 |
| `buildPreview` | Func\<string, string\> | 预览回调（支持 rich text） |
| `onImport` | Func\<string, (int, int, List\<string\>)\> | 导入回调 |
| `onExport` | Action\<string\> | 导出回调 |

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
            subtitle: "Description",
            defaultDir: "Assets/Data/MySystem",
            fileExtension: "json",
            defaultFileName: "my_export",
            filePath: ref _filePath,
            previewText: ref _previewText,
            result: ref _result,
            buildPreview: BuildPreview,
            onImport: path => { /* ... */ },
            onExport: path => { /* ... */ }
        );
    }
}
```

---

## 布局约定

| 约定 | 值 | 说明 |
|------|-----|------|
| 默认内边距 (pad) | 6f | EditorCard.Draw 的 pad 参数 |
| 卡片间距 | `EditorCard.Gap(6f)` | 同级卡片间 |
| 紧凑间距 | `EditorCard.GapTight()` | 关联紧密的元素间 (3px) |
| 默认 Label 宽度 | 90f | EditorForm.DefaultLabelWidth |
| 删除按钮 | `EditorUIUtility.DeleteButton()` | 红色 "x" miniButton |
| 空状态文本 | `EditorUIUtility.GreyPlaceholder` | 灰色居中 |
| 窗口最小尺寸 | 520×420 | 导入/导出窗口标准 |
| 导入按钮 | Success + Large + 120px | 固定宽度 |
| 导出按钮 | Primary + Large + 120px | 固定宽度 |

---

## 依赖关系

```
EditorImportExport
    ├── EditorCard (Draw, DrawLight, Gap, GapTight)
    ├── EditorButton (Draw)
    └── EditorUIUtility (GreyPlaceholder)

EditorUIUtility
    ├── EditorCard (Draw)
    └── EditorButton (Draw, DrawTab)

EditorForm
    └── EditorFormItem (internal)

EditorFormItem
    └── (standalone)
```

所有组件都在 `#if UNITY_EDITOR` 下，不进入运行时构建。
