# Equipment System — 装备系统设计

> 2026-06-08 · 设计文档 · 装备的数据模型、变种体系、零件组装、词条叠加

## 依赖

- 伤害地基 → [damage-source-model.md](damage-source-model.md) — 装备定义伤害基底，Ability 不持有 Damage Effect
- 数值骨架 → `L3-stats/` — StatDefSO 定义属性原子，StatsTreeSO 定义属性集合与结构继承
- 技能管道 → `L3-ability/ability-pipeline-design.md` — ⑤ Effects 阶段从装备获取 DamageSource

---

## 一、核心原则

**装备决定伤害地基，人物和环境做加减乘除。**

```
伤害 = 装备基底 × 人物修正 × 环境修正 × 命中修正
       ────────   ────────   ────────   ────────
       装备体系     属性/技能    天气/地形    部位/角度
       (固定值)    (±%)        (±%)        (±% × multiplier)
```

Ability 是**动作模式**（怎么挥刀 / 怎么开枪 / 怎么包扎），不持有伤害值。同一把刀，LightCut 和 HeavyChop 的区别是动作倍率，不是刀的锋利度变了。

---

## 二、资产分层

### 2.1 两层架构

```
┌──────────────────────────────────────────────────┐
│  定义层 — 回答"这类东西有哪些属性"                 │
│  StatDefSO × 154    ← 世界上存在哪些属性原子       │
│  StatsTreeSO × ~20  ← 每类东西由哪些属性构成       │
│  (结构继承: Pistol : Firearm : RangedWeapon)      │
└────────────────────┬─────────────────────────────┘
                     │ 被引用
┌────────────────────┴─────────────────────────────┐
│  实例层 — 回答"这个东西每个属性是多少"             │
│  GearDefSO × ~300    ← 装备 / 零件 / 武器          │
│  MaterialDefSO × ~50 ← 材料 / 资源 / 消耗品        │
│  AIDefSO × ~30       ← NPC / 敌人 / 生物           │
│  (数值覆写: statsTree + overrides)                │
└──────────────────────────────────────────────────┘
```

**定义层不膨胀**——StatsTreeSO 解决结构继承（Pistol 比 Firearm 多 HolsterSpeed），保持在 ~20 棵。  
**实例层暴增不失控**——GearDefSO 只存覆写值，不重走继承链。300 个 .asset 对 Unity 微不足道。

### 2.2 为什么 StatsTree 不能一路长到叶子

StatsTreeSO.Resolve() 按 node.Id 合并（子覆盖父），同一棵树里不能存在两个同名节点。树为结构继承设计，不为数值变种：

| 树擅长 | 树不擅长 |
|--------|---------|
| "Pistol 比 Firearm 多了 HolsterSpeed" | "Glock 17 和 Desert Eagle 只是 ATK 值不同" |
| 父子合并时追加新 stat 节点 | 同一 stat 集合 200 种取值 |

---

## 三、GearDefSO — 装备资产

### 3.1 统一模型：万物皆 GearDefSO

一把剑、一根枪管、一个消音器都是 `GearDefSO`。区别仅在于是否拥有子槽位：

```csharp
public class GearDefSO : ScriptableObject
{
    // ── 身份 ──
    public string displayName;
    public string description;
    public Sprite icon;
    public GameplayTag gearType;         // Weapon.Ranged.Receiver / Part.Barrel / Armor.Body / Consumable.Medical

    // ── 数值骨架 ──
    public StatsTreeSO statsTree;        // ← 引用类型树，定义"有哪些 stat"
    public StatOverride[] overrides;     // ← 覆写值，"每个 stat 是多少"
    public GameplayTag damageType;       // ← 伤害管线直读。防具/工具为 null
    public GameplayTag[] resistTypes;    // ← 防具用。武器为 null

    // ── 零件兼容性 ──
    public GameplayTag platform;         // ← 同平台零件可互换（"Platform.Glock"）
    public GameplayTag[] compatibleAmmo; // ← 弹药口径（"Caliber.9mm"）。非枪械为 null

    // ── 槽位结构 ──
    public GearSlot[] slots;             // ← [] = 成品/独立零件。非空 = 可装其他装备

    // ── 资源引用 ──
    public GameObject visualPrefab;
    public AnimationOverrideProfileSO animationProfile;
    public AudioProfileSO audioProfile;

    // ── 经济 ──
    public float baseValue;
    public SalvageEntry[] salvageOutputs;
}

[Serializable]
public struct StatOverride
{
    public StatDefinitionSO stat;   // ← 拖拽引用 StatDefSO
    public float value;
}

[Serializable]
public struct GearSlot
{
    public string slotId;           // "Barrel", "Magazine", "Muzzle"...
    public GameplayTag acceptTag;   // 接受什么 gearType 的装备（"Equipment.Part.Barrel"）
    public bool required;           // 必须安装才能正常运作
}
```

