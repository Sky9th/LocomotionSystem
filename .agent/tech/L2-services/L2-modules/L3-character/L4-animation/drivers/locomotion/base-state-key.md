# ⛔ OUTDATED — 7 State → 5 State

> **Status**: 代码中 `BaseStateKey` 枚举已从 7 个值缩减为 5 个。`IdleToMoving` 和 `TurnInMoving` 已删除。
> **当前枚举值**: Idle, Moving, TurnInPlace, AirLoop, AirLand。
> **最后验证**: 2026-07-03

---

# BaseStateKey · FSM 状态枚举

> `Character/Animation/Drivers/Locomotion/BaseStateKey.cs` — enum，BaseLayer FSM 的 5 个状态

## 枚举定义

```csharp
internal enum BaseStateKey
{
    Idle,           // 站立待机
    Moving,         // 移动中
    TurnInPlace,    // 原地转身
    AirLoop,        // 空中循环
    AirLand         // 落地
}
```

## 调用链

```
被谁调:
  BaseLayer 构造 → 注册 5 个 State 实例
  各 State.Tick → TrySetState/ForceSetState
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | BaseLayer | FSM 状态切换和注册 |

## 未来规划

无。
