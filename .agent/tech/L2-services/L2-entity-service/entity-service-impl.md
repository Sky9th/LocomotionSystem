# ⛔ 实现细节过时 — 事件驱动架构未体现

> **Status**: 本文档描述的内部实现（`_views Dictionary`, `Register()`, `Spawn()`, `Despawn()` 公开方法）已过时。
> 当前实现：`OnSpawnRequest(SEntitySpawnRequest)` / `OnDespawnRequest(SEntityDespawnRequest)` 事件驱动，View 追踪移至 `Entity.View` / `Entity.HasView`。
> **以 `L2_EntityService/EntityService.cs` 代码为准。**
> **最后验证**: 2026-07-03

---

# EntityService — 实现

> `L2_EntityService/EntityService.cs` · L2 服务，MonoBehaviour
> **Last Verified**: 2026-06-28 | **Verification**: All referenced files exist. +TryCreateNestedContainer +Unregister cascade.

## 内部机制

继承 `ModuleChildMono`。`OnAssemble` 时注册到 `GameContext`。`OnWire` 时订阅事件通道。

### 内部数据结构

- `_entities: Dictionary<string, Entity>` — 数据注册表（唯一本体）
- View 追踪不在 EntityService 层，而是在 `Entity.View` / `Entity.HasView` 属性上

### Event Channel（序列化字段，Inspector 连线）

```csharp
[SerializeField] private EntitySpawnRequestEvent spawnRequestEvent;
[SerializeField] private EntitySpawnedEvent spawnedEvent;
[SerializeField] private EntityDespawnRequestEvent despawnRequestEvent;
[SerializeField] private EntityDespawnedEvent despawnedEvent;
```

## 调用链

```
GO 层 — 事件驱动:
  调用方 → spawnRequestEvent.Raise(SEntitySpawnRequest)     → OnSpawnRequest
  调用方 → despawnRequestEvent.Raise(SEntityDespawnRequest) → OnDespawnRequest
  OnSpawnRequest → Instantiate(Preset.Prefab) → Identity.BindEntity + Entity.View 赋值
  OnSpawnRequest → spawnedEvent.Raise(SEntitySpawned)        → 通知外部
  OnDespawnRequest → Destroy(go) → Entity.View = null
  OnDespawnRequest → despawnedEvent.Raise(SEntityDespawned)  → 通知外部

数据层 — 公开 API:
  SaveService / 遍历方 → entityService.All / GetByPreset<T>()
  读取方               → entityService.Get(id)
  销毁                 → entityService.Unregister(id)  （级联清理嵌套容器子实体）
  有 GO 实体列表       → entityService.GetSpawnedEntities()

内部:
  Register(entity) → _entities[id] = entity （private，仅 OnSpawnRequest 调用）
```

## 公开属性

无公开属性。通过 GameContext 解析后直接调方法。

## 方法

### 生命周期

#### OnWire()
```csharp
public override void OnWire()
```
- **Purpose**: 订阅事件通道。`spawnRequestEvent.Register(OnSpawnRequest)` / `despawnRequestEvent.Register(OnDespawnRequest)`
- **Notes**: `OnDestroy` 中 Unregister 取消订阅

### GO 生命周期（事件驱动，private）

#### OnSpawnRequest()
```csharp
private void OnSpawnRequest(SEntitySpawnRequest req)
```
- **Purpose**: 响应 Spawn 请求事件。分两条路径：
  1. **新 Entity（Preset != null）**：`new Entity(id, Preset)` → `Register(entity)` → 如果 `Position.HasValue` 则 `Instantiate(prefab)` → `Identity.BindEntity` → 设置 `Identity.Entity` + `Identity.SetProperties` → `entity.View = go` → `spawnedEvent.Raise`
  2. **已有 Entity（仅 Preset 为 null，EntityId 指定）**：从注册表取已有 Entity → 校验无 View → `Instantiate` → 绑定 → `spawnedEvent.Raise`
- **Params**: `req.Preset` — 新建时用；`req.EntityId` — 已有实体 Id；`req.Position/req.Rotation` — GO 位置
- **Notes**: 位置为空且无 Prefab 时报错；已有 View 时报错不允许重复 Spawn

#### OnDespawnRequest()
```csharp
private void OnDespawnRequest(SEntityDespawnRequest req)
```
- **Purpose**: 响应 Despawn 请求事件。取 Entity → 取 View → `entity.View = null` → `Destroy(go)` → `despawnedEvent.Raise`
- **Notes**: Entity 不存在或无 View 时静默跳过（no-op）

### 数据层（公开 API）

#### Get()
```csharp
public Entity Get(string id)
```
- **Purpose**: 按 Id 检索实体。未找到返回 null
- **Notes**: 热路径用 `_entityCache = Get(id)` 后直接引用，不走字典

#### All
```csharp
public IEnumerable<Entity> All
```
- **Purpose**: 所有已注册实体。存档/联机遍历用

#### GetByPreset\<T\>()
```csharp
public IEnumerable<Entity> GetByPreset<T>() where T : PropertyPresetSO
```
- **Purpose**: 按 Preset 类型筛选。如 `GetByPreset<CharacterDefSO>()` 取所有角色
- **Notes**: 类型检查是 `entity.Preset is T`

#### GetSpawnedEntities()
```csharp
public IEnumerable<Entity> GetSpawnedEntities()
```
- **Purpose**: 所有已生成 GO 的实体。筛选 `entity.HasView`

#### Unregister()
```csharp
public void Unregister(string id)
```
- **Purpose**: 注销实体。级联清理嵌套容器子实体；有 View 时 `Destroy(View)` 并清空 View
- **Callers**: 实体彻底销毁时

### 内部（private）

#### Register()
```csharp
private bool Register(Entity entity)
```
- **Purpose**: 注册实体到数据表。Id 重复 → LogError + 返回 false。注册后调用 `TryCreateNestedContainer` 创建嵌套容器
- **Params**: `entity` — 不可为 null
- **Returns**: 成功 true
- **Notes**: 仅供 `OnSpawnRequest` 内部调用，不对外暴露

## 未来规划

| 计划 | 状态 | 来源 |
|------|------|------|
| 事件订阅 | 已实现 | OnWire 订阅 spawnRequestEvent/despawnRequestEvent，Spawn/Despawn 全部事件驱动 |
| Prefab 缓存 | 待定 | Preset.Prefab 重复加载优化 |
| 批量 Spawn/Despawn | 待定 | 读档时批量实例化 |
