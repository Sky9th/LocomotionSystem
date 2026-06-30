# Property System — 通用属性系统

> 2026-06-09 · 设计文档 · 替代 Stats 体系，承载所有实体类型的属性定义、模板继承、实例覆写

## 依赖

- 标签体系 → `L1-core/gameplay-tag.md` — PropertyDefSO 的 GameplayTag 类型依赖 Tag 系统做校验
- 伤害地基 → [damage-source-model.md](damage-source-model.md) — ATK 是 DamageEffectSO[]，不依赖属性系统，但属性系统需支持 AssetRefList 类型
- 物品系统 → [L3_Item](../L3-item/README.md) — ItemDefSO : PropertyPresetSO，所有数据进 PropertyTree
- 容器系统 → [L3_Container](../L3-container/README.md) — SlotDef[] 通过 PropertyType.Struct 存储
- ~~装备系统~~ — GearDefSO 模型已否决。装备 = 物品在身体槽中的状态
- ~~Stats 系统~~ — 已由 Properties 替代。迁移已完成
- **属性清单** → [property-inventory.md](property-inventory.md) — 全量属性与属性树设计（~180 props, 29 trees, 8 族）

---

## 一、根隐喻

**PropertyTree = 数据库 Schema 表。继承链 = DDL 逐级叠加。实例覆写 = INSERT 行。**

```
DDL (PropertyTree · 纯结构，不含值)
  WeaponBase:   { Durability:float, Weight:float }
  Firearm : WeaponBase
    + { ATK:AssetRef[], FireRate:float, Animation:AssetRef, Audio:AssetRef }
  Pistol : Firearm
    + { HolsterSpeed:float, HipFirePenalty:float }

INSERT (GearDefSO overridesJson · 值在这里)
  Glock17_Frame: { ATK: ["Pierce_15","Bleed_3"], Weight: 0.25 }
  未覆写的属性 (FireRate, HolsterSpeed 等) → 取 PropertyDefinition.Default
```

不是"数值系统"，是**属性系统**。Float 只是其中一种列类型。

---

## 二、核心原则

1. **Schema 和 数据分离** — PropertyTree 定义有哪些属性（纯结构，不含值）。PropertyDefinition 持有唯一的默认值。GearDefSO / ActorDefSO / BuildingDefSO 等实例负责给值：覆写了用覆写值，没覆写取 PropertyDefinition.Default。Tree 不做值覆盖——它没有值可覆盖。

2. **类型优先于值** — PropertyDefinition 声明了属性的类型、约束。所有覆写都必须通过类型校验。不可能出现"ATK 被塞了一个 string"。

3. **继承链只做增量** — 子模板继承父模板的全部属性，只能新增，不能修改、禁用、替换父级的任何节点。合并 = 取并集。

4. **Properties 不替代 StatInstance 的运行时行为** — Modifier 累加、Tick 消耗、Min/Max 钳制是 float 运行时引擎的职责。Properties 产出的是"最终静态值"。float 的运行时行为怎么处理，是 Stats 删除时才需要回答的问题。

5. **新增属性 = 注册 Definition + 在 Tree 里加节点** — 不修改任何 .cs 文件。

---

## 三、领域模型

### 3.1 三个聚合根

```
PropertyDefinitionRegistry (全局，运行时只读字典)
  │
  │ 持有所有 PropertyDefinition 的索引
  │ 启动时扫描资产目录构建
  │
  ▼
PropertyTreeSO (模板聚合根)
  │
  │ 持有: treeJson (节点树) + InheritsFrom (继承链)
  │ Resolve() → ResolvedPropertyBag
  │
  ▼
GearDefSO / ActorDefSO / BuildingDefSO ... (实例聚合根)
  │
  │ 持有: template → PropertyTreeSO + overridesJson
  │ Resolve(template) → ResolvedPropertyBag
```

### 3.2 聚合根一：PropertyDefinitionRegistry

**职责**：全局属性定义注册表，编辑器专有。

