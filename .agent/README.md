# .agent — 项目文档目录

## 目录约定

| 目录 | 用途 | 更新时机 |
|------|------|---------|
| `design/` | 设计决策、玩家体验（WHY） | 设计变更、新系统定位 |
| `tech/modules/` | 技术实现、数据流（HOW） | 模块实现变更 |
| `tech/conventions/` | 命名规范、代码风格 | 约定变更 |
| `plans/` | 长期/短期开发计划 | 计划推进 |
| `sessions/` | 会话归档 `YYYY-MM-DD-主题.md` | 每次代码改动后 |
| `references/` | 截图、外部资料、资产清单 | 引入新资源 |
| `versions/` | 版号日志 | 提交时更新 |

## 目录树

```
.agent/
├── README.md
├── VERSION.md
├── design/
│   ├── game-overview.md
│   ├── audio-system.md
│   ├── death-mechanics.md
│   ├── injury-system.md
│   ├── input-bindings.md
│   ├── inventory-weight.md
│   ├── noise-system.md
│   ├── stats-inventory.md
│   ├── stats-system.md
│   └── ui-system.md
├── tech/
│   ├── architecture.md
│   ├── conventions/
│   │   ├── code-naming.md
│   │   └── data-assets.md
│   └── modules/
│       ├── gamecontext.md
│       ├── logging-system.md
│       ├── scene-loading.md
│       ├── service-architecture.md
│       ├── time-system.md
│       ├── ui-system.md
│       ├── camera-system.md
│       ├── event-dispatcher.md
│       ├── prototype-builder.md
│       ├── pathfinding.md
│       ├── animation/
│       │   ├── character-animation.md
│       │   └── headlook-design.md
│       ├── character/
│       │   ├── index.md
│       │   ├── scharacter-snapshot.md
│       │   ├── animation-architecture-plan.md
│       │   ├── animation-design.md
│       │   ├── component-inventory.md
│       │   ├── coverage-analysis.md
│       │   ├── current-callchain.md
│       │   ├── data-structures.md
│       │   ├── evaluation.md
│       │   ├── field-analysis.md
│       │   ├── locomotion-design.md
│       │   ├── module-analysis.md
│       │   ├── runtime-trace.md
│       │   ├── scene-setup.md
│       │   ├── stats-rule-system.md
│       │   └── target-callchain.md
│       └── input/
│           ├── input-manager.md
│           └── mouse-interaction.md
├── plans/
│   ├── long-term.md
│   └── short-term.md
├── references/
│   └── asset-license-tracker.md
├── sessions/
│   ├── 2025-04-29-locomotion-snapshot.md
│   ├── 2026-05-22-survival-system-现状回顾.md
│   ├── 2026-05-23-session-architecture.md
│   ├── 2026-05-25-data-flow-architecture.md
│   ├── 2026-05-25-top-down-camera.md
│   ├── 2026-05-26-mouse-input-pipeline.md
│   ├── 2026-05-27-prototype-builder-tool.md
│   └── 2026-05-27-infrastructure-cleanup.md
└── versions/
    └── v0.0.1.md
```
