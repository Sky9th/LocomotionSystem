# L3_Item · 物品系统

> `L3_Item/` — 独立模块。物品的数据定义、运行时实例、身份索引。与 `L3_Ability` 平级。

> ⚠ **DRAFT** — 未定稿。设计方向已确定，细节仍在讨论中。
>
> **Last Verified**: 2026-06-23 | **Verification**: DESIGN PHASE — 代码尚未创建

## 层级定位

独立 L3 模块，位于 `Services/Modules/L3_Item/`。不是管线系统（对比 `L3_Ability`），是**数据权威模块**——负责回答"这个物品是什么、现在在哪、是否合法"。

| 维度 | L3_Ability | L3_Item |
|------|-----------|---------|
| 核心职责 | 行为管线（②→⑧） | 身份和位置权威 |
| 运行时 | AbilityExecutor（Update/冷却） | ItemInstance（纯 C# 对象） |
| 索引 | — | L2_ItemService.ItemRegistry |
| SO | AbilityDefSO | ItemDefSO |
| 实例 | 无（技能无运行时态） | ItemInstance（ID + 属性管线） |
| 多人 | 无特殊需求 | Registry = 服务端权威边界 |

## 架构概览

```
┌──────────────────────────────────────────────────────────────────┐
│                   定义层 (Design Time)                              │
│                                                                   │
│  ItemDefSO : PropertyPresetSO                                          │
│    ├── [继承] PropertyTreeSO Template     ← 物品属性结构           │
│    └── [继承] string OverridesJson        ← 变种属性值             │
│                                                                   │
│  ItemDefSO 零 C# 字段。所有数据全进 PropertyTree。                  │
│                                                                   │
│  结构化数据（SlotDef[]）通过 PropertyType.Struct 表达：              │
│    PropertyDefSO.Type = Struct                                     │
│    PropertyDefSO.StructTypeName = "SlotDef"                        │
│    → 运行时 JsonUtility.FromJson<SlotDef>(json) 反序列化            │
│    → C# struct 保留编译期类型安全 + Validator 校验                   │
│    → PropertyTree 只需一种新 PropertyType，不改数据结构/合并语义     │
│                                                                   │
│  能力标记靠 ItemTags（GameplayTagList）：                            │
│    Consumable.Medical → 消耗品                                     │
│    Weapon.Blade       → 武器                                       │
│    Material.Metal     → 材料                                       │
│    Container.Backpack → 容器（此物品提供容器槽位）                   │
│                                                                   │
│  效果引用靠 AssetRefList（EffectSO[] 直接存在 PropertyTree 中）：    │
│    绷带的 Effects → [HealEffect_CleanBleed, HealEffect_RestoreHP]  │
└─────────────────────────┬──────────────────────────────────────────┘
                          │ ItemInstance.Create(def)
┌─────────────────────────▼──────────────────────────────────────────┐
│                   运行时实例层                                       │
│                                                                     │
│  ItemInstance                                                       │
│    ├── Id: string                      ← 唯一身份（一个堆叠一个 ID）   │
│    ├── Def: ItemDefSO                  ← 不可变定义                   │
│    ├── Props: PropertyTable         ← 可变属性（耐久/充能/…）      │
│    ├── Count: int                      ← 堆叠数（1=不可堆叠物品）     │
│    └── Tick(float dt)                  ← 驱动属性消耗/恢复/衰减       │
│                                                                     │
│  堆叠规则：有耐久（MaxDurability>0）→ Count 恒为 1，不可堆叠。        │
│  无耐久（绷带/弹药/材料）→ Count ≥ 1，可堆叠。拆分=减原 Count+建新。 │
│                                                                     │
│  静态工厂: ItemInstance.Create(ItemDefSO) → ItemInstance            │
│  销毁: ItemInstance.Destroy() → 清理 Props                          │
│                                                                     │
│  ItemInstance 是纯 C# 类，不是 GameObject。                         │
│  同一把刀在世界里、在背包里、在手里——同一个 ItemInstance。          │
│                                                                     │
│  Tick 由容器所有者驱动，ItemInstance 不自驱：                       │
│    CharacterActor.Update() → item.Tick(dt)     ← 每帧，战斗属性    │
│    WorldManager.Update()  → item.Tick(0.5f)   ← 0.5Hz，物品衰减   │
│  无衰减属性的物品 Tick 空转，FloatState 开头直接 return，零开销。    │
└─────────────────────────┬──────────────────────────────────────────┘
                          │ ItemService.RegisterItem(item, slot)   ← L2
┌─────────────────────────▼──────────────────────────────────────────┐
│               L2_ItemService · 物品协调层                            │
│                                                                     │
│  ItemRegistry: 物品→位置 索引 → ContainerSlotRef（来自 L3_Container） │
│  ContainerResolver: ownerId → Container 解析                         │
│  Transfer: 物品跨容器移动唯一入口 → 原子化 + 回滚                    │
│                                                                     │
│  详见 → .agent/tech/L2-services/L2-item-service/README.md           │
└──────────────────────────────────────────────────────────────────────┘
```

