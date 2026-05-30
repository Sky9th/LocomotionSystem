# CharacterActor · 角色主控

> `Character/Actor/CharacterActor.cs` — MonoBehaviour，角色组合根，每帧流水线入口

## 调用链

```
被谁调:
  Unity 生命周期 → Awake/OnEnable/Update/OnDisable

调谁:
  Awake:
    ├── GetComponentInChildren<AnimationBrain>()  → 先获取动画组件
    ├── new CharacterRig(transform, animation?.transform)  → modelRoot = animation 的 transform
    ├── characterAnimation?.SetRig(rig)           → 注入 Rig 到 Animation
    ├── new PlayerDirector(this)                  → Director (L4_Director)
    ├── new CharacterKinematic(transform, transform, rig)  → Kinematic 模块
    ├── new GroundLocomotion()                    → Locomotion 模块
    ├── new CharacterStats(statsTree)             → Stats 模块
    └── DumpStatsTree()                           → 调试输出

  Update:
    ├── new CharacterFrameContext()               → 创建帧上下文
    ├── director.Evaluate() → SCharacterIntent    → 取得意图
    ├── ctx.Intent = intent                        → 写入帧上下文
    ├── characterKinematic.Evaluate(profile, locomotionHeading, aimDirection, dt)  → ctx.Kinematic
    ├── locomotionSimulator.Simulate(ref ctx, intent, locomotionProfile, dt)  → ctx.Motor/Discrete
    ├── PlanarSpeed + LastKinematic/LastMotor/LastDiscrete 缓存
    ├── stats?.Update(ctx, dt)                    → 数值更新 + 构建 LastStats
    └── characterAnimation?.Apply(in ctx)         → 动画应用

  OnDisable:
    ├── director (PlayerDirector)?.Unsubscribe/Reset
    └── characterKinematic?.Reset
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | CharacterRig | 构造注入，物理实体写入入口 |
| 依赖 | CharacterKinematic | Evaluate() 每帧计算运动学 |
| 依赖 | ILocomotionSimulator | Simulate() 每帧仿真移动 |
| 依赖 | AnimationBrain | Apply() 消费帧上下文驱动动画 |
| 依赖 | CharacterStats | Update() 每帧更新数值 |
| 依赖 | ICharacterDirector (L4_Director) | Evaluate() 产出 SCharacterIntent |
| 依赖 | CharacterProfile | SO 配置 — 地面探针/障碍/HeadLook 参数 |
| 依赖 | LocomotionProfile | SO 配置 — 速度/加速度/转向阈值 |
| 依赖 | StatsTreeSO | Stats 树根节点，运行时实例化 |
| 被消费 | AnimationBrain | 通过 Apply(ctx) 传递帧上下文 |

## 公开属性

```csharp
public bool IsPlayer { get; }                          // 是否为玩家角色
public Dictionary<string, (float current, float max)> LastStats { get; }  // 最新数值快照
internal float PlanarSpeed { get; private set; }       // 当前平面速度
internal SCharacterKinematic LastKinematic { get; private set; }  // 上一帧运动学输出
internal SCharacterMotor LastMotor { get; private set; }          // 上一帧运动输出
internal SCharacterDiscrete LastDiscrete { get; private set; }    // 上一帧离散状态
```

## 方法

### Awake()
```csharp
private void Awake()
```
- **用途**: 构造所有子模块实例，建立引用链
- **调用者**: Unity 生命周期
- **备注**: Animation → Rig → Director → Kinematic → Locomotion → Stats → DumpStatsTree

### Update()
```csharp
private void Update()
```
- **用途**: 每帧评估流水线 — Director → Kinematic → Locomotion → Stats → Animation
- **调用者**: Unity 生命周期
- **备注**: deltaTime <= 0 时跳过整帧；意图由 ICharacterDirector.Evaluate() 统一产出

### OnEnable() / OnDisable()
- OnEnable: PlayerDirector.Subscribe() 订阅输入事件
- OnDisable: PlayerDirector.Unsubscribe/Reset + Kinematic.Reset

## 内部机制

### Update() 帧流水线

```
1. new CharacterFrameContext()
2. var intent = director.Evaluate()                 ← SCharacterIntent (L4_Director)
3. ctx.Intent = intent
4. ctx.Kinematic = characterKinematic.Evaluate(profile, intent.LocomotionHeading, intent.AimDirection, dt)
5. locomotionSimulator.Simulate(ref ctx, intent, locomotionProfile, dt)
6. PlanarSpeed = Motor.ActualPlanarVelocity.magnitude
   缓存 LastKinematic/LastMotor/LastDiscrete
7. stats?.Update(ctx, dt)                          ← 数值规则 Tick
8. characterAnimation?.Apply(in ctx)                ← 动画驱动
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 输入 WASD 移动禁用 — Phase 4 A* Pathfinding 将驱动移动 | 待做 | 代码 TODO |
| 非玩家角色 (NPC) 支持 — AICharacterDirector | 远期 | L4_Director/AI/ 占位 |
| stat 规则数量增长后考虑 Rule 配置化 | 远期 | 代码 TODO |