**生命周期**：编辑器启动时 `[InitializeOnLoad]` 扫描 `Assets/Data/Properties/Definitions/`，构建 `Dictionary<string, PropertyDefSO>`。Play Mode 和 Build 中不需要——Resolve 在编辑时完成，运行时 Bag 已持原生值。

**操作**：
- `FindById(id)` → PropertyDefSO
- `FindByType(PropertyType)` → PropertyDefSO[]
- `Registry.Define(def)` → 创建新 .asset 并注册（仅编辑器）

**不变量**：
- Id 全局唯一（含已废弃的）
- PropertyType 创建后不可变更
- 被至少一个 PropertyTreeSO 引用的 Def 不可删除，仅可标记 `isDeprecated = true`

### 3.3 聚合根二：PropertyTreeSO（模板）

**职责**：定义一个实体类型的属性集合（纯结构，不含值）。支持沿继承链**增量合并**——子只能新增节点，不能修改或禁用父级节点。

**字段**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `InheritsFrom` | PropertyTreeSO | 父模板，null = 根 |
| `treeJson` | string | 本层节点 JSON |

不需要 `defRefs` 查表——节点直接存 PropertyDefSO 的全局 Id 字符串，Resolve 时从 Registry 解析。

**核心操作**：`Resolve() → 属性集合（纯结构）`

```
Resolve():
  1. CollectInheritedLayers()     → 自根向叶收集各层 treeJson
  2. MergeLayers() × N             → 取并集：同 Id 的节点保留最先出现的（祖先优先），
                                      子级同 Id 节点告警后忽略
  3. RefreshPaths()               → 构建路径 "Combat/ATK"、"Presentation/Icon"
  4. BuildPropertySet()           → 收集叶子节点 → 属性集合（仅结构，不含值）
```

**不变量**：
- InheritsFrom 链无环（Resolve 时用 visited Set 检测）
- 每个叶子节点的 DefId 必须在 Registry 中存在
- 合并结果中每个 Id 只出现一次（同 Id 保留祖先版本）
- 树中每个叶子节点必须关联一个 Def（DefId 非空）

**继承语义**：

| 场景 | 行为 |
|------|------|
| 父有，子未提及 | 继承该属性（在子类型上存在） |
| 父无，子新增 | 子自有属性 |
| 父有，子同名 | 冲突 → 告警，保留祖先的节点。子不能修改/禁用/替换父的属性。若子确实不需要父的某个属性，实例层不使用它就是——属性存在于集合中不影响任何行为 |

### 3.4 聚合根三：实例定义（GearDefSO 等）

**职责**：一个具体的装备/角色/建筑实例，覆写模板中声明的属性值。

**与当前 Stats 的关系**：当前 GearDefSO 上 18 个字段（含 float 的 `baseValue`、Tag 的 `gearType`、AssetRef 的 `icon` 等）全部收敛到 `overridesJson` 一个字段。

**核心操作**：`Resolve(PropertyTreeSO template) → ResolvedPropertyBag`

```
Resolve(template):
  1. template.ResolveStructure()           → 属性集合，Path → Def 映射
  2. 解析 overridesJson → Dictionary<string, string> (Path → raw value)
  3. foreach (path, def) in 属性集合:
       if overrides 有 path 对应的 rawValue:
         校验 rawValue 可解析为 def.Type 类型的值
         校验 Float/Int 满足 Min/Max
         → 解析为原生值
       else:
         → 取 def.Default
  4. → ResolvedPropertyBag（按类型分桶存原生值）
```

**不变量**：
- overridesJson 的每个 key 是树的 Path（如 `"Combat/ATK"`），必须在模板合并结果中存在
- rawValue 必须可解析为对应 Def.Type 的目标类型
- overridesJson 本身只存 raw string，不存类型信息。类型由模板 Def 提供

### 3.5 值对象：ResolvedPropertyBag

**职责**：承载一个实体解析后的最终属性值。纯数据，无运行时行为。

