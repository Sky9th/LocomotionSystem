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
