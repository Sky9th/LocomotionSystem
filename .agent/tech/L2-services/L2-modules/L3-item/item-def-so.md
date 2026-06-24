# ItemDefSO — 物品定义资产

> `L3_Item/ItemDefSO.cs` · 技术文档 · 2026-06-24
>
> 继承 `EntityDefSO`。所有数据进 PropertyTree。

## 概述

ItemDefSO 是物品的定义资产（.asset 文件）。回答"这个物品是什么"。

**核心原则**：零 C# 字段。所有数据——身份、属性、效果、标签、容器槽位——全部进 PropertyTree（Template + OverridesJson）。结构化数据（SlotDef[]）通过 `PropertyType.Struct` + `StructTypeName="SlotDef"` 表达——JSON blob 存储 + C# struct 类型安全。PropertyTree 不需要改数据结构。

## 属性分类

### 身份
| 属性 | PropertyType | 示例 |
|------|-------------|------|
| DisplayName | String | "军用背包 MK-III" |
| Description | String | "标准制式军用背包..." |
| Icon | AssetRef | Sprite 引用 |

### 分类
| 属性 | PropertyType | 示例 |
|------|-------------|------|
| ItemTags | GameplayTagList | [Equipment.Backpack, Weapon.Blade] |

> ItemTags 是物品能力的标记系统：`Consumable.Medical`=消耗品，`Container.Backpack`=容器，`Material.Metal`=材料。

### 基础属性
| 属性 | PropertyType | 说明 |
|------|-------------|------|
| Weight | Float | 物品自身重量 (kg) |
| MaxDurability | Float | 最大耐久。0=无耐久（可堆叠物品） |
| MaxStackSize | Int | 最大堆叠数。有耐久物品恒为 1 |

### 战斗（武器物品）
| 属性 | PropertyType | 说明 |
|------|-------------|------|
| ATK | AssetRefList → DamageEffectSO[] | 武器伤害通道 |
| DamageType | GameplayTag | 物理属性路由 |

### 效果
| 属性 | PropertyType | 说明 |
|------|-------------|------|
| Effects | AssetRefList → EffectSO[] | 被动/使用效果。绷带=HealEffect，毒药=DamageEffect |

### 容器
| 属性 | PropertyType | 说明 |
|------|-------------|------|
| CarryWeightMax | Float | 容器总承载重量上限。0=不可装载 |
| CarryVolumeMax | Float | 容器总承载体积上限 |
| Container/Slots | **Struct** | SlotDef[] — 容器槽位定义。StructTypeName="SlotDef" |

### 表现
| 属性 | PropertyType | 说明 |
|------|-------------|------|
| VisualPrefab | AssetRef | 3D 模型 |
| AnimationProfile | AssetRef | 动画覆写 |
| AudioProfile | AssetRef | 音效 |

## PropertyTree 结构继承

```
ItemBase（所有物品的根）
├── Presentation: DisplayName, Description, Icon
├── Base: Weight, MaxDurability, MaxStackSize
└── ItemTags

    ↓ 继承

WeaponBase : ItemBase        ArmorBase : ItemBase
├── Combat: ATK, DamageType  ├── Defense: DEF, Coverage
└── ...                       └── ResistTypes[]
                                      
ConsumableBase : ItemBase    ContainerBase : ItemBase
├── Effects: EffectSO[]        ├── CarryWeightMax, CarryVolumeMax
└── UseTime: Float             └── Container/Slots: SlotDef[] (Struct)
```

## SlotDef[]

槽位结构定义（当前为 ItemDefSO C# 字段）：

```csharp
[Serializable]
public struct SlotDef
{
    public string SlotId;                     // "Main", "WeaponSling", "WaterPouch"
    public GameplayTagDefinitionSO[] AcceptTags; // 过滤。空=全接受
    public int Capacity;                      // 物品数量上限
    public float WeightLimit;                 // 重量上限。0=无限制
}
```

存入 PropertyTree，通过 `PropertyType.Struct` 类型 + `PropertyDefSO.StructTypeName = "SlotDef"` 关联。运行时 `bag.GetStructArray<SlotDef>("Container/Slots")` 读取。C# struct 保留编译期类型安全 + Validator。

> ⚠ **代码尚未同步**：当前 `ItemDefSO.cs` 仍持有 `SlotDef[] Slots` C# 字段。待 PropertyType.Struct 实现后移除。

## 与 EntityDefSO 的关系

```
EntityDefSO (Properties 模块基类)
├── Template: PropertyTreeSO    ← Schema: 此物品"有哪些属性"
└── OverridesJson: string       ← 覆写值: 此物品"每个属性是多少"

ItemDefSO : EntityDefSO
├── [继承] Template + OverridesJson  ← 所有叶数据
└── SlotDef[] Slots                  ← 暂时存留（准备迁移到 Struct）
```

和 CharacterDefSO（角色定义）、BuildingDefSO（建筑定义）共享同一套属性管线。

## 堆叠规则

| 条件 | 堆叠 | Count |
|------|:---:|:---:|
| `MaxDurability > 0`（有耐久） | ✗ | 恒为 1 |
| `MaxDurability = 0`（无耐久） | ✓ | 1 ≤ Count ≤ MaxStackSize |

**拆分**：减少原实例 Count + 创建新 ItemInstance。新实例共享 Def 引用，独立 Props。

**合并**：两个同 Def 的 ItemInstance 合并 → 源 Count += 目标 Count，销毁目标实例。仅无耐久物品可合并。

**容器满处理**：Transfer 到容量已满的槽位 → CanAccept 返回 false → Transfer 终止，不操作源容器。

**部分移动**（远期）：从堆叠中移动 N 个到新容器 → 源 Count -= N，若目标已有同 Def 堆叠则合并。

## 运行时创建

```
ItemDefSO.asset (配置)
    ↓ ItemInstance.Create(def)
EntityProperties.Create(def)    ← 复用 PropertyAgent 管线
    ↓ ResolvedPropertyBag
运行时属性读取: props.GetFloat("Base/Weight")
               props.GetStructArray<SlotDef>("Container/Slots")
```

## 耦合

| 依赖 | 方式 |
|------|------|
| EntityDefSO (L3_Properties) | 继承 |
| SlotDef (L3_Item) | 当前 C# 字段引用；未来通过 StructTypeName |
| PropertyTreeSO | Template 引用 |
| DamageEffectSO, EffectSO (L3_Ability) | AssetRefList 资产引用 |
