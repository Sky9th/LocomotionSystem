# L2_ItemService · 物品协调服务

> `L2_ItemService/` — L2 服务层。物品的全局身份索引和跨容器移动协调。
> 继承 `BaseService`，和 `L2_PlayerService`、`L2_Input` 平级。

> ⚠ **DRAFT** — 设计阶段。代码尚未创建。
>
> **Last Verified**: 2026-06-24 | **Verification**: DESIGN PHASE

## 层级定位

L2 服务，位于 `Services/L2_ItemService/`。L3_Item 和 L3_Container 的胶水层——两个 L3 模块互不发生运行时调用，ItemService 在 L2 做协调。

| 维度 | L3_Item | L3_Container | L2_ItemService |
|------|---------|-------------|----------------|
| 职责 | 物品定义 + 运行时实例 | 容器的 Place/Remove/过滤 | 物品身份索引 + 跨容器移动协调 |
| 依赖 | PropertyTree, PropertyPresetSO | ItemDefSO.SlotDef[] 读取 | L3_Item + L3_Container |
| 感知容器？ | 否 | 否（哑巴机制） | 是——通过 ContainerResolver |

## 为什么 L2 而不是 L3

- **跨 L3 模块协调**：Transfer 需要同时操作 L3_Container（Place/Remove）和更新索引（L3_Item 的数据）。
- **物品和容器互不认识**：Container 是哑巴——不知道 Registry 存在。Registry 不调 Container。需要一个 L2 协调者。
- **L2_EntitiesService 被否决**：Item 是纯 C# 数据对象，Character 是 MonoBehaviour 帧驱动实体——运行时本质不同。先落地 ItemService，以后有共性再抽。[[session]](../../../../sessions/2026-06-24-equipment-item-architecture.md)

## 架构

```
L2_ItemService (继承 BaseService)
│
├── ItemRegistry                    ← 物品身份索引
│     ├── _locationIndex: Dictionary<string, ContainerSlotRef>
│     ├── Track(itemId, slotRef)
│     ├── Untrack(itemId)
│     └── FindLocation(itemId) → ContainerSlotRef?
│
├── ContainerResolver               ← ownerId → Container 解析
│     ├── RegisterContainer(ownerId, container)
│     ├── UnregisterContainer(ownerId)
│     └── Resolve(slotRef) → Container<ItemInstance>?
│
└── Public API
      ├── Transfer(itemId, fromSlot, toSlot) → bool  ← 物品移动唯一入口
      ├── WhereIs(itemId) → ContainerSlotRef?  ← 查询物品位置
      ├── RegisterItem(item, slot)              ← 新物品注册
      └── UnregisterItem(itemId)                ← 物品销毁注销
```

## 调用链

### 物品创建

```
ItemInstance.Create(def)
  → ItemService.RegisterItem(item, slot)    ← L2 入口
    → ItemRegistry.Track(item.Id, slot)
    → ContainerResolver.Resolve(slot).Place(item)
```

### 物品移动

```
ItemService.Transfer(itemId, fromSlot, toSlot) → bool
  │
  ├── 1. var item = ContainerResolver.Resolve(fromSlot).Remove(fromSlot.SlotKey, itemId)
  │      ← 从源容器取出，返回 ItemInstance（L3_Container API）
  │
  ├── 2. ItemRegistry.Untrack(itemId)
  │      ← 清除旧索引
  │
  ├── 3. ContainerResolver.Resolve(toSlot).CanAccept(toSlot.SlotKey, item)?
  │      ← 目标容器过滤检查（L3_Container API）
  │      CanAccept 返回 false → Transfer 终止，回滚到步骤 1:
  │        ContainerResolver.Resolve(fromSlot).Place(fromSlot.SlotKey, item)
  │        ItemRegistry.Track(itemId, fromSlot)
  │
  ├── 4. ContainerResolver.Resolve(toSlot).Place(toSlot.SlotKey, item)
  │      ← 放入目标容器
  │      Place 失败 → 同步骤 3 回滚
  │
  └── 5. ItemRegistry.Track(itemId, toSlot)
         ← 更新索引
```

### Transfer 原子性

Transfer 是一个全成功或全回滚的原子操作。

**正常流程**（5 步）：
1. `var item = Resolve(fromSlot).Remove(fromSlot.SlotKey, itemId)` — 取出，返回 ItemInstance
2. `Untrack(itemId)` — 清除旧索引
3. `Resolve(toSlot).CanAccept(toSlot.SlotKey, item)?` — 过滤检查
4. `Resolve(toSlot).Place(toSlot.SlotKey, item)` — 放入
5. `Track(itemId, toSlot)` — 更新索引

**CanAccept 失败**（步骤 3）：源容器未动（步骤 1 已 Remove），回滚 = `Place(fromSlot.SlotKey, item)` + `Track(itemId, fromSlot)`。

