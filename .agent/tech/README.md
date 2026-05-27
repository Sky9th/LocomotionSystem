# Tech 文档

> 按 L1→L5 架构层级组织。每层目录带 L 前缀。
> 数据流：L1→L2→L3→L4→L5，逐级传递返回，严禁跨级调用。
> shared/ 为全局 Helper，无层级归属。

```
tech/
├── README.md                           # 本文件
│
├── L1-core/                            # GameManager 根
│   ├── README.md
│   ├── game-context.md                 # GameContext — Service Registry + Snapshot Store
│   ├── game-service.md                 # GameService — Bootstrap 五步启动
│   ├── base-service.md                 # BaseService — 四阶段生命周期
│   └── structs.md                      # MetaStruct + Core Context Structs
│
├── L2-services/                        # Service + Module 层
│   ├── README.md
│   ├── L2-event-dispatcher/
│   │   └── event-dispatcher.md
│   ├── L2-scene-service/
│   │   └── scene-service.md
│   ├── L2-time-service/
│   │   └── time-service.md
│   ├── L2-game-state-service/
│   │   └── game-state-service.md
│   ├── L2-player-service/
│   │   └── player-service.md
│   ├── L2-camera-service/
│   │   └── camera-service.md
│   │
│   ├── L2-input/                       # 复合 Service
│   │   ├── README.md
│   │   ├── input-service.md
│   │   ├── L4-actions/
│   │   │   ├── input-action-handler.md
│   │   │   ├── L5-player/
│   │   │   │   ├── ia-player-move.md
│   │   │   │   ├── ia-player-look.md
│   │   │   │   └── L5-button/
│   │   │   │       ├── ia-player-crouch.md
│   │   │   │       ├── ia-player-jump.md
│   │   │   │       ├── ia-player-primary-interact.md
│   │   │   │       ├── ia-player-prone.md
│   │   │   │       ├── ia-player-run.md
│   │   │   │       ├── ia-player-secondary-interact.md
│   │   │   │       ├── ia-player-sprint.md
│   │   │   │       ├── ia-player-stand.md
│   │   │   │       └── ia-player-walk.md
│   │   │   ├── L5-system/
│   │   │   │   ├── ia-system-time-resume.md
│   │   │   │   └── ia-system-time-slow.md
│   │   │   └── L5-ui/
│   │   │       └── ia-ui-escape.md
│   │   └── L4-structs/
│   │       ├── s-action-ui-escape.md
│   │       └── L5-control/
│   │           ├── s-action-move.md
│   │           ├── s-action-look.md
│   │           └── L5-button/
│   │               ├── s-button-input-state.md
│   │               └── s-action-*.md (×9)
│   │
│   ├── L2-ui/                          # 复合 Service
│   │   ├── README.md
│   │   ├── ui-service.md
│   │   ├── L4-core/
│   │   │   ├── ui-screen.md
│   │   │   ├── ui-screen-id.md
│   │   │   ├── ui-overlay.md
│   │   │   ├── ui-overlay-id.md
│   │   │   ├── ui-modal-id.md
│   │   │   └── ui-color-style.md
│   │   ├── L4-components/
│   │   │   ├── ui-button.md
│   │   │   ├── ui-label.md
│   │   │   ├── ui-panel.md
│   │   │   └── ui-stat-bar.md
│   │   ├── L4-config/
│   │   │   ├── ui-panel-config-so.md
│   │   │   └── ui-theme-so.md
│   │   ├── L4-hud/
│   │   │   ├── vitals-overlay.md
│   │   │   ├── status-overlay.md
│   │   │   └── loading-overlay.md
│   │   └── L4-main-menu/
│   │       ├── main-menu-screen.md
│   │       └── pause-menu-screen.md
│   │
│   ├── L2-audio/                       # 复合 Service
│   │   ├── README.md
│   │   ├── audio-manager.md
│   │   ├── L4-data/
│   │   │   ├── audio-set-so.md
│   │   │   └── audio-channel.md
│   │   └── L4-structs/
│   │       ├── audio-request.md
│   │       └── audio-response.md
│   │
│   └── L2-modules/                     # 虚拟 L2 — 独立模块容器
│       ├── L3-character/               # 角色系统 ✅ 来源: tech-v2/L3-character
│       │   ├── README.md
│       │   ├── L4-actor/
│       │   │   ├── character-actor.md
│       │   │   ├── character-actor-debug.md
│       │   │   ├── character-rig.md
│       │   │   └── character-frame-context.md
│       │   ├── L4-config/
│       │   │   ├── character-profile.md
│       │   │   └── locomotion-enums.md
│       │   ├── L4-kinematic/
│       │   │   ├── character-kinematic.md
│       │   │   ├── character-ground-detection.md
│       │   │   ├── character-head-look.md
│       │   │   ├── character-obstacle-detection.md
│       │   │   └── L5-structs/
│       │   │       ├── s-character-kinematic.md
│       │   │       ├── s-forward-obstacle-detection.md
│       │   │       └── s-ground-contact.md
│       │   ├── L4-locomotion/
│       │   │   ├── i-locomotion-simulator.md
│       │   │   ├── ground-locomotion.md
│       │   │   ├── motor.md
│       │   │   ├── stance.md
│       │   │   ├── L5-config/
│       │   │   │   └── locomotion-profile.md
│       │   │   └── L5-structs/
│       │   │       ├── s-character-motor.md
│       │   │       └── s-character-discrete.md
│       │   ├── L4-animation/
│       │   │   ├── animation-brain.md
│       │   │   ├── driver-arbiter.md
│       │   │   ├── L5-config/
│       │   │   │   ├── animation-alias-profile.md
│       │   │   │   ├── locomotion-animation-profile.md
│       │   │   │   └── locomotion-mode-profile.md
│       │   │   ├── L5-drivers/
│       │   │   │   ├── i-character-animation-driver.md
│       │   │   │   ├── base-character-animation-driver.md
│       │   │   │   ├── L5-locomotion/
│       │   │   │   │   ├── locomotion-driver.md
│       │   │   │   │   ├── base-layer.md
│       │   │   │   │   ├── base-state-key.md
│       │   │   │   │   ├── locomotion-layer-fsm-state.md
│       │   │   │   │   └── L5-states/
│       │   │   │   │       ├── base-idle-state.md
│       │   │   │   │       ├── base-moving-state.md
│       │   │   │   │       ├── base-idle-to-moving-state.md
│       │   │   │   │       ├── base-turn-in-place-state.md
│       │   │   │   │       ├── base-turn-in-moving-state.md
│       │   │   │   │       ├── base-air-loop-state.md
│       │   │   │   │       └── base-air-land-state.md
│       │   │   │   └── L5-traversal/
│       │   │   │       └── traversal-driver.md
│       │   │   └── L5-requests/
│       │   │       ├── animation-request.md
│       │   │       ├── on-complete-behavior.md
│       │   │       └── on-interrupted-behavior.md
│       │   ├── L4-stats/
│       │   │   ├── character-stats.md
│       │   │   └── L5-rules/
│       │   │       ├── character-stat-rule.md
│       │   │       ├── damage-rule.md
│       │   │       ├── batch-damage-rule.md
│       │   │       ├── deplete-chain-rule.md
│       │   │       ├── hunger-deplete-rule.md
│       │   │       ├── passive-gain-rule.md
│       │   │       ├── sprint-stamina-rule.md
│       │   │       └── toggle-modifier-rule.md
│       │   ├── L4-audio/
│       │   │   ├── character-audio.md
│       │   │   └── L5-config/
│       │   │       ├── character-audio-config-so.md
│       │   │       └── footstep-set-so.md
│       │   └── L4-input/
│       │       ├── character-event-receiver.md
│       │       └── s-character-input-actions.md
│       │
│       ├── L3-stats/                   # Stat 数值框架
│       │   ├── README.md
│       │   ├── L4-definition/ ─ stat-def-so.md
│       │   ├── L4-tree/ ── stats-node-so.md, stats-tree-so.md
│       │   ├── L4-instance/ ── stat-instance.md
│       │   ├── L4-modifier/ ── stat-modifier.md, modifier-context.md
│       │   ├── L4-interfaces/ ── i-stat-consumable.md, i-stat-cumulative.md, i-stat-derived.md, i-stat-restorable.md
│       │   └── L4-editor/ ── stats-tree-window.md
│       │
│       └── L3-pathfinding/             # 寻路系统
│           └── README.md
│
└── shared/                              # 全局 Helper — 不限层级
    ├── README.md
    ├── data-assets.md
    ├── logging/
    │   ├── README.md
    │   ├── log-manager.md
    │   ├── log-channel.md
    │   ├── log-level.md
    │   ├── L4-appender/
    │   │   ├── i-log-appender.md
    │   │   └── console-appender.md
    │   └── L4-compat/
    │       └── logger.md
    ├── editor/
    │   ├── README.md
    │   ├── editor-core-loader.md
    │   ├── game-context-editor.md
    │   └── L4-prototype/
    │       ├── synty-prototype-browser.md
    │       └── synty-prototype-menu.md
    └── utility/
        ├── README.md
        └── gizmo-debug-utility.md
```