### 3.2 变种方案：一个变种一个 .asset

每种具体的装备/零件都是独立的 `.asset` 文件。不建族内表格，不用 JSON/CSV 替代。

**理由**：装备需要被拖拽引用——掉落表、任务奖励、交易清单、制作配方都需要唯一的引用句柄。JSON 字符串 ID 无法被 Unity 序列化引用。

```
Assets/Data/Equipment/
├── Weapon/
│   ├── Receiver/
│   │   ├── Glock17_Frame.asset
│   │   ├── MP5_Frame.asset
│   │   ├── AK47_Frame.asset
│   │   └── ...
│   ├── Barrel/
│   │   ├── 9mm_Standard_Barrel.asset
│   │   ├── 9mm_Extended_Barrel.asset
│   │   └── ...
│   ├── Magazine/
│   │   ├── Glock_17rd_Mag.asset
│   │   └── ...
│   ├── Muzzle/
│   │   ├── 9mm_Suppressor.asset
│   │   └── ...
│   ├── Optic/
│   │   ├── RedDot_Sight.asset
│   │   └── ...
│   └── Melee/
│       ├── RustyKnife.asset
│       ├── SteelBlade.asset
│       └── FireAxe.asset
├── Armor/
│   ├── Head/
│   │   ├── RiotHelmet.asset
│   │   └── BallisticMask.asset
│   └── Body/
│       ├── PlateCarrier.asset
│       └── Chainmail.asset
├── Ammo/
│   ├── 9mm_Standard.asset
│   ├── 9mm_AP.asset
│   └── ...
└── Tool/
    ├── AxeTool.asset
    └── ...
```

### 3.3 三种形态

| slots | 含义 | 例子 |
|-------|------|------|
| `[]` | 成品 / 独立零件，不能装东西 | 剑、头盔、枪管、消音器、绷带 |
| `[...]` | 可装其他装备（根零件） | 手枪枪身、步枪枪身、复合弓身 |

近战武器是成品（`slots = []`），枪支是零件树（Receiver 的 `slots` 非空）。

---

## 四、枪支 = 零件组装

### 4.1 零件类型

用 GameplayTag 替代枚举，可在不修改代码的情况下扩展新零件类型：

| 零件类型 (gearType) | 职责 | 必装 | 贡献的核心 stat |
|---------------------|------|------|----------------|
| `Equipment.Part.Receiver` | 武器根。定义平台、口径、槽位 | 是 | Weight, Durability（极少）。注：枪身也是零件的一种——它被"安装"到角色的主武器槽位 |
| `Equipment.Part.Barrel` | 弹道核心 | 是 | BarrelLength, MuzzleVelocity 基数, Accuracy |
| `Equipment.Part.Slide` | 自动循环 | 是 | FireRate, Reliability, Recoil |
| `Equipment.Part.Magazine` | 供弹 | 是 | MagSize, ReloadSpeed |
| `Equipment.Part.Trigger` | 击发控制 | 是 | FireRate 微调，可选射击模式 |
| `Equipment.Part.Grip` | 人机工效 | 视平台 | Recoil, HipFirePenalty, HolsterSpeed |
| `Equipment.Part.Muzzle` | 枪口装置 | 否 | NoiseRadius, MuzzleVelocity, Recoil |
| `Equipment.Part.Optic` | 瞄准具 | 否 | AimTime, ScopeZoom, Accuracy |
| `Equipment.Part.Underbarrel` | 下挂 | 否 | Recoil, Accuracy, HipFirePenalty |

