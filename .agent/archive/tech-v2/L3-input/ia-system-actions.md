# IASystemTimeSlow / IASystemTimeResume · 时间控制

> `Assets/Scripts/Inputs/Actions/System/IASystemTimeSlow.cs` 和 `IASystemTimeResume.cs` — 继承 InputActionHandler。按住/释放时发布 SIActionWorldSpeed，控制 Time.timeScale。

## 调用链

```
IASystemTimeSlow.Execute()
  └── context.performed → SIActionWorldSpeed(slowScale) → EventDispatcher
      └── TimeService (订阅) → Time.timeScale = slowScale

IASystemTimeResume.Execute()
  └── context.performed → SIActionWorldSpeed(resumeScale) → EventDispatcher
      └── TimeService (订阅) → Time.timeScale = resumeScale
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | InputActionHandler | 基类生命周期 |
| 发布 | SIActionWorldSpeed (01-core) | EventDispatcher 广播 |
| 消费 | 01-core (TimeService) | 订阅 SIActionWorldSpeed |

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| IASystemTimeSlow.slowScale | float [0.01, 1] | 0.1f | 减速目标值 |
| IASystemTimeResume.resumeScale | float [0.01, ∞) | 1f | 恢复目标值 |

## 方法

### IASystemTimeSlow.Execute()
```csharp
protected override void Execute(InputAction.CallbackContext context)
```
- **用途**: 按住时发布慢放速度 intent
- **参数**: `context` — Unity Input System 回调上下文
- **调用者**: Unity Input System performed
- **备注**: 只在 performed 时发布，canceled 不处理

### IASystemTimeResume.Execute()
```csharp
protected override void Execute(InputAction.CallbackContext context)
```
- **用途**: 释放时恢复时间速度 intent
- **参数**: `context` — Unity Input System 回调上下文
- **调用者**: Unity Input System performed
- **备注**: 只在 performed 时发布

### OnSupportsState()
```csharp
protected override bool OnSupportsState(EGameState state)
// 两者均返回 state == EGameState.Playing
```

## 未来规划

无。
