# UIPanel
> **源文件**: `Assets/Scripts/UI/Components/UIPanel.cs`

面板背景容器。`[ExecuteAlways]`，从 UIThemeSO 取 surface 色设置背景。

## 调用链

```
Awake() → ApplyTheme() → background.color = theme.GetColorSet(style).surface
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | UIThemeSO | 读取 surface 颜色 |
| 依赖 | UIColorStyle | 选择色调 |

## 公开属性

无公开属性。面板通过子 UI 组件（UILabel、UIButton）组合构建。

## 方法

### ApplyTheme()
```csharp
private void ApplyTheme()
```
- **用途**: 从 theme 获取 surface 色并应用
- **调用者**: Awake()
- **备注**: theme 或 background 为 null 时跳过

## 内部机制

- **MonoBehaviour + ExecuteAlways**: 在 Editor 中也执行 `Awake`，保证编辑器中面板背景同步
- **纯视觉组件**: 无公开属性和事件，仅负责背景色渲染

## 配置参数

| 参数 | 类型 | 默认 | 用途 |
|------|------|------|------|
| `style` | UIColorStyle | Normal | 色彩风格 |
| `theme` | UIThemeSO | — | 主题配置 |
| `background` | Image | — | 背景 Image |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 标题栏拖拽 (UIPanelDragHandler) | 待做 | 代码 TODO |
| 右下角缩放 (UIPanelResizeHandler) | 待做 | 代码 TODO |
| 关闭按钮 + OnClose 事件 | 待做 | 代码 TODO |
