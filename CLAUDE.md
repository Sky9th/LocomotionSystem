# RedDust - Project Context

## Project
俯视角末世生存种田流游戏。Unity URP，`feature/character-module-rebuild` 分支。

## Before Any Task
收到任务后，第一步是查文档再碰代码——文档是地图，代码是地形。

1. 先读 `.agent/tech/README.md` 索引，确认有无相关模块文档
2. 有文档 → 先读文档理解架构和设计决策，再定位代码
3. 没文档覆盖 → 才从代码搜索入手
4. 文档与代码有出入 → 先确认是否文档过时，再决定以哪个为准

## Documentation
- `.agent/tech/README.md` — 技术文档索引（按 L1→L5 层级，**改代码前必查**）
- `.agent/design/` — 设计意图 (WHY)
- `.agent/sessions/` — 近期会话归档（了解改动上下文）
- `.agent/README.md` — 文档目录约定
- `.agent/design/game-overview.md` — 游戏设计文档 (GDD)

## Skills
- `/agent-doc` — 归档设计/技术文档到 .agent 目录

## Code Rules
- **修改已有 .cs 文件必须用 Edit，禁止 Write 覆盖** — Edit 逐操作前后对比清晰，Write 替换整个文件无法追踪改动
- 新建文件才用 Write

## Key Design Principles
- 数据由上至下参数传递，不反向查询 GameContext
- CharacterRig 统一物理实体写入入口
- 父模块调用子模块，不跨级调用
- 不过度设计，但当前需要的基础框架现在就搭好——不把架构问题推到后面

