# GameplayTag — 层级标签

> `L4_Combat/GameplayTag.cs`

## 调用链

- 被 `GameplayTagContainer` 持有
- 被 `CombatComponent` 通过 `GameplayTagContainer` 间接使用（门控/冷却/状态查询）

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Tag` | `string` | 层级标签字符串，如 `"State.Attacking"`、`"Skill.Cooldown.Slash"` |

## 方法

### Matches()
```csharp
public bool Matches(string query)
```
- **用途**: 层级匹配。`query "State"` 匹配 `Tag "State.Attacking"`（前缀+`.`），不匹配 `"StateAttacking"`
- **参数**: `query` — 查询字符串
- **返回**: true 如果 query 是 Tag 本身或 Tag 的前缀层级
- **调用者**: `GameplayTagContainer.HasTag()`

### Equals / ==
```csharp
public bool Equals(GameplayTag other)
```
- **用途**: 完整字符串相等比较
- **调用者**: `GameplayTagContainer` 用 HashSet 存储去重

## 设计决策

| 决策 | 原因 |
|------|------|
| `readonly struct` | 轻量值类型，HashSet 友好 |
| `.` 作为层级分隔符 | 简单直观，与 UE GAS 的 Tag 层级一致 |
| 不区分大小写 | Tag 是系统内部标识符，用精确匹配避免歧义 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| — | — | — | — |
