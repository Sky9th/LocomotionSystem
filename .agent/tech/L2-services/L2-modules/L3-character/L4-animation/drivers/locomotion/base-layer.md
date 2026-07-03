# ⛔ CRITICALLY OUTDATED — 需完全重写

> **Status**: 本文档几乎所有技术细节与代码不符。构造函数、依赖、状态数量、方法签名全错。
> **以 `Animation/Drivers/Locomotion/BaseLayer.cs` 代码为准。**
> **最后验证**: 2026-07-03 — 保留作架构参考，技术细节不可信。

---

# BaseLayer · 基础层 FSM

> `Character/Animation/Drivers/Locomotion/BaseLayer.cs` — 纯 C# 类，5 状态 FSM，Base 层动画控制核心

## 调用链

```
被谁调:
  LocomotionDriver.Drive()
    → BaseLayer.Update(ctx, dt)
  LocomotionDriver.OnResumed()
    → BaseLayer.InvalidateAnimationCache()

调谁:
  FSM:
    → ForceSetState / TrySetState
    → CurrentState.Tick()
  State 内部:
    → Owner.Play(transition) / PlayIfChanged(transition) / HasCompleted
    → Owner.ApplyTurnStepRotation()
  Animancer:
    → Layer.Play(transition)
  CharacterRig:
    → ApplyRotation (通过 ApplyTurnStepRotation)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | LocomotionDriver | 唯一调用者 |
| 依赖 | LocomotionAnimationSetSO | 动画集（默认 + 武器集 swap） |
| 依赖 | LocomotionAnimationConfigSO | 转身速度等动画参数 |
| 依赖 | CharacterBuildContext | 提供 Rig、ResolvedLocoAnimSet |
| 依赖 | CharacterRig | 程序化转身旋转（通过 BuildContext） |
| 依赖 | AnimancerLayer | 播放动画 |
| 依赖 | StateMachine<BaseStateKey, ...> | Animancer FSM |
| 内部包含 | 5 个 State 实例 | Idle/Moving/TurnInPlace/AirLoop/AirLand |

## 公开属性

```csharp
internal LocomotionAnimationSetSO AnimSet { get; }            // 当前动画集（可被 EvaluateAnimSet 切换）
internal ITransition IdleOverride { get; }                    // 非 null 时 Idle 使用此 clip 替代 AnimSet.idleL
internal LocomotionAnimationConfigSO AnimProfile { get; }     // 动画参数（转身速度等）
internal CharacterRig Rig { get; }                           // 物理实体（通过 BuildContext）
internal SCharacterFrameContext Ctx => ctx;                   // 当前帧上下文
internal float DeltaTime => deltaTime;                        // 当前帧时间
internal AnimancerLayer Layer { get; }                        // Animancer 层

internal float AirborneStartY;       // 起跳时 Y（AirLoop 记录）
internal float MaxFallDistance;      // 最大坠落距离（AirLoop 记录）
internal System.Action FootstepCallback;  // 脚步事件回调
```

## 方法

### BaseLayer()
```csharp
internal BaseLayer(AnimancerLayer layer, LocomotionAnimationSetSO defaultAnimSet,
    LocomotionAnimationConfigSO animProfile, CharacterBuildContext buildContext)
```
- **用途**: 构造，创建 5 个 State 实例并注册到 StateMachine
- **调用者**: `LocomotionDriver.OnWire()`

### Update()
```csharp
internal void Update(SCharacterFrameContext ctx, float dt)
```
- **用途**: 缓存帧上下文 → EvaluateAnimSet 自决 AnimSet/IdleOverride → 确保有初始状态 → 当前状态 Tick
- **调用者**: `LocomotionDriver.Drive()`

### TrySetState() / ForceSetState()
```csharp
internal bool TrySetState(BaseStateKey key)
internal bool ForceSetState(BaseStateKey key)
```
- **用途**: FSM 状态切换
- **调用者**: 各 State 的 Tick() 方法

### EvaluateAnimSet()
```csharp
private void EvaluateAnimSet()
```
- **用途**: 每帧根据 grip/gait 自动切换 AnimSet 和 IdleOverride
- **细节**: 从 BuildContext.ResolvedLocoAnimSet 读取当前武器动画集；HasFullLocomotion 时全量 swap + 清空 IdleOverride；否则 BaseLayer 保持 defaultSet + IdleOverride 设为武器 idle 供 IdleState 使用
- **调用者**: `Update()` 每帧

### HasCompleted()
```csharp
internal bool HasCompleted()
```
- **用途**: 判断当前动画是否播放完成 (NormalizedTime >= 0.99)

### ApplyTurnStepRotation()
```csharp
internal bool ApplyTurnStepRotation()
```
- **用途**: 程序化转身 — 读取 TurnAngle 和动画转身速度，每帧逐步旋转模型
- **调用者**: TurnInPlace/Moving 状态的 Tick

### InvalidateAnimationCache()
```csharp
internal void InvalidateAnimationCache()
```
- **用途**: 清空 lastPlayedTransition（被中断后恢复时强制重播）

### Play()
```csharp
internal void Play(ITransition transition)
```
- **用途**: 播放动画 + 注入脚步事件

### PlayIfChanged()
```csharp
internal void PlayIfChanged(ITransition transition)
```
- **用途**: 仅当 transition 变化时播放（避免重复设置）

### InjectFootstepEvents()
```csharp
private void InjectFootstepEvents()
```
- **用途**: 在 MixerState 的子动画中注入脚步事件（0.12 和 0.62 归一化时间处）

## 内部机制

### FSM 状态转换图

```
             ┌──────────┐
     ┌──────→│   Idle   │←──────┐
     │       └────┬─────┘       │
     │            │             │
     │       ┌────┼────┐        │
     │       ▼    ▼    ▼        │
     │   TurnIn Moving AirLoop  │
     │   Place  │       │       │
     │     │    │       ▼       │
     │     │    │    AirLand    │
     │     └────┼──────┘        │
     └──────────┘───────────────┘
```

### ApplyTurnStepRotation 逻辑
```
absAngle = abs(TurnAngle)
if absAngle <= Epsilon → return
speed = AnimProfile.GetTurnSpeed(posture, gait, isMoving)
step = Min(speed * dt, absAngle)
delta = Sign(TurnAngle) * step
Rig.ApplyRotation(Quaternion.AngleAxis(delta, up))
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| FootstepCallback 当前由 CharacterAudio 注册，通过 InjectFootstepEvents 触发 | 当前 | 已实现 |
| ELocomotionPhase.Landing 状态在 AirLand 完成后未使用 | 待做 | 枚举已定义 |
