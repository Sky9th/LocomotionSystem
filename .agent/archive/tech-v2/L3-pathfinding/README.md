# 09-pathfinding · 寻路系统

> 基于 Aron Granberg "A\* Pathfinding Project" 外部包的寻路集成。当前处于初期接入阶段，无自有代码。

## 资产位置

| 内容 | 路径 |
|------|------|
| A\* 核心包 | `Packages/com.arongranberg.astar/` |
| 测试场景 | `Assets/Scenes/PathFinding.unity` |
| 测试用 Prototype 资产 | `Assets/Art/PolygonPrototype/` (栅格/建筑/道具) |

## 当前状态

- A\* Pathfinding Project 包已导入并配置为 Package Manager 依赖
- 测试场景 `PathFinding.unity` 已创建，包含 Prototype 栅格地面和建筑
- A\* 官方示例 (`ExampleScenes~/`) 已从版本控制移除 (121 MB)，仅保留核心包代码
- GridGraph 和 Agent 尚未配置

## 集成计划

按 `short-term.md` 优先级排列:

| 步骤 | 内容 | 前置条件 | 预计优先度 |
|------|------|---------|-----------|
| 1 | GridGraph 配置 — 俯视角 2D 网格导航参数 | 地形场景确定 | 高 |
| 2 | FollowerEntity / AIPath Agent 挂载到角色 | GridGraph 就绪 | 高 |
| 3 | 与 CharacterActor Movement 系统对接 | Agent 配置完成 | 中 |
| 4 | 鼠标点击 → 寻路目的地 (右击移动) | CharacterEventReceiver | 中 |
| 5 | 动态障碍物 / 导航区域更新 | GridGraph 配置 | 低 |

## 依赖

| 模块 | 关系 |
|------|------|
| A\* Pathfinding Project | Asset Store 付费插件，核心寻路算法和 GridGraph 生成 |
| 02-character (CharacterActor) | 目标对接方 — Agent 的输出需驱动 CharacterActor 移动 |
| 03-input (CharacterEventReceiver) | 点击事件需转换为寻路请求 |
| 10-editor (SyntyPrototypeBrowser) | 测试场景使用 PolygonPrototype 资产搭建 |

## 接入架构 (规划)

```
Input (鼠标右击)
  │
  ▼
CharacterEventReceiver → SeekRequest(worldPosition)
  │
  ▼
FollowerEntity (A* Agent)
  │
  ├── 读取 GridGraph → 规划路径
  └── 每帧输出 velocity / nextCorner
        │
        ▼
CharacterActor.LocomotionInput → 驱动 GroundLocomotion
```

## 设计决策

| 决策 | 原因 |
|------|------|
| 使用 FollowerEntity 而非 AIPath | FollowerEntity 更轻量且与 Unity ECS 兼容 |
| 示例场景从版本控制移除 | 核心包 121 MB，仅保留必要文件 |
| 不封装自有寻路接口 | A\* 包 API 已成熟，避免过度封装 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| GridGraph 配置脚本化 | 待做 | 旧 pathfinding.md |
| 动态障碍更新 | 待做 | 旧 pathfinding.md |
| 多层 NavGraph (地面/屋顶) | 远期 | 旧 pathfinding.md |
| 与 Stats 系统联动 (体力影响寻路速度) | 远期 | 旧 pathfinding.md |
