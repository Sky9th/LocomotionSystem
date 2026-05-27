# CharacterActor · 角色主控

> `Character/Components/CharacterActor.cs` — MonoBehaviour，角色组合根，每帧 Evaluate() 入口

## 调用链

```
被谁调:
  Unity 生命周期 → Awake/OnEnable/Update/OnDisable

调谁:
  Awake:
    ├── new CharacterRig(transform, modelRoot)
    ├── characterAnimation?.SetRig(rig)
    ├── new CharacterEventReceiver(this)          → Input 模块
    ├── new CharacterKinematic(transform, transform, rig)  → Kinematic 模块
    ├── new GroundLocomotion()                    → Locomotion 模块
    └── new CharacterStats(statsTree)             → Stats 模块

  Update:
    ├── inputModule.ReadActions(out ctx.Input)    → 读取输入
    ├── inputModule.ReadMouseGroundPosition()     → 读取鼠标地面坐标
    ├── characterKinematic.Evaluate()             → 运动学计算 → ctx.Kinematic
    ├── locomotionSimulator.Simulate(ref ctx)     → 移动仿真 → ctx.Motor/Discrete
    ├── stats?.Update(ctx, dt)                    → 数值更新
    └── characterAnimation?.Apply(in ctx)         → 动画应用

  OnDisable:
    ├── inputModule?.Unsubscribe/Reset
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
| 依赖 | CharacterEventReceiver | 输入桥接，读取输入和相机 |
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
- **备注**: 按顺序创建 Rig → Animation 注入 Rig → Input → Kinematic → Locomotion → Stats

### DumpStatsTree()
```csharp
private void DumpStatsTree()
```
- **用途**: 调试用，在 Awake 中将 StatsTree 展开写到 Log
- **调用者**: Awake 末尾
- **备注**: statsTree 为 null 时只打一行日志

### OnEnable()
```csharp
private void OnEnable()
```
- **用途**: 订阅输入事件
- **调用者**: Unity 生命周期

### OnDisable()
```csharp
private void OnDisable()
```
- **用途**: 取消输入订阅，重置 Kinematic 状态
- **调用者**: Unity 生命周期

### Update()
```csharp
private void Update()
```
- **用途**: 每帧评估流水线 — Input → Kinematic → Locomotion → Stats → Animation
- **调用者**: Unity 生命周期
- **备注**: deltaTime <= 0 时跳过整帧；heading 由鼠标地面坐标或 transform.forward 决定

## 内部机制

### Update() 帧流水线

```
1. new CharacterFrameContext()
2. inputModule.ReadActions(out ctx.Input)          ← 读取输入动作聚合
3. 计算 heading:
   - 有鼠标地面位置 → 从角色指向鼠标方向 (y=0)
   - 无鼠标地面 → transform.forward
4. ctx.Kinematic = characterKinematic.Evaluate()   ← 运动学评估 (地面+障碍+朝向)
5. locomotionSimulator.Simulate(ref ctx)            ← 移动仿真 (Motor+Stance)
6. 缓存 LastKinematic/LastMotor/LastDiscrete
7. stats?.Update(ctx, dt)                          ← 数值规则 Tick
8. 构建 LastStats 字典
9. characterAnimation?.Apply(in ctx)                ← 动画驱动
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 输入 WASD 移动禁用 — Phase 4 A* Pathfinding 将驱动移动 | 待做 | 代码 TODO |
| 非玩家角色 (NPC) 支持 | 远期 | 旧 module-analysis.md |
| 事件回调桥接 — 外部系统通过 Dispatcher 通知 CharacterActor | 待做 | 旧 stats-rule-system.md |
| stat 规则数量增长后考虑 Rule 配置化 | 远期 | 代码 TODO |
