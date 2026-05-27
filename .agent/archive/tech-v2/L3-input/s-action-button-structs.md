# SIAction Button Structs · 按钮动作数据结构

> `Assets/Scripts/Inputs/Structs/Control/Button/SIAction*.cs` — 9 个按钮式输入动作的数据结构，全部同构模式。每个 struct 包装一个 `SButtonInputState` 字段。

## 文件清单

| 文件 | 含义 | 使用场景 |
|------|------|---------|
| SIActionCrouch.cs | 蹲下意图 | Locomotion Posture 切换 |
| SIActionJump.cs | 跳跃意图 | Locomotion Phase 切换 |
| SIActionPrimaryInteract.cs | 主交互 (鼠标左键) | 攻击/拾取/使用 |
| SIActionProne.cs | 趴下意图 | Locomotion Posture 切换 |
| SIActionRun.cs | 跑步切换 | Locomotion Gait 切换 |
| SIActionSecondaryInteract.cs | 副交互 (鼠标右键) | 瞄准/副操作 |
| SIActionSprint.cs | 冲刺切换 | Locomotion Gait 切换 |
| SIActionStand.cs | 站立意图 | Locomotion Posture 切换 |
| SIActionWalk.cs | 行走切换 | Locomotion Gait 切换 |

> 注: SIActionRun 和 SIActionWalk 的 XML 注释误写为 "jump intent" (代码缺陷)。

## 调用链

```
IAPlayerXxx.Execute()
  └── SIActionXxx.CreateEvent(isPressed, phase)
  └── eventDispatcher.Publish(struct)
      └── CharacterEventReceiver.PutAction() → 帧缓存
          └── ReadActions() → 聚合到 SCharacterInputActions
          └── ClearFrameSignals() → 清除一次性信号
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 生产者 | 对应 IAPlayerXxx Handler | CreateEvent 生产 |
| 消费 | 02-character (CharacterEventReceiver) | 订阅缓存 + ClearFrameSignals |

## 公共结构

所有 9 个 struct 完全同构：

### 属性
```csharp
public SButtonInputState Button { get; }
```

### 方法

#### CreateEvent()
```csharp
public static SIActionXxx CreateEvent(bool isPressed, InputActionPhase phase)
```
- **用途**: 工厂方法，包装 SButtonInputState.CreateEvent
- **参数**: `isPressed` — 按钮按下状态；`phase` — 输入阶段
- **返回**: SIActionXxx 实例
- **调用者**: 对应 IAPlayerXxx.Execute()

#### ClearFrameSignals()
```csharp
public SIActionXxx ClearFrameSignals()
```
- **用途**: 清除帧信号，返回新实例
- **返回**: Button.ClearFrameSignals() 后的新 struct
- **调用者**: CharacterEventReceiver.ReadActions()

### 静态属性
```csharp
public static SIActionXxx None => new SIActionXxx(SButtonInputState.None);
```

## 未来规划

无。
