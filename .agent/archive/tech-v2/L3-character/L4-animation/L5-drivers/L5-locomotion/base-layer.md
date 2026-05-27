# BaseLayer · 基础层 FSM

> `Character/Animation/Drivers/Locomotion/BaseLayer.cs` — 纯 C# 类，7 状态 FSM，Base 层动画控制核心

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
    → Owner.Play(alias) / PlayIfChanged / PlayFromStart / HasCompleted
    → Owner.ApplyTurnStepRotation()
  Animancer:
    → Layer.TryPlay(alias)
  CharacterRig:
    → ApplyRotation (通过 ApplyTurnStepRotation)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | LocomotionDriver | 唯一调用者 |
| 依赖 | AnimationAliasProfile | 动画别名 |
| 依赖 | LocomotionAnimationProfile | 动画参数 |
| 依赖 | LocomotionProfile | 移动参数 |
| 依赖 | CharacterRig | 程序化转身旋转 |
| 依赖 | AnimancerLayer | 播放动画 |
| 依赖 | StateMachine<BaseStateKey, ...> | Animancer FSM |
| 内部包含 | 7 个 State 实例 | Idle/Moving/TurnInPlace/IdleToMoving/TurnInMoving/AirLoop/AirLand |

## 公开属性

```csharp
internal AnimationAliasProfile Alias { get; }                 // 动画别名
internal LocomotionAnimationProfile AnimProfile { get; }      // 动画参数
internal LocomotionProfile LocoProfile { get; }               // 移动参数
internal CharacterRig Rig { get; }                           // 物理实体
internal CharacterFrameContext Ctx => ctx;                    // 当前帧上下文
internal float DeltaTime => deltaTime;                        // 当前帧时间
internal AnimancerLayer Layer { get; }                        // Animancer 层

internal float AirborneStartY;       // 起跳时 Y（AirLoop 记录）
internal float MaxFallDistance;      // 最大坠落距离（AirLoop 记录）
internal System.Action FootstepCallback;  // 脚步事件回调
```

## 方法

### BaseLayer()
```csharp
internal BaseLayer(AnimancerLayer layer, AnimationAliasProfile alias, LocomotionAnimationProfile animProfile,
    LocomotionProfile locoProfile, CharacterRig rig)
```
- **用途**: 构造，创建 7 个 State 实例并注册到 StateMachine
- **调用者**: `LocomotionDriver.OnEnable()`

### Update()
```csharp
internal void Update(CharacterFrameContext ctx, float dt)
```
- **用途**: 缓存帧上下文 → 确保有初始状态 → 当前状态 Tick
- **调用者**: `LocomotionDriver.Drive()`

### TrySetState() / ForceSetState()
```csharp
internal bool TrySetState(BaseStateKey key)
internal bool ForceSetState(BaseStateKey key)
```
- **用途**: FSM 状态切换
- **调用者**: 各 State 的 Tick() 方法

### PlayFromStart()
```csharp
internal void PlayFromStart(StringAsset alias)
```
- **用途**: 播放动画并重置到第 0 帧
- **备注**: 用于一次性过渡动画（IdleToMoving、TurnInMoving）

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
- **调用者**: TurnInPlace/TurnInMoving/Moving 状态的 Tick

### InvalidateAnimationCache()
```csharp
internal void InvalidateAnimationCache()
```
- **用途**: 清空 lastPlayedAlias（被中断后恢复时强制重播）

### Play()
```csharp
internal void Play(StringAsset alias)
```
- **用途**: 播放动画 + 注入脚步事件

### PlayIfChanged()
```csharp
internal void PlayIfChanged(StringAsset alias)
```
- **用途**: 仅当 alias 变化时播放（避免重复设置）

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
     │   ┌────────┼──────┐      │
     │   ▼        ▼      ▼      │
     │ TurnIn   IdleTo  Moving  │
     │ Place    Moving  /TurnIn │
     │   │        │     Moving  │
     │   └────────┼──────┘      │
     │            ▼             │
     │        AirLoop→AirLand   │
     └──────────────────────────┘
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
