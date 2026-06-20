# LocomotionDriver · 移动动画驱动

> `Character/Animation/Drivers/LocomotionDriver.cs` — BaseCharacterAnimationDriver，连续移动动画 FSM 驱动
>
> **Last Verified**: 2026-06-20 | **Verification**: All referenced files exist, signatures match code

## 调用链

```
被谁调:
  Unity 生命周期 → OnEnable/OnDisable (继承自 BaseCharacterAnimationDriver)
  DriverArbiter  → Drive(ctx, dt)  — 每帧驱动 FSM
  DriverArbiter  → OnResumed()     — 恢复时刷新动画缓存

调谁:
  BaseLayer.Update(ctx, dt)        — FSM 每帧 Tick
  BaseLayer.InvalidateAnimationCache() — 恢复时清空 lastPlayedAlias
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 继承 | BaseCharacterAnimationDriver | 基类 |
| 依赖 | BaseLayer | 7 状态 FSM |
| 依赖 | CharacterBuildContext | 通过 AnimationBrain.BuildContext 读取 ResolvedLocoAnimSet + BodyForm |
| 依赖 | BaseLayer | 5 状态 FSM |
| 依赖 | LocomotionAnimationSetSO | 当前动画集（从 BuildContext 读，不再自行解析） |
| 依赖 | LocomotionAnimationConfigSO | 动画阈值配置 |
| 依赖 | AnimationBrain | 通过基类获取 FullBodyLayer/ArmLayer |

## 公开属性

```csharp
public override int ChannelMask => 1 << 0;   // FullBody 通道
internal BaseLayer BaseLayer => baseLayer;    // 暴露给 CharacterAudio 注册脚步回调
```

## 方法

### OnWire()
```csharp
public override void OnWire()
```
- **用途**: 从 AnimationBrain 获取 BuildContext，创建 BaseLayer，缓存 defaultAnimSet
- **调用者**: CharacterActor.OnWire() 递归

### Evaluate()
```csharp
public override void Evaluate(in CharacterFrameContext ctx, float dt)
```
- **用途**: 检测 BuildContext.ResolvedLocoAnimSet 是否变化，变化时 swap BaseLayer.AnimSet
- **细节**: 从 BuildContext 读 animSet（不再调 GripTable.Resolve）。HasFullLocomotion → 全量 swap + Arm 层 fade out；否则 BaseLayer 保持 defaultSet + Arm 层叠武器 idle
- **调用者**: DriverArbiter（每帧）

### Drive()
```csharp
public override void Drive(in CharacterFrameContext ctx, float dt)
```
- **用途**: 调用 BaseLayer.Update(ctx, dt) 驱 FSM
- **调用者**: DriverArbiter（当此 Driver 为 Active 时）

### OnResumed()
```csharp
public override void OnResumed()
```
- **用途**: 恢复时 InvalidateAnimationCache，强制重播
- **调用者**: DriverArbiter

### OnInterrupted()
```csharp
public override void OnInterrupted(AnimationRequest by)
```
- **用途**: 被中断时的回调 — 当前为空实现

## 未来规划

无。
