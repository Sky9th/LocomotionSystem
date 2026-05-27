# UIButton
> **源文件**: `Assets/Scripts/UI/Components/UIButton.cs`

按钮组件。集成 IPointer 接口 + DOTween 动画 + 主题色。取代 Unity Button 原生 transition。

## 调用链

```
Awake()
  ├── button.onClick.AddListener(HandleClick)
  └── ApplyTheme()
      ├── background.color = theme.GetColorSet(style).primary
      ├── labelText.color/font/fontSize ← 主题配置
      └── button.transition = Transition.None (禁用原生过渡)

鼠标交互事件:
  ├── OnPointerEnter → DOScale(hoverScale) + DOColor(primaryHover)
  ├── OnPointerExit  → DOScale(1f) + DOColor(primary)
  ├── OnPointerDown  → DOScale(pressScale) + DOColor(primaryPressed)
  └── OnPointerUp    → DOScale(hoverScale) + DOColor(primaryHover)

点击事件:
  └── button.onClick → HandleClick → OnClicked?.Invoke()

销毁:
  └── OnDestroy → DOTween.Kill(transform) + button.onClick.RemoveListener
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIThemeSO | ApplyTheme 时读取颜色/字体/动画参数 |
| 依赖 | UIColorStyle | 选择哪种色调 |
| 依赖 | DOTween | 悬停/按压动画 |
| 依赖 | Unity UI (Button/Image/TMP_Text) | 基础 UI 组件 |
| 被消费 | MainMenuScreen / PauseMenuScreen | 按钮点击事件绑定 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Label` | string | 按钮文字 (get/set，委托到 labelText) |
| `Interactable` | bool | 是否可交互，disabled 时用 `theme.buttonDisabled` 色 |
| `Button` | Button (readonly) | 持有 Unity Button 引用 |
| `OnClicked` | event Action | 点击事件 |

## 方法

### SetText()
```csharp
public void SetText(string text)
```
- **用途**: 设置按钮文字
- **调用者**: MainMenuScreen / PauseMenuScreen.OnInitialize()

### SetInteractable()
```csharp
public void SetInteractable(bool interactable)
```
- **用途**: 设置交互性并更新颜色
- **调用者**: MainMenuScreen (loadGame/settings 按钮禁用)
- **备注**: disabled 时使用 `theme.buttonDisabled` 颜色

### ApplyTheme()
```csharp
private void ApplyTheme()
```
- **用途**: 应用主题色、字体、字号；禁用原生 transition
- **调用者**: Awake()

### ApplyColor()
```csharp
private void ApplyColor(Color color)
```
- **用途**: 设置背景颜色
- **调用者**: ApplyTheme / SetInteractable / Pointer 事件

## 内部机制

- **MonoBehaviour + ExecuteAlways**: 在 Editor 中也执行 `Awake`，运行时与编辑器中均应用主题
- **事件清理**: `OnDestroy` 中移除 `button.onClick` 监听，防止残留引用
- **Interactable 检查**: 鼠标事件中先判断 `Interactable && Application.isPlaying`，非可交互时跳过动画

## 配置参数

| 参数 | 类型 | 默认 | 用途 |
|------|------|------|------|
| `style` | UIColorStyle | Normal | 按钮色彩风格 |
| `theme` | UIThemeSO | — | 主题配置 |
| `background` | Image | — | 按钮背景图 |
| `labelText` | TMP_Text | — | 按钮文字 |
| `button` | Button | — | Unity Button 组件 |

## 未来规划

无。
