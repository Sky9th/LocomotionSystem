# Identity — 实体身份组件

> `L3_Identity/Identity.cs` · MonoBehaviour，L3 模块
> **Last Verified**: 2026-06-27 | **Verification**: All referenced files exist, signatures match code.

## 层级定位

L3 独立模块。挂在一切实体 GO（Actor、GroundItem 等）的根节点上，提供 GO 侧身份标识。

不依赖任何具体实体类型——只持有 Id 字符串和设计层标签。

## 调用链

```
调用方:
  EntityService.Spawn     → Identity.BindEntity(entityId)
  AI / 过滤 / UI 系统     → 读 Tags 做阵营/物种判断
  Actor                   → 读 EntityId 缓存 Entity 引用

调谁:
  GameplayTagContainer    → Tags.AddTag / RemoveTag
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GameplayTagContainer | Tags 运行时标签集合 |
| 被消费 | EntityService | Spawn 时 BindEntity |
| 被消费 | AI/UI 系统 | 读 Tags 做类型判断 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `EntityId` | `string { get; }` | EntityService 注册表中的数据锚点。空 = 未绑定 |
| `Tags` | `GameplayTagContainer { get; }` | 设计身份标签（物种、阵营等），运行时可变 |

## 方法

### BindEntity()
```csharp
internal void BindEntity(string entityId)
```
- **Purpose**: 绑定到 EntityService 中的数据。由 EntityService.Spawn 调用
- **Notes**: `internal` — 同一 assembly 内 EntityService 可调用；外部不可改绑定

### Awake()
```csharp
private void Awake()
```
- **Purpose**: 加载 Inspector 中设置的 `initialTags` 到 Tags 容器

## 未来规划

| 计划 | 状态 | 来源 |
|------|------|------|
| 反查 Entity 引用 | 待定 | 加 `GetEntity()` 便捷方法 → `EntityService.Get(EntityId)` |
| Editor 可视化 | 待定 | Inspector 显示绑定状态 + Entity 数据摘要 |