### 4.2 兼容性匹配

```
安装检查:
  1. 槽位的 acceptTag ⊆ 候选 GearDefSO.gearType   ← "这个槽位接受枪管吗"
  2. 候选 GearDefSO.platform == Receiver.platform  ← "这根枪管是 Glock 平台的吗"
  3. 候选 GearDefSO.compatibleAmmo ⊆ Receiver.compatibleAmmo  ← "口径对得上吗"
```

### 4.3 运行时组装

```csharp
public class WeaponAssembly
{
    public GearInstance receiver;
    public Dictionary<string, GearInstance> slots;   // slotId → 零件实例

    public StatInstance[] Resolve()
    {
        // 1. receiver.Resolve() → 枪身自身 stat
        // 2. 遍历所有已安装零件，逐零件 Resolve() → StatInstance 逐项累加
        // 3. 返回求和后的 StatInstance[]
    }
}
```

**管线不关心零件结构**——它只管从组装结果读最终数值。

### 4.4 装备槽位（身体）

角色身上的装备位：

```
CharacterActor → EquipmentComponent
  ├── PrimaryWeapon     ← WeaponAssembly (步枪/霰弹枪/弓)
  ├── SecondaryWeapon   ← WeaponAssembly (手枪/SMG)
  ├── Melee             ← GearInstance (刀/斧/棍)
  ├── Helmet            ← GearInstance (HeadArmor)
  ├── BodyArmor         ← GearInstance (BodyArmor)
  ├── LegArmor          ← GearInstance (LegArmor)
  ├── Backpack          ← GearInstance (决定负重上限)
  └── Throwable         ← GearInstance (手雷/燃烧瓶)
```

---

## 五、数值叠加流程

一把武器最终 ATK 来自三个层面：

```
最终 ATK = 类型基线 + 型号覆写 + 零件叠加 + 词条修正

  ④ 词条 "大口径弹膛"    +5 ATK        ← AffixDefSO (可随机/可锻造)
  ② 型号覆写             ATK=12 (枪管) ← GearDefSO.overrides (每个零件的型号值)
  ③ 零件叠加             组装求和      ← 各零件 stat 递归累加
  ① 类型基线             ATK=15        ← StatsTreeSO.Default (占位值)
```

### 5.1 AffixDefSO — 词条

```csharp
public class AffixDefSO : ScriptableObject
{
    public string displayName;           // "大口径弹膛"
    public AffixTier tier;               // Common / Uncommon / Rare / Epic
    public string description;           // "+5 攻击力"
    public GameplayTag[] tags;           // ["Affix.Damage", "Affix.Weapon"]
    public StatModifierTemplate[] modifiers;
}

[Serializable]
public struct StatModifierTemplate
{
    public StatDefinitionSO stat;        // Ranged_ATK
    public EModifierMode mode;           // Addend / Multiplier
    public float value;
}
```

词条不写入 GearDefSO——同一把 Glock 17，不同存档可能有不同词条。词条挂载在运行时的 `GearInstance.activeAffixes` 上。

### 5.2 Resolve 顺序

```
EquipmentComponent.OnEquip(gear):
  ① 类型基线             ATK=15        ← StatsTreeSO.Default (占位值)
  ② 型号覆写             ATK=12 (枪管) ← GearDefSO.overrides (每个零件的型号值)
  ③ 零件叠加             组装求和      ← 各零件 stat 递归累加
  ③ 词条叠加: 遍历 instance.activeAffixes → AddModifier()  → StatInstance[] (ATK=35)
  ④ 桥接到角色: actorStats.Get(def).AddModifier(gear, gearStat.Current)
```

---

## 六、运行时实例（GearInstance）

GearDefSO 是定义资产（共享），GearInstance 是运行时的装备个体（独立）：

