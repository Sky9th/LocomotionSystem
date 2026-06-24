# L3_Container · 容器系统

> `L3_Container/` — 独立模块。通用容器抽象——管理物品的放置、取出、过滤和嵌套。身体装备槽、背包、技能栏共享同一套容器逻辑。

> ⚠ **DRAFT** — 已定稿核心设计。细节仍在讨论中。
>
> **Last Verified**: 2026-06-24 | **Verification**: DESIGN PHASE — 代码框架已创建，运行时尚未实现

## 层级定位

独立 L3 模块，位于 `Services/Modules/L3_Container/`。比 `L3_Item` 更基础——物品系统定义"是什么"，容器系统回答"在哪"。

不是管线系统。没有 Update、没有 Tick。纯粹的放置/取出/查询。

## 核心

**容器是物品的"位置"。物品放进容器获得状态——放进身体槽=装备，放进背包=物品，放进技能槽=可用技能。**

"装备"不是物品的类型——是物品在特定容器中的状态。同一把刀，在身体槽是装备，在背包是物品。

## 架构

```
Container<T>
  ├── ContainerId: string              ← "char_001/Backpack/Main"
  ├── Slots: Dictionary<string, ContainerSlot>
  │     └── ContainerSlot
  │           ├── Def: SlotDef               ← 静态配置（来自 PropertyTree）
  │           ├── Items: List<T>        ← 当前容纳的物品
  │           └── CurrentWeight: float  ← 当前总重缓存
  │
  ├── CanAccept(slotKey, item) → bool
  ├── Place(slotKey, item) → bool
  ├── Remove(slotKey, itemId) → T
  ├── FindSlotFor(item) → string?
  │
  └── Tick(float dt)                    ← 容器所有者驱动
        → foreach slot → foreach item → item.Tick(dt)
```

## 槽位定义来源

槽位结构定义在 C# struct `SlotDef`（当前在 L3_Item/ItemDefSO.cs，PropertyType.Struct 实现后移至 L3_Container），值存在 ItemDefSO 的 PropertyTree 中，通过 `PropertyType.Struct` 类型：

```csharp
// C# struct — 编译期类型安全 + Validator
[Serializable]
public struct SlotDef
{
    public string SlotId;                     // "Main"
    public GameplayTagDefinitionSO[] AcceptTags; // [] = 全接受
    public int Capacity;                      // 20
    public float WeightLimit;                 // 0 = 无限制
}

// PropertyDefSO 配置
//   Id: "Container/Slots"
//   Type: Struct
//   StructTypeName: "SlotDef"    ← 桥接到 C# struct

// OverridesJson 存值
//   "Container/Slots": [
//     { "SlotId": "Main", "AcceptTags": [], "Capacity": 20, "WeightLimit": 0 },
//     { "SlotId": "WeaponSling", "AcceptTags": ["Weapon.Rifle"], "Capacity": 2, "WeightLimit": 0 }
//   ]
```

**PropertyTree 加一个 PropertyType.Struct**：框架层最小变动——不改 PropertyNode 结构、不改合并语义、不改序列化格式。`StructTypeName` 字符串关联 C# struct，编辑器反射渲染字段，运行时 `JsonUtility.FromJson<T>(json)` 反序列化。

**C# struct 依旧存在**：编译期类型安全、Inspector 校验、Validator 逻辑——都保留在 struct 里。PropertyTree 只负责存储，不试图理解结构体内部。

**不做独立 SO**：SlotDef 1:1 专属单个物品，无复用场景，不值得独立资产化。

运行时 Container 构造：

```csharp
var slots = bag.GetStructArray<SlotDef>("Container/Slots");
foreach (var slot in slots)
{
    var containerSlot = new ContainerSlot(slot.SlotId, slot.AcceptTags,
                                           slot.Capacity, slot.WeightLimit);
    Slots[slot.SlotId] = containerSlot;
}
// bag.GetStructArray<T> 内部：读 JSON → JsonUtility.FromJson → 校验
```

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

**SlotKey 命名规范**：
- 身体槽：`"RightHand"`, `"LeftHand"`, `"Head"`, `"Torso"`, `"Legs"`
- 背包/箱子槽：与 SlotId 相同（如 `"Main"`, `"WeaponSling"`）
- 嵌套路径：`{parentSlotKey}/{childSlotId}`（如 `"Backpack/Main"`）

不存 C# 对象引用——联机兼容。运行时由 L2_ItemService 的 `ContainerResolver.Resolve(ref)` 获取实际 `Container<T>`。

**与 ContainerId 的关系**：`Container<T>.ContainerId` 是容器自标识，`ContainerSlotRef.OwnerId` 是容器所有者的寻址 key。所有者可持有多个 Container（如角色背包装备了嵌套子容器），ContainerResolver 以 OwnerId 为 key 映射到**根容器**，嵌套容器通过根容器路径访问。

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

嵌套通过读取嵌套物品的 PropertyTree Struct 节点获取 SlotDef[]，再 `Container.MergeSlotsFrom(slotDefs)` 实现，子容器槽位加路径前缀避免冲突。

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| Container\<T\> | L3_Item（ItemInstance, ItemDefSO PropertyTree） | 从 PropertyTree Struct 节点读取 SlotDef[]，存放 ItemInstance |
| Container\<T\> | L3_Ability（AbilityDefSO） | 技能槽存放技能定义 |
| Container\<T\> | EquipmentComponent | EquipmentComponent 管理身体容器的创建和切换 |
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
| ContainerSlot 用 ownerId+slotKey 寻址 | C# 引用不可网络传输。字符串 ID 兼容联机 |
| SlotDef[] 走 PropertyType.Struct | JSON blob + C# struct 类型名关联。PropertyTree 最小改动——不改数据结构/合并语义，C# struct 保类型安全 |
| 槽位定义来源 PropertyTree（非 C# 字段） | ItemDefSO 零字段。Container 构造时 bag.GetStructArray&lt;SlotDef&gt;("Container/Slots") 读取 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| Container\<T\> 运行时实现 | 待做 | ItemDefSO + SlotDef |
| ContainerResolver 寻址 | 待做 | Container\<T\> |
| EquipmentComponent | 待做 | Container\<T\> |
| 背包嵌套容器运行时 | 远期 | Container\<T\> + SlotDef[] |
| 技能栏容器 | 远期 | Container\<T\> + AbilityTreeSO |
| 世界容器（箱子/地面） | 远期 | Container\<T\> + WorldManager |

## 已知缺口

| 缺口 | 状态 | 说明 |
|------|:---:|------|
| Container\<T\> 代码 | ❓ | 骨架已有，运行时实现待做 |
| ContainerSlotRef 与 Registry 集成 | ❓ | ownerId+slotKey 模型已定，代码待写 |
| 嵌套容器前缀冲突处理 | ❓ | MergeSlotsFrom 已定，边界情况待定义 |
| 武器握法（单手/双手/双持） | ❓ | 占用规则待详细定义 |
| 代码同步 — PropertyType.Struct + SlotDef | ❓ | PropertyType 枚举 + PropertyDefSO.StructTypeName + ItemDefSO 移除 C# 字段。设计已定，代码尚未同步 |
| 世界物品管理器 | ⏸ 待定 | WorldItemManager 方案论证 |

## 子文档索引

| 文档 | 说明 |
|------|------|
| （待创建）container.md | Container\<T\> — 泛型容器 API 详解 |
| （待创建）container-slot.md | ContainerSlot — 槽位运行时与过滤规则 |
