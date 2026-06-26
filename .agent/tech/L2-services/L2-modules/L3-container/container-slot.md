# ContainerSlot\<T\> -- 槽位运行时

> `L3_Container/ContainerSlot.cs` · `namespace RedDust.Container` · 泛型 class，无 MonoBehaviour
>
> **Last Verified**: 2026-06-27 | **Verification**: CODE LANDED -- 核心结构已实现，Tag/重量过滤等 ItemInstance 到位后接入

## 定位

单个容器槽位的**运行时状态**。由 `Container<T>` 持有和管理，负责容量检查、物品放入/取出。不感知外部世界——不知道 ItemRegistry、不知道 ContainerResolver。

泛型参数 `T` 使同一套槽位逻辑适用于：
- `ItemInstance` -- 物品容器（身体槽、背包、世界箱子）
- `AbilityDefSO` -- 技能槽（Q/E/R/F 技能栏）

## 公共属性

```csharp
public SlotDef Def { get; }                  // 不可变配置（构造注入）
public List<T> Items { get; } = new();        // 当前容纳的物品列表
public float CurrentWeight { get; private set; } // 槽内物品总重缓存
public bool IsFull  => Items.Count >= Def.Capacity;  // 容量满
public bool IsEmpty => Items.Count == 0;             // 空槽
```

## 构造

```csharp
public ContainerSlot(SlotDef def)
```

- `def` 注入后不可变。`SlotDef` 是 struct，按值拷贝。
- `Items` 初始化为空 `List<T>`。
- `CurrentWeight` 初始为 0。

## 方法

### CanAccept

```csharp
public bool CanAccept(T item)
```

检查顺序：
1. **存在性** -- `item == null` → `false`
2. **容量** -- `IsFull`（`Items.Count >= Def.Capacity`）→ `false`
3. **Tag 过滤**（TODO） -- `item.ItemTags` 与 `Def.AcceptTags` 交集非空。`AcceptTags` 空数组 = 全接受
4. **重量检查**（TODO） -- `CurrentWeight + item.Weight > Def.WeightLimit` → `false`。`WeightLimit = 0` 跳过

步骤 3、4 在当前代码中以 TODO 注释标记，等 `ItemInstance` 类型落地后接入。

### Place

```csharp
public bool Place(T item)
```

放入物品。先调 `CanAccept`，失败返回 `false`。成功：
- `Items.Add(item)`
- TODO：`CurrentWeight += item.Weight`

### Remove (by reference)

```csharp
public bool Remove(T item)
```

按 C# 对象引用移除。找到并移除返回 `true`，未找到返回 `false`。
- TODO：移除成功后 `CurrentWeight -= item.Weight`

### Remove (by itemId)

```csharp
public T Remove(string itemId)
```

按 `itemId` 字符串移除。**当前未实现**——`T` 无 `Id` 属性（ItemInstance 尚未落地）。当前实现输出 `Debug.LogWarning` 并返回 `default`。

等 ItemInstance 到位后改为按 `item.Id` 匹配移除。

## 内部机制

### 重量缓存

`CurrentWeight` 是缓存值，不在每次查询时遍历计算。`Place` 时累加，`Remove` 时扣减。性能考量：Place/Remove 是低频操作（每秒几次），不比遍历计算，但保持设计中约定"缓存而非遍历"的一致性。

### 容量基数

`Capacity` 最小值为 1（`[Min(1)]` 约束在 SlotDef 上）。`IsFull` 在 `Items.Count >= Capacity` 时返回 true。`Capacity = 1` 时槽位退化为单物品槽（如武器槽、身体装备槽），`Items` 仍然是 `List<T>` 而非单个 `T`——不做特殊 case。

## 调用链

```
Container<T>.Place(slotKey, item)
  → _slots[slotKey].CanAccept(item)   // 过滤检查
    → 存在性 → 容量 → Tag(待) → 重量(待)
  → _slots[slotKey].Place(item)       // 实际放入
    → Items.Add(item)

Container<T>.Remove(slotKey, item)
  → _slots[slotKey].Remove(item)      // 按引用移除
    → Items.Remove(item) → CurrentWeight 扣减(待)

Container<T>.Remove(slotKey, itemId)
  → _slots[slotKey].Remove(itemId)    // 按 ID 移除(待)
    → 遍历 Items 匹配 item.Id → 移除 → CurrentWeight 扣减(待)
```

## 耦合模块

| 模块 | 关系 | 方向 |
|------|------|------|
| `SlotDef` (L3_Container) | 构造注入 `Def`，用于容量/过滤/重量检查 | 依赖 |
| `Container<T>` (L3_Container) | 持有 `Dictionary<string, ContainerSlot<T>>` 和 `List<ContainerSlot<T>>` | 被持有 |
| `ItemInstance` (L3_Item) | `T` 实参。CanAccept/Place/Remove 需要读 `ItemTags`、`Weight`、`Id` | 远期类型依赖 |
| `GameplayTagDefinitionSO` (L1_Core) | 通过 `SlotDef.AcceptTags` 间接使用，Tag 交集匹配 | 间接依赖 |

## 当前实现状态

| 组件 | 状态 | 说明 |
|------|:---:|------|
| 泛型定义 `ContainerSlot<T>` | 已完成 | class，无 MonoBehaviour |
| `Def` / `Items` / `CurrentWeight` | 已完成 | 属性完整 |
| `IsFull` / `IsEmpty` | 已完成 | 计算属性 |
| 构造 `ContainerSlot(SlotDef)` | 已完成 | 注入 def |
| `CanAccept` 存在性+容量 | 已完成 | 前两步过滤 |
| `CanAccept` Tag 过滤 | 待做 | TODO 注释，等 ItemInstance.ItemTags |
| `CanAccept` 重量检查 | 待做 | TODO 注释，等 ItemInstance.Weight |
| `Place` | 已完成 | 不含重量累计 |
| `Remove(T)` 按引用 | 已完成 | 不含重量扣减 |
| `Remove(string)` 按 ID | 桩 | `Debug.LogWarning` + 返回 `default` |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| ItemInstance 类型接入 | 待做 | L3_Item.ItemInstance |
| Tag 过滤逻辑启用 | 待做 | ItemInstance.ItemTags + GameplayTag |
| 重量累计/扣减启用 | 待做 | ItemInstance.Weight |
| `Remove(string itemId)` 实现 | 待做 | ItemInstance.Id |
