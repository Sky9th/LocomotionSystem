# SCharacterIntent · 角色意图结构体

> `Character/Director/SCharacterIntent.cs` — readonly struct，角色每帧的期望行为

## 调用链

```
创建者:
  ICharacterDirector.Evaluate() → new SCharacterIntent(...)

消费者:
  Motor.Evaluate() → 读取 OverrideMovementVelocity/ExternalMovementVelocity
  Stance.Evaluate() → 读取 DesiredGait/DesiredPosture
  CharacterKinematic.Evaluate() → 读取 LocomotionHeading/AimDirection
  GroundLocomotion.Simulate() → 透传 in SCharacterIntent
```

## 公开属性

```csharp
// ── Direction ──
public readonly Vector3 LocomotionHeading;
public readonly Vector3 AimDirection;

// ── Locomotion ──
public readonly EMovementGait DesiredGait;
public readonly EPosture DesiredPosture;

// ── Actions ──
public readonly bool JumpRequested;

// ── Override ──
public readonly bool OverrideMovementVelocity;         // true=使用 ExternalMovementVelocity
public readonly Vector3 ExternalMovementVelocity;      // world-space 外部速度 (AIPath desiredVelocity)

public bool HasMovement => DesiredGait != EMovementGait.Idle;
public static SCharacterIntent None { get; }
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | PlayerDirector | 创建者 |
| 被依赖 | Motor | 读取 Override/External 字段 |
| 被依赖 | Stance | 读取 Gait/Posture |
| 被依赖 | CharacterKinematic | 读取 Heading/Aim |

## 设计决策

| 决策 | 原因 |
|------|------|
| Override 字段为可选参数（default=false, default=Vector3.zero） | 向后兼容，非寻路路径无需改动 |
| 通过 Intent 传递外部速度而非修改 Motor 签名 | 不改变 ILocomotionSimulator 接口，数据流一致 |
| ExternalMovementVelocity 为 world-space | AIPath 输出即为 world-space，Motor 内部 ConvertToLocal |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| AI Director 使用相同 Override 机制 | 待做 | Phase 4.3 |
