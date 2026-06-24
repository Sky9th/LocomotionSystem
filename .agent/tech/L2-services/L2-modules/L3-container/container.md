# Container\<T\> — 泛型容器 API

> `L3_Container/Container.cs` · 技术文档 · 2026-06-24
>
> 纯 C# 泛型类。不继承 MonoBehaviour，不需要 GameObject。

## 概述

`Container<T>` 是容器系统的核心运行时类。管理物品（或技能）的放置、取出、过滤和 Tick。

T 取值：
| T | 用途 | 示例 |
|---|------|------|
| `ItemInstance` | 物品容器 | 身体槽、背包、世界箱子 |
| `AbilityDefSO` | 技能槽 | Q/E/R/F 技能栏 |

## API

### 构造

```csharp
public Container(string containerId, SlotDef[] slotDefs)

// containerId: 容器唯一标识。如 "char_001/Backpack"
// slotDefs:   来自 ItemDefSO PropertyTree Struct 节点的槽位定义
```

构造时创建内部结构：
- `Slots: Dictionary<string, ContainerSlot>` — 按 SlotKey 索引
- `SlotsOrdered: List<ContainerSlot>` — 有序遍历
- 计算过滤规则、容量、重量限制

### 放置

```csharp
public bool CanAccept(string slotKey, T item)
// 检查 item 是否可放入 slotKey 槽位。
// 顺序: 容量检查 → Tag 过滤(AcceptTags) → 重量检查(WeightLimit)
// AcceptTags 为空 = 接受所有物品。

public bool Place(string slotKey, T item)
// 放入。先调 CanAccept，失败返回 false。
// 成功 → 更新 CurrentWeight 缓存。

public string FindSlotFor(T item)
// 找到第一个能接受该物品的槽位 SlotKey。没有返回 null。
```

### 取出

```csharp
public T Remove(string slotKey, string itemId)
// 按 itemId 从指定槽位移除。返回被移除的对象，未找到返回 null。
```

### Tick

```csharp
public void Tick(float dt)
// 遍历所有槽位的所有物品，逐调 item.Tick(dt)。
// 由容器所有者驱动（CharacterActor 60fps / WorldManager 0.5Hz）。
// 损坏物品（item.IsBroken）自动 Remove。
```

### 查询

```csharp
public IEnumerable<T> AllItems()
// 所有槽位中所有物品的枚举器。

public ContainerSlot GetSlot(string slotKey)
// 获取指定槽位的运行时状态。
```

### 嵌套

```csharp
public void MergeSlotsFrom(SlotDef[] subSlots, string prefix)
// 将子容器的槽位注入当前容器。
// prefix: 嵌套路径前缀，如 "Backpack" → 子槽位变为 "Backpack/Main"
```

## 内部结构

```
Container<T>
├── ContainerId: string               // "char_001/Backpack"
├── Slots: Dictionary<string, ContainerSlot>
│     └── ContainerSlot
│           ├── Def: SlotDef           // 静态配置
│           ├── Items: List<T>         // 当前容纳物品
│           └── CurrentWeight: float   // 总重缓存
├── SlotsOrdered: List<ContainerSlot>  // 有序遍历
├── CurrentTotalWeight: float          // 所有槽位总重
└── CarryWeightMax: float              // 容器承载上限
```

## ContainerSlot

单个槽位的运行时状态：

```csharp
public class ContainerSlot
{
    public SlotDef Def;                // 不可变配置
    public List<T> Items;              // 当前容纳（Capacity=1 时也走 List）
    public float CurrentWeight;        // 槽内总重

    public bool IsFull => Items.Count >= Def.Capacity;

    public bool CanAccept(T item);     // 过滤检查
    public bool Place(T item);         // 放入
    public T Remove(string itemId);    // 取出
}
```

## 过滤规则

1. **容量**：`Items.Count >= Def.Capacity` → 拒绝
2. **标签**：`item.ItemTags` ∩ `Def.AcceptTags` 非空 → 通过。`Def.AcceptTags` 为空 → 全通过
3. **重量**：`CurrentWeight + item.Weight > Def.WeightLimit` → 拒绝。`Def.WeightLimit = 0` → 跳过

## 耦合

| 依赖 | 方式 |
|------|------|
| `SlotDef` (L3_Container) | 构造参数 |
| `ItemInstance` (L3_Item) | T 实参，调 Tick/属性读值 |
| `ContainerResolver` (L2_ItemService) | 外部注册——Container 不感知 Resolver |
| `ItemRegistry` (L2_ItemService) | 不感知——Container 不知 Registry 存在 |

Container 是哑巴机制——只提供 Place/Remove/CanAccept，不关心物品从哪来到哪去。
