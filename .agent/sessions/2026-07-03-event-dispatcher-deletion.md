# 2026-07-03 — EventDispatcher 全量删除 + EventHub 架构落地

## Background

`EventDispatcherService` 是项目早期的泛型事件总线，发布/订阅通过 `Subscribe<T>()`/`Publish<T>()` 配合 `MetaStruct` 传递。随着 EventHub + `GameEvent<T>` SO 通道体系在 Input 模块验证成熟，需要将全量 L1/L2 服务迁移至 EventHub，彻底删除 EventDispatcher。

此前已完成的准备工作：
- 32 个 EventSO 类从散落各处收归 `L1_Core/Events/`，统一命名和 `CreateAssetMenu`
- EventHub 重构为 5 个分组数组（gameStateEvents / sceneEvents / playerEvents / inputEvents / abilityEvents）
- GameService 显式注册 EventHub 到 GameContext

## Changes

### EventDispatcher 退役
- 10 个服务从 `EventDispatcherService` 迁移至 `EventHub.Get<T>().Register/Raise/Unregister`
- 所有 handler 签名从 `(T, MetaStruct)` 改为 `(T)`
- `PublishSnapshot<T>()` helper 方法统一改为仅 `UpdateSnapshot`，EventHub Raise 在调用点内联
- `PlayerInput` 的 EventDispatcher 残留字段和订阅删除
- `PathfindingService` 的未使用 `_dispatcher` 字段删除

### 迁移的服务
- **SceneService** — 3 Subscribe + 8 Publish（SSceneLoadStart/Complete）
- **GameStateService** — 1 Subscribe + 1 Publish（SGameState）
- **PlayerService** — 1 Subscribe + 1 Publish（SPlayerSpawnedEvent）
- **CameraService** — 1 Subscribe + 1 Publish（SCameraSnapshot → CameraSnapshotEvent）
- **UIService** — 4 Subscribe + 6 Publish
- **GameService** — 1 Subscribe + 1 Publish（Editor-only）
- **TimeService** — 3 Subscribe 从 Dispatcher 迁至 EventHub，统一 OnWire
- **PlayerInput** — SCameraSnapshot 迁至 EventHub
- **PathfindingService** — 删除未使用字段和 Resolve

### 新增
- `CameraSnapshotEvent` — SCameraSnapshot 的 EventSO channel
- `CameraSnapshot.asset`

### 命名最终统一
- GameState: `GameStateChangedEvent` / `GameStateChangeRequestEvent`
- Scene: `SceneLoadRequestEvent` / `SceneReloadRequestEvent` / `SceneUnloadRequestEvent` / `SceneLoadStartEvent` / `SceneLoadCompleteEvent`
- Input: `Input{Action}Event`（如 `InputJumpEvent`、`InputTimeSlowEvent`）
- `ThirdInteract` → `ThirdInteract`（修正 typo）

### 代码审计
- 双 Agent 并行审计发现 3 CRITICAL + 4 HIGH（`Get<T>()` null safety），全部修复

### 删除
- `EventDispatcherService.cs`
- `GameContext.HasService()`
- 所有 `.meta` 残留文件

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| `PublishSnapshot` helper 改为仅 `UpdateSnapshot`，Raise 内联 | A: 保留泛型 Publish 并接受 GameEvent 参数 → API 复杂。B: 完全删除 helper → 重复代码多 | 只保留 Snapshot 写入，Event 发布在调用侧显式写 |
| `TryResolveService` 统一 early-return 模式 | A: guard block `if (_eventHub != null) { ... }` → 嵌套深。B: nullable `?.` 链 → 静默失败 | early-return 最干净，Resolve 失败意味着整个服务不可用 |
| CameraSnapshotEvent 放在 Player 目录 | A: 新建 System 目录 → 只有一个 event。B: 放 Camera 目录 → 不是 EventHub 的现有分类 | Player 目录已有 PlayerSpawnedEvent，Camera 属于玩家相关 |

## Known Issues

- [ ] GameManager.prefab 中遗留的 "EventDispatcher" GameObject 需在 Editor 手动删除 — P2
- [ ] `SReloadSceneRequest` 有 EventSO 但无发布方 — P3（死信保留，后续确认是否需要）
- [x] SceneService/PlayerService 缺少 null guard — 已修复
- [x] PlayerInput Get<T>() 无 null safety — 已修复

## Cross-References

### Related Sessions
- [2026-07-02-animation-arbiter-refactor.md](2026-07-02-animation-arbiter-refactor.md) — 动画 Arbiter 重构，同项目上下文

### Related Plans
- [../plans/graceful-inventing-mountain.md](../plans/graceful-inventing-mountain.md) — EventSO 统一收归 + EventDispatcher 删除计划

### Related Tech Docs
- [../tech/L1-core/events/event-channels.md](../tech/L1-core/events/event-channels.md) — EventHub 文档（需更新）
- [../tech/L1-core/events/game-event.md](../tech/L1-core/events/game-event.md) — GameEvent<T> 文档

### Related Versions
- [../versions/v0.33.0.md](../versions/v0.33.0.md)

### Flag for Design Doc Creation
- [x] EventHub 架构已更新 — event-channels.md 需要反映新的分组数组设计
