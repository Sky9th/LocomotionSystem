# L3_Container · 容器系统

> `L3_Container/` — 独立模块。通用容器抽象——管理物品的放置、取出、过滤和嵌套。身体装备槽、背包、技能栏共享同一套容器逻辑。

> **IMPLEMENTED** — 4 个核心文件已落地。Tag/重量过滤等 ItemInstance 依赖的功能为 TODO 桩。
>
> **Last Verified**: 2026-06-27 | **Verification**: CODE LANDED — `SlotDef.cs`, `ContainerSlot.cs`, `Container.cs`, `ContainerSlotRef.cs` 已创建

## 层级定位

独立 L3 模块，位于 `Services/Modules/L3_Container/`。比 `L3_Item` 更基础——物品系统定义"是什么"，容器系统回答"在哪"。

不是管线系统。没有 Update、没有 Tick（`Container.Tick` 由所有者驱动，容器自身不自主 Tick）。

## 核心

**容器是物品的"位置"。物品放进容器获得状态——放进身体槽=装备，放进背包=物品，放进技能槽=可用技能。**

"装备"不是物品的类型——是物品在特定容器中的状态。同一把刀，在身体槽是装备，在背包是物品。

## 架构

```
Container<T>
  ├── ContainerId: string              ← "char_001/Backpack/Main"
  ├── Slots: IReadOnlyDictionary<string, ContainerSlot<T>>
  │     └── ContainerSlot<T>
  │           ├── Def: SlotDef               ← 静态配置（来自 PropertyTree Struct 节点）
  │           ├── Items: List<T>             ← 当前容纳的物品
  │           └── CurrentWeight: float       ← 当前总重缓存
  ├── SlotsOrdered: IReadOnlyList<ContainerSlot<T>>
  ├── CurrentTotalWeight: float              ← 所有槽位总重
  ├── CarryWeightMax: float                  ← 容器承载上限
  │
  ├── CanAccept(slotKey, item) → bool
  ├── Place(slotKey, item) → bool
  ├── Remove(slotKey, item) → bool           ← 按引用
  ├── Remove(slotKey, itemId) → T            ← 按 ID
  ├── FindSlotFor(item) → string?
  ├── AllItems() → IEnumerable<T>
  ├── GetSlot(slotKey) → ContainerSlot<T>?
  │
  └── Tick(float dt)                         ← 空方法，容器所有者驱动（待 ItemInstance 接入）
```

## 模块文件

| 文件 | 类型 | 说明 |
|------|------|------|
| `SlotDef.cs` | `[Serializable]` `[PropertyStruct]` struct | 槽位静态定义——SlotId, AcceptTags, Capacity, WeightLimit |
| `ContainerSlot.cs` | 泛型 `class ContainerSlot<T>` | 单个槽位的运行时状态——Def, Items, CurrentWeight, CanAccept, Place, Remove |
| `Container.cs` | 泛型 `class Container<T>` | 容器核心——Slots 字典, 放置/取出/查询, Tick 驱动 |
| `ContainerSlotRef.cs` | `[Serializable]` struct | 轻量定位符——OwnerId + SlotKey，用于 ItemService 索引和跨容器 Transfer |

## ContainerSlotRef

**属于 L3_Container 模块**。轻量定位符——用于 L2_ItemService 索引和跨容器 Transfer。

```csharp
[Serializable]
public struct ContainerSlotRef
{
    public string OwnerId;   // 容器所有者的唯一 ID，格式: "{type}/{id}"
    public string SlotKey;   // 容器内槽位标识: "RightHand" / "Main" / "Backpack/Main"
}
```

**OwnerId 格式规范**：
| 容器类型 | 格式 | 示例 |
|---------|------|------|
| 角色容器 | `char/{netId}` | `char/NetId_001` |
| 世界箱子 | `world/{uniqueId}` | `world/chest_003` |
| 物品内嵌套容器 | `item/{instanceId}` | `item/a1b2c3d4` |

**SlotId vs SlotKey 区分**：
- `SlotDef.SlotId`：槽位在物品定义层的本地标识（`"Main"`, `"WeaponSling"`）。值仅在单个物品的 SlotDef[] 内唯一。
- `ContainerSlotRef.SlotKey`：槽位在容器树中的寻址路径。平级容器中等于 SlotId，嵌套容器中带前缀（`"Backpack/Main"`）。

不存 C# 对象引用——联机兼容。运行时由 `ContainerResolver.Resolve(ref)` 获取实际 `Container<T>`（远期）。

## 容器所有者与 Tick

容器不负责 Tick。**容器所有者**在 Update 中遍历容器物品并调用 `item.Tick(dt)`。不同所有者可以用不同频率。

```
CharacterActor.Update()           ← 每帧 Tick 装备槽和背包物品
WorldManager.Update()             ← 0.5Hz Tick 世界箱子中的物品
```

## 容器类型

所有容器都是 `Container<T>`。区别在创建方式、过滤和容量：

| 容器 | T | 来源 | 容量 | 过滤 |
|------|---|------|------|------|
| 右手/左手 | ItemInstance | CharacterActor 固定创建 | 1 | Weapon.*（按握法） |
| 头部/躯干/腿部 | ItemInstance | CharacterActor 固定创建 | 1 | Armor.* |
| 背包 | ItemInstance | 物品 PropertyTree (Struct 节点) | N | 由 SlotDef 定义 |
| 技能槽 Q/E/R/F | AbilityDefSO | CharacterActor 固定创建 | 1 | 武器Tag ∩ 技能树 |
| 世界箱子 | ItemInstance | WorldManager 创建 | N | 由箱子 PropertyTree (Struct 节点) 定义 |

## 嵌套容器

背包物品自身有子容器 → 嵌套：