## 迁移来源

> **v1 和 v2 已归档至 `.agent/archive/tech-v1/` 和 `.agent/archive/tech-v2/`，不纳入日常查询。**

| v3 位置 | v2 来源 | 状态 |
|---------|---------|------|
| L1-core/ | L1-core/ | 直接迁移 |
| L2-services/L2-*/ (独立 Service) | L2-services/ | 重组 + L 前缀目录 |
| L2-services/L2-input/ | L3-input/ | 重组 + L 前缀 |
| L2-services/L2-ui/ | L3-ui/ | 重组 + L 前缀 |
| L2-services/L2-audio/ | L3-audio/ | 重组 + L 前缀 |
| L2-services/L2-modules/L3-character/ | L3-character/ | 直接迁移 |
| L2-services/L2-modules/L3-stats/ | L3-stats/ | 重组 + L 前缀 |
| L2-services/L2-modules/L3-pathfinding/ | L3-pathfinding/ | 直接迁移 |
| shared/logging/ | L3-logging/ | 移至 shared |
| shared/editor/ | L3-editor/ | 移至 shared |
| shared/utility/ | L3-utility/ | 移至 shared |

## 层级规则

| 规则 | 说明 |
|------|------|
| L1 只有一个入口 | GameService 是唯一根 |
| L2 Service 不直接互相引用 | 通过 GameContext 或 EventDispatcher |
| L3 不依赖特定 L2 | Character 不 import PlayerService |
| L3 可被多个 L2 共用 | Character ← PlayerService + AIService |
| L4 只被同模块调用 | L4-actor 只被 character 内部调用 |
| L5 纯分组 | 不新增层级语义 |
| shared 不限层级 | 任何层可调用 |
