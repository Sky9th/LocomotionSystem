# GameplayTagContainer — 标签集合

> `L4_Combat/GameplayTagContainer.cs`

## 调用链

- 被 `CombatComponent` 持有，作为门控/冷却/状态标记的统一存储
- 被 `SkillBar` / `CombatDriver` 通过 `CombatComponent` 间接查询

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Count` | `int` | 当前标签数量 |

## 方法

### AddTag()
```csharp
public void AddTag(string tag)
```
- **用途**: 添加标签。已存在则无操作。空字符串忽略
- **参数**: `tag` — 层级标签字符串
- **调用者**: `CombatComponent`（技能激活时施加 abilityTags + 冷却 Effect 过期回调）

### RemoveTag()
```csharp
public void RemoveTag(string tag)
```
- **用途**: 移除标签。不存在则无操作
- **参数**: `tag` — 精确匹配的标签字符串
- **调用者**: `CombatComponent`（技能结束时清理 abilityTags；冷却 Effect 过期自动移除此标签）

### HasTag()
```csharp
public bool HasTag(string query)
```
- **用途**: 层级匹配查询。`HasTag("State")` 匹配 `"State.Attacking"`
- **参数**: `query` — 查询字符串
- **返回**: 是否有标签匹配
- **调用者**: `CombatComponent.TryActivate()` 检查门控标签；外部系统查询状态

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
