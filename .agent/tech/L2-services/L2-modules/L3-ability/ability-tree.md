# AbilityTreeSO · 技能/天赋树

> `L3_Ability/Config/AbilityTreeSO.cs` — 纯数据资产（ScriptableObject）。一切皆 AbilityTree。
>
> 不是 L2 服务，不达 L3 运行时标准。归属 L3_Ability 配置层。

> 纯数据资产。运行时管理见 [ability-forest.md](ability-forest.md)。
>
> **Last Verified**: 2026-07-04 | **Verification**: SAbilityTreeNode.ability → ActiveAbilitySO. GameplayTagDefinitionSO → RdTagDefSO. compatibleGripTags added.

## 层级定位

AbilityTreeSO 是纯数据资产。运行时逻辑仅一行过滤：

```
weapon.ItemTags ∩ compatibleWeaponTags → unlockedNodes中筛选ability[]
```

没有 Update、Tick、跨模块协调。和 Animation/Locomotion 等 L4 子系统不同——不驱动行为。

## 数据结构

```csharp
[Serializable]
public struct SAbilityTreeNode
{
    public string nodeId;              // "ironBones_1"
    public ActiveAbilitySO ability;    // 主动技能（Q/E/R/F）——可选
    public PassiveAbilitySO passive;   // 被动效果——可选
    public string[] prerequisites;     // 前置节点 ID。空 = 根节点（初始解锁）
}

public class AbilityTreeSO : ScriptableObject
{
    // Identity
    public string treeId;                          // "ironBones"
    public string displayName;                     // 显示名称
    public string description;                     // 描述文本
    public Sprite icon;                            // 图标

    // Classification
    public RdTagDefSO[] treeTags;                  // 类别标签（AbilityTree.Innate / Talent / Routine）
    public RdTagDefSO[] compatibleWeaponTags;      // 武器兼容。空 = 不限
    public RdTagDefSO[] compatibleGripTags;        // 握持兼容。空 = 不限握法

    // Mutual Exclusion
    public string exclusiveGroup;                  // 互斥分组。"" = 无互斥

    // Nodes
    public SAbilityTreeNode[] nodes;                // 所有节点
}
```

## 类别（通过 treeTags 区分）

| | `AbilityTree.Innate` | `AbilityTree.Talent` | `AbilityTree.Routine` |
|---|---|---|---|
| **何时获得** | 出生自动 | 创建时选择 | 秘籍学习后装备 |
| **节点解锁** | 全部已解锁 | 创建时逐节点选 / 属性门槛解锁 | 装备后根节点可用，后续秘籍解锁 |
| **可否移除** | ✗ | ✗ | ✅ 可切换 |
| **和武器求交** | ✅ 过滤 | ✗ 不过滤（仅 passive 生效，ability 留空） | ✅ 过滤 |
| **互斥** | ✗ | ✅ via exclusiveGroup | 容器级（技能槽容量=1） |

> 和 Weapon.Blade、ItemTags 一致——用 RdTag 不用 Enum。未来可加 `AbilityTree.Mutation`、`AbilityTree.BossSkill` 等不修改代码。

## 互斥机制

用 `exclusiveGroup` 分组，不靠 pairwise 列表：

```
天生大力:   exclusiveGroup = "innate_body"
天生敏捷:   exclusiveGroup = "innate_body"
天生耐力:   exclusiveGroup = "innate_body"
→ 三选一。加第四个选项不需要改另外三个。

夜猫子:     exclusiveGroup = "sleep_pattern"
早起者:     exclusiveGroup = "sleep_pattern"
→ 二选一

鹰眼:       exclusiveGroup = ""     ← 不参与互斥
```

选中同组内一个 → 其他全部灰掉。

## 角色存储

```csharp
// CharacterActor / CharacterBuildContext
public Dictionary<string, HashSet<string>> unlockedNodes;
//        treeId  →  { nodeId, nodeId, ... }

// 示例:
// "human_innate"  → { "punch", "kick", "grab" }         ← 全部解锁
// "ironBones"     → { "ironBones_1", "ironBones_2" }     ← 逐节点解锁
// "bajiQuan"      → { "baji_root" }                       ← 根节点解锁
```