```
ResolvedPropertyBag（内部按类型分桶存储原生值，构造时一次解析。无 string→float 运行时开销）
  ├── GetFloat(path)           → float
  ├── GetInt(path)             → int
  ├── GetBool(path)            → bool
  ├── GetString(path)          → string
  ├── GetTag(path)             → GameplayTag
  ├── GetTagList(path)         → GameplayTag[]
  ├── GetAsset<T>(path)        → T (T : UnityEngine.Object)
  ├── GetAssetList<T>(path)    → T[]
  └── TryGet(path)             → bool
```

---

## 四、类型系统

### 4.1 PropertyType 枚举

```
Float           浮点数      Weight, FireRate, Durability
Int             整数        MagSize, PelletCount
Bool            布尔        isAutomatic, IsTwoHanded
String          文本        DisplayName, Description
GameplayTag     标签引用    DamageType, GearType, Platform
GameplayTagList 标签数组    CompatibleAmmo, ResistTypes
AssetRef        SO/资产引用  Icon, VisualPrefab, AnimationProfile
AssetRefList    资产引用数组   ATK (DamageEffectSO[])
```

### 4.2 PropertyValue — 类型化值容器

```csharp
[Serializable]
public struct PropertyValue
{
    public PropertyType Type;
    public string SerializedValue;  // JSON / GUID

    // 空串 = 未覆写 → 走 PropertyDefinition.Default
    public bool HasValue => !string.IsNullOrEmpty(SerializedValue);
    public static PropertyValue None => new() { SerializedValue = null };
}
```

**只存在于实例层（overridesJson）和 ResolvedPropertyBag 中。** 模板不持有 PropertyValue——模板只持有结构（哪些属性）。

### 4.3 各类型的 SerializedValue 格式

| PropertyType | 格式 | 示例 |
|---|---|---|
| Float | `"12.5"` | Float.ToString("G") |
| Int | `"3"` | Int32.ToString() |
| Bool | `"true"` / `"false"` | |
| String | `"格洛克 17"` | 直接字符串 |
| GameplayTag | `"Damage.Pierce"` | Tag 全路径（需在 Tag 系统中存在） |
| GameplayTagList | `"["Damage.Pierce","Damage.Slash"]"` | JSON 数组 |
| AssetRef | `"guid://0a1b2c3d..."` | GUID 格式，运行时 AssetDatabase.LoadAssetAtPath 或 Addressables |
| AssetRefList | `"["guid://...","guid://..."]"` | JSON GUID 数组 |

### 4.4 PropertyDefSO — 列定义

按类型显示不同 Inspector 字段组：

```
所有类型共有:
  Id: string                    属性唯一标识
  PropertyType: enum            类型（创建后不可变）
  isDeprecated: bool            是否已废弃

Float 专有:
  Min: float                    最小值
  Max: float                    最大值
  DefaultFloat: float           默认值（唯一默认值来源）

Int 专有:
  MinInt: int
  MaxInt: int
  DefaultInt: int

Bool 专有:
  DefaultBool: bool

String 专有:
  DefaultString: string

GameplayTag / GameplayTagList 专有:
  无额外字段（默认值为空）

AssetRef 专有:
  DefaultAssetGUID: string
  AssetTypeConstraint: string   "UnityEngine.Sprite" / "UnityEngine.GameObject" 等

AssetRefList 专有:
  无额外字段（默认值为空数组）
```

**注意**：Float 无能力标记。运行时行为由伴生属性约定：

| 伴生后缀 | 含义 | 示例 |
|---------|------|------|
| `ConsumeRate` | 消耗型，每秒减此值 | `Vitals/Hunger` + `Vitals/HungerConsumeRate` |
| `RestoreRate` | 恢复型，每秒加此值 | `Vitals/Stamina` + `Vitals/StaminaRestoreRate` |
| （待定） | 累积型 | 同上模式 |

约定：伴生属性与主属性在**同一父文件夹下**，命名为 `{NodeId}{后缀}`。Bag 构造完成后，运行时检查 `{Path}ConsumeRate` 是否存在即知行为。不设显式 Flag。

### 4.5 PropertyNode — 树节点

