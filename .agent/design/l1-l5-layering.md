# L1-L5 架构层级定义

> 为什么需要严格的 L 前缀？—— 防止层级退化回普通代码分组。

## 问题背景

早期代码使用扁平目录（Audio/ Character/ Core/ Inputs/ UI/），目录名仅表示模块归属，不反映架构层级。重构后引入 L 前缀，但初期错误地将所有内部子目录都标上了 L4/L5，导致 L 前缀通胀，失去区分意义。

## 层级定义

| 层级 | 定义 | 判断标准 | 谁带 L 前缀 |
|------|------|---------|------------|
| **L1** | 根管理层 | 持有所有 Service，无业务逻辑 | `L1_Core/` |
| **L2** | 系统服务 | 继承 BaseService，由 GameService 管理生命周期 | 所有 Service 目录 |
| **L3** | 领域模块 | 独立领域，不隶属单一 L2，可被多个 Service 共用 | 所有 Module 目录 |
| **L4** | 领域子系统 | L3 内部的**不同领域系统**，承担独立功能 | Animation/Audio/Kinematic/Locomotion/Stats |
| **L5** | 子系统的子系统 | L4 内部的**附属子系统**，承担其下一级独立功能 | Drivers/Locomotion |

## 不是 L4 的

以下目录**不是 L4 子系统**，只是普通代码分组，不应带 L 前缀：

- `Actor/` — Character 自身组件（CharacterActor, CharacterRig）
- `Actions/` — Input 自身 handler 代码
- `Core/` — UI 自身框架
- `Components/` — UI 自身组件库
- `HUD/` — UI 自身 HUD
- `Config/`、`Structs/`、`Data/` — 按文件类型分组
- `Definition/`、`Instance/`、`Modifier/`、`Tree/`、`Interfaces/`、`Editor/` — 按代码职责分组

**核心区别**：L4 回答"这是什么子系统"，代码分组回答"这是什么文件类型"。

## 不是 L5 的

L5 仅当满足以下**全部**条件时才创建：

1. 是 L4 的附属子系统（非通用分组）
2. ≥3 个文件
3. 承担独立二级功能

以下**不是 L5**：
- `Config/`、`Structs/`、`Data/`、`Rules/`、`States/`、`Requests/` — 按文件类型分组
- `L5_UI/` (1 文件)、`L5_Traversal/` (1 文件)、`L5_Config/` (1 文件) — 不满足 ≥3
- `Player/`、`System/`、`Control/`、`Button/` — 领域分组但不是子系统

## 命名规则

| 位置 | 格式 | 示例 |
|------|------|------|
| 代码目录 (`Assets/Scripts/`) | `L{N}_{PascalCase}` | `L4_Animation/`, `L5_Drivers/` |
| 文档目录 (`.agent/tech/`) | `L{N}-{kebab-case}` | `L4-animation/`, `L5-drivers/` |
| 占位容器 | PascalCase，无 L 前缀 | `Services/`, `Modules/`, `Shared/` |

> 代码目录跟 Unity PascalCase，文档目录用 kebab-case 便于阅读。两者 L 数字含义完全相同。

## 依赖方向

```
L1_Core
  ↑
L2 Services (L2_Audio, L2_Input, L2_UI, ...)
  ↑
L3 Modules (L3_Character, L3_Stats, ...)
  ↑
L4 Subsystems (L4_Animation, L4_Kinematic, ...)
  ↑
L5 Sub-subsystems (L5_Drivers, L5_Locomotion)
```

- L3 不依赖特定 L2（Character 不 import PlayerService）
- L4 只被同 L3 内部调用
- L5 只被父 L4 内部调用
- Shared 不限层级
