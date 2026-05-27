# SIActionUIEscape
> **源文件**: `Assets/Scripts/Inputs/Structs/SIActionUIEscape.cs`

ESC 按钮动作的规范载荷。在按下 ESC 键时由 IAUIEscape handler 发布。

## 调用链

```
IAUIEscape.Execute()
  └── new SIActionUIEscape(isPressed)
  └── eventDispatcher.Publish(struct)
      └── GameStateService (订阅) → 切换 Playing/Paused
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 生产者 | IAUIEscape | 生产实例 |
| 消费 | 01-core (GameStateService) | 订阅并触发暂停状态切换 |

## 属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `IsPressed` | bool | 当前帧是否按下 |

### 静态属性
```csharp
public static SIActionUIEscape Pressed => new SIActionUIEscape(true);
```

## 未来规划

无。