```
NodeId: string        树内唯一 Id（叶子自定义，如 "Combat_ATK"）。不与 DefId 绑定——同一 Def 可在树的不同位置以不同 NodeId 出现
ParentId: string      父节点的 NodeId, ""=根。不存值，仅用于 RefreshPaths 构建路径
DefId: string         PropertyDefSO 的全局 Id。""=文件夹节点（等价于 !string.IsNullOrEmpty(DefId) 为叶子）

// 仅编辑器，不序列化
DefRef: PropertyDefSO 从 Registry.FindById(DefId) 解析（Resolve 时计算）
```

**不存在**：
- `IsFolder` — 冗余，`DefId==""` 即文件夹
- `IsOverride` / `IsEnabled` — 子不能覆写或禁用父属性。继承 = 纯增量并集
- `Depth` — 合并不需要优先级。编辑器需要时从 InheritsFrom 链长计算
- `Path` — 由 RefreshPaths 按父关系动态构建，不存储

### 4.6 PropertyTreeContainer — 序列化包装

```csharp
[Serializable]
public class PropertyTreeContainer
{
    public List<PropertyNode> Nodes = new();
}
```

PropertyTreeSO.treeJson = `JsonUtility.ToJson(container)`。

---

## 五、模板继承链设计

### 5.1 继承树（保留 equipment-tree-design.md 的层级）

```
WeaponBase           (3 属性: Durability, MaxDurability, Weight)
├── MeleeWeapon      (+ATK, AttackSpeed, CritMulti, StunChance, Knockback, Reach, StaminaCost)
├── RangedWeapon     (+ATK, Accuracy, ReloadSpeed, MagSize, AmmoCount, NoiseRadius, Recoil)
│   ├── Firearm      (+FireRate, MuzzleVelocity, BarrelLength, Reliability)
│   │   ├── Pistol   (+HolsterSpeed, HipFirePenalty, Animation, Audio, Icon, VisualPrefab...)
│   │   ├── Rifle    (+ScopeZoom, AimTime, Animation, Audio, ...)
│   │   └── Shotgun  (+PelletCount, Spread, ...)
│   └── Bow          (+DrawSpeed, ArrowVelocity, HoldStamina)
└── Throwable        (+BlastRadius, FuseTime)

AmmoBase             (9 属性)
├── PistolAmmo       (无新增属性，继承 AmmoBase 全部)
├── RifleAmmo        (无新增属性，继承 AmmoBase 全部)
└── ShotgunShell     (无新增属性，继承 AmmoBase 全部)

ArmorBase             (9 属性)
├── HeadArmor         (+FlashResist, NightVision)
├── BodyArmor         (+Knockdown, CarryWeight)
└── LegArmor          (+MoveSpeed, SneakSpeed)

ToolBase              (6 属性)
Building              (8 属性, 独立根)
Environment           (4 属性, 全局单例)
Actor                 (64 属性, Human/Zombie/Creature/Robot)
```

**关键变化**：之前非 float 属性（Animation、Audio、Icon、VisualPrefab、GearType、Platform、CompatibleAmmo 等）分散在 GearDefSO 的独立字段上。现在全部进入 PropertyTree 作为节点。每个树除了 float 战斗属性，还有 Presentation 分组（Icon/VisualPrefab/DisplayName）、Behavior 分组（Animation/Audio）、Compatibility 分组（Platform/CompatibleAmmo）。

---

## 六、合并算法语义

### 6.1 算法伪码

```
CollectInheritedLayers(tree):
  → 自 InheritsFrom 链根向叶遍历
  → 每层解析 treeJson → List<PropertyNode> (带 depth)
  → 检测循环引用 → 报错，停止合并

MergeLayers(layers):
  merged = new Dictionary<string, PropertyNode>()
  for each layer (from root to leaf):
    for each node in layer:
      if merged.ContainsKey(node.Id):
        // 子不能覆写父。告警，保留祖先版本
        Warn("同名冲突: {node.Id}，保留祖先")
        continue
      merged[node.Id] = node

RefreshPaths(merged):
  从 ParentId 为空或 "" 的根节点向下递归构建 Path

BuildPropertySet(merged):
  筛选 !IsFolder && DefRef != null 的节点
  → 属性集合（路径 → DefRef 映射，不含值）
```

### 6.2 与 StatsTreeSO 合并算法的差异

