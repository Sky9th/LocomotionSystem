# UILabel · 文字标签

> `Assets/Scripts/UI/Components/UILabel.cs` — 主题驱动的文字标签。按 UITextStyle 枚举从 UIThemeSO 读取字体/字号/颜色。

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

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Text` | string | 文字内容 (get/set) |

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
- **备注**: 切换后立即 ApplyStyle()

## UITextStyle 枚举

| 值 | 字体 | 字号 | 颜色 |
|----|------|------|------|
| `Title` | theme.titleFont | theme.titleFontSize (48) | theme.titleColor |
| `Subtitle` | theme.titleFont | theme.subtitleFontSize (28) | theme.subtitleColor |
| `Body` | theme.bodyFont | theme.bodyFontSize (18) | theme.bodyColor |
| `Button` | theme.bodyFont | theme.buttonFontSize (22) | theme.normal.onPrimary |
| `Small` | theme.bodyFont | theme.smallFontSize (14) | theme.subtitleColor |

## 未来规划

无。
