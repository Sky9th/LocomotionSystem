# SIActionLook
> **源文件**: `Assets/Scripts/Inputs/Structs/Control/SIActionLook.cs`

朝向/视角意图的规范载荷。包含鼠标 Delta (X=yaw, Y=pitch)。

## 调用链

```
IAPlayerLook.Execute()
  └── new SIActionLook(delta)
  └── eventDispatcher.Publish(struct)
      └── CharacterEventReceiver.PutAction<SIActionLook>() → 帧缓存
          └── Actor.Evaluate() → CharacterFrameContext → CharacterHeadLook
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 生产者 | IAPlayerLook | 生产实例 |
| 消费 | 02-character (CharacterEventReceiver) | 订阅并缓存 |
| 消费 | 02-character (CharacterHeadLook) | 最终消费 |

## 属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `Delta` | Vector2 | 本帧采样到的转向 Delta (X=水平, Y=垂直) |
| `HasDelta` | bool | 是否有有效输入 (sqrMagnitude > Epsilon) |

### 静态属性
```csharp
public static SIActionLook None => new(Vector2.zero);
```

## 未来规划

无。
