# UIColorStyle + UIColorSet · 颜色风格

> `Assets/Scripts/UI/Core/UIColorStyle.cs` — 颜色风格枚举 + 9 色结构体。

## UIColorSet struct

```csharp
[Serializable]
public struct UIColorSet
{
    public Color primary;          // 按钮背景
    public Color primaryHover;     // 按钮悬浮
    public Color primaryPressed;   // 按钮按下
    public Color onPrimary;        // 按钮文字

    public Color surface;          // 面板背景
    public Color surfaceAlt;       // 交替行
    public Color onSurface;        // 面板文字
    public Color onSurfaceMuted;   // 弱化文字

    public Color border;           // 描边
}
```

## UIColorStyle 枚举

| 值 | 用途 |
|----|------|
| `Normal` | 默认灰色调 |
| `Primary` | 强调橙色 |
| `Danger` | 危险红色 |
| `Warning` | 警告黄色 |
| `Success` | 成功绿色 |

## 调用链

```
UIThemeSO.GetColorSet(style) → UIColorSet
  ├── UIButton.ApplyTheme() → primary / primaryHover / primaryPressed / onPrimary
  ├── UIPanel.ApplyTheme() → surface
  └── UILabel.ApplyStyle() → onPrimary (Button 风格时)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被消费 | UIThemeSO | 持有 5 个 UIColorSet 实例 |
| 被消费 | UIButton | 按 style 取色 |
| 被消费 | UIPanel | 按 style 取 surface 色 |
| 被消费 | UILabel | Button 风格时取 onPrimary |

## 未来规划

无。
