# 2026-07-09 — Mod 社区化全流程：决策 → 框架 → 计划重整

## Background

项目原有的 [mod-architecture.md](../plans/mod-architecture.md) 在 10 项战略决策缺失下直接跳到了代码结构。用户要求自审，发现该计划停留在"列出组件名称就叫方案"的粒度。

本日完成了 Mod 社区化从战略到落地的完整链路：**决策制定 → 架构框架 → 开发计划重整 → 双 Agent 审核修正**。

### 上半场：战略决策 + HybridCLR 选型

四轮辩论 + 6 角度评议团审查 → 10 项战略决策 + HybridCLR 技术分析。

### 下半场：从"三份技术文档"到"一套架构框架"

原计划产出三份独立技术文档（ID 审计、JSON Schema、存档兼容）。用户纠正——Mod 不是"一个功能"，是贯穿所有开发阶段的**架构约束**，和 L1→L5 层级规则同级。

关键转折：用户指出"Mod 支持要从需求出发，结合当前开发进度"→ JSON Schema 和存档兼容对于当前阶段（没有物品、没有存档系统）是过早设计 → S0 真正该做的只有 contentId 字段规范化 + 程序集边界定义。

最终：停止三份独立文档计划，重新定义 Mod 支持为框架性架构约束 → 产出 mod-architecture-framework.md → 整合进长期/短期计划 → 双 Agent 交叉审核。

## Changes

### 策划文档（`.agent/design/`）
- `systems/mod.md` — 修正 5 处与决策不一致的内容
- `systems/mod-json-reference.md` — **新建**。面向 Mod 作者的 JSON 格式手册
- `README.md` — 新增 mod-json-reference.md 索引条目

### 技术文档（`.agent/tech/`）
- `mod-architecture-framework.md` — **新建**（tech/ 根）。跨层级 Mod 架构框架——程序集边界、API 设计约束、contentId 规范、覆写语义、扩展点模式、模块落地检查清单、分阶段路线图
- `reference/hybridclr-integration.md` — **新建**。HybridCLR 技术选型分析（后移至 reference/）
- `README.md` — 新增 mod-architecture-framework.md + hybridclr-integration.md 索引条目

### 计划（`.agent/plans/`）
- `mod-community-decision-record.md` — **新建**。10 项战略决策 + 完整四轮论证链
- `mod-architecture.md` — **废弃**，顶部加 deprecated 标记
- `long-term.md` — **重整**。整合 Mod 架构约束到方法论 + Phase 1-4 回顾表 + 每 Phase Mod 义务
- `short-term.md` — **重整**。新增"前置准备"（P0-P3 Mod 补课）+ P5.x 每节 Mod 义务行
- 版本升级：0.43.0 → 0.44.0（文档体系重整，b bump）

## Decisions

### 上半场：10 项战略决策

| # | 决策 | 结论 | 论证方式 |
|---|------|------|---------|
| 0 | 脚本运行时选型 | HybridCLR 社区版（MIT），xLua 被否 | 四轮辩论：初评→二辩(xLua辩护)→三辩(独立开发者视角)→四辩(HybridCLR) |
| 1 | Mod 深度边界 | 目标 2 级（效果组合），Level 4 🟡 不堵门 | 6 角度评议团：架构纯粹/务实交付/社区史学/IL2CPP技术/Mod作者/产品战略 |
| 2 | 数据主权 | 全对象 Override | 否决字段级合并（Schema 复杂度高、官方字段变更时语义错乱） |
| 3 | 内容 ID 体系 | 字符串 + 自动命名空间前缀 + 废弃别名 | 否决 GUID（杀死零代码门槛）、否决混合方案（双体系复杂度） |
| 4 | Mod 间互操作 | 允许硬依赖 | 否决孤立模型（生态碎片化）、可选依赖留 S2 |
| 5 | 加载时机 | S1 启动时静态加载 | 否决热重载（S1 Mod 作者是早期 adopter，容忍重启） |
| 6 | 存档 vs Mod | 警告 + 占位物品（"损坏的遗物"）| 否决禁止加载（丢存档 = 摧毁信任） |
| 7 | 平台策略 | Steam Workshop 首发 | 否决 Mod.io（先聚焦 Steam 生态） |
| 8 | 创作者工具 | S1 文档 + 示例，S2 编辑器导出 | 否决独立 Mod 编辑器（等游戏卖出去再说） |
| 9 | 内容治理 | DMCA 流程，不主动审核 | 否决人工审核（消耗人力、成为社区增长瓶颈） |
| 10 | 分阶段投入 | 激进：S1 直接 Workshop 集成 | S0=ID体系+HybridCLR，S1=Workshop+效果组合+DLL门开着，S2/S3 以社区反馈为门 |

