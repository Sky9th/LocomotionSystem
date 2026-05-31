# LocomotionModeProfile · 移动模式配置

> `Character/Animation/Config/LocomotionModeProfile.cs` — ScriptableObject，特定 posture+gait 的动画参数

## 调用链

```
被谁调:
  LocomotionAnimationProfile.GetTurnSpeed() → 遍历 modeProfiles 匹配
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | LocomotionAnimationProfile | 转身速度查询 |
| 被依赖 | AnimationBrain | 动画原生速度查询（乘积分母） |

## 公开属性

```csharp
public EPosture Posture => posture;                // 姿势
public EMovementGait Gait => gait;                  // 步态
public float AnimNativeSpeed => animNativeSpeed;     // 动画原生速度 (m/s)，Speed=1 时
public float MovingTurnSpeed => movingTurnSpeed;     // 移动中转身速度 (deg/s)
public float EnterAngle => enterAngle;               // 进入转向角度阈值
public float ExitAngle => exitAngle;                 // 退出转向角度阈值
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| EnterAngle/ExitAngle 当前未在流水线中使用 | 待做 | 代码字段存在未接入 |
