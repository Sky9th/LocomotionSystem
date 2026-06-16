# CharacterActor · 角色主控

> `Character/Actor/CharacterActor.cs` — ModuleBehaviour，角色组合根，每帧流水线入口

## 调用链

```
被谁调:
  Unity 生命周期 → Awake / Update / OnDisable
  ModuleBehaviour  → OnAssemble / OnWire (自动)

调谁:
  Awake:
    SetupModel() → Instantiate(modelPrefab) → AddComponent AnimationBrain
    ResolveComponents() → GetComponent 收集引用
    base.Awake() → ModuleBehaviour.Awake → OnAssemble + OnAssembleAll

  OnAssemble:
    new CharacterRig → new CharacterBuildContext → new 所有 C# 子模块(Registry)
    Registry.OnAssembleAll (自动)

  OnWire (base):
    Registry.OnWireAll (自动)
    agent.AddModifier

  Update:
    director.Evaluate() → characterKinematic.Evaluate() → locomotionSimulator.Simulate()
    → characterAnimation.Apply() → pathfindingAgent.SyncLocomotion()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 继承 | ModuleBehaviour | 树形生命周期 |
| 持有 | CharacterBuildContext | 子模块统一依赖入口 |
| 构造 | PlayerDirector / NpcDirector | C# 子模块 (Module) |
| 构造 | CharacterKinematic | C# 子模块 (Module) |
| 构造 | GroundLocomotion | C# 子模块 (Module) |
| 构造 | CharacterCombat | C# 子模块 (Module) |
| 依赖 | AnimationBrain | Model 子节点，独立 ModuleBehaviour |
| 依赖 | PathfindingAgent | MB 子模块 (ModuleComponent) |
| 依赖 | CharacterAudio | MB 子模块 (ModuleComponent) |

## 设计决策

| 决策 | 原因 |
|------|------|
| 继承 ModuleBehaviour | 统一树形生命周期，零手动样板 |
| BuildContext 统一依赖 | 消除构造参数各自为政，Model 替换自动更新 |
| AnimationBrain 独立为 ModuleBehaviour | Drivers 是其子模块，不再挂 CharacterActor |
| OnAssemble 只创建 C# 子模块 | MB 子模块由 ModuleBehaviour.Awake 自动发现 |
| OnWire 只加 Modifier | 子模块 OnWire 由 base.OnWire 自动递归 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| ReplaceModel 完整实现 | TODO Phase 3 | 装备系统 |
| AI Director 替代 NpcDirector | TODO Phase 4 | Director/AI/ |
| AbilitySlotManager 替代 skillSlot1/2 | TODO | 技能树系统 |
| Prefab 移除根节点 LocomotionDriver/TraversalDriver | TODO | 已由 Brain 代码挂载 |
