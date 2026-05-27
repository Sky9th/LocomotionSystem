# OnInterruptedBehavior · 中断行为枚举

> `Character/Animation/Requests/OnInterruptedBehavior.cs` — enum，动画请求被中断时的行为

## 枚举定义

```csharp
public enum OnInterruptedBehavior
{
    Resume,   // 中断后恢复默认 Driver
    Cancel    // 中断后取消（不恢复）
}
```

## 调用链

```
被谁调:
  (预留) — 当前未在 Arbiter 中使用
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | AnimationRequest | 中断行为配置（当前未在 Arbiter 中使用） |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Arbiter 对 InterruptedBehavior 的处理 | 待做 | 当前未使用 |
