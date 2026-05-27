# LocomotionDriver · 移动动画驱动

> `Character/Animation/Drivers/LocomotionDriver.cs` — BaseCharacterAnimationDriver，连续移动动画 FSM 驱动

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
| 依赖 | AnimationAliasProfile | 动画别名 SO |
| 依赖 | LocomotionAnimationProfile | 动画参数 SO |
| 依赖 | LocomotionProfile | 移动参数 SO |
| 依赖 | AnimationBrain | 通过基类获取 FullBodyLayer/Rig |

## 公开属性

```csharp
public override int ChannelMask => 1 << 0;   // FullBody 通道
internal BaseLayer BaseLayer => baseLayer;    // 暴露给 CharacterAudio 注册脚步回调
```

## 方法

### OnEnable()
```csharp
protected override void OnEnable()
```
- **用途**: 基类注册 + 创建 BaseLayer FSM
- **调用者**: Unity 生命周期

### Evaluate()
```csharp
public override void Evaluate(in CharacterFrameContext ctx, float dt)
```
- **用途**: 评估条件 — LocomotionDriver 为连续驱动，Evaluate 为空实现
- **备注**: Continuous Driver 不需要条件评估，直接在 Drive 中驱 FSM

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