StatsTreeSO.MergeLayer() 按 `Id` 合并，子覆盖父（含值覆盖 + Def 替换）。PropertyTreeSO 的合并逻辑大幅退化：

1. **禁止覆盖** — 同 Id 的节点保留祖先。不存在 IsOverride / IsEnabled 概念。
2. **不操作值** — PropertyNode 没有值字段。合并只做节点收集。
3. **Reducer 引用** — Def 通过全局 Id 字符串引用 Registry，不由每棵树的 defRefs 数组管理。

---

## 七、消费模式

### 7.1 装备系统 (GearDefSO)

```
GearDefSO 实例字段:
  template: PropertyTreeSO       ← 指向 "Pistol"
  overridesJson: string          ← { "ATK": [...], "Weight": 0.25 }

GearDefSO.Resolve():
  propSet = template.Resolve()       ← Pistol 的属性集合（纯结构）
  overrides = Parse(overridesJson)   ← 实例覆写
  bag = new ResolvedPropertyBag()
  foreach prop in propSet:
    if overrides has key:
      Validate(overrides[key], prop.Def)  ← 类型 + 范围校验
      bag[prop.Path] = overrides[key]
    else:
      bag[prop.Path] = prop.Def.Default  ← 显式取 Def 默认值
  return bag

消费方:
  Pipeline ⑤ Effects:
    atk = bag.GetAssetList<DamageEffectSO>("Combat/ATK")   ← DamageEffectSO[]
    damageType = bag.GetTag("Combat/DamageType")            ← GameplayTag

  UI Tooltip:
    name = bag.GetString("Presentation/DisplayName")
    icon = bag.GetAsset<Sprite>("Presentation/Icon")

  AnimationBrain:
    anim = bag.GetAsset<AnimationOverrideProfileSO>("Behavior/Animation")

  EquipmentComponent:
    weight = bag.GetFloat("Base/Weight")
    durability = bag.GetFloat("Base/Durability")
```

### 7.2 角色系统 (ActorDefSO)

```
ActorDefSO 实例字段:
  template: PropertyTreeSO       ← 指向 "Human"
  overridesJson: string

消费方:
  VitalsOverlay:
    hp = bag.GetFloat("Vitals/HP")
    maxHp = bag.GetFloat("Vitals/MaxHP")

  Physiology Rules:
    hunger = bag.GetFloat("Vitals/Hunger")
    rate = bag.GetFloat("Vitals/HungerConsumeRate")  ← 伴生 Rate Def
    → 转化为运行时 StatInstance (Tick 消费)
```

### 7.3 建筑 / 工具 / 材料

同模式——各自有 template 引用 + overridesJson。

---

## 八、校验体系

### 8.1 编辑时校验（模板层）

模板不存值，校验只关注结构正确性：

| 规则 | 检测时机 | 违反后果 |
|------|---------|---------|
| InheritsFrom 链无环 | Resolve() | 报错，停止合并 |
| DefId 在 Registry 中存在 | Resolve() | 跳过该节点 + 告警 |
| 同 Id 冲突（子级声明了祖先已有的属性） | MergeLayers() | 告警 + 保留祖先版本 |
| 叶子节点 DefId 非空 | Resolve() | 跳过 + 告警 |

### 8.2 编辑时校验（实例层）

| 规则 | 检测时机 | 违反后果 |
|------|---------|---------|
| overridesJson 的 key 在模板中存在 | Resolve() | 告警 + 跳过 |
| 覆写值类型匹配 template 声明的 Def.PropertyType | Resolve() | 告警 + 跳过 |
| Float 覆写值满足 Def.Min ≤ value ≤ Def.Max | Resolve() | Clamp + 告警 |
| GameplayTag 覆写值在 Tag 系统存在 | Resolve() | 告警 + 跳过 |
| AssetRef GUID 有效且类型满足 Def.AssetTypeConstraint | Resolve() | 告警 + 跳过 |

### 8.3 构建时校验（CI / 资产处理器）

