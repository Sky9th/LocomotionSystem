# CharacterFrameContext · 帧内数据载体

> `Character/Components/CharacterFrameContext.cs` — struct，Actor 每帧填充的帧数据总线

## 调用链

```
创建者:
  CharacterActor.Update()
    → new CharacterFrameContext()
    → 填充 .Input → .Kinematic → .Motor → .Discrete

消费者:
  AnimationBrain.Apply(in ctx)     → 驱动动画
  CharacterStats.Update(ctx, dt)  → 数值规则 Tick
  GroundLocomotion.Simulate(ref ctx) → Motor/Discrete 写入
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | AnimationBrain | Apply() 接收 ctx 读取全部字段 |
| 被依赖 | CharacterStats | Update() 接收 ctx 读取 Discrete/Input |
| 被依赖 | GroundLocomotion | Simulate() 接受 ref ctx 写入 Motor/Discrete |
| 聚合 | SCharacterInputActions | Input 字段 |
| 聚合 | SCharacterKinematic | Kinematic 字段 |
| 聚合 | SCharacterMotor | Motor 字段 |
| 聚合 | SCharacterDiscrete | Discrete 字段 |

## 公开属性

```csharp
public SCharacterInputActions Input;      // 当前帧输入动作聚合
public SCharacterKinematic Kinematic;     // 运动学评估结果
public SCharacterMotor Motor;             // 运动仿真结果 (速度/转角)
public SCharacterDiscrete Discrete;       // 离散状态 (Phase/Gait/Posture)
```

## 使用规则

- Actor 每帧创建新实例，逐步骤填充字段
- GroundLocomotion 通过 `ref` 写入 Motor 和 Discrete
- AnimationBrain 通过 `in` 只读消费
- 不跨帧缓存 — 每帧新建

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 扩展包含 Ability 上下文 | 远期 | 旧 animation-design.md |
