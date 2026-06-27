# 2026-06-27-entity-service-data-model

## Background

上一 session 落地了 CharacterEntity / ItemEntity / Entity 基类加 CharacterActor 壳化方案，
但在实施后发现 CharacterEntity 只是把代码搬了个位置——它通过 BuildContext 依赖
Transform/EventHub/Ability 等所有 GO 引用，根本不是独立实体。用户判断"写法非常不合理"。

随后回滚所有未提交改动，从零重新梳理 Entity 架构。核心洞察：Entity 和 GO 是本质与载体的关系，
EntityService 应是 Entity 数据的唯一拥有者。整个下午围绕这一方向逐步定稿。

这是 Entity 系统重塑的第一步——数据层先定，Character/Item 迁移后续跟进。

## Changes

### L2_EntityService（新建）
- `Entity.cs` — 实体数据模型，Id + Preset（PropertyPresetSO，= EntityType）+ Properties + Tick
- `EntityService.cs` — L2 服务，Dictionary 注册表（数据层）+ Spawn/Despawn（GO 层）
- `Structs/SEntitySpawnRequest.cs` — 请求生成实体 GO 的事件载荷
- `Structs/SEntitySpawned.cs` — 生成完成通知载荷
- `Structs/SEntityDespawnRequest.cs` — 请求销毁 GO 的事件载荷
- `Structs/SEntityDespawned.cs` — 销毁完成通知载荷
- `Events/EntitySpawnRequestEvent.cs` 等 4 个 GameEvent\<T\> SO 通道类型

### L3_Identity（修改）
- `Identity.cs` — 新增 `EntityId` 字段 + `BindEntity(string)` 方法
- GO 侧数据锚点：Actor/GroundItem 通过 Identity.EntityId 关联 EntityService 的注册表

### L3_Properties（修改）
- `PropertyPresetSO.cs` — 新增 `Prefab` 字段（GameObject），EntityService.Spawn 时 Instantiate

### 架构文档
- 更新 `L2-entity-service/entity-service.md` — 重写引用模型、数据流、API、设计决策

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| EntityService 是 Entity 唯一拥有者 | A: 各处持有 Entity 副本 → 引用漂移。B: 纯静态类 → 不好做生命周期。 | 单一数据源，字典即真理 |
| Preset 即 EntityType，不设独立字段 | A: 额外 EntityType string → 冗余。B: enum → 扩展要改代码。 | Preset 已经定义了实体是什么（CharacterDefSO vs ItemDefSO），复用作类型标识 |
| Entity 位于 L2 而非 L3_Properties | A: 放 L3_Properties 下看起来是属性子概念。B: 放 L3_Entity 独立 L3。 | Entity 是所有模块的共有概念，L3 兄弟模块不应向下引用它；L2 对 L3 的单向依赖合理 |
| Spawn/Despawn 与 Register/Unregister 分离 | A: Register 时自动 Spawn → 物品进背包也要 GO。 | 物品在背包无 GO、NPC 卸载保留数据——数据生命周期 ≠ GO 生命周期 |
| GO/Container 同时持有 Id + 缓存引用 | A: 只存 Id，每次字典查找 → 热路径浪费。B: 只存引用，丢了找不回。 | Id 用于持久化/重建，缓存引用用于每帧读取——冗余但正确 |
| PropertyPresetSO 加 Prefab 字段 | A: EntityService 上维护 Preset→Prefab 映射表 → 配置分散。B: 命名约定 → 太脆。 | 一个资产定义一切：属性结构 + 初始值 + Prefab，内聚 |
| 事件用 GameEvent\<T\> 推模式 | A: EventChannelBase OnRaised (Action) → 无数据。B: C# event → 无 SO 可视化。 | GameEvent\<T\> 的 Raise(T) 推 payload，Subscribe 收数据，标准解耦模式 |

## Known Issues

- [ ] EntityService 未挂在 GameManager 上（P0 — 需手动在 Prefab 上加组件并配置 EventHub 通道）
- [ ] EntityService.Register 调用方未迁移——当前没有代码调用 Register（P0 — 等待 CharacterActor / ItemEntity 迁移）
- [ ] Identity.BindEntity 是 internal，依赖同 assembly 访问——如果将来拆 assembly 需要 InternalsVisibleTo（P2）
- [ ] PropertyPresetSO.Prefab 未在现有 SO 资产（Human.asset 等）中赋值（P1 — Unity Editor 手动步骤）
- [ ] GameEvent\<T\> SO 通道的 .asset 文件未创建——需在 Unity 中 CreateAssetMenu 生成（P1 — Unity Editor 步骤）
- [ ] EventChannelBase 和 GameEvent\<T\> 的关系讨论进行到一半——统一为推模式、EventChannelBase OnRaised 去留未定（P1 — 下次 session 继续）

## Cross-References

### Related Sessions
- [2026-06-26-entity-base-tree.md](2026-06-26-entity-base-tree.md) — Entity 基树的初步设计
- [2026-06-27-property-agent-removal.md](2026-06-27-property-agent-removal.md) — PropertyAgent 删除，PropertyTable 直读
- [2026-06-27-container-character-slots.md](2026-06-27-container-character-slots.md) — 容器系统，依赖 ItemEntity Id 引用

### Related Plans
- [plans/sorted-growing-tarjan.md](../plans/sorted-growing-tarjan.md) — 上轮 9 步方案（已回滚，架构方向已变）

### Related Tech Docs
- [tech/L2-services/L2-entity-service/entity-service.md](../tech/L2-services/L2-entity-service/entity-service.md) — EntityService 设计文档（本次重写）
- [tech/L2-services/L2-modules/L3-entity/README.md](../tech/L2-services/L2-modules/L3-entity/README.md) — L3 Entity 旧设计（需更新为 L2）

### Flag for Design Doc Creation
- [x] No design doc needed — 纯架构/数据层，无玩家可见变化。
