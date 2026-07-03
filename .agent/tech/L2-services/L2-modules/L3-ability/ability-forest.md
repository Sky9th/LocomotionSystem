# AbilityForest — 技能树运行时

> `L3_Ability/AbilityForest.cs` · `namespace RedDust.Ability` · 纯 C# 类，无 MonoBehaviour
>
> **Last Verified**: 2026-07-04 | **Verification**: Namespace RedDust.Ability. Constructor takes innateTrees. Resolve() private, SetWeaponTags() public. compatibleGripTags added.

## 定位

AbilityTreeSO 是静态数据（`RedDust.Ability`）。AbilityForest 是角色的运行时技能库存（`RedDust.Ability`）——管理活跃树集合、节点解锁状态、技能解析。

不是 L2，不是 MonoBehaviour。纯 C# 类，由 CharacterActor 在 Awake 创建，通过 BuildContext 暴露解析结果。

树组成森林——单个 `AbilityTreeSO` 是一棵树，角色持有的所有活跃树组成 `AbilityForest`。

## 核心模型：多来源活跃树集合

角色同时持有的不是"三层树"，而是一个**活跃树集合**。不同来源、不同生命周期：

```
角色运行时活跃树集合 (_activeTrees)
│
├── 🧬 种族/身份树     source="innate"      永久，不可移除
├── 🎯 天赋树           source="talent"      永久，不可移除（创建时锁定）
├── 🔫 武器自带树       source=itemInstance   装备→添加，卸下→移除
├── 📖 后天习得树       source="learned"     永久，游玩中获得
└── ...更多来源          source=任意对象       AddTree/RemoveBySource
```

"Source" 是一个 `object` 标签——不参与逻辑，只用于移除时做 identity check。装备树和武器实例绑定：卸下时 `RemoveBySource(weaponInstance)` 精确清走该武器带来的全部树。

## 数据结构

```csharp
/// <summary>活跃树——AbilityTreeSO + 运行时解锁状态 + 来源标识</summary>
internal class ActiveTree
{
    public AbilityTreeSO Tree;
    public HashSet<string> UnlockedNodeIds;   // 这棵树内已解锁的 nodeId
    public object Source;                      // 来源标识，用于 RemoveBySource
}

/// <summary>技能森林——角色持有的所有活跃 AbilityTree 运行时集合。纯 C#，由 CharacterActor 创建并持有。</summary>
internal class AbilityForest
{
    private readonly List<ActiveTree> _activeTrees = new();
    private RdTagContainer _weaponTags;

    // ── 构造 ──
    public AbilityForest(AbilityTreeSO[] innateTrees);

    // ── 武器 ──
    public void SetWeaponTags(RdTagContainer weaponTags);  // 更新武器标签 → 自动重解析

    // ── 树管理 ──
    public void AddTrees(AbilityTreeSO[] trees, object source);        // 批量全解锁
    public void AddTree(AbilityTreeSO tree, object source);            // 单棵全解锁
    public void AddTree(AbilityTreeSO tree, HashSet<string> initialUnlocks, object source);  // 部分解锁
    public void RemoveBySource(object source);

    // ── 节点管理 ──
    public void UnlockNode(string treeId, string nodeId);
    public bool IsNodeUnlocked(string treeId, string nodeId);

    // ── 技能解析（内部） ──
    // Resolve() 是 private，由 SetWeaponTags / AddTree / RemoveBySource 自动触发
    // 结果写入：
    public ActiveAbilitySO[] ResolvedActives { get; private set; }   // Q/E/R/F
    public PassiveAbilitySO[] ResolvedPassives { get; private set; }
}
```

## 树管理 API

### AddTree / AddTrees

三种重载，自动触发 Resolve：

