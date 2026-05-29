# L1-L5 架构合规修复

**日期**: 2026-05-29 | **分支**: feature/l1-l5-restructure | **提交**: 2868983

## 目标

审计发现 29 处跨层违规。经逐项分析，确立豁免规则（契约 enum + event struct 不受层级约束），修复剩余 5 处真实违规 + 清理遗留问题。

## 改动

### 架构修复
- **EventDispatcherService** 从 L2_EventDispatcher 移至 L1_Core（namespace 合并到 RedDust.Core）
- **UIService 解耦**: TryResolveService(PlayerService) → SPlayerSpawnedEvent 订阅存引用；TryResolveService(GameStateService) → 发布 SGameStateRequest 事件
- **PlayerService 清理**: 删除未使用的 CharacterActor 字段/TryGetPlayerStats/CurrentPlayerActor
- **GameService 编辑器路径**: GetComponentInChildren(SceneService) + Publish(SSceneLoadComplete) → Publish(SLoadSceneRequest)
- **SPlayerSpawnedEvent**: class → readonly struct

### Namespace 去重
- 6 个 L2 namespace 改名：GameStateService→GameState, PlayerService→Player, SceneService→GameScene, TimeService→GameTime, CameraService→GameCamera, Input→GameInput

### 清理
- 13 处死 using import（Unity.VisualScripting ×7, System ×8, System.Collections, System.Collections.Generic, UnityEngine）
- 13 个 CreateAssetMenu 路径 Game/ → RedDust/

## 豁免规则

以下类型不受 L1-L5 层级约束：
- 契约枚举（EGameState）
- 事件驱动 Struct（SSceneLoadComplete, SIActionMove 等）

## 已知问题

- L3_Pathfinding 空目录（预留 A* 寻路）
- RedDust.Input → GameInput 后 `Input` 不再冲突，但代码中无不必要的 Unity 前缀
