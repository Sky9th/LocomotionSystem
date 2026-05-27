# LocomotionProfile · 移动参数配置

> `Character/Locomotion/Config/LocomotionProfile.cs` — ScriptableObject，移动速度/加速度/转向参数

## 调用链

```
被谁调:
  CharacterActor.Awake()          → 序列化引用
  CharacterActor.Update()         → 传递给 GroundLocomotion
  GroundLocomotion.Simulate()     → 传递给 Motor/Stance
  Motor.Evaluate()                → 读取 moveSpeed/acceleration
  Stance.Evaluate()               → 读取 turnEnterAngle/turnCompletionAngle/canSprint/canCrouch/canProne
  BaseLayer.FSM States            → 通过 LocomotionDriver 间接读取
  LocomotionDriver                → 序列化引用并传给 BaseLayer
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | Motor | moveSpeed/acceleration |
| 被依赖 | Stance | turnEnterAngle/turnCompletionAngle + 能力开关 |
| 被依赖 | BaseLayer | 通过 LocomotionDriver 读取用于 Mixer 参数 |

## 公开属性

```csharp
[Header("Motion")]
[Min(0)] public float moveSpeed = 4f;                  // 最大移动速度
[Min(0)] public float acceleration = 5f;                // 加速度 (m/s^2)

[Header("Abilities")]
public bool canSprint = true;                            // 可冲刺
public bool canCrouch = true;                            // 可蹲伏
public bool canProne = true;                             // 可趴下

[Header("Turning")]
[Range(0, 180)] public float turnEnterAngle = 65f;      // 进入转向阈值
[Range(0, 25)] public float turnCompletionAngle = 5f;    // 退出转向阈值
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Gait 独立速度/加速度（走/跑/冲刺不同值） | 待做 | 代码 TODO |
