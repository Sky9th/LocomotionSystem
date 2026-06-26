# Session: 装备/物品系统架构设计

> 2026-06-23 ~ 2026-06-24 | v0.22.7

## Background

动画资源和 Ability 链路已补齐，下一步回到 Ability 开发。但在做 Ability UI 之前，需要理清装备系统的数据来路。结果发现装备系统根本没有运行时代码，进而追到"装备到底是什么"这个根问题。

经过两天的架构讨论，确立了"一切皆属性"的核心方向，推翻/修正了多个原有设计假设。

## Changes

### 架构方向

- **确立"一切皆属性"核心**：角色、物品、NPC 共享 PropertyPresetSO + PropertyTreeSO + PropertyAgent 管线
- **装备不是物品类型**：是物品在身体槽容器中的状态。GearDefSO 概念废弃
- **武器类型缩减为 GameplayTag**：WeaponTypeSO 被拆解——damageType→技能、gripTags→玩家选择、equipSlot→容器过滤、compatRoutines→AbilityTreeSO 反向声明
- **ItemDefSO 零 C# 字段**：所有叶子数据进 PropertyTree。能力靠 ItemTags 标记。放弃 Config struct（ConsumeData 等）
- **技能来源修正**：武器不决定技能。技能树 × 武器 Tag = 可用技能。和设计文档的武学套路系统一致
- **物品能力槽概念淡化**：最终只有 GearSlot[]（结构数据）需留在 C#，属容器系统范围

### 运行时机理

- **容器是物品的"位置"**：Container\<T\> 泛型抽象，Place/Remove/CanAccept
- **容器所有者驱动 Tick**：角色 60fps、箱子 0.5Hz。ItemInstance 不自驱。Registry 不管 Tick
- **Registry 管身份不管行为**：Track/Untrack/FindLocation/Transfer。Transfer 原子化跨容器移动+回滚
- **堆叠规则**：有耐久不可堆叠（Count=1），无耐久可堆叠（Count≥1）

### 文档

- 新增 `L3_Item` 模块文档
- 新增 `L3_Container` 模块文档
- 新增 `L4_Equipment` 模块文档
- 新增 `AbilityTreeSO` 模块文档
- 更新 `tech/README.md` 索引

## Decisions

| 决策 | 替代方案 | 原因 |
|------|---------|------|
| 统一物品概念（ItemDefSO: PropertyPresetSO） | 多个子类（WeaponItem, ArmorItem...） | CDDA/RimWorld/PZ 都是统一类型。tag 分类 |
| ItemDefSO 零 C# 字段 | Config struct（ConsumeData 等） | 大部分能力字段属性能由 PropertyTree 表达。残留的 GearSlot[] 属容器系统 |
| 武器类型=GameplayTag | WeaponTypeSO | 所有字段被拆解到各自归宿 |
| Composition over Inheritance | 子类继承 | 一把刀同时是武器+制造材料，子类做不到 |
| 装备=容器里的状态 | GearDefSO 独立类 | 同一把刀在身体槽=装备，在背包=物品 |
| 容器所有者 Tick | Registry 集中 Tick | 不同容器需要不同频率；集中 Tick 有节奏耦合 |
| WeaponTypeSO 不存在 | 类型级共享 SO | damageType→技能、gripTags→玩家、equipSlot→容器、compatRoutines→技能树反向 |

## Known Issues

- 容器嵌套（背包装胸挂再装弹匣袋）未完全建模——等容器模块定稿
- 物品世界表示（C# ItemInstance 无 GameObject）待 WorldItemManager 方案论证
- 存档/加载延期
- AbilityTree 与 L3_Ability 的关系——✅ 已厘清。AbilityTreeSO 归属 L3_Ability Config 层，纯数据 SO，2026-06-25
- L3_Equipment 模块去向待各系统定义后再决定
- ContainerSlot API 未定义

## Cross-References

- Tech: [L3_Item](../tech/L2-services/L2-modules/L3-item/README.md)
- Tech: [L3_Container](../tech/L2-services/L2-modules/L3-container/README.md)
- Tech: [L4_Equipment](../tech/L2-services/L2-modules/L3-character/L4-equipment/README.md)
- Tech: [AbilityTreeSO](../tech/L2-services/L2-modules/L3-ability/ability-tree.md)
- Plan: [sorted-growing-tarjan.md](../../../plans/sorted-growing-tarjan.md)
- Design: [damage-source-model.md](../design/damage-source-model.md)
- Design: [equipment-system.md](../design/equipment-system.md) — GearDefSO 原始设计，部分方向已修正
- Tech: [L3_Ability](../tech/L2-services/L2-modules/L3-ability/README.md)

---

## 2026-06-24 (下午) — 容器系统设计

### 核心产出

- **ContainerSlot 寻址**：`ownerId + slotKey` 字符串模型，兼容联机。不存 C# 引用。
- **槽位定义来源**：`ItemDefSO.SlotDef[]`（唯一 C# 字段）。运行时 Container 构造时 `foreach` 遍历创建 ContainerSlot。
- **SlotDef 命名**：放弃 GearSlot（Gear 是已废弃概念），用 SlotDef——轻量，和运行时 ContainerSlot 区分。

