# ILocomotionSimulator · 仿真接口

> `Character/Locomotion/ILocomotionSimulator.cs` — interface，移动仿真可替换入口
>
> **Last Verified**: 2026-06-20 | **Verification**: All referenced files exist, signatures match code

## 调用链

```
被谁调:
  CharacterActor.Update() → locomotionSimulator.Simulate(ref frameCtx, intent, buildCtx, dt)

实现者:
  GroundLocomotion
```

## 接口定义

```csharp
internal interface ILocomotionSimulator
{
    void Simulate(ref CharacterFrameContext frameCtx, in SCharacterIntent intent,
        CharacterBuildContext buildCtx, float dt);
}
```

- v0.20.2: 删 `LocomotionAnimationSetSO animSet` 参数，改为从 `buildCtx.ResolvedLocoAnimSet` 读

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterActor | 持引用并每帧调用 |
| 实现 | GroundLocomotion | 唯一实现 |
| 参数 | SCharacterIntent | 意图输入 |
| 参数 | CharacterBuildContext | 配置 + 运行时上下文（含 ResolvedLocoAnimSet） |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 飞行/游泳仿真模型 | 远期 | 接口设计预留 |
| NPC 专用仿真模型 | 远期 | 接口设计预留 |