## 目录结构

```
L3_Item/
├── ItemDefSO.cs                   # [SO] 物品定义 — 继承 PropertyPresetSO，零 C# 字段
└── ItemInstance.cs                # [class] 运行时个体 — ID + Props
```

无 Config/ 目录。所有数据全进 PropertyTree——叶数据走标量类型，结构化数据走 `PropertyType.Struct`（JSON blob + C# struct 类型名关联）。能力靠 ItemTags 标记。
`SlotDef` struct 当前定义在 L3_Item/ItemDefSO.cs。PropertyType.Struct 实现后移至 L3_Container。

## 调用链

```
物品创建:
  ItemDefSO.asset（配置）
    → ItemInstance.Create(def)
      → PropertyTable.Create(def)        ← 复用 PropertyAgent 管线
      → L2_ItemService.RegisterItem(item, slot)

物品移动:
  L2_ItemService.Transfer(itemId, fromSlot, toSlot) → bool
    1. var item = Resolve(fromSlot).Remove(fromSlot.SlotKey, itemId)
    2. Untrack(itemId)
    3. Resolve(toSlot).CanAccept(toSlot.SlotKey, item)?
       CanAccept 失败 → 回滚 Place(fromSlot.SlotKey, item) + Track(fromSlot)
    4. Resolve(toSlot).Place(toSlot.SlotKey, item)
       Place 失败 → 同上回滚
    5. Track(itemId, toSlot)
  容器不知 Registry 存在，只负责 CanAccept/Place/Remove

物品属性驱动（Tick）:
  容器所有者.Update()
    → item.Tick(dt)                   ← 由所有者决定频率
      → PropertyTable.Tick(dt)     ← 驱动 FloatState 消耗/恢复/衰减
    → 无衰减属性的物品 Tick 空转，FloatState 直接 return，零开销

物品销毁:
  ItemInstance.Destroy()
    → Props 清理
    → L2_ItemService.UnregisterItem(id)

物品查询:
  L2_ItemService.WhereIs(id) → ContainerSlotRef?   ← O(1)
```

**Tick 模型**：ItemInstance.Tick(dt) 由容器所有者驱动——详见 [L3_Container 文档](../L3-container/README.md)。手雷引信不走被动 Tick——由技能管线在激活后计时。

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| ItemDefSO | L3-properties (PropertyPresetSO, PropertyTreeSO) | 继承属性框架 |
| ItemInstance | L3-properties (PropertyTable) | 持有物品运行时属性 |
| L2_ItemService | L3_Item（ItemInstance） | （L2 间接依赖，非 L3_Item 直接依赖 L2） |
| L2_ItemService | L3_Container（Container\<T\>, ContainerSlotRef, SlotDef） | （同上，L2 胶水层协调） |
| ItemDefSO | L3_Container（SlotDef struct） | 类型级引用：StructTypeName="SlotDef" 关联（非服务调用） |
| ItemDefSO | 容器系统 | ItemTags 被容器过滤规则消费（GameplayTag 匹配） |
| ItemDefSO | AbilityTreeSO（将来） | 武器标签匹配技能树兼容性 |
| ItemInstance | UI（装备栏/物品栏） | 展示物品信息和状态 |
| L2_ItemService | 多人服务端 | 物品移动权威验证——ItemService 为唯一入口 |