### 设计辩论：槽位定义放哪

| 方案 | 结论 |
|------|------|
| PropertyTree GroupProp 扩展 | ❌ 拒绝。890+ 行改动，合并语义分裂，PropertyNode 被绑架 |
| 独立 ContainerSlotDefSO 资产 + AssetRefList | ❌ 拒绝。槽位 1:1 专属物品，无复用场景，资产爆炸无收益 |
| **SlotDef[] C# struct 字段** | ✅ 采纳。4 字段轻量 struct，Inspector 原生支持，运行时零开销遍历 |

关键区分：EffectSO 有 Execute() 行为 + 6 子类 + 被 50 技能复用 → 值得独立 SO。SlotDef 是纯配置数据 + 无复用 → 不值得。

### 改动文件

- `Assets/Scripts/Modules/L3_Item/ItemDefSO.cs` — SlotDef struct + ItemDefSO.SlotDef[] Slots
- `.agent/tech/L2-services/L2-modules/L3-container/README.md` — 重写，落地最终设计
- `.agent/tech/L2-services/L2-modules/L3-item/README.md` — 更新 SlotDef[] 相关描述
- 删除反方 Agent 遗留文件：`GroupTypeTemplates.cs`, `PropertyGroup.cs`

### 已知缺口

- Container\<T\> 运行时实现待做
- ContainerResolver（ownerId → Container 解析）待做
- 嵌套容器 MergeSlotsFrom 边界情况待定义
- 武器握法占用规则（单手/双手/双持）待定

---

## 2026-06-24 (晚间) — 最终决策：PropertyType.Struct

### 转折

用户质疑："EffectSO 放进 PropertyTree（AssetRefList），但 SlotDef 不放——三心二意。"

经过重新审视，结论是应该统一：ItemDefSO 零 C# 字段，SlotDef[] 也进 PropertyTree。

### 方案：PropertyType.Struct

| 组件 | 职责 |
|------|------|
| `PropertyType.Struct` | 新增枚举值。值存 JSON 字符串，运行时 JsonUtility 反序列化 |
| `PropertyDefSO.StructTypeName` | 字符串 "SlotDef"——桥接到 C# struct 类型 |
| C# `SlotDef` struct | 保留——编译期类型安全 + Validator 校验 |
| OverridesJson | `"Container/Slots": [{"SlotId":"Main",...}]` — JSON 数组 |

**和 GroupProp 方案的区别**：
- GroupProp：PropertyTree 理解每个 Group 内部有子节点 → 需改 PropertyNode 结构/合并语义/序列化/编辑器
- Struct 方案：PropertyTree 只存不透明 JSON blob → 改一个 PropertyType + 一个 StructTypeName 字符串。C# struct 负责结构

**C# struct 不消失**：类型安全、Validator、运行时强类型——全部保留在 `SlotDef` struct 里。PropertyTree 只是换了存储位置——从 C# 字段移到 PropertyTree JSON 节点。

### 决策链

```
1. SlotDef[] C# 字段       → 破零字段原则，拒绝
2. PropertyTree GroupProp   → 框架改动过大，拒绝
3. 独立 ContainerSlotDefSO  → 无复用场景，资产爆炸，拒绝
4. PropertyType.Struct      → ✅ 框架最小改动，零字段原则不破，类型安全保留
```

### 设计原理

"一切皆属性"的边界终于清晰：
- **叶数据**：走标量 PropertyType（Float/Int/String/Tag/AssetRef...）
- **结构化数据**：走 PropertyType.Struct——JSON blob + C# struct 类型名
- **复用行为资产**：独立 SO + AssetRefList 引用（EffectSO 模式）
- PropertyPresetSO 子类永远零 C# 字段

---

## 2026-06-24 (深夜) — L2_ItemService vs L2_EntitiesService 辩论

### 裁决

**反方胜利**：L2_ItemService 专注物品管理。L2_EntitiesService 为过早抽象。

### 理由

- Item 纯 C# 数据对象（无 Update、无 Transform），Character 是 MonoBehaviour 帧驱动——运行时本质不同
- 先落地 ItemService，等 Character/Building 也有索引需求后再观察共性——从具体到抽象比从抽象到具体安全
- CDDA/RimWorld 都是"定义层统一，服务层分立"
- ItemInstance 一行代码没写，谈统一是纸上设计

### L2_ItemService 设计

```
L2_ItemService（继承 BaseService）
  ├── ItemRegistry          ← 物品→位置 索引
  ├── ContainerResolver     ← ownerId → Container 解析
  └── Transfer(item,from,to) → bool  ← 物品移动唯一入口
```

Transfer 流程：Resolve(from).Remove → Untrack → Resolve(to).CanAccept? → Place → Track。Place 失败回滚。

Item 和 Container 模块互不认识——ItemService 在 L2 做胶水。

### 文档

- 新增 `.agent/tech/L2-services/L2-item-service/README.md`
- 更新 `tech/README.md` 索引
- 更新 L3_Item、L3_Container 耦合表指向 L2_ItemService
