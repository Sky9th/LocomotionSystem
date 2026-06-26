# Container\<T\> -- 泛型容器

> `L3_Container/Container.cs` · `namespace RedDust.Container` · 泛型 class，无 MonoBehaviour
>
> **Last Verified**: 2026-06-27 | **Verification**: CODE LANDED -- 核心结构已实现，ItemInstance 到位后接入 Tick/重量跟踪

## 定位

`Container<T>` 是容器系统的核心运行时类——管理物品（或技能）的放置、取出、过滤和 Tick。是"哑巴机制"——只提供 Place/Remove/CanAccept，不关心物品从哪来到哪去。

泛型参数 `T` 使同一套容器逻辑适用于：
| T | 用途 | 示例 |
|---|------|------|
| `ItemInstance` | 物品容器 | 身体槽、背包、世界箱子 |
| `AbilityDefSO` | 技能槽 | Q/E/R/F 技能栏 |

容器不负责 Tick——**容器所有者**在 Update 中调用 `Container.Tick(dt)`。不同所有者可以用不同频率：
```
CharacterActor.Update()   ← 每帧 Tick 装备槽和背包物品
WorldManager.Update()     ← 0.5Hz Tick 世界箱子中的物品
```

## 公共属性

```csharp
public string ContainerId { get; }                                  // 容器唯一标识，如 "char_001/Backpack"
public IReadOnlyDictionary<string, ContainerSlot<T>> Slots { get; } // 按 SlotKey 索引的槽位表（只读）
public IReadOnlyList<ContainerSlot<T>> SlotsOrdered { get; }        // 有序槽位列表（按构造顺序）
public float CurrentTotalWeight { get; private set; }               // 所有槽位物品总重
public float CarryWeightMax { get; }                                // 容器承载重量上限。0 = 无限制
```

内部字段：
```csharp
private readonly Dictionary<string, ContainerSlot<T>> _slots = new();
private readonly List<ContainerSlot<T>> _slotsOrdered = new();
```

`Slots` 和 `SlotsOrdered` 公开 IReadOnly 接口，写操作只能通过 Container 方法。

## 构造

```csharp
public Container(string containerId, SlotDef[] slotDefs, float carryWeightMax = 0f)
```

- `containerId`：容器唯一标识。如 `"char_001/Backpack"`、`"world/chest_003"`
- `slotDefs`：来自 ItemDefSO PropertyTree Struct 节点的槽位定义数组
- `carryWeightMax`：容器承载重量上限。`0` = 无限制

构造行为：
- `slotDefs == null || Length == 0` → 创建空容器（`_slots` 空），不抛异常
- `SlotId` 为空 → `Debug.LogError` + skip
- `SlotId` 重复 → `Debug.LogError` + skip（不抛异常，保持健壮）
- 正常 def → `new ContainerSlot<T>(def)`，按 `SlotId` 加入 `_slots`，按顺序加入 `_slotsOrdered`

## 方法

### CanAccept

```csharp
public bool CanAccept(string slotKey, T item)
```

检查 `item` 是否可放入 `slotKey` 槽位。
- `slotKey` 不存在 → `false`
- 存在 → 转发到 `ContainerSlot<T>.CanAccept(item)`

委托给 ContainerSlot 做实际的容量/Tag/重量检查。

### Place

```csharp
public bool Place(string slotKey, T item)
```

放入物品。流程：
1. `slotKey` 解析（`_slots.TryGetValue`）——不存在返回 `false`
2. 转发到 `slot.Place(item)`——内部调 `CanAccept`，失败返回 `false`
3. 成功 → TODO：`CurrentTotalWeight += item.Weight`

### Remove (by itemId)

```csharp
public T Remove(string slotKey, string itemId)
```

按 `itemId` 从指定槽位移除。
- `slotKey` 不存在 → 返回 `default`
- 转发到 `slot.Remove(itemId)` → 返回被移除的对象
- 成功移除后 TODO：`CurrentTotalWeight -= item.Weight`

当前 `Remove(string)` 在 ContainerSlot 层是桩（等 ItemInstance 到位）。

### Remove (by reference)

```csharp
public bool Remove(string slotKey, T item)
```

按 C# 引用移除物品。
- `slotKey` 不存在 → `false`
- 转发到 `slot.Remove(item)` → 返回值是移除成功/失败
- 成功移除后 TODO：`CurrentTotalWeight -= item.Weight`

### FindSlotFor

```csharp
public string FindSlotFor(T item)
```

找到第一个能接受该物品的槽位 `SlotKey`。遍历 `_slotsOrdered`（保持插入顺序），逐个调 `slot.CanAccept(item)`。没有找到返回 `null`。

### AllItems

```csharp
public IEnumerable<T> AllItems()
```

所有槽位中所有物品的延迟枚举器。双层 `foreach`（槽 → 物品），`yield return`。不创建中间集合。

### GetSlot

```csharp
public ContainerSlot<T> GetSlot(string slotKey)
```

