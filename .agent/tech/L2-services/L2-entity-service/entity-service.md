# ⛔ API 章节过时 — Spawn/Despawn 已改为事件驱动

> **Status**: API 章节已更新。CreateGameObject 统一入口，Spawn/Despawn 事件驱动。
> **最后验证**: 2026-07-11

---

# EntityService — 实体管理服务

> `L2_EntityService/EntityService.cs` · L2 服务
> **Last Verified**: 2026-07-11

## 层级定位

L2 服务。所有 `Entity` 实例的**唯一拥有者**——数据本体只存于此处。其他系统（Actor、Container、地面 GO）只持有 Id 或缓存引用，不存在多份拷贝。

不依赖任何 L3 模块的具体类型。只依赖 `Entity` 的 `Id`、`EntityType`、`Properties`。

## 核心原则

### 引用模型

```
EntityService._entities (Dictionary<string, Entity>)  ← 数据唯一本体

所有其他方:
  ├── string _entityId          ← 持久引用（存档/联机/事件）
  └── Entity _entityCache       ← 本地引用（热路径直接读，一次字典查找后缓存）

GO / Container / 地面物品:
  └── 只持有 Id，或持有 Entity 引用（指向同一对象，非拷贝）
```

- Entity 是引用类型（class），多处持有引用都指向同一个对象
- Id 是冗余索引——用于缓存失效后重建引用（读档加载、联机同步）
- **不存在**"把 Entity 从 A 移动到 B"——引用可以同时被 Actor 和 Container 持有

### 数据生命周期 vs GO 生命周期

| | Entity 数据（Register/Unregister） | GO 载体（Spawn/Despawn） |
|---|---|---|
| 触发 | 创建时 Register，彻底消失时 Unregister | 进入视图/卸载时 Spawn/Despawn |
| 独立性 | 可以没有 GO（物品在背包、未加载 NPC） | GO 可以销毁而不影响 Entity 数据 |
| 调用方 | 创建者（ItemEntity 工厂、角色创建流程） | 场景管理器、容器系统、调用方请求 |

## 职责

| 职责 | 说明 |
|------|------|
| **数据管理** | Register / Unregister / Get / All / GetByType —— 永远生效 |
| **GO 生命周期** | Spawn / Despawn —— 接收请求、执行 Instantiate/Destroy |
| **遍历** | SaveService 遍历 All 序列化；未来联机遍历 All 同步 |
| **类型筛选** | 按 EntityType 过滤（"Character" / "Item" / …） |

## 调用链

```
被谁调（数据层 — 公开 API）:
  SaveService            → entityService.All → 遍历序列化
  Container / UI         → entityService.Get(id) → 读取实体
  各系统                 → entityService.GetByPreset<T>() → 类型筛选
  实体销毁方             → entityService.Unregister(id) → 注销实体

被谁调（GO 层 — 事件驱动）:
  调用方                 → spawnRequestEvent.Raise(SEntitySpawnRequest)
  调用方                 → despawnRequestEvent.Raise(SEntityDespawnRequest)
  EntityService          → OnWire 中订阅 spawnRequestEvent / despawnRequestEvent

EntityService 发布:
  spawnedEvent.Raise     → SEntitySpawned (生成完成通知)
  despawnedEvent.Raise   → SEntityDespawned (销毁完成通知)

内部持有:
  _entities: Dictionary<string, Entity>      → 数据注册表（唯一本体）
  Event Channel SO:
    spawnRequestEvent                        → 序列化字段，Inspector 连线
    despawnRequestEvent                      → 序列化字段，Inspector 连线
    spawnedEvent / despawnedEvent            → 序列化字段，Inspector 连线
```

## 数据流

```
角色创建:
  PlayerService 请求 → EntityService.SpawnCharacter(config, position)
    → new CharacterEntity(id, type, properties)
    → _entities[id] = entity
    → 加载对应 Prefab → Instantiate → Actor.Init(entity)
    → 返回 Actor 引用给 PlayerService

物品创建:
  ItemEntity.Create(def)
    → EntityService.Register(entity)
    → entity 已存在于注册表，无 GO
  
  放入地面:
    → EntityService.Spawn(entity.Id, position)
    → CreateGameObject() — VisualPrefab → defaultItemPrefab → Cube
  
  捡进背包:
    → EntityService.Despawn(entity.Id)  ← GroundItem GO 销毁
    → Container.Place(slot, entity.Id)  ← entity 仍在注册表

存档:
  SaveService.Write()
    → foreach entity in EntityService.All
      → entity.Properties 序列化
    → 容器数据：槽位 → entity.Id 映射（Id 引用，不存 Entity 本体）

读档:
  LoadSave() → 反序列化每个 entity → EntityService.Register(entity)
    → 当前场景的 Spawn，不在场景的只保留数据
```

