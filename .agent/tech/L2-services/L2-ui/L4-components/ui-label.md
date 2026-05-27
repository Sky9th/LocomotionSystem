# UILabel
> **源文件**: `Assets/Scripts/UI/Components/UILabel.cs`

主题驱动的文字标签。按 UITextStyle 枚举从 UIThemeSO 读取字体/字号/颜色。

## 调用链

```
Awake() → ApplyStyle() → 根据 textStyle 设置 font/size/color

SetStyle(newStyle) → textStyle = newStyle → ApplyStyle()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIThemeSO | 读取字体/字号/颜色 |
| 依赖 | TMP_Text | 文字渲染组件 |

## UITextStyle 枚举

定义在 `UILabel.cs` 文件中。

| 值 | 字体 | 字号 | 颜色 |
|----|------|------|------|
| `Title` | theme.titleFont | theme.titleFontSize (48) | theme.titleColor |
| `Subtitle` | theme.titleFont | theme.subtitleFontSize (28) | theme.subtitleColor |
| `Body` | theme.bodyFont | theme.bodyFontSize (18) | theme.bodyColor |
| `Button` | theme.bodyFont | theme.buttonFontSize (22) | theme.normalColors.onPrimary |
| `Small` | theme.bodyFont | theme.smallFontSize (14) | theme.subtitleColor |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Text` | string | 文字内容 (get/set，委托到 tmpText) |

## 方法

### SetText()
```csharp
public void SetText(string text)
```
- **用途**: 设置文字内容
- **调用者**: MainMenuScreen (versionText)

### SetStyle()
```csharp
public void SetStyle(UITextStyle style)
```
- **用途**: 运行时切换文字风格
- **调用者**: 外部
- **备注**: 切换后立即调用 `ApplyStyle()` 刷新

### ApplyStyle()
```csharp
private void ApplyStyle()
```
- **用途**: 按 textStyle 应用字体/字号/颜色
- **调用者**: Awake / SetStyle

## 内部机制

- **MonoBehaviour + ExecuteAlways**: 在 Editor 中也执行 `Awake`，保证编辑器中样式同步
- **theme 为空保护**: theme 或 tmpText 为 null 时跳过 ApplyStyle

## 配置参数

| 参数 | 类型 | 默认 | 用途 |
|------|------|------|------|
| `theme` | UIThemeSO | — | 主题配置 |
| `tmpText` | TMP_Text | — | 文字渲染组件 |
| `textStyle` | UITextStyle | Body | 文字风格 |

## 未来规划

无。
