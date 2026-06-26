# ContainerSlotRef -- 槽位定位符

> `L3_Container/ContainerSlotRef.cs` · `namespace RedDust.Container` · `[Serializable]` struct
>
> **Last Verified**: 2026-06-27 | **Verification**: CODE LANDED -- 结构体已定义

## 定位

轻量槽位定位符——用于 L2_ItemService 索引和跨容器 Transfer。存字符串 ID 而非 C# 对象引用，兼容网络传输和序列化。

不存 C# 对象引用——联机兼容。运行时由 `ContainerResolver.Resolve(ref)` 获取实际 `Container<T>`（远期）。

## 数据结构

```csharp
[Serializable]
public struct ContainerSlotRef
{
    public string OwnerId;  // 容器所有者的唯一 ID
    public string SlotKey;  // 容器内槽位标识
}
```

## 字段说明

### OwnerId

容器所有者的唯一标识。格式规范：

| 容器类型 | 格式 | 示例 |
|---------|------|------|
| 角色容器 | `char/{netId}` | `char/NetId_001` |
| 世界箱子 | `world/{uniqueId}` | `world/chest_003` |
| 物品内嵌套容器 | `item/{instanceId}` | `item/a1b2c3d4` |

### SlotKey

容器内槽位的寻址路径：

- **平级容器**中等于 `SlotId`（如 `"RightHand"`, `"Main"`）
- **嵌套容器**中带路径前缀（如 `"Backpack/Main"`, `"Backpack/WeaponSling"`）

SlotKey 命名规范：
| 用途 | 示例 |
|------|------|
| 身体槽 | `"RightHand"`, `"LeftHand"`, `"Head"`, `"Torso"`, `"Legs"` |
| 背包/箱子槽 | 与 SlotId 相同（如 `"Main"`, `"WeaponSling"`） |
| 嵌套路径 | `{parentSlotKey}/{childSlotId}`（如 `"Backpack/Main"`） |

## SlotId vs SlotKey 区分

| 概念 | 定义 | 唯一性范围 |
|------|------|-----------|
| `SlotDef.SlotId` | 槽位在物品定义层的本地标识 | 单个物品的 SlotDef[] 内唯一 |
| `ContainerSlotRef.SlotKey` | 槽位在容器树中的寻址路径 | 全局（可通过路径唯一定位） |

## 与 ContainerId 的关系

`Container<T>.ContainerId` 是容器自标识（如 `"char_001/Backpack"`）。`ContainerSlotRef.OwnerId` 是容器所有者的寻址 key。

所有者可持有多个 Container（如角色背包装备了嵌套子容器）。`ContainerResolver` 以 `OwnerId` 为 key 映射到**根容器**，嵌套容器通过根容器路径访问。

## 调用链

```
L2_ItemService.FindItem(ContainerSlotRef ref)
  → ContainerResolver.Resolve(ref.OwnerId)   // 获取根容器
    → 如果 ref.SlotKey 含 '/' → 沿路径下钻嵌套容器
    → 定位到目标 ContainerSlot<T> → 返回 Items 中的匹配项
```

`ContainerSlotRef` 自身是纯数据 struct——不包含任何方法，不持有任何引用。

## 耦合模块

| 模块 | 关系 | 方向 |
|------|------|------|
| `L2_ItemService` | 通过 ContainerSlotRef 索引物品位置 | 消费方 |
| `ContainerResolver` (L2_ItemService) | `Resolve(ref)` 将定位符解析为实际 Container | 远期消费方 |
| `Container<T>` (L3_Container) | ContainerSlotRef 指向 Container 内的槽位 | 指向目标 |
| `SlotDef` (L3_Container) | SlotKey 在平级容器中等同于 SlotId | 概念关联 |

## 当前实现状态

| 组件 | 状态 | 说明 |
|------|:---:|------|
| `ContainerSlotRef` struct | 已完成 | `[Serializable]`，`OwnerId` + `SlotKey` |
| `ContainerResolver` | 待做 | 定位符 → 实际 Container 的解析器 |
| L2_ItemService 集成 | 待做 | ItemService 通过 ContainerSlotRef 索引 |
| 网络传输 | 远期 | 字符串 ID 兼容序列化，联机就绪 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| `ContainerResolver` 实现 | 待做 | Container<T> 完整 |
| L2_ItemService.FindByRef | 待做 | ContainerResolver |
| 跨容器 Transfer | 待做 | 以上全部 + ItemInstance |
| 嵌套容器路径解析 | 远期 | MergeSlotsFrom + SlotKey 前缀 |