```csharp
// 批量全解锁。null/空数组安全。
public void AddTrees(AbilityTreeSO[] trees, object source);

// 单棵全解锁。
public void AddTree(AbilityTreeSO tree, object source);

// 部分解锁——initialUnlocks 只取 this tree 的有效 nodeId
public void AddTree(AbilityTreeSO tree, HashSet<string> initialUnlocks, object source)
{
    var valid = new HashSet<string>();
    foreach (var node in tree.nodes)
        if (initialUnlocks.Contains(node.nodeId))
            valid.Add(node.nodeId);

    _activeTrees.Add(new ActiveTree
    {
        Tree = tree,
        UnlockedNodeIds = valid,
        Source = source
    });
}
```

- Innate 树：构造函数中自动注入，全解锁，source="innate"
- Talent 树：`initialUnlocks` = 创建时选择的 nodeId（可能仅根节点）
- 武器树：`initialUnlocks` = 全部 nodeId（武器自带技能无需逐节点解锁）
- 习得树：`initialUnlocks` = 学习时解锁的 nodeId

### RemoveBySource

```csharp
public void RemoveBySource(object source)
{
    _activeTrees.RemoveAll(t => t.Source == source);
}
```

O(n) 线性扫描——活跃树总量在个位数到十位数，零性能问题。

## 技能解析

`Resolve()` 是 **private**，由 `SetWeaponTags()` / `AddTree()` / `RemoveBySource()` 等公共 API 在状态变更后自动触发。外部不需要手动调用。

```csharp
// 公共入口：武器切换时调用
public void SetWeaponTags(RdTagContainer weaponTags)
{
    _weaponTags = weaponTags;
    Resolve();
}

// 内部解析
private void Resolve()
{
    var actives = new List<ActiveAbilitySO>();
    var passives = new List<PassiveAbilitySO>();

    foreach (var at in _activeTrees)
    {
        var tree = at.Tree;

        // 武器兼容检查
        if (!IsWeaponCompatible(tree.compatibleWeaponTags, _weaponTags))
            continue;

        // 握持兼容检查
        if (!IsGripCompatible(tree.compatibleGripTags, _weaponTags))
            continue;

        foreach (var node in tree.nodes)
        {
            if (!at.UnlockedNodeIds.Contains(node.nodeId))
                continue;

            // 主动技能：需通过武器 + 握持两层过滤
            if (node.ability != null)
                actives.Add(node.ability);

            // 被动：无条件生效
            if (node.passive != null)
                passives.Add(node.passive);
        }
    }

    ResolvedActives = actives.ToArray();
    ResolvedPassives = passives.ToArray();
}

// compatibleWeaponTags 为空 → 不过滤（徒手/纯被动树）
// compatibleWeaponTags 非空 → 必须和 weaponTags 有交集
private static bool IsWeaponCompatible(
    RdTagDefSO[] compatibleTags,
    RdTagContainer weaponTags)
{
    if (compatibleTags == null || compatibleTags.Length == 0)
        return true;  // 无武器限制——徒手技能/纯被动树
    if (weaponTags == null)
        return false; // 有兼容要求但无武器 → 不通过
    foreach (var tag in compatibleTags)
        if (weaponTags.HasTag(tag.FullTag))
            return true;
    return false;
}

// compatibleGripTags 为空 → 不过滤
// compatibleGripTags 非空 → 必须和 equipmentTags 有交集（握法匹配）
private static bool IsGripCompatible(
    RdTagDefSO[] compatibleGripTags,
    RdTagContainer equipmentTags)
{
    if (compatibleGripTags == null || compatibleGripTags.Length == 0)
        return true;
    if (equipmentTags == null)
        return false;
    foreach (var tag in compatibleGripTags)
        if (equipmentTags.HasTag(tag.FullTag))
            return true;
    return false;
}
```

> **技能槽溢出处理（远期）**：过滤后 actives.Count > 4 时，按节点在树中的顺序取前 4 个。排序/优先级系统远期补充。
>
> **两层兼容过滤**：武器兼容 (`compatibleWeaponTags`) + 握持兼容 (`compatibleGripTags`) 双重过滤，两者都通过才生效。空数组 = 无条件通过。空数组同时用于徒手技能、纯被动天赋、不限握法的武器技能。

