# LocomotionAnimationProfile · 移动动画参数配置

> `Character/Animation/Config/LocomotionAnimationProfile.cs` — ScriptableObject，动画阈值/转身速度/落地分级

## 调用链

```
被谁调:
  LocomotionDriver.OnEnable()   → 传给 BaseLayer
  AnimationBrain.UpdateHeadLook() → 读取 headLookSmoothingSpeed
  BaseLayer.ApplyTurnStepRotation() → GetTurnSpeed()
  BaseAirLoop/AirLand → 读取落地分级参数
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | AnimationBrain | headLookSmoothingSpeed |
| 被依赖 | BaseLayer | 转身速度/落地参数 |
| 被依赖 | BaseAirLoop/AirLand | 落地距离阈值 |
| 依赖 | LocomotionModeProfile[] | 各模式独立转身速度 |

## 公开属性

```csharp
[Header("Head Look")]
[Min(0)] public float headLookSmoothingSpeed = 540f;   // 头部注视平滑速度 (deg/s)

[Header("Turn Speeds By Mode")]
public LocomotionModeProfile[] modeProfiles;              // 各模式转身速度配置
[Min(0)] public float defaultInPlaceTurnSpeed = 100f;    // 原地转身默认速度
[Min(0)] public float defaultMovingTurnSpeed = 360f;     // 移动中转身默认速度

[Header("Airborne")]
public float landDistanceThreshold = 0.5f;                 // 落地判定距离阈值

[Header("Landing Levels")]
public float landMinFallDistance = 0.2f;                   // 最小触发落地距离
public float landLightMaxFallDistance = 1.0f;             // 轻落地最大距离
public float landMediumMaxFallDistance = 3.0f;            // 中落地最大距离
public float landLightTriggerDistance = 0.3f;             // 轻落地触发距离
public float landMediumTriggerDistance = 0.6f;            // 中落地触发距离
public float landHardTriggerDistance = 1.0f;               // 重落地触发距离
```

## 方法

### GetTurnSpeed()
```csharp
public float GetTurnSpeed(EPosture posture, EMovementGait gait, bool isMoving)
```
- **用途**: 获取指定姿势+步态的转身速度
- **参数**: `posture` — 姿势；`gait` — 步态；`isMoving` — 是否移动中
- **返回**: 转身速度 (deg/s)
- **调用者**: `BaseLayer.ApplyTurnStepRotation()`
- **备注**: 非移动时使用 defaultInPlaceTurnSpeed；移动时遍历 modeProfiles 匹配 posture+gait

## 未来规划

无。
