# BaseStateKey · FSM 状态枚举

> `Character/Animation/Drivers/Locomotion/BaseStateKey.cs` — enum，BaseLayer FSM 的 7 个状态

## 枚举定义

```csharp
internal enum BaseStateKey
{
    Idle,           // 站立待机
    Moving,         // 移动中
    TurnInPlace,    // 原地转身
    IdleToMoving,   // 待机→移动过渡
    TurnInMoving,   // 移动中转身
    AirLoop,        // 空中循环
    AirLand         // 落地
}
```

## 调用链

```
被谁调:
  BaseLayer 构造 → 注册 7 个 State 实例
  各 State.Tick → TrySetState/ForceSetState
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | BaseLayer | FSM 状态切换和注册 |

## 未来规划

无。