```csharp
public class GearInstance
{
    public GearDefSO def;                    // ← 共享的资产引用
    public float durability;                 // ← 独立：当前耐久
    public List<AffixDefSO> activeAffixes;   // ← 独立：随机/锻造的词条
    public Dictionary<string, GearInstance> installedParts; // ← 独立：已安装的子零件（仅 Receiver）
    public int currentAmmo;                  // ← 独立：弹匣内剩余弹药
}
```

同一把 Glock 17，地上的两把有不同的耐久、不同的配件、不同的词条。

---

## 七、其他实例层资产

### 7.1 MaterialDefSO — 材料 / 消耗品

```csharp
public class MaterialDefSO : ScriptableObject
{
    public string displayName;
    public GameplayTag materialType;    // Resource.Wood / Resource.Metal / Consumable.Food
    public StatsTreeSO statsTree;       // ← 同一数值骨架
    public StatOverride[] overrides;
    public Sprite icon;
    public int maxStackSize;            // ← 与装备的核心区别：可堆叠
    public float unitWeight;
    public GameplayTag[] craftTags;
}
```

### 7.2 AIDefSO — NPC / 敌人

```csharp
public class AIDefSO : ScriptableObject
{
    public string displayName;
    public GameplayTag aiType;          // Enemy.Zombie / NPC.Trader / Creature.Wolf
    public StatsTreeSO statsTree;       // ← Actor 族 StatsTree
    public StatOverride[] overrides;
    public GameObject visualPrefab;
    public AIDefSO[] behaviors;      // ← 行为树引用（非 stat）
    public GameplayTag faction;         // ← 阵营（非 stat）
    public LootTableSO lootTable;       // ← 掉落表（非 stat）
}
```

### 7.3 共性

三种实例资产的共性：**statsTree + overrides**，依靠同一套 Stat 骨架。差异在各自由业务字段——堆叠、槽位、行为树、阵营。这些差异用 StatsTree 吞不掉，因为它们概念上就不属于 stat。

---

## 八、装备系统边界

### 紧耦合（影响 GearDefSO 数据结构）

| 系统 | 数据需求 | 承载位置 |
|------|---------|---------|
| 弹药兼容 | 枪的 compatibleAmmo 决定能用什么子弹 | GearDefSO.compatibleAmmo |
| 零件组装 | Receiver 的 slots 定义可装零件 | GearDefSO.slots |
| 身体槽位 | 双手/单手/头盔/护甲槽位限制 | EquipmentComponent |
| 耐久与报废 | 当前耐久、归零行为、低耐久惩罚 | GearInstance.durability |
| 词条 | 随机/固定/锻造词条 | AffixDefSO + GearInstance.activeAffixes |
| 视觉效果 | 3D 模型、挂载骨骼、收起位置 | GearDefSO.visualPrefab |
| 动画覆写 | 手枪单手 vs 步枪抵肩 vs 弓拉弓 | GearDefSO.animationProfile |
| 音频 | 击发/换弹/空仓音效 | GearDefSO.audioProfile |
| 经济 | 基础价值、拆解产出 | GearDefSO.baseValue |

### 中耦合（消费装备数据但不修改结构）

| 系统 | 数据需求 | 承载位置 |
|------|---------|---------|
| 背包与负重 | 所有权重之和 → 移动惩罚 | EquipmentComponent.totalWeight |
| 掉落生成 | 掉落表 → GearDefSO 引用 → GearInstance | LootTableSO |
| 制造与拆解 | 配方输入/输出、品质系数 | CraftingRecipeSO |
| UI / 背包界面 | 显示名称、图标、耐久度、词条列表 | GearDefSO + GearInstance |

---

## 九、与 Ability Pipeline 的接口

AbilityExecutor ⑤ Effects 阶段：

```
TryActivate():
  ⑤ Effects:
    assembly = Equipment.GetWeaponAssembly()
    hit.IncomingDamage = assembly.Resolve()["ATK"].Current     ← 所有零件求和
    hit.DamageType = assembly.receiver.damageType              ← Receiver 决定
    hit.StaggerValue = assembly.Resolve()["Stagger"].Current
    
    // IEffectModifier 链: 仅近战注册（力量/熟练度修正）
    EffectCallback?.Invoke(ctx, hit, target)
    
    target.HitReactionComponent.Resolve(hit)
```

