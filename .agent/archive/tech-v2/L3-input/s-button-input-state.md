# SButtonInputState · 按钮状态模型

> `Assets/Scripts/Inputs/Structs/Control/Button/SButtonInputState.cs` — 按钮式输入的共享状态模型。分离 held/requested/released 三种信号语义。

## 调用链

```
IAPlayerXxx.Execute()
  └── SButtonInputState.CreateEvent(isPressed, phase)
  └── SIActionXxx.CreateEvent() 包装
  └── eventDispatcher.Publish(struct)
      └── CharacterEventReceiver.PutAction()
          └── ReadActions() → ClearFrameSignals()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 生产者 | 各 IAPlayerButtonXxx | CreateEvent 生产 |
| 消费 | SIActionCrouch / Jump 等 struct | 作为 Button 属性嵌套 |
| 消费 | 02-character (CharacterEventReceiver) | ClearFrameSignals 消费 |

## 属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `IsPressed` | bool | 当前是否按住（持续状态） |
| `Phase` | InputActionPhase | Unity Input System 回调阶段 |
| `IsRequested` | bool | 本帧是否请求（performed+isPressed，一次性信号） |
| `IsReleased` | bool | 本帧是否释放（canceled+!isPressed，一次性信号） |

## 方法

### CreateEvent()
```csharp
public static SButtonInputState CreateEvent(bool isPressed, InputActionPhase phase)
```
- **用途**: 工厂方法，根据按钮状态和阶段计算 isRequested/isReleased
- **参数**: `isPressed` — ReadValueAsButton() 结果；`phase` — 回调阶段
- **返回**: SButtonInputState
- **调用者**: 各 SIActionXxx.CreateEvent()
- **备注**: isRequested = isPressed && phase == Performed；isReleased = !isPressed && phase == Canceled

### ClearFrameSignals()
```csharp
public SButtonInputState ClearFrameSignals()
```
- **用途**: 清除一次性信号 (isRequested=false, isReleased=false)，保留 IsPressed
- **返回**: 新的 SButtonInputState 实例
- **调用者**: CharacterEventReceiver.ReadActions()
- **备注**: 每帧调用，确保帧信号不被多帧重复消费

### 静态属性
```csharp
public static SButtonInputState None => new(false, InputActionPhase.Waiting, false, false);
```

## 关键设计点

- **区分 held vs requested**: IsPressed 是持续状态，用于 Movement 等需要知道"是否按住"的场景；IsRequested 是一次性信号，用于 Toggle/Jump 等"触发一次"的场景
- **ClearFrameSignals 不可逆**: 清除后只能用 CreateEvent 重新生成

## 未来规划

无。
