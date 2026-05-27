# ICharacterAnimationDriver · 动画驱动接口

> `Character/Animation/Drivers/ICharacterAnimationDriver.cs` — interface，所有动画驱动必须实现

## 调用链

```
被谁调:
  DriverArbiter:
    → Evaluate()     — 每帧评估条件
    → Drive()        — 激活时驱动动画
    → OnStarted()    — 请求被接受时
    → OnCompleted()  — 动画播放完成时
    → OnInterrupted(by) — 被更高优先级请求打断时
    → OnResumed()    — 恢复为 Active 时

实现者:
  BaseCharacterAnimationDriver (抽象基类)
  → LocomotionDriver
  → TraversalDriver
```

## 接口定义

```csharp
internal interface ICharacterAnimationDriver
{
    int ChannelMask { get; }                                              // 占用的通道
    void Evaluate(in CharacterFrameContext ctx, float dt);                // 每帧评估（提交流程）
    void Drive(in CharacterFrameContext ctx, float dt);                   // 激活时驱动动画
    void OnStarted();                                                      // 请求被接受
    void OnCompleted();                                                    // 动画完成
    void OnInterrupted(AnimationRequest by);                               // 被中断
    void OnResumed();                                                      // 恢复激活
}
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | DriverArbiter | 调度生命周期 |
| 实现 | BaseCharacterAnimationDriver | 抽象基类 |
| 参数 | CharacterFrameContext | 帧上下文 |
| 参数 | AnimationRequest | 中断参数 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 额外生命周期回调（如 OnPaused/OnCancelled） | 远期 | 旧 animation-design.md |
