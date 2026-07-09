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
├── design/                              ← 策划文档 (WHY)
│   ├── README.md                       ← 策划文档索引
│   ├── game-overview.md                ← GDD 总览 — 定位、世界观摘要、系统全景、核心循环
│   ├── systems/
│   │   ├── combat/                     ← 战斗系统
│   │   │   ├── README.md               ← 总览：操作 + 6 武器类型
│   │   │   ├── skills.md               ← 技能：Ability 动作模式、武器-技能绑定
│   │   │   ├── proficiency.md          ← 熟练度：成长、命中/暴击、近战vs枪械
│   │   │   └── damage-model.md         ← 伤害模型：装备地基 + 管道修正
│   │   ├── base/                       ← 据点建设
│   │   │   ├── README.md               ← 总览：4 子系统协作
│   │   │   ├── building.md             ← 建造：网格化/分类/耐久/拆除
│   │   │   ├── tech-tree.md            ← 科技树：图纸/前置/解锁
│   │   │   ├── resources-tools.md      ← 资源工具：六大类/耐久维修/存储
│   │   │   └── farming-cooking.md      ← 农业烹饪：种植→收获→食谱→士气
│   │   ├── npc.md                      ← NPC：指挥/工作/成长/招募 + 士气
│   │   ├── horde.md                    ← 尸潮：触发/规模/构成/行为/后效
│   │   ├── injury.md                   ← 伤病：部位/5伤害类型/治疗/丧尸化
│   │   ├── death-save.md               ← 死亡与存档
│   │   ├── noise.md                    ← 噪音：6等级/传播/丧尸反应
│   │   └── inventory-weight.md         ← 负重与背包
│   ├── world/
│   │   ├── setting.md                  ← 世界观：八重岛/母体雾区/封锁/结局
│   │   └── spore-erosion-rules.md      ← 孢子侵蚀抗性梯度
│   └── data/
│       └── stats-inventory.md          ← 全量 Stats 属性树
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
│   │   │   ├── L3-equipment/
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
│   ├── art-assets/                    ← PolygonApocalypse Prefab 清单 (1820 个)
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
