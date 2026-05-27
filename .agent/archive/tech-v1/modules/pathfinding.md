# PathFinding — A* 寻路系统

## 概述

使用 Aron Granberg 的 A\* Pathfinding Project 实现俯视角寻路。当前处于早期集成阶段。

## 资产位置

| 内容 | 路径 |
|------|------|
| A\* 包 | `Packages/com.arongranberg.astar/` |
| 测试场景 | `Assets/Scenes/PathFinding.unity` |

## 依赖

- **A\* Pathfinding Project** — Asset Store 付费插件
- 示例文件已从版本控制移除（`ExampleScenes~/`，121 MB），仅保留核心包代码

## 集成计划

按 `short-term.md` 优先级：

1. Grid graph 配置 — 俯视角 2D 网格导航
2. AIPath / FollowerEntity Agent 挂载
3. 与 CharacterRig 移动系统对接
4. 鼠标点击 → 寻路目的地

## 相关文件

- `Assets/Scripts/Character/Input/CharacterEventReceiver.cs` — 角色输入事件（未来与寻路对接）

## 开发状态

2026-05-27: 测试场景已创建，网格和 Agent 待配置。
