# Namespace 整理

**日期**: 2026-05-29 | **分支**: feature/l1-l5-restructure | **提交**: c9ffae4

## 目标

上阶段完成 L1-L5 目录结构重构后，namespace 仍沿用旧 `Game.*` 命名，存在 8 处不一致（`Game.Locomotion.Animation.Config` 等错误映射）。本次将所有 namespace 统一迁移到 `RedDust.*`，严格按目录的 `L#_` 前缀生成 namespace 段。

## 映射规则

- **根**: `RedDust`（替换 `Game`）
- **L#_ 目录**: 去前缀后作为 namespace 段（`L3_Character` → `Character`）
- **非 L#_ 目录**: 归入父级 namespace（`Actor/`, `Config/`, `Structs/`, `Data/`, `Rules/` 等）
- **容器目录**: `Services/`, `Modules/` 跳过
- **Shared/**: 特例，作为顶层 namespace 段

## 改动范围

155 文件，21 个 namespace：

| 层 | Namespace | 来源 |
|----|-----------|------|
| Shared | `RedDust.Shared` | Shared/ (Logging, Constants, Editor, Utility) |
| L1 | `RedDust.Core` | L1_Core/ |
| L2 | `RedDust.Audio` ~ `.UI` | 9 个 L2 服务 |
| L3 | `RedDust.Character`, `.Stats`, `.Pathfinding` | 3 个 L3 模块 |
| L4 | `RedDust.Character.{Animation,Audio,Kinematic,Locomotion,Stats}` | 5 个 L4 子系统 |
| L5 | `RedDust.Character.Animation.Drivers` / `.Locomotion` | 2 个 L5 驱动 |

## 已知问题

- 5 个 L2 服务的 namespace 名和类名相同（GameStateService, PlayerService, SceneService, TimeService, CameraService），跨 namespace 引用时需完全限定名（如 `RedDust.GameStateService.GameStateService`）
- `RedDust.Input` namespace 与 `UnityEngine.Input` 冲突，需用 `UnityEngine.Input.mousePosition` 等完全限定
- IDE 格式化后部分文件缩进仍不一致（tab/空格混用），建议全量格式化
