# IASystemTimeResume
> **源文件**: `Assets/Scripts/Inputs/Actions/System/IASystemTimeResume.cs`

继承 InputActionHandler。释放时发布 SIActionWorldSpeed，控制 Time.timeScale 恢复。

## 调用链

```
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

## 公开属性

无。（继承自 InputActionHandler 的 IsContextBound、IsEnabled 属性，参见 input-action-handler.md）

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| `resumeScale` | float [0.01, infinity) | 1f | 恢复目标值 |

## 方法

### Execute()
```csharp
protected override void Execute(InputAction.CallbackContext context)
```
- **用途**: 恢复时间速度 intent
- **参数**: `context` — Unity Input System 回调上下文
- **调用者**: Unity Input System performed
- **备注**: 只在 performed 时发布；IsEnabled 时处理

### OnSupportsState()
```csharp
protected override bool OnSupportsState(EGameState state)
```
- **用途**: 只在 Playing 状态启用
- **参数**: `state` — 当前游戏状态
- **返回**: state == EGameState.Playing

## 未来规划

无。
