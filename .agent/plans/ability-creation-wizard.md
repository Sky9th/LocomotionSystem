# Ability Creation Wizard — 完整方案

> 归档时间: 2026-06-07
> 状态: 方案设计完成，待标签编辑器落地后实施
> 依赖: 标签编辑器 (TagEditor + TagPicker)

## Context

创建**一个**主动技能目前需要手动操作 **4-8 个 Ability SO 资产 + 2-10 个 GameplayTag 资产**，分布在 `Assets/Data/Ability/` 和 `Assets/Data/Tags/` 两棵目录树下，然后逐一拖拽关联。项目规划的 199 标签树目前只有 ~32 个实际资产。

### 核心痛点

1. **资产太多**：一个 Slash 技能 = 5+ 次 `CreateAssetMenu` → 填参 → 拖拽
2. **标签链断裂**：选 `Damage.Elemental.Fire` 作为伤害类型时，`Tag_Fire`、`Tag_Elemental`、`Tag_Damage` 可能都不存在
3. **目录规则记不住**：伤害效果进 `Effects/Damage/Physical_Slash/`，标签进 `Tags/Damage/Physical/Slash/`，路径相似但结构不同
4. **重复劳动**：Activation/Search 可在技能间复用，但每次都重建

## Approach

一个 `EditorWindow` 表单，填完所有参数后一键创建所有资产（Ability SO + 子 SO + 所需 Tag），自动放到正确目录，自动关联引用。内部使用 TagPicker 组件做标签选择。

## 文件组织（依赖 TagEditor 模块）

```
Assets/Scripts/Services/Modules/L3_Ability/Editor/
├── AbilityDefSOEditor.cs          (已有)
├── PassiveAbilitySOEditor.cs       (已有)
└── AbilityWizard/
    ├── AbilityCreationWizard.cs
    ├── AbilityCreationWizard_FormData.cs
    ├── AbilityCreationWizard_UI.cs
    ├── AbilityCreationWizard_AssetFactory.cs
    └── (TagResolver 来自 Shared/Editor/TagEditor/)
```

## 标签存储格式

FullTag 字符串（如 `"Damage.Elemental.Fire"`），可靠因为 `GameplayTagDefinitionSO` 有两层机械保证：

- `AutoDeriveLeafName()` 在 `OnValidate()` 强制执行 `leafName = 文件名 - "Tag_"` 前缀
- `RefreshCache()` 由 parent 链级联拼接 `FullTag`

标签解析使用 TagEditor 模块的 `TagResolver`，双重键（FullTag → leafName+Depth 回退 → 创建）。

## UI 布局

```
┌──────────────────────────────────────────────────────────────┐
│  RedDust Ability Creation Wizard                              │
├──────────────────────────────────────────────────────────────┤
│  [Preset: Melee Slash ▼]  [Load]  [Save As...]               │
├──────────────────────────────────────────────────────────────┤
│  Identity / Classification / Activation / Search /           │
│  Target Effects / Self Effects / Gating / Noise /            │
│  Validation                                                   │
│  [★ Create Ability]                                          │
└──────────────────────────────────────────────────────────────┘
```

## 创建管线

```
Phase 0: Validate
Phase 1: TagResolver.ResolveOrCreate() 所有标签
Phase 2: 创建 Activation SO (if CreateNew)
Phase 3: 创建 Search SO (if CreateNew)
Phase 4: 创建 Effect SOs
Phase 5: 创建 Noise SO (if CreateNew)
Phase 6: 创建 AbilityDefSO + 关联引用
Phase 7: AssetDatabase.SaveAssets()
```

## 预设（5 个内置）

| 预设 | 搜索 | 激活 | 伤害 | 噪音 |
|------|------|------|------|------|
| Melee Slash | Cone 90° | Instant | Physical.Slash | MeleeSwing |
| Melee Stab | Ray 2m | Instant | Physical.Pierce | — |
| Cone AoE | Cone 180° | Channel | Elemental | Explosion |
| Ranged Shot | Ray 20m | Instant | Physical.Pierce | WeaponFire |
| Empty | — | — | — | — |

## 关键设计决策

1. TagEditor 模块独立，AbilityWizard 消费 TagPicker 组件
2. TagResolver 惰性创建标签链，不跨级
3. Effect 目录与 Damage Tag 耦合（Physical 子类型用 `Physical_{leafName}`）
4. 编辑已有技能走已有 Inspector，Wizard 只管创建