- 所有 PropertyTreeSO 中每个节点的 DefId 能在 Registry 中找到对应 Def
- 所有 GearDefSO 的 template 引用非空
- 所有 GearDefSO 的 overridesJson 可成功解析
- 无废弃 Def 仍被 Tree 引用（Warning 级别）

---

## 九、与 Stats 系统的关系（历史）

> ⚠ **Stats 迁移已完成。L3_Stats 模块已删除。** 此节仅作历史记录。

- `StatInstance`、`StatModifier` 保留——纯 float 运行时引擎，不依赖 Stats 定义系统。
- `FloatState` 是 StatInstance 的后继，由 PropertyAgent 管理帧驱动。

但如何从 ResolvedPropertyBag 创建 StatInstance 是阶段三才需要回答的问题。当前不过度设计。

---

## 十、设计决策

| 决策 | 原因 |
|------|------|
| Stats 不扩，新建 Properties 模块 | Stats 的名字、类型系统（纯 float）、合并算法 Sentinel（float.MinValue）都暗示数值。强行扩展增加认知负担 |
| 默认值只在 PropertyDefinition，模板不存值 | 模板的职责是"有哪些属性"，不是"值是什么"。旧 Stats 模型三处有值（Def.Default / Tree.OverrideValue / Instance.Override），冗余且混乱。现在只有两处：Def.Default（唯一默认值来源）和 Instance（覆写源）。实例必须对每个属性显式选择：覆写 or 取 Default |
| 全局 Registry | 和 GameplayTag 模式一致——属性定义是全局资源，不在每个 Tree 里独立定义 |
| overridesJson 不存类型信息 | 模板是 Schema。Schema 校验数据。不冗余存类型 |
| 合并 = 取并集，禁止覆盖 | 子模板只能新增属性，不能修改/禁用/替换父属性。多余属性实例层可以不覆写、不读取——存在于集合不影响任何行为。维持模型的极简性 |
| Def 用全局 Id 字符串引用，不用数组索引 | 避免 defRefs 数组中增删元素导致索引集体偏移、所有 treeJson 静默损坏。和 GameplayTag 一致——存路径字符串，运行时查字典 |
| PropertyDefinition 的 PropertyType 不可变 | 改类型可能导致已有数据的覆写值全部无效。正确做法：deprecate 旧 Def + 新建 |
| AssetRef 存 GUID 字符串 | JSON 可 diff、可批量编辑。Unity 原生资产引用（fileID + GUID）天然支持此格式 |
| 零能力标记，行为由伴生属性约定 | Hunger 是否为消耗型 → 看 Bag 里有无 `HungerConsumeRate`。不为 Hunger 单独声明一个 Flag。模型更纯——PropertyDefSO 只回答"这个属性值是什么" |

---

## 十一、Editor 实现 (v0.10.5)

### PropertyTreeEditorWindow

三栏布局：

| 栏 | 宽度 | 内容 |
|---|---|---|
| 左 | 320px | Tree 列表（继承链 + 本地节点数），搜索/新建子Tree/刷新 |
| 中 | expand | 树编辑：Folder 卡片（可编辑名称）嵌套 Property 卡片（名称 + Type）|
| 右 | 320px | Property Pool：Def 列表 + 搜索 + 拖拽创建 |

卡片风格：统一 `EditorUIUtility.DrawCard(Pad)`，继承属性灰底灰字。

### PropertyTreeListView

左侧 Tree 列表渲染，选中高亮 + 折叠展开。每个节点显示：

- 蓝色选中 / 搜索粗体高亮
- 继承链标签（灰色 `<- Ancestor`）
- 绿色 `+` 按钮：快速创建子 Tree（InheritsFrom 预填）
- 红色 `x` 按钮：删除叶子 Tree（无继承者时）

### PropertyTreeEditorPopups (v0.10.5 提取)

两个独立弹窗类：
- `NewTreeDialog.Show(cb, parent)` — 支持可选预填父 Tree
- `CreateDefDialog.Show(onCreated)` — 按 PropertyType 显示对应字段组

### PropertyDefSOEditor

PropertyDefSO 按类型显示不同 Inspector 字段组的自定义 Editor。

### PropertyImportExport

