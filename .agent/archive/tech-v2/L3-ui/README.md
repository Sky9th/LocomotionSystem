# 04-ui · UI 系统

> 三层架构：Screen(全屏面板，互斥) / Overlay(HUD 叠加层，可并存) / Modal(弹窗，预留)。UIService 统一调度生命周期，DOTween 驱动动画，时间线独立于 Gameplay。

## 调用链

```
SGameState 变化 (EventDispatcher)
  └── UIService.HandleGameState()
      ├── EGameState.MainMenu → ShowScreen(MainMenu)
      ├── EGameState.Paused   → ShowScreen(PauseMenu)
      └── EGameState.Playing  → HideScreen(current) + ShowOverlay(Vitals)

场景加载流程
  ├── UIService.RequestNewGame()
  │   ├── 淡出当前 Screen → Destroy
  │   └── Dispatcher.Publish(SLoadSceneRequest)
  │       └── SceneService 加载 → SSceneLoadComplete
  │           └── UIService.HandleSceneLoadComplete → loading 隐藏 → GameStateService

返回主菜单
  ├── UIService.RequestMainMenu()
  │   ├── 淡出当前 Screen → Destroy
  │   └── Dispatcher.Publish(SUnloadSceneRequest)

暂停
  ├── UIService.RequestResume()
  │   └── GameStateService.RequestState(Playing)

UI 组件交互
  ├── MainMenuScreen
  │   ├── "新游戏" → uiService.RequestNewGame()
  │   └── "退出"   → uiService.RequestQuit()
  ├── PauseMenuScreen
  │   ├── "继续"   → uiService.RequestResume()
  │   └── "返回"   → uiService.RequestMainMenu()
  └── VitalsOverlay
      └── Update 循环 → TryGetPlayerStats → UIStatBar.SetValue()
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| UIService | 01-core (EventDispatcherService) | 订阅 SGameState / SSceneLoadStart / SSceneLoadComplete |
| UIService | 01-core (GameStateService) | 通过 RequestResume / RequestNewGame 发布状态 |
| UIService | 01-core (SceneService) | 发布 SLoadSceneRequest / SUnloadSceneRequest |
| UIService | 01-core (PlayerService) | TryGetPlayerStats 查询游戏内数值 |
| UIScreen / UIOverlay | UIService | Service 统一创建和管理 |
| UIButton / UILabel / UIPanel | UIThemeSO | Awake 时从 Theme 读取颜色/字体配置 |
| UIStatBar | UIThemeSO | 读取颜色阈值和 StatBar 配置 |
| VitalsOverlay | PlayerService | 每帧拉取角色数值 |

## 设计决策

| 决策 | 原因 |
|------|------|
| Core.unity Canvas 常驻，Screen/Overlay 动态实例化 | 场景切换不销毁 UI 根节点 |
| Screen 互斥、Overlay 并存 | 全屏面板互斥 (主菜单/暂停)，HUD 叠加 (血条/状态) 可同时存在 |
| DOTween defaultTimeScaleIndependent = true | Time.timeScale=0 时 UI 动画不冻结 |
| UIThemeSO 统一管理视觉参数 | 主题切换只改一个 SO，无需改组件 |
| PlayEnterSequence/PlayExitSequence 返回 Sequence | UIService 可以用 OnComplete 链式控制销毁时机 |
| UIScreen 有 Pause/Resume | 暂停时禁用交互，防止后台误触 |
| LoadingCanvas (sortOrder=999) 独立 Canvas | 加载遮罩永远在最上层 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| Modal 弹窗系统 (确认框/提示框) | 待做 | UIModalId 预留 | 旧 ui-system.md |
| UIPanel 拖拽支持 (DragHandler) | 待做 | — | 代码 TODO (UIPanel.cs) |
| UIPanel 缩放支持 (ResizeHandler) | 待做 | — | 代码 TODO (UIPanel.cs) |
| UIPanel 关闭按钮支持 | 待做 | — | 代码 TODO (UIPanel.cs) |
| StatusOverlay 状态效果显示 (Buff/Debuff) | 待做 | 02-character Stats | 代码 TODO (StatusOverlay.cs) |
| LoadingOverlay 进度条显示 | 待做 | SceneService | 代码 TODO (LoadingOverlay.cs) |
| 设置面板 (SettingsScreen) | 待做 | — | 旧 ui-system.md |
| 存档/读档面板 | 待做 | SaveSystem | 旧 ui-system.md |
| 按键绑定 UI | 远期 | 03-input | 旧 input-manager.md |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [ui-service.md](ui-service.md) | UIService — Screen/Overlay 生命周期、场景过渡、导航 |
| [ui-screen.md](ui-screen.md) | UIScreen — 全屏面板基类、Enter/Exit/Pause/Resume |
| [ui-overlay.md](ui-overlay.md) | UIOverlay — 叠加层基类、Enter/Exit |
| [ui-screen-id.md](ui-screen-id.md) | UIScreenId 枚举 |
| [ui-overlay-id.md](ui-overlay-id.md) | UIOverlayId 枚举 |
| [ui-modal-id.md](ui-modal-id.md) | UIModalId 枚举 (预留) |
| [ui-color-style.md](ui-color-style.md) | UIColorSet struct + UIColorStyle 枚举 |
| [ui-button.md](ui-button.md) | UIButton — 悬停/按压动画、主题色、Interactable |
| [ui-label.md](ui-label.md) | UILabel — 字体/字号/颜色的主题驱动 |
| [ui-panel.md](ui-panel.md) | UIPanel — 面板背景容器 |
| [ui-stat-bar.md](ui-stat-bar.md) | UIStatBar — 填充条、三色阈值、DOTween 平滑 |
| [ui-panel-config-so.md](ui-panel-config-so.md) | UIPanelConfigSO — id→Prefab 映射配置 |
| [ui-theme-so.md](ui-theme-so.md) | UIThemeSO — 颜色/字体/间距/动画参数集中管理 |
| [vitals-overlay.md](vitals-overlay.md) | VitalsOverlay — HP/Hunger/Thirst/Stamina 实时显示 |
| [status-overlay.md](status-overlay.md) | StatusOverlay — 状态效果显示 (骨架) |
| [loading-overlay.md](loading-overlay.md) | LoadingOverlay — 场景加载过渡遮罩 |
| [main-menu-screen.md](main-menu-screen.md) | MainMenuScreen — 主菜单 (新游戏/加载/设置/退出) |
| [pause-menu-screen.md](pause-menu-screen.md) | PauseMenuScreen — 暂停菜单 (继续/设置/保存/返回) |
