# CharacterActor · 角色主控

> **Last Verified**: 2026-07-03 | **Verification**: Director 消除、字段重组、子模块暴露、InputState 新增、EvaluateCharacterIntent 删除

> `Character/Actor/CharacterActor.cs` — ModuleHub，角色组合根，每帧流水线入口

## Layer Position

L4 — Actor 层，L3_Character 的组合根。装配所有子模块，驱动每帧流水线。

## Call Chain

```
CharacterActor.Update()
  ├── characterKinematic.Evaluate(InputState, deltaTime)
  │     └── 内部: pathfinding → heading, InputState → aim
  ├── equipment.SyncEquipment()
  ├── locomotionSimulator.Simulate(ref frameCtx, InputState, buildCtx, deltaTime)
  │     ├── motor.Evaluate(kinematic, pathfinding, desiredSpeed, accel, dt)
  │     └── stance.Evaluate(motor, kinematic, InputState, gait, animSet, dt)
  ├── characterAnimation.Apply(in frameCtx)
  └── buildCtx.Properties.Tick(deltaTime)
```

## Coupled Modules

| 方向 | 模块 | 关系 |
|------|------|------|
| → | CharacterKinematic | 每帧调用 Evaluate |
| → | ILocomotionSimulator | 每帧调用 Simulate |
| → | CharacterEquipment | 每帧调用 SyncEquipment |
| → | AnimationBrain | 每帧调用 Apply |
| ← | EntityCommandModule | 通过 internal 属性调用子模块 |
| ← | PlayerService | 写入 InputState |

## Public Properties

- `IsPlayer` — 是否玩家角色
- CharacterAnimationProfile / CharacterAudioConfig — Config SO
- ForwardRootMotion / ApplyRootMotionRotation / AutoMatchAnimationSpeed — Animation flags
- UpperBodyMask ~ FootMask — Avatar masks

## Internal Properties

- `BuildContext` / `CharacterRig` / `LastKinematic` / `LastMotor` / `LastDiscrete` — Runtime state
- `Pathfinding` / `Ability` / `Container` — Module access (Command/Query 直接调用)
- `InputState` — 外部写入的连续输入（PlayerService/AIService）

## Methods

### Update()
每帧流水线入口。不再有 Director 或 Intent 评估——子模块各自从 InputState + pathfinding + camera 推算所需数据。

### ReplaceModel(GameObject)
TODO Phase 3: 完整模型替换（装备系统依赖）。

## Future Plans

| 计划 | 状态 | 来源 |
|------|------|------|
| BodyForm 由装备系统决定 | TODO 标记 | CharacterActor.Update() |
| 饥饿消耗测试代码移除 | TODO 标记 | CharacterActor.Start() |
| EvaluateCharacterIntent 已删除 | ✅ Done | v0.35.0 |
