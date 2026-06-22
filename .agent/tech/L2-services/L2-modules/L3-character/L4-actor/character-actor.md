# CharacterActor · 角色主控

> **Last Verified**: 2026-06-22 | **Verification**: All referenced files exist, signatures match code

> `Character/Actor/CharacterActor.cs` — ModuleHub，角色组合根，每帧流水线入口

## 调用链

```
被谁调:
  Unity 生命周期 → Awake / Start / Update / OnDisable

调谁:
  Awake:
    SetupModel() → Instantiate(modelPrefab) → AddComponent AnimationBrain
    ResolveComponents() → GetComponent 收集引用
    pre-assemble: new CharacterRig → new CharacterBuildContext → new 所有 C# 子模块(Registry)
    base.Awake() → ModuleHub.Awake → 扫描 MB 子节点 → Register → OnAssembleAll

  Start:
    base.Start() → Registry.OnWireAll (递归)
    agent.AddModifier (post-wire)

  Update:
    director.Evaluate() → characterKinematic.Evaluate() → locomotionSimulator.Simulate()
    → characterAnimation.Apply() → pathfindingAgent.SyncLocomotion()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 继承 | ModuleHub | 树形生命周期 |
| 持有 | CharacterBuildContext | 子模块统一依赖入口 |
| 构造 | PlayerDirector / NpcDirector | C# 子模块 (ModuleChild) |
| 构造 | CharacterKinematic | C# 子模块 (ModuleChild) |
| 构造 | GroundLocomotion | C# 子模块 (ModuleChild) |
| 构造 | CharacterCombat | C# 子模块 (ModuleChild) |
| 依赖 | AnimationBrain | Model 子节点，独立 ModuleHub |
| 依赖 | PathfindingAgent | MB 子模块 (ModuleChildMono) |
| 依赖 | CharacterAudio | MB 子模块 (ModuleChildMono) |

## 设计决策

| 决策 | 原因 |
|------|------|
| 继承 ModuleHub | 统一树形生命周期，pre/base/post 三段式 |
| BuildContext 统一依赖 | 消除构造参数各自为政，Model 替换自动更新 |
| AnimationBrain 独立为 ModuleHub | Drivers 是其子模块，生命周期独立于 CharacterActor |
| C# 子模块在 base.Awake 之前构造 | 构造自注册 → base.Awake 扫描 MB 子节点 → OnAssembleAll 包含全部子模块 |
| OnWire 只加 Modifier | 子模块 OnWire 由 base.Start 自动递归 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| ReplaceModel 完整实现 | TODO Phase 3 | 装备系统 |
| AI Director 替代 NpcDirector | TODO Phase 4 | Director/AI/ |
| AbilitySlotManager 替代 skillSlot1/2 | TODO | 技能树系统 |
| Prefab 移除根节点 LocomotionDriver/TraversalDriver | TODO | 已由 Brain 代码挂载 |
