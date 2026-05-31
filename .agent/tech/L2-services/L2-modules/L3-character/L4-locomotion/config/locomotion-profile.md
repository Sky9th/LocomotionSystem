# LocomotionProfile · 角色期望移速配置

> `Character/Locomotion/LocomotionProfile.cs` — ScriptableObject，按 gait 的角色期望移速 + 加速度 + 转向参数

## 调用链

```
被谁调:
  CharacterActor.Awake()          → 序列化引用
  CharacterActor.Update()         → 传递给 GroundLocomotion
  GroundLocomotion.Simulate()     → 传递给 Motor/Stance
  Motor.Evaluate()                → 读取 GetSpeedForGait() / acceleration
  Stance.Evaluate()               → 读取 turnEnterAngle/turnCompletionAngle
  PathfindingAgent                → 同步 ai.maxSpeed 用 GetSpeedForGait()
  AnimationBrain                  → 乘积分子 GetSpeedForGait()
  BaseLayer.FSM States            → 通过 LocomotionDriver 间接读取
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | Motor | GetSpeedForGait() / acceleration |
| 被依赖 | Stance | turnEnterAngle / turnCompletionAngle |
| 被依赖 | PathfindingAgent | ai.maxSpeed 同步 |
| 被依赖 | AnimationBrain | SpeedMultiplier 分子 |
| 被依赖 | BaseMovingState | Mixer 参数归一化分母 |

## 公开属性

### 角色期望移速 (m/s)
按角色类型配置（强化人 10m/s，老人 2m/s）。与 LocomotionModeProfile.animNativeSpeed（动画原生速度）不同。

```csharp
[Min(0)] public float walkSpeed = 2f;      // Walk 期望移速
[Min(0)] public float runSpeed = 5f;       // Run 期望移速
[Min(0)] public float sprintSpeed = 8f;    // Sprint 期望移速
[Min(0)] public float crawlSpeed = 1f;     // Crawl 期望移速
[Min(0)] public float acceleration = 5f;   // 加速度 (m/s²)
```

### 方法

### GetSpeedForGait()
```csharp
public float GetSpeedForGait(EMovementGait gait)
```
- **用途**: 返回指定步态对应的角色期望移速
- **参数**: `gait` — 步态枚举
- **返回**: 该步态的最大移速 (m/s)，Idle/未匹配时返回 runSpeed

### 能力开关

```csharp
public bool canSprint = true;
public bool canCrouch = true;
public bool canProne = true;
```

### 转向参数

```csharp
[Range(0, 180)] public float turnEnterAngle = 65f;
[Range(0, 25)] public float turnCompletionAngle = 5f;
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| L4_Locomotion 目录结构重新梳理 | TODO | 用户反馈 |
