# GameplayTagContainer — 标签集合

> `L1_Core/GameplayTag/GameplayTagContainer.cs` · `namespace RedDust.Core`

## 调用链

- 被各子系统（`CombatComponent`、AI FSM、Stats、Item 等）持有，作为门控/冷却/状态标记的统一存储
- 全系统通用，每个实体持有一个独立实例

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Count` | `int` | 当前标签数量 |

## 方法

### AddTag()
```csharp
public void AddTag(string tag)
public void AddTag(GameplayTag tag)
```
- **用途**: 添加标签。已存在则无操作。空字符串/无效标签忽略
- **参数**: `tag` — 层级标签字符串或 GameplayTag 实例
- **调用者**: `CombatComponent`（技能激活时施加 abilityTags + 冷却 Effect 过期回调）

### RemoveTag()
```csharp
public void RemoveTag(string tag)
public void RemoveTag(GameplayTag tag)
```
- **用途**: 移除标签。不存在则无操作
- **参数**: `tag` — 精确匹配的标签字符串或 GameplayTag 实例
- **调用者**: `CombatComponent`（技能结束时清理 abilityTags；冷却 Effect 过期自动移除此标签）

### HasTag()
```csharp
public bool HasTag(string query)
```
- **用途**: 层级匹配查询。`HasTag("State")` 匹配 `"State.Attacking"`
- **参数**: `query` — 查询字符串
- **返回**: 是否有标签匹配
- **调用者**: `CombatComponent.TryActivate()` 检查门控标签；外部系统查询状态

### HasTagExact()
```csharp
public bool HasTagExact(string query)
public bool HasTagExact(GameplayTag query)
```
- **用途**: 精确匹配（不使用层级匹配）。`"Skill.Cooldown.Slash"` 不匹配 `"Skill.Cooldown.Slash.Extra"`
- **参数**: `query` — 精确标签字符串或 GameplayTag 实例
- **返回**: 是否有标签字符串完全相等
- **调用者**: `CombatComponent.TryActivate()` 检查冷却标签（冷却标签必须精确匹配，不同技能冷却独立）

### HasTagAtDepth()
```csharp
public bool HasTagAtDepth(int depth)
```
- **用途**: 是否有指定深度的标签。O(n)，n 为活跃标签数
- **参数**: `depth` — 层级深度（1=根）
- **调用者**: 游戏逻辑需要判断"是否有第 N 层标签"时

### MaxDepthUnder()
```csharp
public int MaxDepthUnder(string ancestor)
```
- **用途**: 获取指定祖先下最深标签的深度。无匹配返回 0
- **参数**: `ancestor` — 祖先层级查询字符串
- **调用者**: 受伤严重度判断：`MaxDepthUnder("State.Injury")` 返回最深伤重等级

### HasAny()
```csharp
public bool HasAny(params string[] queries)
```
- **用途**: 任意一个匹配即返回 true。用于多条件门控
- **调用者**: `CombatComponent.TryActivate()` 检查 `activationBlockedTags`

### HasAll()
```csharp
public bool HasAll(params string[] queries)
```
- **用途**: 全部匹配才返回 true
- **调用者**: （预留，后续复杂门控使用）

### GetAll()
```csharp
public string[] GetAll()
```
- **用途**: 获取所有标签字符串数组，调试用
- **调用者**: Editor/Gizmo/日志

### Clear()
```csharp
public void Clear()
```
- **用途**: 清空所有标签
- **调用者**: `CombatComponent` 重置/销毁时

## 设计决策

| 决策 | 原因 |
|------|------|
| `HashSet<GameplayTag>` 底层存储 | 自动去重，O(1) 查询 |
| AddTag/RemoveTag 接受 string 非 GameplayTag | 简化调用侧，不需要手动构造 |
| 空字符串静默忽略 | 配置容错，不会因 SO 字段空白而异常 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| — | — | — | — |