管线不感知零件结构——它只读最终统计值。

---

## 十、设计决策记录

| 决策 | 原因 |
|------|------|
| 装备定义伤害基底，Ability 不持有 Damage Effect | 硬核生存现实映射。生锈的刀和精制钢刀是不同的物理物体。详见 [damage-source-model.md](damage-source-model.md) |
| 两层架构（定义层 + 实例层），不合并 | StatsTree.Resolve() 按 Id 合并，不适合存同一 stat 的 200 种取值。实例层做数值覆写，定义层做结构继承 |
| **槽位留在 GearDefSO，不进 StatsTreeSO** | [辩论结论] 槽位（id+tag+bool）是无行为的静态结构，StatInstance 是帧驱动的数值实例。把最轻的数据塞进最重的解析管道会：(1) 污染 Resolve() 单一职责，(2) 无法处理槽位横向复用（Optic 跨手枪/步枪/弩），(3) StatsTreeSO 没有"移除继承槽位"机制，(4) n 个可选槽位 = 2^n 棵树的组合爆炸。槽位集合是类型属性，但"类型属性"不等于"必须在 StatsTreeSO 里"——displayName/icon/visualPrefab 也是类型属性，也在 GearDefSO 上 |
| **MountPoint 延后设计，GearSlot 作为占位** | [辩论结论] GearSlot → MountPoint 的迁移不改数据结构——两者用同一个 GameplayTag 做匹配媒介，换的是标签语义（Equipment.Part.* → Mount.*），不是字段类型。多零件→完整装备的三段式（暴露槽位→声明兼容→匹配函数）已被当前模型捕捉。在没有足够游戏内容验证前，定义物理接口规格是空中楼阁 |
| 一个变种一个 .asset | 装备需要被掉落表/任务/配方拖拽引用。Unity 序列化不支持引用 JSON 字符串 ID。350 个 .asset 在 Unity 里微不足道 |
| 枪支 = 零件组装，Receiver 为根 | 每一把枪都是玩家自己拼出来的，没有两把完全一样的枪。零件兼容性由 platform + gearType tag 匹配 |
| 零件类型用 GameplayTag 不用枚举 | 新零件类型（弹链供弹机、下挂榴弹）加 tag 即可，不修改代码 |
| 万物皆 GearDefSO | 枪管和剑在数据层完全平权——都能捡、交易、损坏、修理。区别仅 slots 是否为空 |
| 近战无零件树 | 一把斧子不需要组装。复合弓远期可支持零件 |
| 三层数值叠加（基线→覆写→词条） | 类型基线共享，型号覆写差异化，词条提供随机/锻造深度 |
| 词条不写入 GearDefSO | 同一把 Glock 17 不同存档有不同词条。词条挂载在运行时的 GearInstance 上 |
| MaterialDefSO / AIDefSO 同理 | 万物共用 statsTree + overrides 骨架，仅业务字段不同 |
| 装备属性走 StatOverride[] 而非 treeJson | GearDefSO 不做结构继承，只覆写叶子值。Inspector 编辑更友好，策划无需接触 treeJson |
| StatsTreeSO 保持 ~20 棵不膨胀 | 结构继承不重复计算。GearDefSO 做缓存层，不重走继承链 |

---

## 十一、下一步

1. **GearDefSO 落地** — 脚本 + Editor 工具（从 StatsTree + JSON 表批量生成装备资产）
2. **GearInstance 运行时** — 组装、耐久、词条应用
3. **EquipmentComponent** — 身体槽位、装备/卸下 API、负重计算
4. **零件组装** — GearInstance 递归 Resolve 求和 + 兼容性校验
5. **AffixDefSO** — 词条资产 + 随机词条池 + 叠加逻辑
6. **掉落与制造** — LootTableSO → GearInstance 工厂、CraftingRecipeSO
