# RedDust - Project Context

## Project
俯视角末世生存种田流游戏。Unity URP，`feature/character-module-rebuild` 分支。

## Documentation
- `.agent/README.md` — 文档目录约定
- `.agent/design/game-overview.md` — 游戏设计文档 (GDD)

## Skills
- `/agent-doc` — 归档设计/技术文档到 .agent 目录

## Key Design Principles
- 数据由上至下参数传递，不反向查询 GameContext
- CharacterRig 统一物理实体写入入口
- 父模块调用子模块，不跨级调用

## Sub-Agent Model Selection

根据任务性质选择模型，避免高能力模型做低价值工作。

| 任务类型 | 模型 | 说明 |
|----------|------|------|
| 文件搜索 / Grep / Glob | `haiku` | 纯检索，无需推理 |
| "Where is X defined?" / 查找引用 | `haiku` | 模式匹配 |
| 读取已知文件提取信息 | `haiku` | 无判断需求 |
| Explore agent | `haiku` | 只读搜索，不写代码 |
| Code Review / 一致性检查 | `sonnet` | 需要中等判断力 |
| 代码编写 / 重构 / 实现 | 继承主模型 (`opus`) | 需要深度推理 |
| 架构设计 / 多文件改动 | 继承主模型 (`opus`) | 需要全局理解 |
| Bug 分析 / 调试 | 继承主模型 (`opus`) | 需要因果推理 |
| 规划 (Plan agent) | 继承主模型 (`opus`) | 需要架构权衡 |

**规则**: 任务目标只是"找东西"→ `haiku`；需要"判断好坏"→ `sonnet`；需要"创造/推理"→ 继承主模型。
