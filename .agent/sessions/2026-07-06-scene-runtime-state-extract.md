# 2026-07-06 — RuntimeSceneState 抽出 + S 前缀统一

## Background

`SceneService.cs` 内部定义了一个 `private readonly struct RuntimeSceneState`（14 行），用于追踪当前场景的运行时状态。将其保留为嵌套 struct 违反了模块结构一致性：`L2_SceneService/Structs/` 目录已存在，同目录的 `SSceneRequest`、`SSceneTransition`、`SLoadingProgress` 均为独立文件且统一使用 `S` 前缀（`public readonly struct`）。

## Changes

### L2_SceneService
- `SceneService.cs` — 删除嵌套 `private readonly struct RuntimeSceneState`（14 行），4 处引用 `RuntimeSceneState` → `SRuntimeSceneState`
- `Structs/SRuntimeSceneState.cs`（新）— `public readonly struct`，字段 `SceneName` / `ScenePath` / `AssetLabels`，命名空间 `RedDust.GameScene`

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 抽为 `public` struct 放入 `Structs/` | A: 保持 `internal` → 不必要，同目录 struct 全部 `public`。B: 留在 `SceneService` 内部 → 违背模块结构一致性。 | 统一 `Structs/` 目录下的 S 前缀 + public 约定 |
| 命名为 `SRuntimeSceneState` | A: 保留原名 `RuntimeSceneState` → 同目录其他 struct 全是 S 前缀 | S 前缀是项目 struct 命名统一规范 |

## Known Issues

_None — 纯机械重构，行为完全不变。_

## Cross-References

### Related Tech Docs
- [../tech/L2-services/L2-scene-service/scene-service-loading.md](../tech/L2-services/L2-scene-service/scene-service-loading.md) — Structs 表需新增 SRuntimeSceneState

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactor, no design-facing changes.
