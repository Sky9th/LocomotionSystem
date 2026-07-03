# Entity — 实体数据模型

> `L2_EntityService/Entity.cs` · 纯 C# 数据类
> **Last Verified**: 2026-07-03 | **Verification**: +Command/Query/View/HasView/StackCount 属性未在文档中体现。

## 调用链

```
调用方:
  EntityService.Spawn        → 查找 Entity + 读 Preset.Prefab + Properties
  EntityService.Register     → 存入 _entities 字典
  SaveService (未来)          → 遍历 EntityService.All → 序列化 Properties
  CharacterActor            → 通过 Identity 组件持有 Entity 引用（已实现）
  ItemEntity.Create (未来)    → new Entity(id, preset)

调谁:
  PropertyTable.FromPreset   → 构造时从 Preset 创建 Properties
  PropertyPresetSO.Prefab    → Spawn 时读 Prefab 引用
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | PropertyPresetSO | Preset 字段 |
| 依赖 | PropertyTable | Properties 字段 |
| 被消费 | EntityService | 管理对象 |
| 被消费 | Container (L3) | 通过 NestedContainer 引用嵌套容器 |
| 被消费 | Identity          → BindEntity 提供 Id（已实现） |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Id` | `string { get; }` | 持久标识，存档/联机引用锚点。null → Guid 兜底 |
| `Preset` | `PropertyPresetSO { get; }` | 属性预设资产，定义模板+初始值+实体种类（= EntityType） |
| `Properties` | `PropertyTable { get; }` | 运行时属性数据，与 Preset 共享结构 |
| `NestedContainer` | `Container.Container { get; internal set; }` | 嵌套容器。容器类实体（背包等）Register 时由 EntityService 自动创建，非容器实体为 null |
| `Command` | `EntityCommandModule { get; }` | ⚠️ 文档缺失 — 实体命令模块（Command/Query 架构） |
| `Query` | `EntityQueryModule { get; }` | ⚠️ 文档缺失 — 实体查询模块（Inventory/Equipment/Ability/Vitals 子查询） |
| `StackCount` | `int { get; set; }` | ⚠️ 文档缺失 — 堆叠数量 |
| `View` | `GameObject { get; set; }` | ⚠️ 文档缺失 — GO 视图引用（原 `_views` Dictionary 已删除） |
| `HasView` | `bool { get; }` | ⚠️ 文档缺失 — 是否有活跃 GO 视图 |

## 方法

### Entity()
```csharp
public Entity(string id, PropertyPresetSO preset)
```
- **Purpose**: 构造实体。Id 为 null 时自动生成 Guid
- **Params**: `id` — 持久标识（可传 Guid 或预制名）；`preset` — 属性预设（决定类型+模板+Prefab）
- **Notes**: Properties 在构造时即创建（`PropertyTable.FromPreset(preset)`），失败返回 null 由调用方检查

### Tick()
```csharp
public void Tick(float dt) => Properties?.Tick(dt);
```
- **Purpose**: 每帧驱动属性变化（modifier 衰减等）。只 Tick Properties，不管容器递归
- **Callers**: Container.Tick 遍历 AllItems 时调用。嵌套 NestedContainer 由 Container.Tick(depth+1) 驱动

## 未来规划

| 计划 | 状态 | 来源 |
|------|------|------|
| 存档序列化 | 待定 | Entity 不挂 Save/Load — 由 SaveService 按 Preset 类型分发 |
| ItemEntity 继承 | 待建 | ItemEntity : Entity，追加 Def/Count/Weight/ItemTags |
| CharacterActor 持有 | 待迁移 | Actor 持有 `_entityId` + `_entityCache` |
