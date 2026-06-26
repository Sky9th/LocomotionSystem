# SlotDef -- 槽位定义结构体

> `L3_Container/SlotDef.cs` · `namespace RedDust.Container` · `[Serializable]` `[PropertyStruct]`
>
> **Last Verified**: 2026-06-27 | **Verification**: CODE LANDED -- 结构体已定义，`[PropertyStruct]` attribute 已落地

## 定位

容器槽位的**静态定义**——描述一个独立容纳空间的约束。通过 `[PropertyStruct]` attribute 标记，远期由 PropertyTree 的 `PropertyType.Struct` 存储和反序列化。

归属 L3_Container——Container 是 SlotDef 的主要运行时消费者，PropertyTree 仅负责存储。

## 数据结构

```csharp
[Serializable]
[PropertyStruct]
public struct SlotDef
{
    public string SlotId;                        // 槽位标识，同一容器内唯一
    public GameplayTagDefinitionSO[] AcceptTags; // 接受标签。空数组 = 接受所有物品
    [Min(1)] public int Capacity;                // 物品数量上限
    [Min(0f)] public float WeightLimit;          // 槽内总重量上限。0 = 无限制
}
```

## 字段说明

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `SlotId` | `string` | `""` | 槽位标识，当前容器内唯一。如 `"Main"`, `"RightHand"`, `"WeaponSling"` |
| `AcceptTags` | `GameplayTagDefinitionSO[]` | `null` | 此槽位接受什么类型的物品。匹配候选物品的 ItemTags。空数组 = 接受所有物品 |
| `Capacity` | `int` | `0` | 槽位容量（物品数量上限）。`[Min(1)]` 约束最低 1 |
| `WeightLimit` | `float` | `0f` | 槽内物品总重量上限。`0` = 无限制。`[Min(0f)]` 约束非负 |

## `[PropertyStruct]` Attribute

`SlotDef` 标记了 `[PropertyStruct]` attribute（定义在 `RedDust.Properties`），用于：

- **标记**：此 struct 可通过 PropertyTree 的 `PropertyType.Struct` 存储和反序列化
- **桥接**：`StructTypeName` 字符串关联到此 C# struct 类型，编辑器通过反射渲染字段
- **反序列化**：运行时 `JsonUtility.FromJson<SlotDef>(json)` 将 JSON 还原为类型安全的结构体

SlotDef 本身仍是一个普通 C# struct——编译期类型安全、Inspector 校验逻辑都在 struct 里。PropertyTree 只负责存储 JSON 数据。

## 调用链

```
ItemDefSO.PropertyTree (PropertyType.Struct 节点, StructTypeName="SlotDef")
  → bag.GetStructArray<SlotDef>("Container/Slots")
    → JsonUtility.FromJson<SlotDef>(json) 反序列化
      → foreach def → new ContainerSlot<T>(def) → Container<T>._slots[def.SlotId]
```

SlotDef 不主动调用任何模块——它是纯数据 struct，被动消费。

## 耦合模块

| 模块 | 关系 | 方向 |
|------|------|------|
| `PropertyTree` (L3_Properties) | PropertyType.Struct 存储 SlotDef 数组。远期实现 | 被 PropertyTree 持有 |
| `Container<T>` (L3_Container) | 构造时遍历 SlotDef[] 创建 ContainerSlot | 消费方 |
| `ContainerSlot<T>` (L3_Container) | 持有 `SlotDef Def` 引用，用于 CanAccept 过滤 | 消费方 |
| `GameplayTagDefinitionSO` (L1_Core) | AcceptTags 类型。空数组表示全接受 | 类型依赖 |

## 当前实现状态

| 组件 | 状态 | 说明 |
|------|:---:|------|
| `SlotDef` struct | 已完成 | `[Serializable]` `[PropertyStruct]` 完整字段 |
| `[PropertyStruct]` attribute | 已完成 | 定义在 `RedDust.Properties` |
| `PropertyType.Struct` 枚举 | 待做 | PropertyTree 框架层改动 |
| PropertyTree 存储/反序列化 | 待做 | `GetStructArray<T>()` 方法 |
| `AcceptTags` 过滤逻辑 | 待做 | `ContainerSlot.CanAccept` 中 TODO，等 ItemInstance 到位 |
| `WeightLimit` 检查 | 待做 | `ContainerSlot.CanAccept` 中 TODO，等 ItemInstance.Weight |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| PropertyType.Struct 框架落地 | 待做 | PropertyTree 框架 |
| SlotDef 数据从 PropertyTree 读取 | 待做 | PropertyType.Struct + GetStructArray |
| ItemDefSO 移除零散字段，统一走 PropertyTree | 远期 | 以上全部 |
