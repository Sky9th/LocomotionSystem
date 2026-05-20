# UI 系统设计

> 更新: 2026-05-20
> 状态: Phase 3.5 完成，完整游戏循环闭环

## 全貌

类似 Project Zomboid——面板是核心玩法。角色面板、物品、制造、建造都通过 UI 完成。

### 总是可见（6 个独立 Overlay，Playing 状态激活）

```
┌──────────────────────────────────────────────────┐
│ Vitals            Clock       StatusIcons         │
│ HP ████████      14:30 D3    [血][毒][饱]         │
│ Hunger ████                                       │
│ Thirst ██████                                     │
│ Stamina ███                                       │
│                                                    │
│                   Notifications                    │
│                   "背包已满"                        │
│                                                    │
│               InteractionPrompt                    │
│               "[E] 打开箱子"                         │
│                                                    │
│         ┌───┬───┬───┬───┬───┐                      │
│         │ 1 │ 2 │ 3 │ 4 │ 5 │   Hotbar             │
│         └───┴───┴───┴───┴───┘                      │
└──────────────────────────────────────────────────┘
```

| Overlay | 位置 | 内容 | 状态 |
|---------|------|------|------|
| VitalsOverlay | 左上 | HP / Hunger / Thirst / Stamina | ✅ |
| ClockOverlay | 顶部居中 | 第 N 天 HH:MM | 待时间系统 |
| StatusIcons | 右上 | Buff/Debuff 图标 + 计时环 | 待 Buff 系统 |
| HotbarOverlay | 底部居中 | 1-5 装备/消耗品槽 | 待物品系统 |
| InteractionPrompt | 屏幕中央 | "[E] 打开" 动态提示 | 待交互系统 |
| NotificationOverlay | 上方中央 | 浮现→停留→淡出通知 | 后续 |
| LoadingOverlay | 全屏居中 | "Loading..." + 进度条（后续）| 🔧 计划完成 |
| PauseMenu | 全屏 | 遮罩 + 继续/设置/保存/主菜单 | 🔧 计划完成 |

每个 Overlay 独立注册、独立生命周期，UIManager 在 Playing 状态下统一激活。

### 切换窗口（后续）

| 窗口 | 键 | 内容 |
|------|-----|------|
| 角色面板 | C | 属性值 + 熟练度 + Stats |
| 物品栏 | I | 网格背包 + 装备槽 + 地面容器 |
| 制造 | B | 配方列表 + 材料需求 |
| 地图 | M | 已探索区域 |

### 特殊画面

| 画面 | 内容 | 状态 |
|------|------|------|
| 主菜单 | 新游戏 / 加载存档 / 设置 / 退出 | ✅ |
| 暂停菜单 | 继续 / 设置 / 保存 / 回主菜单 | 🔧 计划完成，待落地 |
| 加载画面 | "Loading..." + 进度条（后续）| 🔧 计划完成，待落地 |
| 死亡画面 | 存活天数 + 返回主菜单 | 后续 |

### 游戏闭环

```
MainMenu ──→ Loading ──→ Playing ──[ESC]──→ PauseMenu ──→ MainMenu
```

场景切换统一走 `TransitionWithLoading(sceneName, targetState)`，自动显示/隐藏 LoadingOverlay。

## 三层架构

```
UIManager (BaseService)
├── Screen 层    全屏互斥，Enter/Exit，默认 fade 过渡
├── Overlay 层   HUD 并存，Enter/Exit
└── Modal 层     弹窗栈，Enter/Exit/Pause/Resume（后续）
```

- Screen：同时只有一个活跃。切换时旧面板 fade out → 销毁 → 新面板 fade in
- Overlay：多个共存，不互斥。Show 时直接激活叠加
- Modal：压栈管理，下层暂停交互

## 通信原则

- **跨模块通信（Core→UI）走 EventDispatcher**：UIManager 订阅 `SGameState` 事件，根据游戏状态切换画面
- **UI 模块内部通信走层级链**：面板持有 UIManager 引用，导航操作（新游戏、退出）直接调用 UIManager 方法，不发全局事件

## 主菜单（PZ 风格）

### 视觉布局

- 全屏暗色背景
- 顶部居中：游戏标题 "RED DUST"，白色大字
- 居中：竖排按钮（280×50px，间距 12px）
- 右下角：版本文本，小字
- Hover 时按钮缩放 1.05 + 亮色过渡

