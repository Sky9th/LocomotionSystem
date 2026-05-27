# GroundLocomotion · 地面移动仿真

> `Character/Locomotion/GroundLocomotion.cs` — ILocomotionSimulator 实现，串联 Motor → Stance

## 调用链

```
被谁调:
  CharacterActor.Update() → locomotionSimulator.Simulate(ref ctx, profile, dt)

调谁:
  Motor.Evaluate()     → ctx.Motor 写入
  Stance.Evaluate()    → ctx.Discrete 写入
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | Motor | 速度/转角计算 |
| 依赖 | Stance | 离散状态判定 |
| 输出 | CharacterFrameContext.Motor/Discrete | 通过 ref 写入 |
| 实现 | ILocomotionSimulator | 接口实现 |

## 方法

### Simulate()
```csharp
public void Simulate(ref CharacterFrameContext ctx, LocomotionProfile profile, float dt)
```
- **用途**: 单步移动仿真 — Motor 计算速度 → Stance 判定离散状态
- **参数**: `ctx` — 帧上下文（ref 写入 Motor/Discrete）；`profile` — 移动配置；`dt` — 帧时间
- **调用者**: `CharacterActor.Update()`

## 未来规划

无。