```
CharacterActor
  └── Container (身体根容器)
        ├── "RightHand" → 太刀
        ├── "Backpack"  → Container (来自军用背包的 ItemDefSO.Slots)
        │     ├── "Main"          → 弹药盒、绷带...
        │     ├── "WeaponSling"   → 步枪
        │     └── "WaterPouch"    → 水袋
        └── "Armor"     → 防弹衣
```

嵌套通过 `MergeSlotsFrom(SlotDef[] subSlots, string prefix)` 实现——远期功能。

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| Container\<T\> | L3_Item（ItemInstance, ItemDefSO PropertyTree） | 从 PropertyTree Struct 节点读取 SlotDef[]，存放 ItemInstance |
| Container\<T\> | L3_Ability（AbilityDefSO） | 技能槽存放技能定义 |
| Container\<T\> | EquipmentComponent (L4_equipment) | EquipmentComponent 管理身体容器的创建和切换 |
| Container\<T\> | L2_ItemService | ItemService 通过 ContainerSlotRef 索引 + ContainerResolver 解析 |
| Container\<T\> | UI（装备栏/物品栏/技能栏） | 展示容器内容 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 泛型 Container\<T\> | 身体槽装 ItemInstance、技能槽装 AbilityDefSO——同一个结构，不同 T |
| 容器不负责 Tick | 不同容器所有者需要不同 Tick 频率。角色 60fps，箱子 0.5Hz |
| 过滤用 GameplayTag | 武器/护甲类型不需要枚举——Tag 匹配即可 |
| 嵌套容器 | 背包物品自身有子容器——MergeSlotsFrom 实现 |
| 装备是状态不是类型 | 物品在身体槽容器里 = 装备态。同一个 ItemDefSO |
| ContainerSlotRef 用字符串寻址 | C# 引用不可网络传输。字符串 ID 兼容联机 |
| SlotDef[] 走 PropertyType.Struct | JSON blob + C# struct 类型名关联。PropertyTree 最小改动 |
| IReadOnly 暴露内部集合 | 写操作只能通过 Container 方法，保证重量缓存一致性 |
| 构造错误 Log + skip，不抛异常 | 运行时健壮——一个坏 SlotDef 不应阻止容器创建 |

## 当前实现状态

| 组件 | 状态 | 说明 |
|------|:---:|------|
| `SlotDef` struct + `[PropertyStruct]` | 已完成 | 4 字段完整 |
| `ContainerSlot<T>` 核心结构 | 已完成 | Def, Items, CurrentWeight, IsFull, IsEmpty |
| `ContainerSlot<T>.Place/Remove` | 已完成 | 不含重量累计 |
| `Container<T>` 核心结构 | 已完成 | 构造 + Slots/SlotsOrdered + CanAccept/Place/Remove/FindSlotFor/AllItems/GetSlot |
| `ContainerSlotRef` struct | 已完成 | OwnerId + SlotKey，Serializable |
| Tag 过滤 (AcceptTags) | 待做 | ContainerSlot.CanAccept 中 TODO，等 ItemInstance.ItemTags |
| 重量检查 (WeightLimit) | 待做 | ContainerSlot.CanAccept 中 TODO，等 ItemInstance.Weight |
| 重量缓存累计/扣减 | 待做 | CurrentWeight/CurrentTotalWeight 声明但未更新 |
| `Container<T>.Tick` | 桩 | 空方法，等 ItemInstance.Tick(dt) |
| `Remove(string itemId)` | 桩 | ContainerSlot 层 LogWarning + default(T) |
| PropertyType.Struct 框架 | 待做 | PropertyTree 框架层改动 |
| SlotDef[] 从 PropertyTree 读取 | 待做 | GetStructArray + PropertyType.Struct |

## 已知缺口

| 缺口 | 状态 | 说明 |
|------|:---:|------|
| ItemInstance 类型落地 | 待做 | Tag/重量/Tick/Id 都依赖 ItemInstance |
| ContainerResolver 寻址 | 待做 | ContainerSlotRef → Container 解析器 |
| EquipmentComponent | 待做 | 身体容器创建和切换 |
| 嵌套容器 MergeSlotsFrom | 远期 | 前缀冲突处理 + 路径导航 |
| 武器握法（单手/双手/双持） | 远期 | 占用规则待详细定义 |
| 世界物品管理器 | 远期 | WorldItemManager 方案论证 |

## 子文档索引

| 文档 | 说明 |
|------|------|
| `slot-def.md` | SlotDef — 槽位静态定义 struct + `[PropertyStruct]` |
| `container-slot.md` | ContainerSlot\<T\> — 槽位运行时与过滤规则 |
| `container.md` | Container\<T\> — 泛型容器 API 详解 |
| `container-slot-ref.md` | ContainerSlotRef — 轻量槽位定位符 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| Container\<T\> 核心 | 已完成 | — |
| ContainerSlot\<T\> 核心 | 已完成 | — |
| SlotDef 结构体 | 已完成 | — |
| ContainerSlotRef 结构体 | 已完成 | — |
| Tag 过滤 + 重量检查 | 待做 | ItemInstance |
| Container\<T\>.Tick 实际实现 | 待做 | ItemInstance.Tick |
| ContainerResolver 寻址 | 待做 | Container\<T\> 完整 |
| EquipmentComponent | 待做 | Container\<T\> 完整 |
| PropertyType.Struct 框架 | 待做 | PropertyTree |
| 背包嵌套容器运行时 | 远期 | Container\<T\> + SlotDef[] + MergeSlotsFrom |
| 技能栏容器 | 远期 | Container\<T\> + AbilityTreeSO |
| 世界容器（箱子/地面） | 远期 | Container\<T\> + WorldManager |
