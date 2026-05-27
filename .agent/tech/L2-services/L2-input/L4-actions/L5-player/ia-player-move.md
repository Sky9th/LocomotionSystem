# IAPlayerMove
> **源文件**: `Assets/Scripts/Inputs/Actions/Player/IAPlayerMove.cs`

继承 InputActionHandler。读取 WASD/摇杆 Vector2 值，经 deadzone 滤波和归一化后转为世界方向，发布 SIActionMove。

## 调用链

```
Unity Input System (Move Action)
  └── performed/canceled
      └── IAPlayerMove.Execute(context)
          ├── context.ReadValue<Vector2>() → rawInput
          ├── deadZone 滤波 / normalizeWorldDirection
          ├── CalculateWorldDirection(rawInput) → worldSpace Vector3
          └── eventDispatcher.Publish(SIActionMove)
              └── CharacterEventReceiver (订阅) → 帧缓存
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | InputActionHandler | 基类生命周期 |
| 发布 | SIActionMove | EventDispatcher 广播 |
| 消费 | 02-character (CharacterEventReceiver) | 订阅 SIActionMove |
| 消费 | 02-character (GroundLocomotion.Motor) | 最终消费方向数据 |

## 公开属性

无。（继承自 InputActionHandler 的 IsContextBound、IsEnabled 属性，参见 input-action-handler.md）

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| `deadZone` | float [0,1] | 0.15f | 输入死区，小于此值视为零 |
| `normalizeWorldDirection` | bool | true | 是否将世界方向归一化 |

## 方法

### Execute()
```csharp
protected override void Execute(InputAction.CallbackContext context)
```
- **用途**: 读取 Vector2 输入 → 死区滤波 → 归一化 → 转为世界方向 → 发布 SIActionMove
- **参数**: `context` — Unity Input System 回调上下文
- **调用者**: Unity Input System performed/canceled 回调
- **备注**: 仅在 IsEnabled 时处理；`rawInput.magnitude < deadZone` 时清零

### CalculateWorldDirection()
```csharp
private Vector3 CalculateWorldDirection(Vector2 planarInput)
```
- **用途**: 将平面 Vector2 (X=左右, Y=前后) 映射到世界空间 X/Z 平面
- **参数**: `planarInput` — 经过滤波的输入向量
- **返回**: worldSpace Vector3，Y=0
- **调用者**: Execute()
- **备注**: 当前未参考摄像机朝向，需要时可在 InitializeHandler 注入 Camera Transform

### OnSupportsState()
```csharp
protected override bool OnSupportsState(EGameState state)
```
- **用途**: 只在 Playing 状态启用
- **参数**: `state` — 当前游戏状态
- **返回**: state == EGameState.Playing

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 摄像机朝向对齐（Camera-Relative 移动） | 待做 | 旧 input-manager.md |
