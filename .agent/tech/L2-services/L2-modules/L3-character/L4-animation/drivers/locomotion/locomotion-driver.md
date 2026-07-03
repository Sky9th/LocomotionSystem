# LocomotionDriver · 移动动画驱动

> `Character/Animation/Drivers/Locomotion/LocomotionDriver.cs` — BaseAnimationDriver，连续移动动画 FSM 驱动
>
> **Last Verified**: 2026-06-20 | **Verification**: All referenced files exist, signatures match code

## 调用链

```
被谁调:
  CharacterActor.OnWire() → OnWire()  — 构造 BaseLayer + ArmPoseLayer
  DriverArbiter  → Evaluate(ctx, dt) — 每帧评估（空实现）
  DriverArbiter  → Drive(ctx, dt)    — 每帧驱动 FSM + ArmPoseLayer
  DriverArbiter  → OnResumed()       — 恢复时刷新动画缓存
  DriverArbiter  → OnInterrupted()   — 被抢占时淡出 Arm

调谁:
  BaseLayer.Update(ctx, dt)          — FSM 每帧 Tick
  BaseLayer.InvalidateAnimationCache() — 恢复时清空 lastPlayedTransition
  ArmPoseLayer.Update(ctx)           — 每帧管理 Arm 层武器姿态
  ArmPoseLayer.FadeOut()             — 被中断时淡出 Arm 层
  ArmPoseLayer.Invalidate()          — 恢复时强制下一帧重新评估
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 继承 | BaseAnimationDriver | 基类 |
| 依赖 | BaseLayer | 5 状态 FSM，Base 层动画控制 |
| 依赖 | ArmPoseLayer | Arm 层武器姿态管理 |
| 依赖 | CharacterBuildContext | 通过 AnimationBrain.BuildContext 提供 DefaultLocomotionSet + LocomotionAnimConfig + ResolvedLocoAnimSet |
| 依赖 | AnimationBrain | 通过基类 brain 获取 FullBodyLayer/ArmLayer/BuildContext |

## 公开属性

```csharp
public override int ChannelMask => 1 << 0;   // FullBody 通道
internal BaseLayer BaseLayer => baseLayer;    // 暴露给 CharacterAudio 注册脚步回调
```

## ArmPoseLayer

> `ArmPoseLayer` 是 LocomotionDriver 的内部子模块，管理 Arm 层的武器姿态动画。

- **创建**: `LocomotionDriver.OnWire()` 中通过 `new ArmPoseLayer(brain?.ArmLayer, buildCtx)` 实例化
- **驱动**: `Drive()` 每帧调 `armPoseLayer.Update(ctx)`，根据 grip/gait 和 ResolvedLocoAnimSet 决定 Arm 层行为
  - Full grip / 静止 / HasFullLocomotion → Arm 淡出（FadeOut）
  - Partial grip 移动且无 FullLocomotion → 播放武器 idleL 补武器 pose
- **中断**: `OnInterrupted()` 调 `armPoseLayer.FadeOut()` 立即淡出 Arm
- **恢复**: `OnResumed()` 调 `armPoseLayer.Invalidate()` 强制下一帧重新评估

## 方法

### OnWire()
```csharp
public override void OnWire()
```
- **用途**: 从 AnimationBrain 获取 BuildContext，创建 BaseLayer 和 ArmPoseLayer
- **调用者**: CharacterActor.OnWire() 递归

### Evaluate()
```csharp
public override void Evaluate(in SCharacterFrameContext ctx, float dt) { }
```
- **用途**: 空实现 — AnimSet 切换逻辑已移至 `BaseLayer.EvaluateAnimSet()`（每帧 Update 中自决）
- **调用者**: DriverArbiter（每帧）

### Drive()
```csharp
public override void Drive(in SCharacterFrameContext ctx, float dt)
```
- **用途**: 调用 BaseLayer.Update(ctx, dt) 驱 FSM + ArmPoseLayer.Update(ctx) 管理武器姿态
- **调用者**: DriverArbiter（当此 Driver 为 Active 时）

### OnResumed()
```csharp
public override void OnResumed()
```
- **用途**: 恢复时 BaseLayer.InvalidateAnimationCache() 强制重播 + ArmPoseLayer.Invalidate() 强制重新评估
- **调用者**: DriverArbiter

### OnInterrupted()
```csharp
public override void OnInterrupted(AnimationRequest by)
```
- **用途**: 被中断时调用 ArmPoseLayer.FadeOut() 淡出 Arm 层武器姿态

## 未来规划

无。
