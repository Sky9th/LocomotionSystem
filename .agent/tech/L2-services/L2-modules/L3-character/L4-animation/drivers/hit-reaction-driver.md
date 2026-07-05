# HitReactionDriver · 受击动画驱动

> **Last Verified**: 2026-07-05 | **Verification**: All referenced files exist, signatures match code

> `Character/Animation/Drivers/HitReaction/HitReactionDriver.cs` — `internal sealed class`，继承 `BaseAnimationDriver`。播放受击 MixerTransition2D 混合动画，由 CharacterCombat 通过 AnimationBrain.SubmitRequest 触发。

## 调用链

```
CharacterCombat.OnReaction() / OnDamaged()
  → 构建 AnimationRequest(DriverType=HitReaction, CustomData=SHitReactionData)
  → ctx.Animation.SubmitRequest(request)
  → Brain.SubmitRequest() → DriverType=HitReaction → HitReactionDriver
  → Arbiter 入队 → 抢占仲裁

Arbiter 驱动:
  OnStarted(request)
    → (SHitReactionData)request.CustomData 解包
    → 临时覆写 Mixer.FadeDuration = request.FadeIn（AnimancerLayer.Play(ITransition) 无 fade 参数）
    → brain.FullBodyLayer.Play(mixer)
    → 恢复 Mixer.FadeDuration
    → if (state is MixerState<Vector2> mixerState)
        mixerState.Parameter = new Vector2(data.DirX, data.DirY)

  OnCompleted()  → _currentRequest.OnCompleted?.Invoke() → 链式提交起身（Impact Knockdown）
  OnInterrupted(by) → _currentRequest.OnInterrupt?.Invoke()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | AnimationBrain | Awake 注册 + SubmitRequest 路由 |
| 被依赖 | DriverArbiter | 生命周期驱动 |
| 依赖 | SHitReactionData | CustomData 解包 → Mixer + DirX + DirY |
| 依赖 | MixerTransition2D | 播放 4 方向受击混合动画 |
| 依赖 | AnimationRequest | 读取 CustomData / FadeIn / OnCompleted |

## 公开属性

```csharp
public override int ChannelMask => 1 << 0;  // FullBody
```

## SHitReactionData

```csharp
internal struct SHitReactionData
{
    public MixerTransition2D Mixer;  // 受击动画混合器（Flinch/Stagger/Knockdown/GetUp）
    public float DirX;               // 混合参数 X（左右，-1..1）
    public float DirY;               // 混合参数 Y（前后，-1..1）
}
```

同文件定义，`internal` 跨命名空间可访问（无 asmdef 边界）。仅 CharacterCombat 构建，外部不直接使用。

## 方法

### OnStarted(AnimationRequest request)
```csharp
public override void OnStarted(AnimationRequest request)
```
- **Purpose**: 解包 CustomData → 播放受击混合动画
- **Notes**: `FadeDuration` 临时覆写模式——`ITransition` 只支持单参 Play，`request.FadeIn` 通过临时修改 `Mixer.FadeDuration` 生效。Play() 同步读取一次，立即恢复。

### OnCompleted()
```csharp
public override void OnCompleted()
```
- **Purpose**: 触发 `request.OnCompleted` 回调 → Impact Knockdown 链式提交 ChainGetUp

### OnInterrupted(AnimationRequest by)
```csharp
public override void OnInterrupted(AnimationRequest by)
```
- **Purpose**: 触发 `request.OnInterrupt` 回调，清理 `_currentRequest`

## 未来规划

| 计划 | 状态 | 来源 |
|------|------|------|
| Knockdown→GetUp 内联提交（消除 1 帧间隙） | P2 | session 2026-07-05 |
| 受击动画 FadeOut 过渡 | P2 | — |