> ⛔ **以下 API 已过时** — 当前公开 API 为 `Get(string)`, `All`, `GetByPreset<T>()`, `Unregister(string)`, `GetSpawnedEntities()`。
> Spawn/Despawn 全部走事件通道。

## API

```csharp
public class EntityService : ModuleChildMono
{
    // ── 数据管理（公开 API）──
    public Entity Get(string id);                    // 按 Id 检索，未找到返回 null
    public IEnumerable<Entity> All { get; }          // 所有已注册实体
    public IEnumerable<Entity> GetByPreset<T>()      // 按 Preset 类型筛选
        where T : PropertyPresetSO;
    public void Unregister(string id);               // 注销实体（级联清理嵌套容器）
    public IEnumerable<Entity> GetSpawnedEntities(); // 所有已生成 GO 的实体

    // ── GO 生命周期 ── 不暴露公开方法 ──
    // Spawn/Despawn 通过 Event Channel 驱动：
    //   调用方 Raise SEntitySpawnRequest   → EntityService.OnSpawnRequest
    //   调用方 Raise SEntityDespawnRequest → EntityService.OnDespawnRequest
    // EntityService 在 OnWire 中订阅这两个事件通道。
}
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | Entity（L3_Properties/Entity） | 管理对象类型 |
| 依赖 | EntityType→Prefab 映射配置 | Spawn 时加载哪个 Prefab |
| 被消费 | PlayerService | 请求生成玩家角色 |
| 被消费 | 场景管理器 | 控制 GO 潮汐 |
| 被消费 | SaveService | 存档遍历 |
| 被消费 | 容器系统 | 读取 Entity 属性（Weight/Tags） |
| 被消费 | Actor / GroundItem | 持有 Entity 缓存引用 |

## 设计决策

| 决策 | 原因 |
|------|------|
| EntityService 是 Entity 唯一拥有者 | 避免多份拷贝、引用漂移；存档/联机只需序列化一个字典 |
| Entity 为引用类型（class） | 热路径一次字典查找后缓存引用，指向同一对象 |
| Id + 缓存引用 双重引用模式 | Id 用于持久化/跨系统事件，缓存引用用于每帧热路径 |
| EntityType 用 string 不用 enum | 新实体类型加字符串即可，不改 Service 代码 |
| Spawn/Despawn 与 Register/Unregister 分离 | 物品进背包脱 GO 但 Entity 不销毁；NPC 卸载同理 |
| 不持有 GO 引用 | Entity 是纯数据；GO 是表现层，EntityService 只管生成/销毁 |
| 继承 ModuleChildMono | 与其他 Service 统一生命周期（OnAssemble / OnWire） |

## 与 PlayerService 的关系

PlayerService **不创建 Entity**。它只是：
1. 持有玩家 EntityId
2. 请求 EntityService.Spawn（生成 GO）
3. 绑定输入/相机到生成的 Actor
4. 提供 `CurrentPlayer` 给 UI 等系统

```
PlayerService:     "需要生成玩家" → EntityService.SpawnCharacter(config, pos)
                   EntityService 返回 Actor → 绑定输入/相机

PlayerService:     "玩家离线/死亡" → EntityService.Despawn(id)
                   玩家 Entity 仍在注册表（存档用）
```

## Entity 最小定义

```csharp
public class Entity
{
    // ── 身份数据 ──
    public string Id { get; }                    // 持久标识（Guid 或指定名）
    public PropertyPresetSO Preset { get; }      // 属性预设资产（EntityType 角色）
    public PropertyTable Properties { get; }     // 全量运行时属性数据

    // ── 堆叠 ──
    public int StackCount { get; set; }          // 堆叠数量（1=独件，>1=合并堆叠）
    public int MaxStackSize { get; }             // 最大堆叠数（读 Properties）
    public bool CanStack { get; }                // 堆叠未满

    // ── GO 载体 ──
    public GameObject View { get; }              // 场景中的 GO 载体（无 GO 时为 null）
    public bool HasView { get; }                 // 是否有活跃的 GO

    // ── 命令/查询门面 ──
    public EntityCommandModule Command { get; }  // 命令门面（外部向此实体下达命令）
    public EntityQueryModule Query { get; }      // 查询门面（外部读取此实体数据）

    // ── 嵌套容器 ──
    public RdContainer NestedContainer { get; }  // 容器类实体（背包等），Register 时自动创建

    public void Tick(float dt) => Properties?.Tick(dt);
}
```

不设 abstract Save/Load 方法——序列化由 SaveService 按类型分发，不挂在 Entity 上。

## 何时建代码

设计已定案。代码落地条件：
- Entity 类落地（L3_Properties/Entity/）
- ItemEntity 落地（L3_Item/）→ ItemEntity : Entity
- PlayerService 改造为 Id + Spawn 请求模式
- Container 改造为 Id 引用模式
- 存档需要遍历 EntityService.All
