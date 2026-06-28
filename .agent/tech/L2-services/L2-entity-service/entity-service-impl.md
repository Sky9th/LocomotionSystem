# EntityService — 实现

> `L2_EntityService/EntityService.cs` · L2 服务，MonoBehaviour
> **Last Verified**: 2026-06-28 | **Verification**: All referenced files exist. +TryCreateNestedContainer +Unregister cascade.

## 内部机制

继承 `ModuleChildMono`。`OnAssemble` 时注册到 `GameContext`。

两个内部字典：
- `_entities: Dictionary<string, Entity>` — 数据注册表
- `_views: Dictionary<string, GameObject>` — Spawn 生成的 GO 追踪

## 调用链

```
调用方:
  PlayerService (未来)        → entityService.Spawn(playerId, pos)
  场景管理器 (未来)            → entityService.Spawn / Despawn
  创建者                       → entityService.Register(entity)
  SaveService (未来)           → entityService.All → 遍历序列化

调谁:
  Instantiate(Preset.Prefab)  → 生成 GO
  Identity.BindEntity(Id)     → 绑定 GO 到 Entity
```

## 公开属性

无公开属性。通过 GameContext 解析后直接调方法。

## 方法

### Register()
```csharp
public bool Register(Entity entity)
```
- **Purpose**: 注册实体到数据表。Id 重复 → LogError + 返回 false
- **Params**: `entity` — 不可为 null
- **Returns**: 成功 true

### Unregister()
```csharp
public void Unregister(string id)
```
- **Purpose**: 注销实体。同时 Despawn（如果有 GO）
- **Callers**: 实体彻底销毁时

### Get()
```csharp
public Entity Get(string id)
```
- **Purpose**: 按 Id 检索实体。未找到返回 null
- **Notes**: 热路径用 `_entityCache = Get(id)` 后直接引用，不走字典

### All
```csharp
public IEnumerable<Entity> All
```
- **Purpose**: 所有已注册实体。存档/联机遍历用

### GetByPreset\<T\>()
```csharp
public IEnumerable<Entity> GetByPreset<T>() where T : PropertyPresetSO
```
- **Purpose**: 按 Preset 类型筛选。如 `GetByPreset<CharacterDefSO>()` 取所有角色
- **Notes**: 类型检查是 `entity.Preset is T`

### Spawn()
```csharp
public GameObject Spawn(string id, Vector3? position = null, Quaternion? rotation = null)
```
- **Purpose**: 为实体生成 GO 载体。从 `entity.Preset.Prefab` Instantiate → `Identity.BindEntity(id)`
- **Params**: `id` — 实体 Id；`position/rotation` — 可选 Transform
- **Returns**: 生成的 GO，失败返回 null
- **Notes**: 已存在 GO 时先 Despawn 旧 GO 再实例化新 GO

### Despawn()
```csharp
public void Despawn(string id)
```
- **Purpose**: 销毁实体 GO。Entity 数据保留
- **Notes**: 无 GO 时 no-op

### IsSpawned()
```csharp
public bool IsSpawned(string id)
```
- **Purpose**: 实体是否有活跃的 GO

### GetView()
```csharp
public GameObject GetView(string id)
```
- **Purpose**: 获取实体对应的 GO。未生成返回 null

## 未来规划

| 计划 | 状态 | 来源 |
|------|------|------|
| 事件订阅 | 待实现 | 订阅 `SEntitySpawnRequest`/`SEntityDespawnRequest`，移除直接 Spawn/Despawn 公开方法 |
| Prefab 缓存 | 待定 | Preset.Prefab 重复加载优化 |
| 批量 Spawn/Despawn | 待定 | 读档时批量实例化 |
