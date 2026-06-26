# ⛔ DEPRECATED — GearDefSO — 装备/零件定义资产

> ⚠ **GearDefSO 概念已被否决。** 装备不是独立的物品类型——是物品在身体槽容器中的状态。
> 替代：ItemDefSO : PropertyPresetSO（零 C# 字段，全进 PropertyTree）。
> 详见 → [L3_Item](../L3-item/README.md) | [Session 决策](../../../../sessions/2026-06-24-equipment-item-architecture.md)

---

# 以下为旧文档内容

## 定位

`GearDefSO` 是装备系统的核心定义资产。一个 `.asset` 文件 = 一种具体的装备或零件。它与 `StatsTreeSO`（类型层）配合，形成"类型继承 + 数值覆写"的两层架构。

**回答两个问题**：
- 这是什么？→ `gearType` + `displayName` + `icon`
- 它的属性是多少？→ `statsTree`（哪个类型） + `overrides`（值是什么）

---

## 与 StatsTreeSO 的关系

```
StatsTreeSO (共享，~20 个)              GearDefSO (独立，~300 个)
───────────────────────                ──────────────────────
Pistol : Firearm : RangedWeapon        Glock17_Frame.asset (Receiver)
  ├── Ranged_ATK (Default=15)            ├── statsTree → Pistol
  ├── Ranged_Accuracy (Default=70)       ├── overrides:
  ├── Ranged_MagSize (Default=10)        │     (Weight, 0.25)    ← 枪身只覆写自身重量
  ├── Ranged_ReloadSpeed (Default=1.0)   │
  ├── ...                                ├── gearType: Equipment.Weapon.Ranged.Receiver
  └── (16 个 stat 定义)                  ├── slots: [Barrel, Slide, Magazine, ...]
                                         └── ...

"手枪是什么"                             "这把 Glock 17 枪身是什么"
定义 stat 集合，不存具体值                槽位结构 + 自身重量。战斗 stat (ATK、
                                         Accuracy 等) 由各零件分别贡献，组装后求和
                                         覆写具体值，不定义 stat 集合
```

StatsTreeSO 解决**结构继承**（Pistol 比 Firearm 多了 HolsterSpeed + HipFirePenalty）。GearDefSO 解决**数值变种**（Glock 17 和 Desert Eagle 的 ATK 不同）。

---

## 字段定义

### 身份组

| 字段 | 类型 | 说明 | 示例 |
|------|------|------|------|
| `displayName` | `string` | 显示名称 | `"Glock 17 枪身"` |
| `description` | `string` | 描述文本（tooltip 用） | `"9mm 半自动手枪枪身，广泛装备执法机构"` |
| `icon` | `Sprite` | UI 图标 | `Glock17_Icon` |
| `gearType` | `GameplayTag` | 装备/零件类别 | `Equipment.Weapon.Ranged.Receiver` / `Equipment.Part.Barrel` / `Equipment.Armor.Body` |

`gearType` 的常见值：

| Tag | 含义 | slots 通常 |
|-----|------|-----------|
| `Equipment.Weapon.Melee` | 近战成品武器 | `[]` |
| `Equipment.Weapon.Ranged.Receiver` | 枪械根零件 | `[...]` |
| `Equipment.Part.Barrel` | 枪管 | `[]` |
| `Equipment.Part.Slide` | 套筒/枪机 | `[]` |
| `Equipment.Part.Magazine` | 弹匣 | `[]` |
| `Equipment.Part.Muzzle` | 枪口装置 | `[]` |
| `Equipment.Part.Optic` | 瞄准具 | `[]` |
| `Equipment.Part.Trigger` | 扳机组 | `[]` |
| `Equipment.Part.Grip` | 握把/枪托 | `[]` |
| `Equipment.Part.Underbarrel` | 下挂 | `[]` |
| `Equipment.Armor.Head` | 头部防具成品 | `[]` |
| `Equipment.Armor.Body` | 躯干防具成品 | `[]` |
| `Equipment.Armor.Leg` | 腿部防具成品 | `[]` |
| `Equipment.Tool` | 工具成品 | `[]` |
| `Equipment.Consumable.Medical` | 医疗消耗品 | `[]` |
| `Equipment.Ammo` | 弹药 | `[]` |

### 数值组

| 字段 | 类型 | 说明 |
|------|------|------|
| `statsTree` | `StatsTreeSO` | 引用类型树，决定这个装备"有哪些 stat" |
| `overrides` | `StatOverride[]` | 覆写值数组，每个条目 = (哪个 stat, 值多少) |
| `damageType` | `GameplayTag` | 伤害类型，管线 ⑤ Effects 阶段直读。非武器为 null |
| `resistTypes` | `GameplayTag[]` | 防具抗性类型列表。非防具为 null |

`StatOverride` 结构：