**Place 失败**（步骤 4）：目标容器过滤通过但实际写入失败，回滚同上。

**并发保护**：Transfer 期间物品处于"移动中"状态。同一 itemId 不能同时执行两个 Transfer。物品销毁（UnregisterItem）需等待 Transfer 完成。

**源容器无此物品**（步骤 1 Remove 返回 null）：Transfer 返回 false，不触发回滚。

### 物品查询

```
ItemService.WhereIs(itemId) → ContainerSlotRef?
  → ItemRegistry.FindLocation(itemId)   ← O(1) 字典查找
```

### 物品销毁

```
ItemInstance.Destroy()
  → ItemService.UnregisterItem(itemId)
    → ItemRegistry.Untrack(itemId)
    → 所在容器.Remove(itemId)
```

## ContainerSlotRef

**定义在 L3_Container**——轻量定位符 struct（`OwnerId: string` + `SlotKey: string`）。不存对象引用，联机兼容。

OwnerId 格式规范（见 L3_Container 文档）：
- 角色：`char/{netId}`
- 世界箱子：`world/{uniqueId}`
- 嵌套容器：`item/{instanceId}`

L2_ItemService 消费 ContainerSlotRef——用于 ItemRegistry 索引和 Transfer 寻址。运行时由 `ContainerResolver.Resolve(ref)` 获取实际容器。

## ContainerResolver

容器注册和解析——L2 级全局容器索引：

```csharp
public class ContainerResolver
{
    private Dictionary<string, Container<ItemInstance>> _containers;

    public void RegisterContainer(string ownerId, Container<ItemInstance> container);
    public void UnregisterContainer(string ownerId);
    public Container<ItemInstance> Resolve(ContainerSlotRef ref);
    public Container<ItemInstance> Resolve(string ownerId);
}
```

容器所有者（CharacterActor、WorldManager）在创建 Container 后向 Resolver 注册。

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| ItemService | L3_Item（ItemInstance, ItemDefSO） | 管理 ItemInstance 的生命周期索引 |
| ItemService | L3_Container（Container\<T\>, ContainerSlotRef, SlotDef） | Transfer 调 Container API，ContainerSlotRef 寻址，SlotDef 过滤校验 |
| ItemService | EquipmentComponent（L4） | 装备切换触发 Transfer |
| ItemService | WorldManager（L2） | 世界容器注册/注销 |
| ItemService | UI（装备栏/物品栏） | 查询物品位置和状态 |
| ItemService | 多人服务端（远期） | 物品移动权威验证 |

## 设计决策

| 决策 | 原因 |
|------|------|
| **L2 而非 L3** | Transfer 跨 L3_Item + L3_Container，需要 L2 协调层 |
| **L2_ItemService 而非 L2_EntitiesService** | Item 纯数据 vs Character 帧驱动——运行时本质不同。先细化后抽象 |
| **Container 不感知 Registry** | ItemRegistry 只存身份索引。Container 只管 Place/Remove——单向依赖 |
| **ContainerSlotRef 用 ownerId + slotKey** | 字符串寻址——联机兼容，不存 C# 引用 |
| **Transfer 原子化 + 回滚** | Place 失败 → 重新放回 fromSlot + 恢复索引。保证数据一致性 |
| **ContainerResolver 在 ItemService** | 容器注册/注销的唯一入口，和物品索引同生命周期 |
| **两字典事务一致性** | RegisterItem 先确认容器已注册 → 再写 `_locationIndex` 和容器。原子操作保证一致性 |

## 边界情况

| 情况 | 处理策略 |
|------|---------|
| RegisterItem 时容器尚未注册 | 拒绝注册，记录警告。调用方须先注册容器再注册物品 |
| Transfer 时目标容器已满 | CanAccept 返回 false，Transfer 返回 false，不触发回滚（fromSlot 未动） |
| Transfer 时源容器无此物品 | Remove 返回 null，Transfer 返回 false |
| 物品销毁时已在 Transfer 中 | 由 Transfer 原子性保证：未完成 Transfer 的物品不被 Destroy |

纯 C# 模块。ItemService 继承 BaseService，在 GameContext 中初始化。

## 目录结构

```
L2_ItemService/
├── ItemRegistry.cs           # 物品身份索引
├── ContainerResolver.cs      # ownerId → Container 解析
└── ItemService.cs            # 协调服务 — Bootstrap 注册、Transfer API
```

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| ItemService 代码实现 | 待做 | L3_Item + L3_Container 运行时实现 |
| ContainerResolver 代码 | 待做 | Container\<T\> 运行时实现 |
| Transfer 回滚逻辑 | 待做 | Container.Place/Remove API |
| 多人联机验证 | 远期 | 网络同步层 |
