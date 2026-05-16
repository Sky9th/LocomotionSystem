# UI 系统设计

> 日期: 2026-05-16
> 状态: 代码完成，待 Unity Editor 搭建 Prefab
> 定位: 俯视角末世生存游戏的 UI 系统，主菜单参考 Project Zomboid 风格

## 全貌

类似 Project Zomboid——面板是核心玩法。角色面板、物品、制造、建造都通过 UI 完成。

### 总是可见

| 元素 | 位置 | 内容 |
|------|------|------|
| 生命体征面板 | 左上 | HP / Hunger / Thirst / Stamina 数值条 |
| 状态图标 | 右上 | Buff/Debuff 图标（后续）|
| 快捷栏 | 底部居中 | 1-5 装备/消耗品槽位（后续）|
| 时钟 | 顶部居中 | 游戏内时间/日期（后续）|

### 切换窗口（后续）

| 窗口 | 键 | 内容 |
|------|-----|------|
| 角色面板 | C | 属性值 + 熟练度 + Stats |
| 物品栏 | I | 网格背包 + 装备槽 + 地面容器 |
| 制造 | B | 配方列表 + 材料需求 |
| 地图 | M | 已探索区域 |

### 特殊画面

| 画面 | 内容 |
|------|------|
| 主菜单 | 新游戏 / 加载存档 / 设置 / 退出 |
| 暂停菜单 | 继续 / 设置 / 保存 / 回主菜单 |
| 死亡画面 | 存活天数 + 返回主菜单 |

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

## 主题驱动

所有颜色/字体/字号/间距/动画参数集中在 `UIThemeSO` 一份 SO 中。组件在 Awake 时读取应用。策划在 Inspector 中可直接调色——不需要改代码或重新运行场景。

## 组件库

| 组件 | 作用 |
|------|------|
| UIButton | 主题按钮，DOTween hover/press 动画，OnClicked event |
| UILabel | 主题文本，枚举驱动（Title/Subtitle/Body/Button/Small）|
| UIStatBar | 水平填充条，颜色阈值自动变色，max=0 时显示 "--" |