## 设计决策

| 决策 | 原因 |
|------|------|
| ItemDefSO 纯 PropertyTree，零 C# 字段 | 所有数据全进 PropertyTree。叶数据走标量类型，结构化数据（SlotDef[]）走 PropertyType.Struct + StructTypeName 关联 |
| 不做能力 struct（ConsumeData 等） | 绷带的 `useTime`=Float、`effects`=AssetRefList→全部可由 PropertyTree 表达。标签即可标记能力 |
| 不做独立 Capability SO | 三个商业游戏均未采用。WeaponTypeSO 被拆解——字段各有归属 |
| 结构化数据走 PropertyType.Struct | 不降维拆成 key-value（失内聚），不做独立 SO（无复用），不扩展 GroupProp（改动大）。JSON blob + C# struct 类型名——框架改动最小，类型安全保留 |
| 物品定义继承 PropertyPresetSO | 和角色共享同一套属性管线 |
| 武器类型 = GameplayTag | Weapon.Blade / Weapon.Pistol。被容器过滤、被技能树匹配 |
| ItemInstance 是纯 C# 类 | 物品在容器间移动只改引用，不 Instantiate/Destroy |
| 需要 ItemRegistry（在 L2_ItemService） | 多人联机要求物品身份有服务端权威。L2_ItemService 持有 Registry |
| Registry 不管 Tick | 容器所有者驱动 Tick。Registry（L2）只做身份索引 |
| 容器所有者决定 Tick 频率 | 角色 60fps，箱子 0.5Hz |
| 装备不是物品类型 | "装备"是物品在身体槽容器中的状态。GearDefSO 原建模将状态和定义混淆——此概念已废弃 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| ItemDefSO 代码实现 | 待做 | 本文档 |
| ItemInstance 运行时 | 待做 | ItemDefSO |
| ItemRegistry 身份索引 | 待做 | ItemInstance + Container |
| 物品 PropertyAgent 管线 | 待做 | ItemDefSO + PropertyAgent |
| 容器系统 | 待做 | ItemInstance + ItemRegistry |
| 与 AbilityTreeSO 的技能树匹配 | 远期 | 容器 + ItemRegistry |
| 多人服务端权威 | 远期 | ItemRegistry |

## 编辑器工具

| 工具 | 菜单 | 说明 |
|------|------|------|
| ItemEditorWindow | `RedDust/Item Editor` | 两栏编辑器：左侧物品列表（按 Template 分组）+ 右侧槽位表 + 属性覆写表单 |
| ItemImportExport | `RedDust/Item Import-Export` | JSON 导入/导出，复用 EditorImportExport 骨架 |

详见 → [tech/editor/tools/L3_Item/Editor/](../../../editor/tools/L3_Item/Editor/)

## 已知缺口

| 缺口 | 状态 | 说明 |
|------|:---:|------|
| 存档/加载 | ⏸ 待定 | PropertyTable 序列化、Registry 重建——暂不处理 |
| 世界物品管理 | ⏸ 待定 | 类似 PlayerService 的统一管理器——待论证 |
| 物品转移调用方 | ✅ | L2_ItemService.Transfer 为唯一入口。容器只负责 Place/Remove |
| 代码同步 — ItemDefSO 零字段 | ❓ | 当前代码仍持有 SlotDef[]。设计已定：All data → PropertyTree，代码尚未同步 |
| ContainerSlot 定义 | ✅ | Container\<T\> 提供 Place/Remove/CanAccept（详见 L3_Container） |
| 具体 PropertyTree 结构 | — | 不属 Item 模块范围 |
| 物品 ID 生成 | ✅ | GUID |

## 子文档索引

| 文档 | 说明 |
|------|------|
| （待创建）item-def-so.md | ItemDefSO — 物品定义资产，字段详解 |
| （待创建）item-instance.md | ItemInstance — 运行时个体，工厂 + 属性管线 |