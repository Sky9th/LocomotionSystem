# UIStatBar
> **源文件**: `Assets/Scripts/UI/Components/UIStatBar.cs`

水平填充条。支持 DOTween 平滑变化，三色阈值动态切换，数值标签显示 "--" 降级。

## 调用链

```
VitalsOverlay.Update()
  └── TryUpdateBar(bar, statPath, stats)
      └── bar.SetValue(current, max)
          ├── 归一化 → targetFill
          ├── fillImage.DOFillAmount(targetFill, fillDuration).SetEase(Ease.OutCubic)
          └── valueLabel.text = "current/max"

每帧 Update()
  └── 根据 targetFill 与阈值对比 → 设置 fillImage.color
      ├── > statHighThreshold (0.66) → statHighColor (绿)
      ├── > statLowThreshold (0.33) → statMidColor (黄)
      └── ≤ statLowThreshold → statLowColor (红)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIThemeSO | 读取颜色阈值和 StatBar 配置 |
| 依赖 | DOTween | fillAmount 平滑动画 |
| 被消费 | VitalsOverlay | 每帧刷新 HP/Hunger/Thirst/Stamina |

## 公开属性

无公开属性（C# auto-property）。公开 API 由以下方法提供：

| 方法 | 签名 | 用途 |
|------|------|------|
| SetName | `void SetName(string name)` | 设置 StatBar 标签名 |
| SetValue | `void SetValue(float normalized)` | 设置归一化值 (0~1)，带 DOTween 平滑 |
| SetValue | `void SetValue(float current, float max)` | 设置当前值/最大值，max ≤ 0 时显示 "--" |

## 方法

### SetName()
```csharp
public void SetName(string name)
```
- **用途**: 设置 StatBar 标签名
- **参数**: `name` — 标签文字
- **调用者**: VitalsOverlay.OnInitialize()

### SetValue(float normalized)
```csharp
public void SetValue(float normalized)
```
- **用途**: 设置归一化值 (0~1)
- **参数**: `normalized` — 0~1 之间的值
- **调用者**: SetValue(float, float)
- **备注**: 运行时用 DOTween 平滑，Editor 下直接设置

### SetValue(float current, float max)
```csharp
public void SetValue(float current, float max)
```
- **用途**: 设置当前值/最大值
- **参数**: `current` — 当前值；`max` — 最大值
- **调用者**: VitalsOverlay.TryUpdateBar()
- **备注**: max ≤ 0 时显示 "--"（角色未生成时安全降级）

### Update()
```csharp
private void Update()
```
- **用途**: 根据 targetFill 与阈值对比更新颜色
- **调用者**: Unity Engine（每帧）
- **备注**: 读取 `targetFill` 而非 `fillImage.fillAmount`，避免补间期间颜色闪烁

### Awake()
```csharp
private void Awake()
```
- **用途**: 初始化背景色和 fillImage 配置（type=Filled, fillMethod=Horizontal）
- **调用者**: Unity Engine

## 内部机制

- **color vs targetFill**: 颜色判断基于 `targetFill` 而非实际 fillAmount，保证动画过渡期间颜色不来回闪烁
- **安全降级**: max ≤ 0 时显示 "--"，防止角色未生成时显示异常数值
- **Awake 初始化**: 设置 fillImage.type 为 Filled + Horizontal，背景色为深灰半透明

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| `fillDuration` | float | 0.2f | DOTween 补间时长 |
| `theme` | UIThemeSO | — | 颜色阈值来源 |
| `fillImage` | Image | — | 填充前景 |
| `backgroundImage` | Image | — | 背景底色 |
| `nameLabel` | TMP_Text | — | 名称标签 |
| `valueLabel` | TMP_Text | — | 数值标签 |

## 未来规划

无。