## 技能查询

```
allAbilities = unlockedNodes 中所有 node.ability
  → treeTags 含 AbilityTree.Innate 或 AbilityTree.Routine → compatibleWeaponTags ∩ weapon.ItemTags 过滤后进入 Q/E/R/F
  → treeTags 含 AbilityTree.Talent → 不过滤，passives 直接生效

allPassives = unlockedNodes 中所有 node.passive（全部生效）
```

## 三层技能体系

```
角色主动技能 = Innate.abilities(过滤后) ∪ Routine.abilities(过滤后)
角色被动效果 = Innate.passives ∪ Talent.passives ∪ Routine.passives

Layer 1: Innate  → 出生全解锁，武器兼容 + 握持兼容 双过滤
Layer 2: Talent  → 创建时选，互斥分组，逐节点解锁
Layer 3: Routine → 装备切换，武器兼容 + 握持兼容 双过滤
```

## 目录结构

### 代码

```
L3_Ability/
├── Config/
│   ├── AbilitySO.cs               ← 已有
│   ├── AbilityDefSO.cs            ← 已有
│   ├── PassiveAbilitySO.cs        ← 已有
│   └── AbilityTreeSO.cs           ← 新增
│
└── Editor/
    └── AbilityTreeEditor/
        ├── AbilityTreeEditorWindow.cs      ← 编辑器窗口（骨架）
        └── AbilityTreeImportExport.cs      ← JSON 导入/导出
```

### 资产

```
Assets/Data/Ability/
├── Abilities/
│   ├── abilities_all.json          ← Ability 导出 JSON
│   └── Actives/...              ← AbilityDefSO .asset
├── AbilityTrees/
│   ├── abilityTrees_all.json       ← AbilityTree 导出 JSON
│   └── Innate/..., Talent/..., Routine/...  ← AbilityTreeSO .asset
    ├── Innate/Human.asset ...
    ├── Talent/IronBones.asset ...
    └── Routine/BajiQuan.asset ...
```

> 按 treeTags 分类到子目录。Talent 的互斥由 `exclusiveGroup` 字段保证，目录结构不反映分组。

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| AbilityTreeSO | L3_Ability（ActiveAbilitySO, PassiveAbilitySO） | 持有技能/被动引用 |
| AbilityTreeSO | ItemDefSO（ItemTags） | compatibleWeaponTags ∩ ItemTags 求交 |
| AbilityTreeSO | AbilityForest | compatibleGripTags 用于握法兼容过滤 |
| AbilityTreeSO | Container（技能槽） | Routine 装备到技能槽容器，过滤后主动技能填入 Q/E/R/F |

## 已知缺口

| 缺口 | 状态 | 说明 |
|------|:---:|------|
| 属性门槛解锁 | ❓ | `SSAbilityTreeNode` 暂无 `unlockCondition` 字段。远期补充 |
| 秘籍解锁媒介 | ❓ | Routine 节点解锁的"秘籍"类型未定义——ItemDefSO? 独立类型？ |
| 技能槽溢出 | ❓ | 过滤后技能数 > 4 时如何选择/排序——未定义 |
| 节点新增后角色同步 | ❓ | AbilityTree 资产更新后，已有角色是否自动获得新节点——未定 |
| unlockedNodes 序列化 | ❓ | 存档格式未定义 |
| 编辑器 | 远期 | EUI 原则——先画 UI 图再改代码 |
| 武器熟练度归属 | ❓ | GDD 要求但未关联到 AbilityTree 模块 |

## 设计决策

| 决策 | 原因 |
|------|------|
| 一切皆 AbilityTree | 天赋、套路、种族——底层同构 |
| 节点系统 | 一棵树内技能逐节点解锁，不全给 |
| exclusiveGroup 代替 mutuallyExclusiveWith | 分组互斥比 pairwise 干净——加第三个选项不改已有节点 |
| 直接继承 ScriptableObject | 不需要 PropertyPresetSO 属性管线——数据是引用列表不是属性值 |
| 归属 L3_Ability Config | 纯配置资产，和 AbilityDefSO 同级 |