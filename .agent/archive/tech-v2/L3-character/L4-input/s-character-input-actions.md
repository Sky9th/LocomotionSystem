# SCharacterInputActions · 输入聚合结构体

> `Character/Input/SCharacterInputActions.cs` — [Serializable] readonly struct，所有角色输入动作的当前状态

## 调用链

```
创建者:
  CharacterEventReceiver.ReadActions() → new SCharacterInputActions(...)

消费者:
  CharacterActor.Update() → ctx.Input 赋值
  Motor.Evaluate()         → 读取 MoveAction/LastMoveAction
  Stance.Evaluate()        → 读取 MoveAction/SprintAction/CrouchAction/ProneAction/StandAction
  TraversalDriver.Evaluate() → 读取 JumpAction
  SprintStaminaRule         → 通过 ctx.Discrete.Gait 间接
```

## 公开属性

```csharp
public SIActionMove MoveAction { get; }                      // 移动输入 (WASD/摇杆)
public SIActionMove LastMoveAction { get; }                  // 上一帧移动输入
public SIActionLook LookAction { get; }                      // 朝向输入
public SIActionCrouch CrouchAction { get; }                  // 蹲下
public SIActionProne ProneAction { get; }                    // 趴下
public SIActionWalk WalkAction { get; }                      // 行走切换
public SIActionRun RunAction { get; }                        // 跑步切换
public SIActionSprint SprintAction { get; }                  // 冲刺切换
public SIActionJump JumpAction { get; }                      // 跳跃
public SIActionStand StandAction { get; }                    // 站立

public static SCharacterInputActions None { get; }           // 全空值
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterEventReceiver | ReadActions() 创建者 |
| 被依赖 | CharacterFrameContext | Input 字段类型 |
| 被依赖 | Motor/Stance | 读取输入动作 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| PrimaryInteract/SecondaryInteract 纳入聚合 | 待做 | 当前在 CharacterEventReceiver 中单独处理 |
