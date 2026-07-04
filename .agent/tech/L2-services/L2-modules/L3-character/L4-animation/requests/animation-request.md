# AnimationRequest · 动画请求数据包

> `Character/Animation/Requests/AnimationRequest.cs` — class，Driver 提交给 Arbiter 的动画播放请求

## 调用链

```
创建者:
  TraversalDriver.Evaluate() → new AnimationRequest { ... }

消费者:
  DriverArbiter.AcceptRequest() → 读取 Clip/Alias/FadeIn → layer.Play
  DriverArbiter.CheckCompletion → 读取 OnComplete → Resume/Stay
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | DriverArbiter | 裁决和播放 |
| 被依赖 | TraversalDriver | 请求提交 |
| 被依赖 | AbilityDriver | 读取 CustomData、调回调 |
| 被依赖 | AnimationBrain | 按 DriverType 路由 |
| 依赖 | AnimationClip | 直接播放 Clip |

## 公开属性

```csharp
// 播放参数
public AnimationClip Clip;                        // 直接播放的 Clip
public float FadeIn;                               // 淡入时间
public float FadeOut;                              // 淡出时间

// 仲裁协商
public int Tags;                                   // 标签位 (预留)
public int Resistance;                             // 优先级 (数字越大越优先)

// 完成/中断行为
public OnCompleteBehavior OnComplete;              // 播完后的行为
public OnInterruptedBehavior OnInterrupted;        // 被中断后的行为

// 通道 + 路由
public int ChannelMask;                            // 目标动画层通道
public EDriverType DriverType;                     // Brain 据此路由请求到对应 Driver

// 载荷 + 回调
public object CustomData;                          // 调用方附加数据（如 AbilityActivationSO）
public System.Action OnMarker;                     // 内部标记事件（如激发帧）
public System.Action OnCompleted;                  // 正常播完回调
public System.Action OnInterrupt;                  // 被中断回调

public bool HasClip => Clip != null;
```

### EDriverType

```csharp
public enum EDriverType { Ability, Traversal }
```

Brain 的 `SubmitRequest(request)` 重载根据此字段解析对应 Driver，调用方无需持有 Driver 引用。

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Tags 用于过滤 HeadLook（战斗/反应时关闭） | 待做 | 旧 animation-design.md |
| OnMarker/OnCompleted/OnInterrupt 回调 | ✅ Done 2026-07-04 | ability-driver-callback-refactor |
| Tags 用于过滤 HeadLook（战斗/反应时关闭） | 待做 | 旧 animation-design.md |
