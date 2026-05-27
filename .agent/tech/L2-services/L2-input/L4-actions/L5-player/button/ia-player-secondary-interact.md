# IAPlayerSecondaryInteract
> **源文件**: `Assets/Scripts/Inputs/Actions/Player/Button/IAPlayerSecondaryInteract.cs`

继承 InputActionHandler。读取按钮状态 → 组装 SIActionSecondaryInteract → 发布事件。

## 调用链

```
Unity Input System (Performed/Canceled)
  └── handler.Execute(context)
      ├── context.ReadValueAsButton() → bool
      ├── SIActionSecondaryInteract.CreateEvent(isPressed, context.phase)
      └── eventDispatcher.Publish(struct)
          └── CharacterEventReceiver (订阅) → 帧缓存
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | InputActionHandler | 基类生命周期 |
| 发布 | SIActionSecondaryInteract | EventDispatcher 广播 |
| 消费 | 02-character (CharacterEventReceiver) | 订阅 SIActionSecondaryInteract |

## 公开属性

无。（继承自 InputActionHandler 的 IsContextBound、IsEnabled 属性，参见 input-action-handler.md）

## 方法

### Execute()
```csharp
protected override void Execute(InputAction.CallbackContext context)
```
- **用途**: 读取按钮状态 → 组装 SIActionSecondaryInteract → 发布事件
- **参数**: `context` — Unity Input System 回调上下文
- **调用者**: Unity Input System performed/canceled
- **备注**: 仅在 IsEnabled 时处理

### OnSupportsState()
未覆写 OnSupportsState，默认返回 true。由 InputService.EnforceHandlerStatePermissions 统一控制。

## 未来规划

无。