## 集成点

### 1. CharacterActor — 创建 + 持有

```csharp
// CharacterActor.cs — 新增字段
[Header("Ability Trees")]
[SerializeField] private AbilityTreeSO[] innateTrees;       // 种族/身份树
[SerializeField] private AbilityTreeSO[] initialTalents;    // MVP 硬编码天赋（远期来自 SCharacterBuild）

// 运行时
private AbilityForest abilityForest;

// Awake() 中：构造函数直接注入天生树，全解锁 source="innate"
abilityForest = new AbilityForest(innateTrees);
foreach (var t in initialTalents)
    abilityForest.AddTree(t, source: "talent");
```

### 2. CharacterBuildContext — 传递解析结果

```csharp
// 替代现有 SkillSlot1/SkillSlot2 临时字段
public ActiveAbilitySO[] AbilitySlots { get; internal set; } = Array.Empty<ActiveAbilitySO>();
public PassiveAbilitySO[] ActivePassives { get; internal set; } = Array.Empty<PassiveAbilitySO>();
```

CharacterActor 中武器变化后调用 `abilityForest.SetWeaponTags(weaponTags)`，写回 ctx：

```csharp
abilityForest.SetWeaponTags(currentWeaponTags);
ctx.AbilitySlots = abilityForest.ResolvedActives;
ctx.ActivePassives = abilityForest.ResolvedPassives;
```

### 3. PlayerDirector — 消费技能槽

```csharp
// 替代硬编码的 ctx.SkillSlot1/SkillSlot2
private void ProcessSkillInput()
{
    var slots = ctx.AbilitySlots;  // ActiveAbilitySO[]
    if (input.FirstSkillRequested  && slots.Length > 0) EnqueueAbility(slots[0], "Q");
    if (input.SecondSkillRequested && slots.Length > 1) EnqueueAbility(slots[1], "E");
    // R, F 远期
}
```

### 4. AbilityExecutor — 消费被动列表

```csharp
// OnWire 或 Update 中同步
abilityExecutor.SyncPassives(ctx.ActivePassives);
```

### 5. 武器物品 → 树关联

```
装备武器
  → itemInstance.Def.GrantedAbilityTrees  ← MVP: ItemDefSO 临时 C# 字段
  → abilityForest.AddTrees(trees, source: itemInstance)

卸下武器
  → abilityForest.RemoveBySource(oldItemInstance)
```

ItemDefSO 新增临时字段（远期进 PropertyTree）：

```csharp
/// <summary>
/// 此物品装备后授予的技能树。
/// TODO: PropertyType.Struct 实现后移至 PropertyTree。
/// </summary>
public AbilityTreeSO[] GrantedAbilityTrees = Array.Empty<AbilityTreeSO>();
```

## SCharacterBuild — 天赋持久化

```csharp
/// <summary>
/// 角色 Build 数据——创建时生成，存档持久化，运行时加载。
/// 串起 创建UI → 存档 → CharacterActor 初始化 全链路。
/// </summary>
[Serializable]
public struct SCharacterBuild
{
    public string characterDefId;

    /// <summary>天赋选择：每棵树选中的 nodeId 列表</summary>
    public TreeSelection[] talentSelections;
}

[Serializable]
public struct TreeSelection
{
    public string treeId;
    public string[] nodeIds;   // 创建时解锁的节点（可能仅根节点）
}
```

CharacterActor 初始化路径：

```
Awake:
  innateTrees   ← CharacterDefSO（编译期已知）
  
Load(SCharacterBuild build):
  talentTrees   ← build.talentSelections → 查找 AbilityTreeSO 资产 → AddTree
  routineTrees  ← 空，等装备物品时添加
```

MVP 阶段用 Inspector 硬编码 `initialTalents` 跳过 `SCharacterBuild`。

## 完整数据流

