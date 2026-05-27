# UIThemeSO · 主题配置

> `Assets/Scripts/UI/Config/UIThemeSO.cs` — ScriptableObject。集中管理所有 UI 视觉参数：颜色风格、字体、字号、动画参数、StatBar 阈值。

## 调用链

```
UIButton.Awake() → ApplyTheme()
  └── theme.GetColorSet(style) → primary/Hover/Pressed + onPrimary
  ├── theme.buttonHoverScale / buttonPressScale / buttonAnimDuration
  └── theme.buttonDisabled

UILabel.Awake() → ApplyStyle()
  ├── theme.titleFont / titleFontSize / titleColor
  ├── theme.bodyFont / bodyFontSize / bodyColor
  └── theme.GetColorSet(Normal).onPrimary  (Button 风格)

UIPanel.Awake() → ApplyTheme()
  └── theme.GetColorSet(style).surface

UIStatBar.Update() → color threshold
  ├── theme.statHighColor / statHighThreshold
  ├── theme.statMidColor / statLowThreshold
  └── theme.statLowColor
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被消费 | UIButton | 颜色/动画参数 |
| 被消费 | UILabel | 字体/字号/颜色 |
| 被消费 | UIPanel | surface 颜色 |
| 被消费 | UIStatBar | 颜色阈值 |
| 依赖 | UIColorSet / UIColorStyle | GetColorSet 返回 |

## 配置参数

### 面板颜色

| 字段 | 默认值 |
|------|--------|
| `screenBackgroundColor` | (0.08, 0.08, 0.08, 1) |
| `overlayBackgroundColor` | (0.12, 0.12, 0.12, 0.85) |

### 按钮

| 字段 | 默认值 |
|------|--------|
| `buttonDisabled` | (0.1, 0.1, 0.1, 0.5) |

### 5 色风格 (`normalColors / primaryColors / dangerColors / warningColors / successColors`)

每个包含 9 个 Color 字段 (见 UIColorSet struct)。

### 文字

| 字段 | 默认值 |
|------|--------|
| `titleColor` | White |
| `bodyColor` | (0.85, 0.85, 0.85, 1) |
| `subtitleColor` | (0.7, 0.7, 0.7, 1) |
| `accentColor` | (0.85, 0.45, 0.1, 1) |
| `dangerColor` | Red |

### StatBar

| 字段 | 默认值 |
|------|--------|
| `statHighColor` | (0.3, 0.8, 0.3) — 绿色 |
| `statMidColor` | (0.9, 0.72, 0.2) — 黄色 |
| `statLowColor` | (0.9, 0.2, 0.2) — 红色 |
| `statHighThreshold` | 0.66f |
| `statLowThreshold` | 0.33f |

### 字形

| 字段 | 默认值 |
|------|--------|
| `titleFontSize` | 48 |
| `subtitleFontSize` | 28 |
| `bodyFontSize` | 18 |
| `buttonFontSize` | 22 |
| `smallFontSize` | 14 |

### 布局

| 字段 | 默认值 |
|------|--------|
| `elementSpacing` | 12 |
| `buttonSize` | (280, 50) |

### 动画

| 字段 | 默认值 |
|------|--------|
| `fadeDuration` | 0.3s |
| `slideDuration` | 0.35s |
| `buttonHoverScale` | 1.05 |
| `buttonPressScale` | 0.97 |
| `buttonAnimDuration` | 0.1s |
| `statBarFillSpeed` | 0.2s |

## 方法

### GetColorSet()
```csharp
public UIColorSet GetColorSet(UIColorStyle style)
```
- **用途**: 按风格返回对应 UIColorSet
- **参数**: `style` — 风格枚举
- **调用者**: UIButton / UIPanel / UILabel

## 未来规划

无。
