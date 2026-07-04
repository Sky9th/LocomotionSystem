# AbilityDriver · 技能动画驱动

> **Last Verified**: 2026-07-04 | **Verification**: All referenced files exist, signatures match code

> `Character/Animation/Drivers/Ability/AbilityDriver.cs` — `internal sealed class`，继承 `BaseAnimationDriver`，一次性动画驱动。Arbiter 仲裁 → `OnStarted` 播放 clip + 注入 Animancer 事件 + 调回调。

## 调用链

```
外部:
  Brain.SubmitRequest(request) → DriverType=Ability → 解析 AbilityDriver → Arbiter 入队

Arbiter 驱动:
  OnStarted(request)
    → state.Time = 0f  // 重复播放同一 clip 从头开始
    → 从 CustomData 取 AbilityActivationSO
    → layer.Play(clip, fadeIn) + state.Speed = animationSpeed
    → 注入 Animancer 事件: Events(ref _fireSequence) + Add(fireNorm, callback)
      → 回调 request.OnMarker?.Invoke(request)
    → fireNorm >= 1f 或无 clip: 不触发 OnMarker（靠管道计时器兜底）

  OnCompleted()                     → _currentRequest.OnCompleted?.Invoke(_currentRequest)
  OnInterrupted(by)                 → _currentRequest.OnInterrupt?.Invoke(_currentRequest)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | AnimationBrain | 按 DriverType 解析 + Brain.SubmitRequest |
| 依赖 | AbilityActivationSO | 通过 request.CustomData 读取 activation 配置 |
| 依赖 | AnimancerState | 播 clip + 设 Speed + 注入事件 |
| 依赖 | AnimationRequest | 读取 CustomData/回调，通过 DriverArbiter 生命周期调回调 |

## 公开属性

```csharp
public override int ChannelMask => 1 << 0;  // FullBody
```

无其他公开 API。请求构建不由 Driver 负责——外部构建 `AnimationRequest` 后通过 `Brain.SubmitRequest(request)` 提交。

## 方法

### OnStarted(AnimationRequest request)
- 保存 `_currentRequest = request`
- 从 `request.CustomData` 取 `AbilityActivationSO`
- 播 clip + 设 Speed = `animationSpeed`（防御除零）
- 注入 Animancer 事件：
  - `windupDuration > 0`：在 `windupDuration / clipLength` 归一化位置注册回调 → 调 `request.OnMarker?.Invoke()`
  - `windupDuration <= 0` 或 edge case：直接调 `OnMarker`

### OnCompleted()
- 调 `_currentRequest.OnCompleted?.Invoke()` + `_currentRequest = null`
- 由 `DriverArbiter.CheckCompletion` 驱动（`NormalizedTime >= 0.99`）

### OnInterrupted(AnimationRequest by)
- 调 `_currentRequest.OnInterrupt?.Invoke()` + `_currentRequest = null`
- 由 `DriverArbiter` 在队列替换或 `ReleaseActive` 时调用

### Evaluate / Drive / OnResumed
- 空实现。一次性 clip 播放，无需持续驱动。

## 设计决策

| Decision | Reason |
|----------|--------|
| 不持有 `SubmitAbility` 方法 | 请求构建是谁调用谁负责；Driver 只管播放 |
| `_currentRequest` 保存引用 | `OnCompleted/OnInterrupted` 时 Arbiter 可能已清理 `activeRequest` |
| `CustomData` 传 AbilityActivationSO | AnimationRequest 在 L3_Character 域，不能硬依赖 L3_Ability 类型 |
| Animancer 事件用 `Events(ref _fireSequence)` | `Events(this, ...)` 在 state 复用时触发 AssertOwnership 冲突。ref 重载是官方推荐方式——多个调用方轮流复用同一 state 时使用。`_fireSequence = null` 每次请求强制重建 |
