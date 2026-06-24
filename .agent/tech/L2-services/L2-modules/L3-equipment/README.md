# ⛔ DEPRECATED — L3_Equipment · 装备系统

> ⚠ **此文档基于被否决的 GearDefSO 模型。已被新架构替代。**
>
> 新方向：装备 = 物品在身体槽容器中的状态。ItemDefSO + Container\<T\> + PropertyTree。
> 详见 → [L3_Item](../L3-item/README.md) | [L3_Container](../L3-container/README.md) | [L2_ItemService](../../L2-item-service/README.md) | [Session 决策](../../../../sessions/2026-06-24-equipment-item-architecture.md)

---

# 以下为旧文档内容

> `L3_Equipment/` — 独立模块。装备/零件/武器的数据定义与运行时实例管理。被 Character（装备槽位）、Ability Pipeline（伤害地基）、掉落/制造/交易等系统消费。

## 层级定位

独立 L3 模块，位于 `Services/Modules/L3_Equipment/`。负责装备的数据模型、零件组装、实例生命周期——不负责动画播放、属性存储、UI 渲染。

层级关系：
- L1-app → GameContext
- L2-services → 各 Service 通过 EquipmentComponent 消费
- L3-equipment → 数据模型（本模块）
- L3-stats → 数值骨架（StatsTreeSO 定义装备属性集合）

## 架构概览

```
┌──────────────────────────────────────────────────────────────────┐
│                   定义层 (Design Time)                              │
│                                                                   │
│  StatsTreeSO（~20 棵）              GearDefSO（~300 个）            │
│  ──────────────────────             ──────────────────             │
│  Pistol : Firearm :                  Glock17_Frame.asset           │
│    RangedWeapon : WeaponBase           ├── statsTree → Pistol      │
│                                         ├── overrides: {           │
│  回答"这类装备有哪些 stat"              │       Weight=0.25,             │
│  结构继承，不存具体数值                  │       slots=[...]         │
│                                         │       gearType=Receiver}│
│                                         ├── gearType: Receiver     │
│                                         ├── platform: Platform.Glock│
│                                         ├── compatibleAmmo: [9mm]  │
│                                         └── slots: [Barrel, ...]   │
│                                                                   │
│  定义层 ~20 个资产                   实例层 ~300 个资产             │
│  不随装备数量膨胀                     每个变种一个 .asset           │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│                   运行时 (Runtime)                                  │
│                                                                   │
│  GearInstance                                                    │
│  ────────────                                                     │
│  def: GearDefSO (共享引用)                                         │
│  durability: float (独立)                                         │
│  activeAffixes: AffixDefSO[]                                      │
│  installedParts: Dict<string, GearInstance>  ← def.slots 非空时   │
│                                                （枪支 = Receiver  │
│  Resolve() → StatInstance[]                     + 所有零件 stat    │
│  → 递归求和所有零件 stat                       递归求和）           │
│                                                                   │
│  装备实体 — 同一把 Glock 17，地上的两把耐久/词条/配件不同           │
│  枪支只是 GearInstance 的特例：def.slots 非空 + installedParts 有值│
└──────────────────────────────────────────────────────────────────┘
```

### 数值叠加流程

```
最终 ATK = 类型基线 + 型号覆写 + 零件叠加 + 词条修正

  ③ 词条 "大口径弹膛"    +5 ATK        ← AffixDefSO (可随机/可锻造)
  ② 型号覆写             Weight=0.25   ← GearDefSO.overrides (枪身自身)
  ① 类型基线             ATK=15        ← StatsTreeSO.Default (占位)
```

### 万物皆 GearDefSO

一把剑、一根枪管、一个消音器都是 `GearDefSO`。区别仅 `slots: []`（不可再装东西）vs `slots: [...]`（可装子零件）。

| slots | 含义 | 例子 |
|-------|------|------|
| `[]` | 成品 / 独立零件 | 剑、头盔、绷带、枪管、消音器 |
| `[...]` | 可装其他装备（根零件） | 手枪枪身、步枪枪身、复合弓身 |

## 目录结构

```
L3_Equipment/
├── GearDefSO.cs              # [SO] 装备/零件定义资产 — 身份 + 数值骨架 + 槽位 + 资源引用
├── GearInstance.cs           # [class] 运行时装备个体 — def 引用 + 耐久 + 词条 + 子零件（含 Resolve 递归求和）
├── Config/
│   └── GearSlot.cs           # [Serializable] 槽位定义 — slotId + acceptTag + required
├── Structs/
│   └── StatOverride.cs       # [Serializable] 数值覆写 — (StatDefSO, float value)
└── Editor/
    └── GearDefImporter.cs    # [Editor] 从 StatsTree + JSON 表批量生成 GearDefSO 资产
```

## 核心数据类型

### GearDefSO — 装备定义资产

一个 `.asset` 文件 = 一种具体的装备/零件。字段分组：

| 分组 | 字段 | 类型 | 说明 |
|------|------|------|------|
| 身份 | `displayName` | string | "Glock 17 枪身" |
| | `description` | string | 描述文本 |
| | `icon` | Sprite | UI 图标 |
| | `gearType` | GameplayTag | Equipment.Weapon.Ranged.Receiver / Part.Barrel / Armor.Body / Consumable.Medical |
| 数值 | `statsTree` | StatsTreeSO | 引用类型树，决定"有哪些 stat" |
| | `overrides` | StatOverride[] | 覆写值，"每个 stat 是多少" |
| | `damageType` | GameplayTag | 伤害管线直读。防具/工具为 null |
| | `resistTypes` | GameplayTag[] | 防具抗性类型。武器为 null |
| 兼容 | `platform` | GameplayTag | 同平台零件可互换（"Platform.Glock"） |
| | `compatibleAmmo` | GameplayTag[] | 弹药口径（"Caliber.9mm"）。非枪械为 null |
| 结构 | `slots` | GearSlot[] | [] = 成品/独立零件，非空 = 可装其他装备 |
| 资源 | `visualPrefab` | GameObject | 3D 模型 |
| | `animationProfile` | AnimationOverrideProfileSO | 动画覆写（手枪单手 vs 步枪抵肩） |
| | `audioProfile` | AudioProfileSO | 击发/换弹/空仓音效 |
| 经济 | `baseValue` | float | 基础价值 |
| | `salvageOutputs` | SalvageEntry[] | 拆解产出 |

