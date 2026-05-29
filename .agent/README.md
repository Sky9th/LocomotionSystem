# .agent — 项目文档目录

## 目录约定

| 目录 | 用途 | 更新时机 |
|------|------|---------|
| `design/` | 设计决策、玩家体验（WHY） | 设计变更、新系统定位 |
| `tech/` | 技术实现，按 L1→L5 层级 + Shared 组织 | 模块实现变更 |
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
│   ├── l1-l5-layering.md
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
│   ├── README.md                       ← 技术文档索引
│   ├── conventions/
│   │   └── namespace-rules.md          ← namespace 映射表 + 豁免规则
│   ├── shared/
│   │   └── data-assets.md
│   ├── L1-core/
│   ├── L2-services/
│   │   ├── README.md
│   │   ├── L2-audio/
│   │   ├── L2-camera-service/
│   │   ├── L2-event-dispatcher/
│   │   ├── L2-game-state-service/
│   │   ├── L2-input/
│   │   ├── L2-modules/
│   │   │   ├── L3-character/
│   │   │   ├── L3-pathfinding/
│   │   │   └── L3-stats/
│   │   ├── L2-player-service/
│   │   ├── L2-scene-service/
│   │   ├── L2-time-service/
│   │   └── L2-ui/
│   └── archive/
│       ├── tech-v1/
│       └── tech-v2/
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
│   ├── 2026-05-27-infrastructure-cleanup.md
│   └── 2026-05-29-namespace-migration.md
└── versions/
    ├── v0.0.1.md
    ├── v0.1.0.md
    └── v0.2.0.md
```
