# Session: 装备/物品系统架构设计

> 2026-06-23 ~ 2026-06-24 | v0.22.7

## Background

动画资源和 Ability 链路已补齐，下一步回到 Ability 开发。但在做 Ability UI 之前，需要理清装备系统的数据来路。结果发现装备系统根本没有运行时代码，进而追到"装备到底是什么"这个根问题。

经过两天的架构讨论，确立了"一切皆属性"的核心方向，推翻/修正了多个原有设计假设。

## Changes

### 架构方向

- **确立"一切皆属性"核心**：角色、物品、NPC 共享 EntityDefSO + PropertyTreeSO + PropertyAgent 管线
- **装备不是物品类型**：是物品在身体槽容器中的状态。GearDefSO 概念废弃
- **武器类型缩减为 GameplayTag**：WeaponTypeSO 被拆解——damageType→技能、gripTags→玩家选择、equipSlot→容器过滤、compatRoutines→SkillTreeSO 反向声明
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
- 新增 `L3_SkillTree` 模块文档
- 更新 `tech/README.md` 索引

## Decisions

| 决策 | 替代方案 | 原因 |
|------|---------|------|
| 统一物品概念（ItemDefSO: EntityDefSO） | 多个子类（WeaponItem, ArmorItem...） | CDDA/RimWorld/PZ 都是统一类型。tag 分类 |
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
- L3_SkillTree 与 L3_Ability 的关系待厘清
- L3_Equipment 模块去向待各系统定义后再决定
- ContainerSlot API 未定义

## Cross-References

- Tech: [L3_Item](../tech/L2-services/L2-modules/L3-item/README.md)
- Tech: [L3_Container](../tech/L2-services/L2-modules/L3-container/README.md)
- Tech: [L4_Equipment](../tech/L2-services/L2-modules/L3-character/L4-equipment/README.md)
- Tech: [L3_SkillTree](../tech/L2-services/L2-modules/L3-skill-tree/README.md)
- Plan: [sorted-growing-tarjan.md](../../../plans/sorted-growing-tarjan.md)
- Design: [damage-source-model.md](../design/damage-source-model.md)
- Design: [equipment-system.md](../design/equipment-system.md) — GearDefSO 原始设计，部分方向已修正
- Tech: [L3_Ability](../tech/L2-services/L2-modules/L3-ability/README.md)