获取指定槽位运行时状态。`slotKey` 不存在返回 `null`。`TryGetValue` 失败时 `out` 为 default(null)。

### Tick

```csharp
public void Tick(float dt)
```

遍历所有槽位的所有物品，逐调 `item.Tick(dt)`。由容器所有者驱动。

**当前实现是空方法**——`T` 无 `Tick` 约束/接口，等 ItemInstance 到位后接入：
```csharp
// TODO ItemInstance 到位后:
// foreach (var item in AllItems())
//     item.Tick(dt);
```

## 内部结构

```
Container<T>
├── ContainerId: string                        // "char_001/Backpack"
├── _slots: Dictionary<string, ContainerSlot<T>>  // 按 SlotKey 索引
│     └── ContainerSlot<T>
│           ├── Def: SlotDef                   // 不可变静态配置
│           ├── Items: List<T>                 // 当前容纳物品
│           └── CurrentWeight: float           // 槽内总重缓存
├── _slotsOrdered: List<ContainerSlot<T>>      // 按构造顺序的有序引用
├── CurrentTotalWeight: float                  // 所有槽位总重
└── CarryWeightMax: float                      // 容器承载上限
```

## 调用链

```
CharacterActor.Update()
  → Container<T>.Tick(dt)                    // 容器所有者驱动 Tick（待实现）
    → foreach slot → foreach item → item.Tick(dt)

PlayerDirector.ProcessEquipInput()
  → Container<T>.CanAccept("RightHand", item)  // 检查是否可装备
    → _slots["RightHand"].CanAccept(item)      // 容量 → Tag → 重量
  → Container<T>.Place("RightHand", item)      // 放入
    → _slots["RightHand"].Place(item) → Items.Add(item) → 重量累加(待)

ItemService.Transfer()
  → Container<T>.Remove("Backpack", itemId)    // 从源容器取出
  → Container<T>.FindSlotFor(item)             // 找目标槽位
  → Container<T>.Place(targetSlot, item)       // 放入目标容器
```

## 耦合模块

| 模块 | 关系 | 方向 |
|------|------|------|
| `SlotDef` (L3_Container) | 构造参数，创建 ContainerSlot | 依赖 |
| `ContainerSlot<T>` (L3_Container) | 持有 `_slots` + `_slotsOrdered` | 持有 |
| `ItemInstance` (L3_Item) | `T` 实参。Tick/属性读值/重量 | 远期类型依赖 |
| `AbilityDefSO` (L3_Ability) | `T` 实参。技能槽容器 | 类型依赖 |
| `ContainerResolver` (L2_ItemService) | 外部注册——Container 不感知 Resolver | 被索引 |
| `ItemRegistry` (L2_ItemService) | 不感知——Container 不知 Registry 存在 | 无直接耦合 |
| `EquipmentComponent` (L3_Character) | 管理身体容器的创建和切换 | 消费方 |
| UI（装备栏/物品栏/技能栏） | 展示容器内容 | 消费方 |

## 当前实现状态

| 组件 | 状态 | 说明 |
|------|:---:|------|
| 泛型定义 `Container<T>` | 已完成 | class，无 MonoBehaviour |
| 构造 + SlotId 校验 | 已完成 | 空/重复 SlotId → LogError + skip |
| `CanAccept` / `Place` / `Remove` | 已完成 | 核心放置/取出逻辑 |
| `FindSlotFor` / `AllItems` / `GetSlot` | 已完成 | 查询方法完整 |
| `Slots` / `SlotsOrdered` IReadOnly 暴露 | 已完成 | 写保护 |
| `Tick` | 桩 | 空方法，等 ItemInstance 接入 |
| 重量跟踪 | 待做 | `CurrentTotalWeight` 声明但未累计 |
| `Remove(string itemId)` 链路 | 待做 | Container 层调用 ContainerSlot 桩 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| `Tick` 实现 | 待做 | ItemInstance.Tick(dt) |
| `CurrentTotalWeight` 累计/扣减 | 待做 | ItemInstance.Weight |
| `Remove(string itemId)` 完整链路 | 待做 | ItemInstance.Id |
| 嵌套容器 `MergeSlotsFrom` | 远期 | SlotDef[] 读取 + 前缀管理 |
| 容器所有者集成（CharacterActor/WorldManager） | 待做 | Container<T> 完整 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 泛型 `Container<T>` | 身体槽装 ItemInstance、技能槽装 AbilityDefSO——同一个结构，不同 T |
| 容器不负责 Tick | 不同容器所有者需要不同 Tick 频率。角色 60fps，箱子 0.5Hz |
| IReadOnly 暴露内部集合 | 写操作只能通过 Container 方法，保证重量缓存一致性 |
| 构造时错误 Log + skip，不抛异常 | 运行时健壮——一个坏 SlotDef 不应阻止容器创建 |
| `Remove` 参数重载 (string/T) | 外部按 ID 移除（联机+序列化），内部可按引用移除（省查找） |
