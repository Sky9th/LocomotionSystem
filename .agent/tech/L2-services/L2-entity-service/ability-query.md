# AbilityQuery

> **Last Verified**: 2026-07-03 | **Verification**: All referenced files exist, signatures match code

**源文件**: `Assets/Scripts/Services/L2_EntityService/AbilityQuery.cs`

技能查询门面——封装 AbilityExecutor + AbilityForest 只读访问。由 EntityQueryModule.Ability 惰性创建。

## 调用链

```
Entity.Query.Ability
  → _entity.View → CharacterActor → BuildContext
    → AbilityExecutor + AbilityForest
      → AbilityQuery(executor, forest)

UI:
  AbilityBarOverlay.Update()
    → entity.Query.Ability?.ActiveAbilities
    → ability.GetCooldownRemaining(a)
    → ability.IsActive(a)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | AbilityExecutor | 读冷却、管道状态 |
| 依赖 | AbilityForest | 读 ResolvedActives |
| 被调用 | EntityQueryModule | 惰性 getter 创建 |
| 被调用 | AbilityBarOverlay | 技能栏显示 |
| 被调用 | DebugOverlay | 调试信息 |

## 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `ActiveAbilities` | `ActiveAbilitySO[]` | 当前可用的主动技能 |

## 方法

### GetCooldownRemaining()
```csharp
public float GetCooldownRemaining(ActiveAbilitySO ability)
```
- **用途**: 获取技能剩余冷却时间（秒）
- **返回**: 不在冷却中返回 0

### IsActive()
```csharp
public bool IsActive(ActiveAbilitySO ability)
```
- **用途**: 指定技能是否正在管道中执行

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Pipeline 状态暴露 | 待做 | DebugOverlay 移除了 stateTimeLabel |
