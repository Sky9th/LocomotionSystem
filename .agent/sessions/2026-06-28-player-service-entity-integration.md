# 2026-06-28-player-service-entity-integration

## Background

EntityService 就位后，PlayerService 仍直接 `Instantiate(playerPrefab)`。本次接入：PlayerService 只发 SpawnRequest，EntityService 包办创建 Entity + Register + Instantiate + 通知。

## Changes

### EntityService
- 补 4 个序列化通道字段 + `OnWire` 订阅 SpawnRequest/DespawnRequest
- `OnSpawnRequest`: `new Entity → Register → Instantiate → BindEntity → Raise(Spawned)`
- `OnDespawnRequest`: `Destroy → Raise(Despawned)`
- 移除公开 `Spawn()`/`Despawn()` 方法

### PlayerService
- `playerPrefab` → `CharacterDefSO characterDef`
- 加 3 个序列化通道：`spawnRequestEvent / spawnedEvent / despawnRequestEvent`
- `CreatePlayer()`: publish `SEntitySpawnRequest(characterDef, pos, rot)` — 不 new Entity，不持 EntityService
- `OnPlayerSpawned`: 缓存 `playerInstance` + `playerEntityId` + publish `SPlayerSpawnedEvent`

### SEntitySpawnRequest
- `EntityId` → `Preset` (PropertyPresetSO) — 请求方不分配 Id

### Entity 事件通道
- 重命名：`EntitySpawnRequestEvent` (去 Channel 后缀)
- CreateAssetMenu fileName 简化：`EntitySpawnRequest`

## Decisions

| Decision | Reason |
|----------|--------|
| PlayerService 不持有 EntityService | 全事件通道解耦，PlayerService 不知道 Entity 如何创建 |
| SEntitySpawnRequest 传 Preset 不传 EntityId | 请求方不能也不应该分配 Entity Id |
| 不移除 EventDispatcherService | Camera/UIService 仍依赖 _dispatcher，后续统一迁移 |

## Cross-References

### Related Sessions
- [2026-06-27-entity-service-data-model.md](2026-06-27-entity-service-data-model.md) — EntityService 数据模型落地
- [2026-06-27-event-system-unification.md](2026-06-27-event-system-unification.md) — GameEvent<T> 统一

### Flag for Design Doc Creation
- [x] No design doc needed — internal integration.
