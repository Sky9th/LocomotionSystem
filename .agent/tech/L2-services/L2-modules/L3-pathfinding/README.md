# L3-pathfinding · A* 寻路系统

> L3 独立模块 — 使用 Aron Granberg 的 A* Pathfinding Project 实现俯视角寻路。当前处于早期集成阶段。

## 层级定位

L3 独立模块，位于 `L2-modules/` 虚拟容器下。被 PlayerService（玩家点击移动）和未来 AIService（敌人寻路）共用。

## 调用链

```
鼠标点击 → InputActionHandler
  → CharacterEventReceiver
    → 寻路请求 (未来)

GridGraph → FollowerEntity/AIPath → CharacterRig 移动 (未来)
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| A* Pathfinding | — (付费插件) | 第三方依赖 |
| L3-pathfinding | L3-character (CharacterRig) | 寻路结果驱动角色移动 (未来) |
| L3-pathfinding | L2-input | 鼠标点击 → 寻路目的地 (未来) |

## 资产位置

| 内容 | 路径 |
|------|------|
| A* 包 | `Packages/com.arongranberg.astar/` |
| 测试场景 | `Assets/Scenes/PathFinding.unity` |

## 依赖

- **A* Pathfinding Project** — Asset Store 付费插件
- 示例文件已从版本控制移除（`ExampleScenes~/`，121 MB），仅保留核心包代码

## 设计决策

| 决策 | 原因 |
|------|------|
| A* Pathfinding Project | 成熟付费方案，支持 GridGraph + FollowerEntity |
| 测试场景先行 | 在正式集成前验证网格和 Agent 行为 |

## 集成计划

1. Grid graph 配置 — 俯视角 2D 网格导航
2. AIPath / FollowerEntity Agent 挂载
3. 与 CharacterRig 移动系统对接
4. 鼠标点击 → 寻路目的地

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| GridGraph 配置 | 待做 | A* 插件 | pathfinding.md |
| Agent 挂载 | 待做 | GridGraph | pathfinding.md |
| CharacterRig 对接 | 待做 | Agent + L3-character | pathfinding.md |
| 鼠标寻路 | 待做 | CharacterRig 对接 | pathfinding.md |

## 子文档索引

| 文档 | 源文件 | 说明 |
|------|--------|------|
| (暂无 — 模块代码尚未实现) | — | — |
