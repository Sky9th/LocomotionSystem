# DriverArbiter · Driver 仲裁器

> **Last Verified**: 2026-07-05 | **Verification**: All referenced files exist, signatures match code

> `Character/Animation/DriverArbiter.cs` — 纯 C# 类，动画驱动优先级仲裁 + 请求队列调度。v0.36.10: ProcessQueue 改为 DriverType 硬编码抢占规则。

## 调用链

```
被谁调:
  AnimationBrain.Apply()
    → fullBodyArbiter.Resolve(ctx, dt)   ← 每帧调度入口

调谁:
  driver.Evaluate()       → 所有 Driver 评估
  driver.Drive()          → Active Driver 驱动
  driver.OnStarted()      → 请求被接受时
  driver.OnCompleted()    → 动画完成时
  driver.OnInterrupted()  → 被更高优先级请求打断时
  driver.OnResumed()      → 恢复为默认驱动时
  layer.Play()            → Animancer 播放动画
  layer.TryPlay()         → Animancer 尝试播放
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | AnimationBrain | 唯一持有者 |
| 依赖 | ICharacterAnimationDriver | 驱动接口 |
| 依赖 | AnimancerLayer | 动画播放目标层 |
| 依赖 | CharacterFrameContext | 帧上下文传递 |

## 公开属性

```csharp
public AnimationRequest ActiveRequest => activeRequest;  // 当前活跃请求
```

## 方法

### DriverArbiter()
```csharp
internal DriverArbiter(AnimancerLayer layer)
```
- **用途**: 构造，指定仲裁的 Animancer 层
- **调用者**: `AnimationBrain.Awake()`

### RegisterDriver()
```csharp
public void RegisterDriver(ICharacterAnimationDriver driver)
```
- **用途**: 注册 Driver，第一个注册的自动成为默认 Driver
- **调用者**: `BaseCharacterAnimationDriver.OnEnable()`

### UnregisterDriver()
```csharp
public void UnregisterDriver(ICharacterAnimationDriver driver)
```
- **用途**: 注销 Driver，如果为当前 active 则清空，如果为默认则顺位下一个
- **调用者**: `BaseCharacterAnimationDriver.OnDisable()`

### SubmitRequest()
```csharp
public void SubmitRequest(ICharacterAnimationDriver driver, AnimationRequest request)
```
- **用途**: Driver 主动提交动画请求（排队，同 Driver 覆盖）
- **调用者**: Driver 在 Evaluate() 中提交
- **备注**: 同 Driver 重复提交 → 最后一次覆盖

### ReleaseActive()
```csharp
public void ReleaseActive()
```
- **用途**: 释放当前活跃 Driver，归还默认 LocomotionDriver
- **调用者**: `AnimationBrain.Release()`
- **备注**: 替代旧 `Release(ICharacterAnimationDriver driver)`（2026-07-04 移除，同一时间只有一个活跃 Driver）

### Resolve()
```csharp
public void Resolve(in CharacterFrameContext ctx, float dt)
```
- **用途**: 每帧调度入口 — EvaluateDrivers → ProcessQueue → CheckCompletion → Drive → ActivateDefaultIfNeeded
- **调用者**: `AnimationBrain.Apply()`

### EvaluateDrivers()
```csharp
private void EvaluateDrivers(in CharacterFrameContext ctx, float dt)
```
- **用途**: 遍历所有 Driver 执行 Evaluate（让 Driver 检查条件并提交请求）
- **调用者**: Resolve()

### ProcessQueue()
```csharp
private void ProcessQueue()
```
- **用途**: DriverType 硬编码仲裁 → Accept/Reject
- **规则**: H1 idle→接受任意；H2 HitReaction→抢占一切（含互打断）；else→拒绝（Traversal↔Ability 互斥）。Resistance 字段保留但不消费。
- **调用者**: Resolve()

### AcceptRequest()
```csharp
private void AcceptRequest(ICharacterAnimationDriver driver, AnimationRequest request)
```
- **用途**: 接受新请求 — 中断旧 Driver → 设置新 Active → 播放动画
- **调用者**: ProcessQueue

### CheckCompletion()
```csharp
private void CheckCompletion()
```
- **用途**: 检查当前动画是否播放完成 (NormalizedTime >= 0.99)
- **调用者**: Resolve()
- **备注**: OnCompleteBehavior.Resume → 恢复默认 Driver；Stay → 保持当前

### ActivateDefaultIfNeeded()
```csharp
private void ActivateDefaultIfNeeded()
```
- **用途**: 无 ActiveRequest 且当前 Driver != 默认时，恢复默认 Driver
- **调用者**: Resolve()

## 内部机制

### 仲裁流程
```
Resolve(ctx, dt):
  1. EvaluateDrivers: 遍历 drivers，调用 driver.Evaluate()
     → Driver 在此阶段检测条件并 SubmitRequest
  2. ProcessQueue:
     → 取 queue[0]（FIFO，不排序）
     → 硬编码 DriverType 抢占规则:
       - H1: activeRequest==null → Accept（任意 Driver）
       - H2: request.DriverType==HitReaction → Interrupt active → Accept（抢占一切）
       - else: Reject（Traversal↔Ability 互斥）
  3. CheckCompletion:
     → NormalizedTime >= 0.99:
       - Resume → activeRequest=null, 恢复默认
       - Stay → activeCompleted=true
  4. activeDriver?.Drive(ctx, dt)
  5. ActivateDefaultIfNeeded: activeRequest==null && driver!=default → 恢复
```

### 请求裁决规则
```
排序: Resistance 高的优先
裁决: 新请求 >= 当前活跃 Resistance → 中断并接受
默认 Driver: 第一个注册的，无请求时自动激活
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| ChannelMask 分层仲裁（FullBody vs UpperBody） | 待做 | 旧 animation-design.md |
| 请求 Tags 过滤 HeadLook | 待做 | 旧 animation-design.md |