```
装备武器 (玩家按 1)
  │
  ▼
PlayerDirector.ProcessEquipInput()
  │
  ▼
CharacterActor.SwitchWeapon(primaryWeaponDef)
  ├── Container<ItemInstance>.Place("RightHand", itemInstance)
  │     └── 旧武器 → Container.Remove → abilityForest.RemoveBySource(oldItem)
  │
  ├── abilityForest.AddTrees(
  │       itemInstance.Def.GrantedAbilityTrees,
  │       source: itemInstance)
  │
  └── abilityForest.SetWeaponTags(itemInstance.ItemTags)
        ├── ResolvedActives   → ctx.AbilitySlots[0..3]
        └── ResolvedPassives  → ctx.ActivePassives

下一帧:
  PlayerDirector.ProcessSkillInput()
    → AbilityExecutor.Enqueue(ctx.AbilitySlots[0])   // Q 键
    → AbilityExecutor.Pipeline.Start(...)            // ②→③→④→⑤→⑥→⑧
```

## 目录结构

```
L3_Ability/
├── AbilityForest.cs               ← 纯 C# 运行时
├── AbilityExecutor.cs             ← 技能释放管线
├── AbilityReactor.cs              ← 技能反应器
└── Config/
    └── AbilityTreeSO.cs           ← 静态数据资产

L3_Character/Actor/
├── CharacterActor.cs              ← 修改：创建 AbilityForest + SwitchWeapon
└── CharacterBuildContext.cs       ← 修改：SkillSlot1/2 → AbilitySlots[]

L3_Character/Director/Player/
└── PlayerDirector.cs              ← 修改：消费 ctx.AbilitySlots

L3_Item/
└── ItemDefSO.cs                   ← 修改：新增 GrantedAbilityTrees 临时字段
```

## MVP 范围

| 优先级 | 事项 | 说明 |
|--------|------|------|
| P0 | `AbilityForest.cs` | 纯 C# 类，AddTree/RemoveBySource/Resolve |
| P0 | `CharacterBuildContext` 改造 | SkillSlot1/2 → AbilitySlots[] + ActivePassives[] |
| P0 | `CharacterActor` 改造 | 创建 Forest + innateTrees 字段 + SwitchWeapon 桩 |
| P0 | `PlayerDirector` 改造 | 消费 ctx.AbilitySlots |
| P0 | `ItemDefSO.GrantedAbilityTrees` | 临时 C# 字段 |
| P1 | `AbilityExecutor.SyncPassives` | 被动列表同步 |
| P1 | `SCharacterBuild` | 天赋持久化结构体 |
| P2 | 技能槽溢出处理 | actives > 4 排序/选择 |
| 远期 | PropertyTree 替代 GrantedAbilityTrees | PropertyType.Struct 实现后 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 活跃树集合（非三层固定槽） | 树来源多样——种族/天赋/武器/习得——一个 List 承载所有 |
| source: object 标识 | 移除武器时精确清除，不依赖 treeId 猜测 |
| 纯 C# 类，非 ModuleChild | 无生命周期钩子需求。CharacterActor 直接调用 Resolve |
| 不用事件驱动树切换 | 切换是确定性的，消费者唯一（技能槽重解析），需要同步完成 |
| Resolve 直接写 ctx | 遵循"数据由上至下参数传递"——Forest → ctx → PlayerDirector |
| GrantedAbilityTrees 在 ItemDefSO | MVP 临时妥协。远期 PropertyType.Struct 承载 |
| compatibleWeaponTags 空 = 不过滤 | 徒手技能树、纯被动天赋树不依赖武器 |
| compatibleGripTags 空 = 不过滤 | 不限握法的技能树——双手/单手/双持通用 |
| 两层兼容过滤 | 武器兼容 + 握持兼容双重过滤——两者都通过才生效，任一为空不限制 |
| SetWeaponTags 作为公共入口 | Resolve 私有化，状态变更自动触发——不暴露内部解析，外部只需告知武器变化 |
