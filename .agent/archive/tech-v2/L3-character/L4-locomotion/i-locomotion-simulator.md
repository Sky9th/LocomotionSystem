# ILocomotionSimulator · 仿真接口

> `Character/Locomotion/ILocomotionSimulator.cs` — interface，移动仿真可替换入口

## 调用链

```
被谁调:
  CharacterActor.Update() → locomotionSimulator.Simulate(ref ctx, profile, dt)

实现者:
  GroundLocomotion
```

## 接口定义

```csharp
internal interface ILocomotionSimulator
{
    void Simulate(ref CharacterFrameContext ctx, LocomotionProfile profile, float dt);
}
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterActor | 持引用并每帧调用 |
| 实现 | GroundLocomotion | 唯一实现 |
| 参数 | LocomotionProfile | 配置注入 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 飞行/游泳仿真模型 | 远期 | 接口设计预留 |
| NPC 专用仿真模型（不同移动行为） | 远期 | 接口设计预留 |
