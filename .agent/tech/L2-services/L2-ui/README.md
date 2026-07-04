# L2_UI · UI Service 模块

UI 系统全局模块，隶属 Services 层。提供全生命周期统一调度、Screen/Overlay 双模式面板管理、DOTween 驱动过渡动画、主题驱动的视觉参数管理。

## 层级定位

| 维度 | 说明 |
|------|------|
| Services 层 | 属于独立 Service 模块，不依赖其他 L2 模块 |
| 所属 L2 | GameState, EventDispatcher, Scene, Player 等 Service |
| 跨层调用 | 通过 EventDispatcher 订阅 GameState/Scene 事件；通过 Service 代理读取 PlayerService |

## 调用链

```
Game Input
  │
  ▼
GameStateService (SGameState)
  │
  ▼
UIService.HandleGameState()
  ├── MainMenu → ShowScreen(MainMenu)
  │                └── MainMenuScreen
  │                     ├── "新游戏" → RequestNewGame → SLoadSceneRequest → SceneService
  │                     └── "退出"   → RequestQuit    → Application.Quit()
  │
  ├── Paused  → ShowScreen(PauseMenu)
  │                └── PauseMenuScreen
  │                     ├── "继续"   → RequestResume → GameStateService.RequestState(Playing)
  │                     └── "返回主菜单" → RequestMainMenu → SUnloadSceneRequest → SceneService
  │
  └── Playing → HideScreen(current)
                 └── ShowOverlay(VitalsOverlay)  (非暂停恢复时跳过)
                      ├── VitalsOverlay (HP/Hunger/Thirst/Stamina ←── PlayerEntity.Query.Properties)
                      ├── AbilityBarOverlay (主动技能槽位 Q/E/R/F — UIIconSlot ×4)
                      ├── WeaponBarOverlay (武器槽位 — UIIconSlot ×2)
                      ├── DamageNumberOverlay (伤害飘字 — 订阅 HitEvent, 对象池, WorldToScreenPoint)
                      └── DebugOverlay (调试信息显示)

场景加载:
  SSceneLoadStart ──→ UIService ──→ LoadingOverlay alpha=1
  SSceneLoadComplete ──→ UIService ──→ LoadingOverlay alpha=0 → GameStateService

UI 组件主题驱动:
  UIButton / UILabel / UIPanel / UIStatBar
    └── Awake() → UIThemeSO.GetColorSet() / 字体/动画参数
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| UIService | EventDispatcherService | 订阅 SGameState / SSceneLoadStart / SSceneLoadComplete |
| UIService | GameStateService | 通过 RequestResume / RequestNewGame 发布状态 |
| UIService | SceneService | 发布 SLoadSceneRequest / SUnloadSceneRequest |
| UIService | EntityService | 通过 PlayerSpawnedEvent 获取玩家 Entity |
| UIScreen / UIOverlay | UIService | Service 统一创建和管理 |
| UIButton / UILabel / UIPanel | UIThemeSO | Awake 时从 Theme 读取颜色/字体配置 |
| UIStatBar | UIThemeSO | 读取颜色阈值和 StatBar 配置 |
| VitalsOverlay | UIService.PlayerEntity | 每帧拉取角色属性（通过 Entity.Query.Properties） |
| AbilityBarOverlay | UIService.PlayerEntity | 技能槽位显示和冷却刷新 |
| WeaponBarOverlay | UIService.PlayerEntity | 武器槽位显示 |
| DamageNumberOverlay | EventHub (HitEvent) | 订阅伤害事件，WorldToScreenPoint 坐标转换 |
| DamageNumberOverlay | DamageNumberWidget | 对象池管理，Play/Recycle |

## 设计决策

| 决策 | 原因 |
|------|------|
| Core.unity Canvas 常驻，Screen/Overlay 动态实例化 | 场景切换不销毁 UI 根节点 |
| Screen 互斥、Overlay 并存 | 全屏面板互斥（主菜单/暂停），HUD 叠加（血条/状态）可同时存在 |
| DOTween defaultTimeScaleIndependent = true | Time.timeScale=0 时 UI 动画不冻结 |
| UIThemeSO 统一管理视觉参数 | 主题切换只改一个 SO，无需改组件 |
| PlayEnterSequence/PlayExitSequence 返回 Sequence | UIService 可以用 OnComplete 链式控制销毁时机 |
| UIScreen 有 Pause/Resume | 暂停时禁用交互，防止后台误触 |
| LoadingCanvas (sortOrder=999) 独立 Canvas | 加载遮罩永远在最上层 |
| PlayEnterSequence 通过[SerializeField]暴露 CanvasGroup | 各 Screen/Overlay 可以在 Prefab 中独立配置 CanvasGroup |
| UI 组件标注 `[ExecuteAlways]` | 编辑器中实时预览主题效果 |
| 事件绑定在 OnDestroy 中退订 | 防止跨场景时残留回调导致 NullReference |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| Modal 弹窗系统（确认框/提示框） | 待做 | UIModalId 预留 | 旧 ui-system.md |
| UIPanel 拖拽支持 (DragHandler) | 待做 | — | 代码 TODO |
| UIPanel 缩放支持 (ResizeHandler) | 待做 | — | 代码 TODO |
| UIPanel 关闭按钮支持 | 待做 | — | 代码 TODO |
| StatusOverlay 状态效果显示 (Buff/Debuff) | 待做 | Stats 系统 | 代码 TODO |
| LoadingOverlay 进度条显示 | 待做 | SceneService | 代码 TODO |
| 设置面板 (SettingsScreen) | 待做 | — | 旧 ui-system.md |
| 存档/读档面板 | 待做 | SaveSystem | 旧 ui-system.md |
| 按键绑定 UI | 远期 | Input 系统 | 旧 input-manager.md |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [ui-service.md](ui-service.md) | UIService — Screen/Overlay 生命周期、场景过渡、导航 |
| [ui-communication-architecture.md](ui-communication-architecture.md) | **决策记录** — UI ↔ L3 通信架构（Query/Event/Input/Command 边界） |
| [L4-core/ui-screen.md](L4-core/ui-screen.md) | UIScreen — 全屏面板基类、Enter/Exit/Pause/Resume |
| [L4-core/ui-overlay.md](L4-core/ui-overlay.md) | UIOverlay — 叠加层基类、Enter/Exit |
| [L4-core/ui-screen-id.md](L4-core/ui-screen-id.md) | UIScreenId 枚举 |
| [L4-core/ui-overlay-id.md](L4-core/ui-overlay-id.md) | UIOverlayId 枚举 |
| [L4-core/ui-modal-id.md](L4-core/ui-modal-id.md) | UIModalId 枚举（预留） |
| [L4-core/ui-color-style.md](L4-core/ui-color-style.md) | UIColorSet struct + UIColorStyle 枚举 |
| [L4-components/ui-button.md](L4-components/ui-button.md) | UIButton — 悬停/按压动画、主题色、Interactable |
| [L4-components/ui-label.md](L4-components/ui-label.md) | UILabel — 字体/字号/颜色的主题驱动 |
| [L4-components/ui-panel.md](L4-components/ui-panel.md) | UIPanel — 面板背景容器 |
| [L4-components/ui-stat-bar.md](L4-components/ui-stat-bar.md) | UIStatBar — 填充条、三色阈值、DOTween 平滑 |
| [L4-components/ui-icon-slot.md](L4-components/ui-icon-slot.md) | UIIconSlot — 通用槽位（图标 + 冷却覆层 + 选中边框 + 快捷键标签） |
| [L4-config/ui-panel-config-so.md](L4-config/ui-panel-config-so.md) | UIPanelConfigSO — id→Prefab 映射配置 |
| [L4-config/ui-theme-so.md](L4-config/ui-theme-so.md) | UIThemeSO — 颜色/字体/间距/动画参数集中管理 |
| [L4-hud/vitals-overlay.md](L4-hud/vitals-overlay.md) | VitalsOverlay — HP/Hunger/Thirst/Stamina 实时显示 |
| [L4-hud/status-overlay.md](L4-hud/status-overlay.md) | StatusOverlay — 状态效果显示（骨架） |
| [L4-hud/loading-overlay.md](L4-hud/loading-overlay.md) | LoadingOverlay — 场景加载过渡遮罩 |
| [L4-hud/ability-bar-overlay.md](L4-hud/ability-bar-overlay.md) | AbilityBarOverlay — 主动技能槽位 Q/E/R/F（UIIconSlot ×4） |
| [L4-hud/weapon-bar-overlay.md](L4-hud/weapon-bar-overlay.md) | WeaponBarOverlay — 武器槽位（UIIconSlot ×2） |
| [L4-hud/debug-overlay.md](L4-hud/debug-overlay.md) | DebugOverlay — 调试信息显示 |
| [L4-hud/damage-number-overlay.md](L4-hud/damage-number-overlay.md) | DamageNumberOverlay — 伤害飘字管理，HitEvent 订阅 + 对象池 |
| [L4-hud/damage-number-widget.md](L4-hud/damage-number-widget.md) | DamageNumberWidget — 单个飘字动画（DOAnchorPosY + DOFade） |
| [L4-main-menu/main-menu-screen.md](L4-main-menu/main-menu-screen.md) | MainMenuScreen — 主菜单（新游戏/加载/设置/退出） |
| [L4-main-menu/pause-menu-screen.md](L4-main-menu/pause-menu-screen.md) | PauseMenuScreen — 暂停菜单（继续/设置/保存/返回） |