```csharp
[Serializable]
public struct StatOverride
{
    public StatDefinitionSO stat;   // 拖拽引用 —— Assets/Data/Stats/Definitions/ 下的 StatDefSO
    public float value;             // 覆写值 —— 覆盖 StatsTreeSO 里该 stat 的 Default
}
```

**覆写语义**：`Resolve()` 时，`statsTree.Resolve()` 先产出所有 stat 的 Default 值。然后遍历 `overrides`，找到对应 StatInstance 并覆写其值为 `OverrideValue`。未在 `overrides` 中出现的 stat 保持 StatsTreeSO 的默认值。

### 兼容性组

| 字段 | 类型 | 说明 | 适用 |
|------|------|------|------|
| `platform` | `GameplayTag` | 所属平台，同平台零件可互换 | 所有零件/武器 |
| `compatibleAmmo` | `GameplayTag[]` | 兼容的弹药口径 | 枪械 Receiver / 枪管 |

兼容性匹配规则（安装零件时）：

```
1. 槽位的 acceptTag ⊆ 候选 GearDefSO.gearType
2. 候选 GearDefSO.platform == Receiver.platform
3. 候选 GearDefSO.compatibleAmmo 与 Receiver.compatibleAmmo 有交集
```

### 结构组

| 字段 | 类型 | 说明 |
|------|------|------|
| `slots` | `GearSlot[]` | 可安装其他装备的槽位。`[]` = 成品/独立零件，非空 = 根零件 |

`GearSlot` 结构：

```csharp
[Serializable]
public struct GearSlot
{
    public string slotId;            // 槽位标识 —— "Barrel", "Magazine", "Muzzle", "Optic", "Underbarrel"
    public GameplayTag acceptTag;    // 接受什么 gearType 的装备 —— "Equipment.Part.Barrel"
    public bool required;            // 是否必须安装才能正常运作
}

// 示例 —— Glock 17 枪身的 slots:
// { slotId:"Barrel",    acceptTag:"Equipment.Part.Barrel",    required:true  }
// { slotId:"Slide",     acceptTag:"Equipment.Part.Slide",     required:true  }
// { slotId:"Magazine",  acceptTag:"Equipment.Part.Magazine",  required:true  }
// { slotId:"Trigger",   acceptTag:"Equipment.Part.Trigger",   required:true  }
// { slotId:"Grip",      acceptTag:"Equipment.Part.Grip",      required:true  }
// { slotId:"Muzzle",    acceptTag:"Equipment.Part.Muzzle",    required:false }
// { slotId:"Optic",     acceptTag:"Equipment.Part.Optic",     required:false }
// { slotId:"Underbarrel", acceptTag:"Equipment.Part.Underbarrel", required:false }
```

### 资源组

| 字段 | 类型 | 说明 |
|------|------|------|
| `visualPrefab` | `GameObject` | 3D 模型 Prefab |
| `animationProfile` | `AnimationOverrideProfileSO` | 动画覆写配置（持枪姿势、换弹动画等） |
| `audioProfile` | `AudioProfileSO` | 音效配置（击发、换弹、空仓） |

### 经济组

| 字段 | 类型 | 说明 |
|------|------|------|
| `baseValue` | `float` | 基础交易价值 |
| `salvageOutputs` | `SalvageEntry[]` | 拆解产出（物品 + 数量） |

---

## 资产示例

### 近战武器（成品，无槽位）

```
GearDefSO "RustyKnife"
  displayName:    "生锈的刀"
  gearType:       Equipment.Weapon.Melee
  statsTree:      Blade (MeleeWeapon)
  overrides:
    (Melee_ATK,        3)
    (Melee_AttackSpeed, 0.7)
    (Melee_Reach,       0.6)
    (StaminaCost,       2)
    (Weight,            0.3)
  damageType:     Damage.Slash
  platform:       null
  compatibleAmmo: null
  slots:          []                        ← 成品，不能装东西
```

### 枪械 Receiver（根零件，有槽位）

```
GearDefSO "Glock17_Frame"
  displayName:    "Glock 17 枪身"
  gearType:       Equipment.Weapon.Ranged.Receiver
  statsTree:      Pistol
  overrides:
    (Weight,      0.25)                    ← 枪身本身不贡献 ATK
  damageType:     Damage.Pierce
  platform:       Platform.Glock
  compatibleAmmo: [Caliber.9mm]
  slots:
    { Barrel,      Equipment.Part.Barrel,      required:true  }
    { Slide,       Equipment.Part.Slide,       required:true  }
    { Magazine,    Equipment.Part.Magazine,    required:true  }
    { Trigger,     Equipment.Part.Trigger,     required:true  }
    { Grip,        Equipment.Part.Grip,        required:true  }
    { Muzzle,      Equipment.Part.Muzzle,      required:false }
    { Optic,       Equipment.Part.Optic,       required:false }
    { Underbarrel, Equipment.Part.Underbarrel, required:false }
```

### 独立零件（无槽位）

