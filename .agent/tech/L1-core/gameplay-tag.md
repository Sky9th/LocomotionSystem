# L1_Core GameplayTag — 全系统层级标签基础设施

> `L1_Core/GameplayTag/` · `namespace RedDust.Core` · 对标 UE GAS FGameplayTag

## 文件

| 文件 | 说明 |
|------|------|
| `GameplayTag.cs` | readonly struct — 运行时值类型，HashSet 友好 |
| `GameplayTagContainer.cs` | sealed class — 标签集合，每个实体独立实例 |
| `GameplayTagDefinitionSO.cs` | [SO] 标签定义资产，父子引用层级 |

## 两层架构

```
设计时 (Unity Editor)              运行时 (C#)
━━━━━━━━━━━━━━━━━━━━━          ━━━━━━━━━━━━━━━
GameplayTagDefinitionSO  ─隐式转换→  GameplayTag struct
 ├ leafName                        ├ Tag: string
 ├ parent (SO ref)                 ├ Depth: int
 └ FullTag (缓存 getter)            └ Matches/IsAncestorOf/IsDescendantOf

SO 资产: Assets/Data/GameplayTags/    GameplayTagContainer
  按目录层级组织                        └ HashSet<GameplayTag> — O(1) 查询
```

## 子文档

| 文档 | 说明 |
|------|------|
| [gameplay-tag.md](../L2-services/L2-modules/L3-character/L4-combat/gameplay-tag.md) | GameplayTag struct 详解 |
| [gameplay-tag-container.md](../L2-services/L2-modules/L3-character/L4-combat/gameplay-tag-container.md) | GameplayTagContainer 详解 |

## 命名空间约定

| 前缀 | 用途 | 示例 |
|------|------|------|
| `State.*` | 角色/实体状态 | `State.Attacking`, `State.Dead`, `State.Stunned` |
| `Skill.Cooldown.*` | 技能冷却 | `Skill.Cooldown.SlashHorizontal` |
| `Effect.*` | Buff/Debuff | `Effect.Buff.DamageUp`, `Effect.Debuff.Poison` |
| `State.Injury.*` | 伤病状态 | `State.Injury.Laceration.Mild` |
| `State.AI.*` | 敌人行为状态 | `State.AI.Alerted`, `State.AI.Chase` |