### GearSlot — 槽位定义

```csharp
[Serializable]
public struct GearSlot
{
    public string slotId;           // "Barrel", "Magazine", "Muzzle"...
    public GameplayTag acceptTag;   // 接受什么 gearType（"Equipment.Part.Barrel"）
    public bool required;           // 必须安装才能正常运作
}
```

### StatOverride — 数值覆写

```csharp
[Serializable]
public struct StatOverride
{
    public StatDefinitionSO stat;   // 拖拽引用 StatDefSO
    public float value;             // 覆写值
}
```

### GearInstance — 运行时装备个体

```csharp
public class GearInstance
{
    public GearDefSO def;                    // 共享资产引用
    public float durability;                 // 当前耐久（独立）
    public List<AffixDefSO> activeAffixes;   // 词条（独立）
    public Dictionary<string, GearInstance> installedParts; // 子零件（仅 Receiver）
    public int currentAmmo;                  // 弹匣内剩余弹药

    public StatInstance[] Resolve();         // 四步叠加：基线 → 覆写 → 零件 → 词条
}
```

## 调用链

```
EquipmentComponent.OnEquip(gearDef):
  ├── ① 类型基线: gearDef.statsTree.Resolve()         → StatInstance[] (ATK=15)
  ├── ② 型号覆写: 遍历 gearDef.overrides → Override()  → StatInstance[] (Weight=0.25)
  ├── ③a 零件叠加: 如果 def.slots 非空，遍历 installedParts 递归累加
  ├── ③b 词条叠加: 遍历 instance.activeAffixes → AddModifier()
  │                                                      → StatInstance[] (assembly result)
  └── ④ 桥接角色: actorStats.Get(def).AddModifier(gear, value)

GearInstance.Resolve()（完整流程，def.slots 为空时跳过 ③a）：
  ├── ① 自身 Resolve()（基线 + 覆写）                    → StatInstance[]
  ├── ③a 遍历 installedParts: 每个子 GearInstance 递归 Resolve() 逐项累加
  └── ③b 词条叠加                                       → 最终 StatInstance[]
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| GearDefSO | L3-stats (StatsTreeSO, StatDefSO) | 引用 StatsTreeSO 获取 stat 集合，StatOverride 引用 StatDefSO |
| GearInstance | L3-stats (StatInstance) | Resolve() 产出 StatInstance[]（含递归零件求和） |
| 整个模块 | L3-character (EquipmentComponent) | 被 CharacterActor 持有和管理 |
| GearDefSO | Ability Pipeline ⑤ Effects | 提供 damageType + Resolve() 产出的 ATK |
| GearDefSO | 掉落/制造/交易 | 被 LootTableSO / CraftingRecipeSO 拖拽引用 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 两层架构（StatTree + GearDefSO），不合并 | StatsTree.Resolve() 按 Id 合并，不适合存同一 stat 的 200 种取值 |
| 一个变种一个 .asset | 装备需要被掉落表/任务/配方拖拽引用。Unity 序列化不支持引用 JSON 字符串 |
| 万物皆 GearDefSO | 枪管和剑在数据层完全平权——都能捡、交易、损坏、修理。仅 slots 区分 |
| 零件类型用 GameplayTag 不用枚举 | 新零件类型加 tag 即可，不修改代码 |
| 枪支 = 零件树，Receiver 为根 | 每把枪都是玩家拼出来的，混搭零件的乐趣 |
| 近战无零件树 | 一把斧子不需要组装。复合弓远期可支持 |
| 数值叠加流程（基线→覆写→零件→词条） | 类型基线共享，型号覆写差异化，零件贡献累加，词条随机/锻造深度 |
| 词条不写入 GearDefSO | 同一把 Glock 17 不同存档有不同词条。词条在 GearInstance 运行时挂载 |
| 装备属性走 StatOverride[] 而非 treeJson | GearDefSO 不做结构继承，只覆写叶子值。更轻量，Inspector 编辑更友好 |

## 未来规划

| 规划 | 状态 | 依赖 |
|------|------|------|
| GearDefSO 代码实现 | 待做 | 本文档 |
| GearInstance 运行时 | 待做 | GearDefSO |
| EquipmentComponent | 待做 | GearInstance |
| GearInstance 零件组装 + Resolve 递归 | 待做 | GearDefSO |
| AffixDefSO 词条系统 | 待做 | GearInstance 的 AddModifier |
| MaterialDefSO / AIDefSO | 远期 | 同骨架（statsTree + overrides） |
| GearDefImporter 批量生成工具 | 待做 | GearDefSO |
| 掉落/制造/交易对接 | 待做 | GearDefSO 可引用 |

## 子文档索引

| 文档 | 说明 |
|------|------|
| [gear-def-so.md](gear-def-so.md) | GearDefSO — 装备/零件定义资产，字段详解 + StatOverride + GearSlot |
| [gear-instance.md](gear-instance.md) | GearInstance — 运行时装备个体，工厂流程 + 三层叠加 + 与 SO 的对比 |
