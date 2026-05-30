# L4_Director · 角色意图层

> `L3_Character/L4_Director/` — 将外部输入翻译为 `SCharacterIntent`，隔离输入来源差异

## 调用链

```
被谁调:
  CharacterActor.Update()        → director.Evaluate()

调谁:
  ICharacterDirector             → Evaluate() 返回 SCharacterIntent
  PlayerInputReceiver (Player)   → Subscribe / Unsubscribe / Reset
  L2 Input System                → 事件订阅 (SCameraSnapshot, SIAction*)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | L2 Input | 订阅 SIAction* 事件 |
| 依赖 | L1 Core (EventDispatcher) | 玩家 Director 订阅事件 |
| 被依赖 | CharacterActor | 消费 Evaluate() 产出 |
| 被依赖 | Motor / Stance | 消费 SCharacterIntent 字段 |

## 接口

### ICharacterDirector

```csharp
public interface ICharacterDirector
{
    SCharacterIntent Evaluate();
}
```

## Struct

### SCharacterIntent

```csharp
public readonly struct SCharacterIntent
{
    Vector3 LocomotionHeading;   // 移动方向
    Vector3 AimDirection;        // 注视/瞄准方向
    EMovementGait DesiredGait;   // 想要的步态
    EPosture DesiredPosture;     // 想要的姿态
    bool JumpRequested;          // 攀爬/跳跃请求 (由寻路决定)
}
```

## 实现

| 实现 | 来源 | 状态 |
|------|------|------|
| PlayerDirector | 鼠标 + 按键 → 事件订阅 → 翻译为 Intent | 已实现 |
| AIDirector | 行为树黑板 → 直接构造 Intent | 占位 |

## 目录

```
L4_Director/
├── ICharacterDirector.cs
├── SCharacterIntent.cs
├── Player/
│   ├── PlayerDirector.cs
│   └── PlayerInputReceiver.cs
└── AI/
    └── .gitkeep
```