```
GearDefSO "9mm_Standard_Barrel"
  displayName:    "9mm 标准枪管"
  gearType:       Equipment.Part.Barrel
  statsTree:      Barrel
  overrides:
    (BarrelLength,     4)
    (Accuracy_Bonus,   +5)
    (Weight,            0.1)
  damageType:     null                       ← 零件不直接产生伤害
  platform:       Platform.Glock
  compatibleAmmo: [Caliber.9mm]
  slots:          []                         ← 枪管上不能再装东西
```

### 防具（成品）

```
GearDefSO "PlateCarrier"
  displayName:    "插板背心"
  gearType:       Equipment.Armor.Body
  statsTree:      BodyArmor
  overrides:
    (DEF,              20)
    (Coverage,         60)
    (TraumaTransfer,   30)
    (Weight,           5)
    (MoveSpeedPenalty, 15)
    (KnockdownBonus,   20)
    (CarryWeightBonus, 15)
  damageType:     null
  resistTypes:    [Damage.Pierce, Damage.Slash]
  slots:          []                         ← 成品，不可拆
```

---

## 数据流

### Resolve 流程

```
GearDefSO.Resolve():
  1. statsTree.Resolve()
     → 沿 InheritsFrom 链合并所有祖先树
     → 产出 StatInstance[]（每个 stat 的值 = StatDefSO.Default）
     → 例: ATK=15, Accuracy=70, MagSize=10

  2. 应用 overrides
     foreach override in overrides:
         stat = FindStatInstance(override.stat)
         stat.Override(override.value)
     → StatInstance[]（覆写后的实际值）
     → 例: Weight=0.25 (枪身)。组装后整枪 stat 由各零件累加得出

  3. 返回 StatInstance[]
     → 这些实例的 Current = 装备的物理属性
     → 被 EquipmentComponent 用来 AddModifier 到角色 Stats
     → 被 Ability Pipeline ⑤ Effects 读取 baseDamage
```

### Resolve 后消费

```
┌─────────────────────┐
│   GearDefSO         │
│   .Resolve()        │
│   → StatInstance[]  │
└────────┬────────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌─────────┐ ┌──────────────┐
│ Pipeline│ │ Equipment    │
│ ⑤ Effects│ │ Component    │
│         │ │              │
│ hit.    │ │ actorStats   │
│ Incoming│ │ .AddModifier │
│ Damage  │ │ (gear, stat. │
│ = stat  │ │  Current)    │
│ ["ATK"] │ │              │
│ .Current│ └──────────────┘
└─────────┘
```

---

## 变种方案

一个装备变种 = 一个 `GearDefSO.asset`。同族装备（如所有 Pistol 变种）共享同一个 `statsTree` 引用，差异完全在 `overrides[]` 里。

```
Assets/Data/Equipment/Gear/Pistol/
├── Glock17_Frame.asset   statsTree → Pistol,  Weight=0.25 (Receiver)
├── M1911_Frame.asset      statsTree → Pistol,  Weight=0.30 (Receiver)
├── DesertEagle_Frame.asset statsTree → Pistol, Weight=0.45 (Receiver)
├── P226_Frame.asset        statsTree → Pistol,  Weight=0.28 (Receiver)
└── ...
```

每个 .asset 可被掉落表/任务/配方拖拽引用。Unity 序列化支持唯一的文件引用句柄（GUID），不需要字符串 ID 系统。

**批量生成**：编辑器工具 `GearDefImporter` 从 JSON 表（策划维护）批量生成/更新这些 .asset 文件。策划不手动建几百个资产。

---

## 设计决策

| 决策 | 原因 |
|------|------|
| `overrides` 用 `(StatDefSO, float)` 而非 `treeJson` | GearDefSO 不做结构继承，只覆写叶子值。StatDefSO 拖拽引用比 treeJson 的 DefRef 索引更直观 |
| `gearType` 用 GameplayTag 不用枚举 | 新零件类型加 tag 即可，不改代码 |
| `slots` 用 `acceptTag` 而非 `PartType` 枚举 | 同一理由：扩展性 |
| `platform` 独立于 `gearType` | 同是枪管，Glock 枪管和 AK 枪管不互通。platform 锁定生产商/枪族 |
| 原版 GearDefSO 不嵌词条 | 同一把 Glock 17 不同存档有不同词条。词条在 GearInstance 运行时挂载 |
| **槽位留在 GearDefSO，不进 StatsTreeSO** | 槽位（id+tag+bool）与 stat 本质不同——无运行时行为、无需帧驱动、无 Min/Max 钳制。StatsTreeSO 保持 ~20 棵不变。MountPoint 未来替换 slots 字段，不影响其他任何字段 |
| **MountPoint 延后设计** | GearSlot → MountPoint 迁移不改数据结构——同一 GameplayTag 类型，换标签语义。在有实际游戏内容验证前不定义物理接口规格 |