### 关键逆转

本 Session 最大的方向修正：

1. **Level 4 从 `❌ 不做，不预留，不准备` 改为 `🟡 不做，不堵门`**——因为发现后期加脚本 ≈ 重写游戏（RimWorld 3 DLC 后都没做 Lua），初期不堵门比什么都不做更重要
2. **xLua → HybridCLR**——HybridCLR 消解了 xLua 三大痛点（学新语言、维护绑定层、跨语言调试），且社区版免费够用
3. **"成功了再重构"被推翻**——成功游戏从不重构 Mod API（RimWorld 770 万份零脚本 API），不是不想，是做不起

### 下半场：架构框架 vs 独立文档

4. **从"三份技术文档"到"一套框架约束"**——原计划 ID 审计 + JSON Schema + 存档兼容三份文档。用户纠正：Mod 不是功能模块，是贯穿所有阶段的架构约束。JSON Schema 和存档兼容对当前进度（没有物品、没有存档）是过早设计。
5. **S0 只做 contentId + 程序集边界**——实际需要现在就做的只有给 PropertyPresetSO 加 contentId 字段（Phase 5 正在建物品）+ 程序集边界定义（不改代码，只定方案）。
6. **整合到长期计划而非独立计划**——前端补课并入 long-term.md + short-term.md，不另起计划文件。
7. **双 Agent 交叉审核**——长短期计划分别由独立 Agent 审核，发现 28 项问题（short-term 16 + long 12），已全部修正。

## Known Issues

- [ ] 所有产出均为文档和决策——零代码。contentId 字段、ModService 等实现在后续 Session
- [ ] API Compatibility Level 可能需从 `.NET Standard 2.1` 切换到 `.NET Framework`（HybridCLR 要求），影响范围待评估
- [ ] HybridCLR 补充元数据接入 Addressables 的具体 Group 配置未做
- [ ] AbilityReactor / AbilityExecutor 已确认 sealed——违反 Mod 框架，P3 修复
- [ ] P5.0 已有 49 件物品需 contentId 回填（short-term.md P0.4）

## Cross-References

### Related Sessions
- [2026-07-09-design-doc-restructure.md](2026-07-09-design-doc-restructure.md) — 同一天的策划文档体系重构

### Related Plans
- [../plans/mod-community-decision-record.md](../plans/mod-community-decision-record.md) — 10 项战略决策记录
- [../plans/mod-architecture.md](../plans/mod-architecture.md) — 已废弃的旧方案
- [../plans/long-term.md](../plans/long-term.md) — 长期计划（Mod 约束整合 + Phase 1-4 回顾）
- [../plans/short-term.md](../plans/short-term.md) — 短期计划（前置准备 P0-P3 + P5 Mod 义务）

### Related Tech Docs
- [../tech/mod-architecture-framework.md](../tech/mod-architecture-framework.md) — **新建**。Mod 架构框架（跨层级）
- [../tech/reference/hybridclr-integration.md](../tech/reference/hybridclr-integration.md) — **新建**。HybridCLR 技术分析

### Related Design Docs
- [../design/systems/mod.md](../design/systems/mod.md) — Mod 系统策划文档（已修正）
- [../design/systems/mod-json-reference.md](../design/systems/mod-json-reference.md) — **新建**。Mod JSON 格式手册

### Related Design Docs
- [../design/systems/mod.md](../design/systems/mod.md) — **更新**。Mod 系统策划文档
- [../design/systems/mod-json-reference.md](../design/systems/mod-json-reference.md) — **新建**。Mod JSON 格式手册
- [../design/game-overview.md](../design/game-overview.md) — 未修改，已定位 Mod 为核心关键词

### Flag for Design Doc Creation
- [x] No new design doc needed beyond what was already created this session