JSON ↔ .asset 往返导入导出。支持 `test_import.json` 格式：`version + description + definitions[] + trees[{treeName, inheritsFrom, nodes[{nodeId, parentId, defId}]}]`。

### NodeId 冲突防护 (v0.10.5)

- **检测**: `MergeAllNodes(out ancestorConflicts)` 记录被祖先遮盖的 NodeId
- **IsLocal 判断**: `_localIds.Contains(nodeId) && !ancestorConflicts.Contains(nodeId)` — 对文件夹和叶子都正确
- **预防**: `AddFolder`, `TryRenameFolder`, `AddDefToFolder` 三个入口检查继承节点名，冲突时自动后缀或弹窗拒绝
- **警告**: 每个冲突每 session 仅警告一次（`_warnedConflicts` HashSet 去重）
- **排序**: `SortTreeNodes` 用 `IsLocal` 属性分组（继承优先），不依赖有歧义的 `_ownNodes.FindIndex`

### GUIStyle 缓存 (v0.10.5)

惰性属性 (`??=`) 替代 `static readonly` — Unity Editor 的 `EditorStyles` 在 static ctor 阶段未初始化，必须延迟到首次 OnGUI 访问。

### EditorUIUtility.DrawHeaderCard (v0.10.5)

标准编辑器 Header 卡片：`[Title] [Subtitle(灰色右对齐)] [..FlexibleSpace..] [Save* 按钮]`。供所有 EditorWindow 复用。

---

## 十二、与现有系统的上下文映射

```
PropertySystem ───── 替代 ────→ StatsSystem (废弃)
      │
      ├── 被引用 ──── GearDefSO, ActorDefSO, BuildingDefSO, MaterialDefSO ...
      │                (所有"带属性的实体"都引用 PropertyTreeSO + overridesJson)
      │
      ├── 消费方 ──── Ability Pipeline (DamageEffectSO[], DamageType Tag)
      │               UI (DisplayName, Icon, Description)
      │               AnimationBrain (AnimationProfile, AudioProfile)
      │               EquipmentComponent (Weight, Durability)
      │
      ├── 依赖 ────── GameplayTag 系统 (Tag 类型校验)
      │
      └── 独立 ────── StatInstance / StatModifier (float 运行时引擎，阶段三桥接)
```

---

## 十三、消费层（运行时）

> 2026-06-10 · 已在 `Assets/Scripts/Services/Modules/L3_Properties/` 实现

### 架构

```
PropertyPresetSO (资产)                        PropertyComponent (MonoBehaviour 门面)
  ├── Template → PropertyTreeSO                 ├── _def → PropertyPresetSO
  └── OverridesJson                             ├── _props → PropertyTable (内部)
                                                 │     ├── _structure (Path→Def)
PropertyTreeSO.ResolveStructure()                │     ├── _floats / _ints / _strings / ... (类型分桶)
        │                                        │     ├── _floatStates (FloatState 运行时)
        ▼                                        │     ├── _guards (修改前拦截)
PropertyTable 构造 (一次性全解析)              │     └── _modifiers (Modifier 索引)
        │                                        │
        ▼                                        └── 公开 API: Get/Set/Modify/AddModifier/...
  _resolved 字典 (所有属性最终值)
  FloatState[] (存在伴生 Rate 的 Float)

所有消费者 → PropertyComponent (GetComponent)
```

### 文档索引

| 文件 | 内容 |
|------|------|
| [property-preset-so.md](property-preset-so.md) | PropertyPresetSO — 属性预设基类 |
| [property-table.md](property-table.md) | PropertyTable — 运行时属性平表、Set/Modify/Load、Guard、事件、Tick |
| [property-agent.md](property-agent.md) | ⛔ DEPRECATED — PropertyAgent 类不存在，PropertyTable 由消费者直接使用 |
| [property-def-so-subclasses.md](property-def-so-subclasses.md) | PropertyDefSO 子类体系 — 9 个类型化 SO |
| — | FloatState / FloatModifier 属于 Character/Stats — 见 [float-state.md](../L3-character/L4-stats/float-state.md)
