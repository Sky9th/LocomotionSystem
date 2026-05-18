---
name: ui-conventions
description: RedDust UI 系统的开发约定，制作新面板/组件/Overlay 时必须遵循。
when_to_use: 编写 UI 代码、创建 Prefab、新增面板、修改主题时遵守
---

## 架构

```
UIManager (BaseService)
├── Screen 层    全屏互斥，fade Enter/Exit，同时只一个
├── Overlay 层   HUD 并存，fade Enter/Exit
└── Modal 层     弹窗栈（后续）
```

- 跨模块通信（Core→UI）走 EventDispatcher：UIManager 订阅 SGameState
- UI 内部通信走层级链：面板持有 UIManager 引用，直接调用方法，**不发全局事件**

## 组件不做 Layout 容器

不使用 Header/Content/Footer 插槽。每个 Screen/Overlay 在 Prefab 里直接用 RectTransform 锚点 + VerticalLayoutGroup 自由布局。Panel.prefab 只提供统一背景（Image + UIPanel），内容作为子节点放入。

## 标识符

面板 ID 用 `UIPanelId` 枚举，不用魔术字符串。代码 `ShowScreen(UIPanelId.MainMenu)` 编译期校验，SO 上 Inspector 下拉框选择。新增面板：加枚举值 → SO 注册 → 代码 handling。

## 颜色

全局走 UIThemeSO。组件只声明颜色角色（primary/surface/onPrimary），具体颜色由 `UIColorStyle` 切换。新增风格：加 `UIColorSet` 字段 + `UIColorStyle` 枚举值 + `GetColorSet` 分支。

Button 用 primary/primaryHover/primaryPressed/onPrimary。Panel 用 surface。Label 的 `UITextStyle.Button` 取 `GetColorSet(Normal).onPrimary`。

## 非空原则

组件上的必需引用（Theme、Background、Label Text 等）不设空值守卫。缺引用就崩——暴露配置错误。边界防护只用于外部数据（SCharacterSnapshot 不存在等）。

## Canvas

只有 UIManager 下一张 Canvas。Screen/Overlay/MainMenu prefab **不加自己的 Canvas**——嵌套 Canvas 导致 Z 序混乱。CanvasGroup 用于 fade 过渡（加在根节点）。

## Edit 模式预览

`[ExecuteAlways]` 标签使 Theme SO 在 Edit 模式下即时生效。所有 DOTween 调用加 `if (!Application.isPlaying) return` 守卫。UIStatBar.SetValue Edit 模式走直接赋值。

## Prefab

每个可复用组件建基础 Prefab（Button/Label/Panel/StatBar），所有组件引用在 Prefab 内预连线。**Prefab Variant 只用于结构差异**（如 Button_Icon 加图标子节点），Size/Interactable/Style 等属性在实例上直接改。

## 组件暴露方法

不向外暴露原生 Unity 组件（TMP_Text、Button）。通过 `SetText()` `SetInteractable()` `SetValue()` 等方法封装。

## 布局

外层容器定宽高 + anchor 定位。内层全 stretch + VerticalLayoutGroup 自适应。LayoutGroup 设 Control Child Size + Child Force Expand 两者都勾 Width 和 Height。

## 填充条

StatBar Fill Image：Sprite=WhiteSquare，Type=Filled，Method=Horizontal，Origin=Left，FillAmount=0。

## 字体

中文用 Noto Sans SC SDF（Unicode Range: 20-7E, 4E00-9FFF），存放在 Assets/Fonts/。

## 颜色角色速查

| 组件 | 颜色来源 |
|------|---------|
| UIButton 背景/悬浮/按下 | GetColorSet(style).primary / primaryHover / primaryPressed |
| UIButton 文字 | GetColorSet(style).onPrimary |
| UIPanel 背景 | GetColorSet(style).surface |
| UILabel | 直接读 theme.titleColor / bodyColor 等，或 GetColorSet(style).onPrimary（Button 场景）|
