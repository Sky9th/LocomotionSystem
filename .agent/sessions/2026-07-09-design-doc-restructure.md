# 2026-07-09 — 策划文档体系重整 + Mod 社区 + 多语言方案

## Background

`.agent/design/` 此前是 9 个平铺文件，`game-overview.md` 膨胀到 3,256 行——9 个 GDD 版本拼接、7 个子系统文档嵌入、3 个开发规划文档混在一起。策划找不到具体系统的详细设计，开发者分不清哪些是设计意图哪些是规划笔记。

本次会话以 `game-overview.md` 瘦身为起点，逐步演变为完整的 `design/` 目录重构——从平铺到层次化、从混合到分离、从代码泄露到纯策划文档。过程中额外新增了 Mod 社区设计、多语言技术方案、Agent Dispatch Rules 三项工程约定。

## Changes

### design/ 目录重构
- **game-overview.md**: 3,256 行 → 106 行，删除 8 个旧版 GDD（v0.1–v0.8）、3 个开发规划文档、所有 A测范围/暂缓内容/时间线/可用资源限制
- **新建目录结构**: `systems/combat/`（4 文档）、`systems/base/`（5 文档）、`world/`、`data/`
- **子系统文档提取**: 从 game-overview.md 中拆出 NPC、尸潮、建造、科技树、资源工具、农业烹饪 6 个独立子系统文档
- **已有文件搬家**: damage-source-model → systems/combat/damage-model、death-mechanics → systems/death-save、injury-system → systems/injury、inventory-weight → systems/inventory-weight、noise-system → systems/noise、spore-erosion-rules → world/spore-erosion-rules、stats-inventory → data/stats-inventory
- **新建 design/README.md**: 策划文档索引 + 快速导航表

### 非策划内容迁移
- `effect-inventory.md` → `tech/L3-ability/`（EffectSO 全量资产目录）
- `input-bindings.md` → `tech/L2-input/`（键鼠按键绑定表）
- `art-assets/`（11 文件）→ `references/art-assets/`（PolygonApocalypse Prefab 清单）

### 代码泄露清理
- `damage-source-model.md`: 删除 `AbilityExecutor.TryActivate()` C# 伪代码，替换为文字描述 + tech 引用
- `noise-system.md`: 删除 `SNoiseEvent` struct 定义，替换为字段描述 + tech 引用

### 新增设计文档
- **Mod 系统** (`systems/mod.md`): 纯数据驱动、JSON 格式、零代码门槛、Steam Workshop 分发
- **世界观设定** (`world/setting.md`): 八重岛/母体雾区/封锁/幸存者/结局/设定逻辑自检

### 新增技术文档
- **多语言方案** (`tech/shared/localization.md`): 中英官方 + 社区翻译 JSON 外部化、Fallback 链、字体方案

### 工程约定
- **CLAUDE.md**: 新增 Agent Dispatch Rules（推理用 opus，收集用 haiku，禁止 general-purpose 一把梭）
- **.agent/README.md**: 目录树更新，反映新结构

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| design/ 分 4 个子目录（systems/ world/ data/）而非更细颗粒 | A: 每个子系统独立目录 → 15+ 目录难导航。B: 全部平铺 → 回到老问题 | 4 个分类够用但不冗余，combat/ 和 base/ 只在内容量真正需要时拆子目录 |
| Mod 设计为策划文档而非技术文档 | A: 只写 tech 方案 → 策划看不到 Mod 对游戏生态的价值 | 从玩家/创作者视角写，强调零门槛、社区驱动、对游戏寿命的影响 |
| 士气系统合并进 NPC 文档 | A: 独立 morale.md → 士气只影响 NPC，独立文件内容不足 50 行 | 士气就是 NPC 的属性，分离是过度设计 |
| game-overview 保留子系统概要（表格式一句定位）而非纯目录链接 | A: 只留链接 → 读者不能一眼扫完所有系统 | 概要表让读者 30 秒理解游戏全貌，链接供深入 |
| 文档改名统一去后缀（injury-system → injury, death-mechanics → death-save） | A: 保留原名 → 名字暗示"系统"但与文件名重复 | 简洁命名 + 目录路径已提供上下文 |
| Agent 模型选择规则写入 CLAUDE.md 而非仅靠 memory | A: 只靠 memory → 上次会话就没检查 memory | CLAUDE.md 每次任务必读，不会漏 |

## Known Issues

- [ ] 4 个文档超出 200 行目标（npc 387 / horde 362 / setting 311 / injury 272）— 内容密度高非水分，后续可精炼 (P2)
- [ ] 搬家文件增强 Agent 部分完成——部分文件修改时间戳仍为旧日期，但链接已手动修复 (P1)
- [ ] `tech/shared/localization.md` 字体方案引用思源黑体，需确认 Unity 中 SDF 字体实际可用性
- [ ] Mod 方案的技术落地（GameRegistry 动态注入、ModService）留待 `.agent/plans/mod-architecture.md`，尚未排入开发计划

## Cross-References

### Related Sessions
- [2026-07-08-asset-catalog-gen.md](2026-07-08-asset-catalog-gen.md) — 生成了 art-assets/，本次将其从 design/ 搬到 references/
- [2026-06-08-effect-editor.md](2026-06-08-effect-editor.md) — 创建 effect-inventory.md，本次将其从 design/ 搬到 tech/

### Related Plans
- [../plans/mod-architecture.md](../plans/mod-architecture.md) — Mod 系统技术落地计划（暂存，未排期）

### Related Tech Docs
- [../tech/shared/localization.md](../tech/shared/localization.md) — 多语言架构（新建）
- [../tech/L2-services/L2-modules/L3-ability/effect-inventory.md](../tech/L2-services/L2-modules/L3-ability/effect-inventory.md) — EffectSO 资产目录（从 design/ 迁入）
- [../tech/L2-services/L2-input/input-bindings.md](../tech/L2-services/L2-input/input-bindings.md) — 按键绑定表（从 design/ 迁入）
- [../tech/README.md](../tech/README.md) — 新增 3 个条目

### Related Design Docs
- [../design/README.md](../design/README.md) — 策划文档索引（新建）
- [../design/systems/mod.md](../design/systems/mod.md) — Mod 系统设计（新建）
- [../design/world/setting.md](../design/world/setting.md) — 世界观设定（新建）
- [../design/systems/npc.md](../design/systems/npc.md) — NPC + 士气（新建）
- [../design/systems/horde.md](../design/systems/horde.md) — 尸潮系统（新建）
- [../design/systems/combat/](../design/systems/combat/) — 战斗系统（4 文档，新建 3 + 搬家 1）
- [../design/systems/base/](../design/systems/base/) — 据点建设（5 文档，全部新建）

### Flag for Design Doc Creation
- [x] No new design doc needed — all design content created in this session is above.
