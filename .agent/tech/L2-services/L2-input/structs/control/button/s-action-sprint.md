# SIActionSprint
> **源文件**: `Assets/Scripts/Inputs/Structs/Control/Button/SIActionSprint.cs`

冲刺切换意图的规范载荷。包装 SButtonInputState，标识 Locomotion Gait 切换。

## 调用链

```
IAPlayerSprint.Execute()
  └── SIActionSprint.CreateEvent(isPressed, phase)
  └── eventDispatcher.Publish(struct)
      └── CharacterEventReceiver.PutAction() → 帧缓存
          └── ReadActions() → 聚合到 SCharacterInputActions
          └── ClearFrameSignals() → 清除一次性信号
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 生产者 | IAPlayerSprint | CreateEvent 生产 |
| 消费 | 02-character (CharacterEventReceiver) | 订阅缓存 + ClearFrameSignals |

## 属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Button` | SButtonInputState | 按钮状态 |

## 方法

### CreateEvent()
```csharp
public static SIActionSprint CreateEvent(bool isPressed, InputActionPhase phase)
```
- **用途**: 工厂方法，包装 SButtonInputState.CreateEvent
- **参数**: `isPressed` — 按钮按下状态；`phase` — 输入阶段
- **返回**: SIActionSprint 实例
- **调用者**: IAPlayerSprint.Execute()

### ClearFrameSignals()
```csharp
public SIActionSprint ClearFrameSignals()
```
- **用途**: 清除帧信号，返回新实例
- **返回**: Button.ClearFrameSignals() 后的新 struct
- **调用者**: CharacterEventReceiver.ReadActions()

### 静态属性
```csharp
public static SIActionSprint None => new SIActionSprint(SButtonInputState.None);
```

## 未来规划

无。
