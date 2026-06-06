# GameplayTag — 层级标签

> `L1_Core/GameplayTag/GameplayTag.cs` · `namespace RedDust.Core`
> 设计时 SO: `GameplayTagDefinitionSO.cs`

## 架构定位

GameplayTag 是**全系统通用基础设施**，位于 L1_Core。对标 UE GAS `FGameplayTag`，服务于战斗、AI、伤病、物品、建造、任务等所有子系统。

两层设计：
- **设计时** — `GameplayTagDefinitionSO`（SO 资产，父子引用，改父自动级联子孙 FullTag）
- **运行时** — `GameplayTag`（readonly struct，隐式转换获取，HashSet 存储）

## 调用链

- 被 `GameplayTagContainer` 持有
- 被 `AbilityComponent`（及其他子系统）通过 `GameplayTagContainer` 间接使用（门控/冷却/状态查询）
- SO 侧被 `AbilityDefSO` 等配置引用

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Tag` | `string` | 完整层级标签字符串，如 `"State.Attacking"`、`"Skill.Cooldown.Slash"` |
| `Depth` | `int` | 层级深度，构造时预计算（根=1）。O(1) |
| `IsValid` | `bool` | Tag 字符串是否非空。用于配置校验和门控 guard |

## 方法

### Matches()
```csharp
public bool Matches(string query)
```
- **用途**: 层级匹配。`query "State"` 匹配 `Tag "State.Attacking"`（前缀+`.`），不匹配 `"StateAttacking"`
- **参数**: `query` — 查询字符串
- **返回**: true 如果 query 是 Tag 本身或 Tag 的前缀层级
- **调用者**: `GameplayTagContainer.HasTag()`

### Matches(GameplayTag)
```csharp
public bool Matches(GameplayTag query)
```
- **用途**: 类型安全重载，行为与 `Matches(string)` 一致
- **参数**: `query` — GameplayTag 实例
- **调用者**: 类型安全的门控查询

### IsAncestorOf / IsDescendantOf
```csharp
public bool IsAncestorOf(GameplayTag other)
public bool IsDescendantOf(GameplayTag other)
```
- **用途**: 祖先/后代关系判断。`"State".IsAncestorOf("State.Attacking")` → true
- **调用者**: 跨系统层级关系查询

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
| `Depth` 构造时预计算 | O(1) 层级深度查询，无运行时开销 |
| 设计时 SO + 运行时 struct 双层 | SO 负责编辑器组织与重命名安全，struct 负责运行时性能 |
| SO 父子引用（非字符串副本） | 改父级 leafName → 所有子孙 FullTag 自动更新 |

## GameplayTagDefinitionSO

```csharp
public sealed class GameplayTagDefinitionSO : ScriptableObject
{
    public string leafName;               // 本层级名称，如 "Attacking"
    public GameplayTagDefinitionSO parent; // 父级 SO，根为 null
    public string FullTag { get; }        // 递归拼接，如 "State.Attacking"
    public int Depth { get; }             // 层级深度，OnEnable 缓存

    public static implicit operator GameplayTag(GameplayTagDefinitionSO def);
}
```

**隐式转换**: `AbilityDefSO.cooldownTag` 字段类型为 `GameplayTagDefinitionSO`，运行时查询自动转为 `GameplayTag`，代码不感知差异。

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| — | — | — | — |