### 按钮

| 按钮 | MVP 状态 | 行为 |
|------|---------|------|
| 新游戏 | 可用 | 淡出 → 加载 SampleScene → 显示 VitalsOverlay |
| 加载存档 | 灰色不可点 | 预留给存档系统 |
| 设置 | 灰色不可点 | 预留给设置面板 |
| 退出游戏 | 可用 | Application.Quit() |

### 子面板（后续）

点击设置/加载存档时，右侧滑入 300px 子面板 + 返回按钮。内部由 MainMenuScreen 自己管理，不走 UIManager。

## 生命体征面板

左上角固定，深色半透明底，四条上下排列：

```
HP       绿色条 + 95/100
Hunger   绿色条 + 60/100     >66% 绿, 33-66% 黄, <33% 红
Thirst   绿色条 + 80/100
Stamina  绿色条 + 40/100
```

每 0.1s 从 `SCharacterSnapshot.Stats` 字典读取，DOTween 驱动 fillAmount 平滑过渡。角色未生成时（Stats == null）条保持初始空值，显示 "--"。

## 主题驱动与所见即所得

所有颜色/字体/字号/间距/动画参数集中在 `UIThemeSO`。组件加 `[ExecuteAlways]` 标签，Awake 在 Edit 模式也执行——拖入 Prefab 即显示主题色，改 SO 值即时刷新。

DOTween 调用（Pointer 回调、UIStatBar.Update）加 `if (!Application.isPlaying) return` 守卫，Edit 模式跳过补间。

### 颜色风格系统

每种风格（Normal / Primary / Danger / Warning / Success）定义一套完整色板 `UIColorSet`，包含 9 个颜色角色：

| 角色 | 用途 |
|------|------|
| primary / primaryHover / primaryPressed | 按钮背景三态 |
| onPrimary | 按钮文字色 |
| surface / surfaceAlt | 面板/卡片背景 |
| onSurface / onSurfaceMuted | 面板内文字 |
| border | 描边 |

组件只声明"我的颜色角色是什么"，具体颜色由当前 `UIColorStyle` 决定。切 Normal → Danger 改 Inspector 下拉框，按钮/面板/文字全系自动换色。所有组件共享同一套风格枚举和同一份 ThemeSO 色板定义，一处改色全局生效。

## 组件 Prefab 库

不设 Slot 抽象，Prefab 自带完整子级结构。基础 Prefab 内置所有组件并预连线引用，拖入场景只需改 Label 文字。

通过 Prefab Variant 派生变体——尺寸/状态变化从基础 Prefab 继承，改基础则全局生效。

### MainMenu 所需（立即制作）

| Prefab | 内容 | 用途 |
|--------|------|------|
| `Button.prefab` | UIButton + Button + Image + 子 TMP_Text，全预连线 | 主菜单全部 4 个按钮 |
| `Label.prefab` | UILabel + TMP_Text | 标题 + 版本号 |

### Variant 准则

**不使用 Variant 做属性差异。** Size、Interactable、Label 文字是实例属性，在实例上直接改。Variant 用在**结构变化**时——例如 `Button_Icon.prefab` 在按钮前加 Icon 子节点。

所以 MainMenu 没有 Variant。4 个按钮全是 `Button.prefab` 实例，各自改 Label 和 Interactable；2 个文本全是 `Label.prefab` 实例，各自改 textStyle。

### TODO

| Prefab | 内容 | 用途 |
|--------|------|------|
| `StatBar.prefab` | UIStatBar + 背景 + 填充 + Name + Value 全预连线 | VitalsOverlay |
| `Button_Icon.prefab` | Button Variant + Icon 子节点（结构变化） | 带图标的按钮 |
| `Panel.prefab` | 圆角背景 Image，可拖拽/缩放标记 | 通用面板容器 |
| `Loading.prefab` | 转圈动画 + 提示文字 | 场景加载过渡 |
| `Tab.prefab` | 标签按钮组 | 多页签面板 |
| `Tree.prefab` | 可折叠节点 | 存档列表、设置目录树 |

### 全局改样式

- 改颜色/字体 → 改 UIThemeSO 一处，所有 `[ExecuteAlways]` 组件即时刷新
- 改 Button 结构（如加 Icon 子节点）→ 改 `Button.prefab`，所有变体自动继承
