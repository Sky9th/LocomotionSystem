# GroundLocomotion · 地面移动仿真

> `L4_Locomotion/Ground/GroundLocomotion.cs` — ILocomotionSimulator 实现，串联 Motor → Stance

## 目录

```
L4_Locomotion/
├── ILocomotionSimulator.cs          (接口)
├── Config/LocomotionProfile.cs      (配置)
├── Structs/                         (数据容器)
└── Ground/
    ├── GroundLocomotion.cs          (编排)
    ├── Motor.cs                     (速度计算)
    └── Stance.cs                    (离散状态 + MotionSpeedScale)
```

## 调用链

```
被谁调:
  CharacterActor.Update() → locomotionSimulator.Simulate(ref ctx, intent, profile, dt)

调谁:
  Motor.Evaluate()     → ctx.Motor
  Stance.Evaluate()    → ctx.Discrete (含 MotionSpeedScale + EffectiveMaxSpeed)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | Motor | 速度/转角计算 |
| 依赖 | Stance | 离散状态判定 + MotionSpeedScale 计算 |
| 输出 | CharacterFrameContext.Motor/Discrete | 通过 ref 写入 |
| 实现 | ILocomotionSimulator | 接口实现 |

## 方法

### Simulate()
```csharp
public void Simulate(ref CharacterFrameContext ctx, in SCharacterIntent intent, LocomotionProfile profile, float dt)
```
- Stance 从 `ctx.LocomotionAnimationProfile` 读取 animNativeSpeed 计算 MotionSpeedScale
- 输出 `ctx.Discrete.EffectiveMaxSpeed` 供 Pathfinding 直接消费

## MotionSpeedScale 数据流

```
ctx.LocomotionProfile + ctx.LocomotionAnimationProfile
  → Stance.ComputeBaseSpeedScale(gait, posture, profile, animProfile)
    → gaitSpeed / animNativeSpeed
      → 缓存 (仅 gait/posture 变化时重算)
        → SCharacterDiscrete.MotionSpeedScale
        → SCharacterDiscrete.EffectiveMaxSpeed = gaitSpeed × MotionSpeedScale
          → PathfindingAgent.SyncLocomotion() 直接消费
```
