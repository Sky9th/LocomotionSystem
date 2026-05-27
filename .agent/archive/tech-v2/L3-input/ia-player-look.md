# IAPlayerLook · 朝向输入

> `Assets/Scripts/Inputs/Actions/Player/IAPlayerLook.cs` — 继承 InputActionHandler。读取鼠标 Delta，应用灵敏度缩放和 Y 轴反转后发布 SIActionLook。

## 调用链

```
Unity Input System (Look Action)
  └── performed
      └── IAPlayerLook.Execute(context)
          ├── context.ReadValue<Vector2>() * sensitivity
          ├── invertY ? delta.y = -delta.y
          └── eventDispatcher.Publish(SIActionLook)
              └── CameraService / CharacterHeadLook (订阅) → 消费转向数据
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | InputActionHandler | 基类生命周期 |
| 发布 | SIActionLook | EventDispatcher 广播 |
| 消费 | 02-character (CharacterHeadLook) | 订阅 SIActionLook |

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| `sensitivity` | float | 1f | 鼠标灵敏度倍数 |
| `invertY` | bool | true | 是否反转 Y 轴 |

## 方法

### Execute()
```csharp
protected override void Execute(InputAction.CallbackContext context)
```
- **用途**: 读取鼠标 Delta → 灵敏度缩放 → Y 轴反转 → 发布 SIActionLook
- **参数**: `context` — Unity Input System 回调上下文
- **调用者**: Unity Input System performed 回调
- **备注**: 仅在 IsEnabled 时处理

### OnSupportsState()
```csharp
protected override bool OnSupportsState(EGameState state)
```
- **用途**: 只在 Playing 状态启用
- **参数**: `state` — 当前游戏状态
- **返回**: state == EGameState.Playing

## 未来规划

无。
