# SIActionMove · 移动动作数据

> `Assets/Scripts/Inputs/Structs/Control/SIActionMove.cs` — 平面移动意图的规范载荷。包含原始输入、世界方向、输入阶段。

## 调用链

```
IAPlayerMove.Execute()
  └── new SIActionMove(rawInput, worldDirection, phase)
  └── eventDispatcher.Publish(struct)
      └── CharacterEventReceiver.PutAction<SIActionMove>() → 帧缓存
          └── Actor.Evaluate() → CharacterFrameContext → Motor.ComputeDesired()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 生产者 | IAPlayerMove | 生产实例 |
| 消费 | 02-character (CharacterEventReceiver) | 订阅并缓存 |
| 消费 | 02-character (Motor.ComputeDesired) | 最终消费 |

## 属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `RawInput` | Vector2 | 原始 WASD/摇杆输入值 |
| `WorldDirection` | Vector3 | 世界空间方向 (X/Z 平面) |
| `Phase` | InputActionPhase | 输入阶段 (Performed/Canceled/Waiting) |
| `HasInput` | bool | 是否有有效输入 (sqrMagnitude > Epsilon) |

### 静态属性
```csharp
public static SIActionMove None => new(Vector2.zero, Vector3.zero, InputActionPhase.Waiting);
```

## 未来规划

无。
