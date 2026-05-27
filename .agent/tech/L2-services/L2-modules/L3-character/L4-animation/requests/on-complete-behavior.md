# OnCompleteBehavior · 播放完成行为枚举

> `Character/Animation/Requests/OnCompleteBehavior.cs` — enum，动画播放完成后的行为

## 枚举定义

```csharp
public enum OnCompleteBehavior
{
    Resume,   // 恢复默认 Driver（LocomotionDriver）
    Stay      // 保持当前动画（不自动切换）
}
```

## 调用链

```
被谁调:
  DriverArbiter.CheckCompletion() → 读取 activeRequest.OnComplete
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | DriverArbiter | CheckCompletion 时根据此枚举判定行为 |

## 未来规划

无。
