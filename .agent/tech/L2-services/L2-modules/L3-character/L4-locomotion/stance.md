# Stance · 离散状态判定 + MotionSpeedScale

> **Last Verified**: 2026-07-05 | **Verification**: All referenced files exist, signatures match code
>
> `L3_Character/Locomotion/Ground/Stance.cs` — 纯 C# 类，Phase/Gait/Posture/Turning + MotionSpeedScale 评估
>
> v0.36.11: motionSpeedScale 从硬编码 1f 改为外部传入（由 GroundLocomotion 计算）。

## 调用链

```
被谁调:
  GroundLocomotion.Simulate() → stance.Evaluate(in motor, in kin, in input, gait, animSet, motionSpeedScale, dt)

调谁:
  EvaluatePhase / EvaluateTurning
  LocomotionAnimationSetSO.GetNativeSpeed()

输出:
  SCharacterDiscrete (motionSpeedScale 由外部注入，EffectiveMaxSpeed = nativeSpeed × motionSpeedScale)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | SCharacterMotor | 读取 ActualPlanarVelocity/TurnAngle |
| 依赖 | SCharacterKinematic | 读取 GroundContact |
| 依赖 | SCharacterIntent | 读取 DesiredGait/DesiredPosture |
| 依赖 | LocomotionProfile | 读取 gaitSpeed/turnEnterAngle/turnCompletionAngle |
| 依赖 | LocomotionAnimationProfile | 读取 animNativeSpeed（modeProfiles 匹配） |
| 输出 | SCharacterDiscrete | Phase/Posture/Gait/IsTurning/MotionSpeedScale/EffectiveMaxSpeed |

## 内部状态

```csharp
private bool isTurning;
private float cachedMotionSpeedScale = 1f;    // MotionSpeedScale 缓存
private EMovementGait cachedGait;              // 用于检测变化
private EPosture cachedPosture;                // 用于检测变化
```

## 方法

### Evaluate()
```csharp
internal SCharacterDiscrete Evaluate(
    in SCharacterMotor motor, in SCharacterKinematic kin,
    in SCharacterIntent intent, LocomotionProfile profile,
    LocomotionAnimationProfile animProfile, float dt)
```
- Gait/Posture 从 Intent 透传（不由 Stance 决策）
- **MotionSpeedScale 缓存**: 仅 gait/posture 变化时调用 ComputeBaseSpeedScale
- 计算 `EffectiveMaxSpeed = profile.GetSpeedForGait(gait) * motionSpeedScale`

### ComputeBaseSpeedScale()
```csharp
private static float ComputeBaseSpeedScale(
    EMovementGait gait, EPosture posture,
    LocomotionProfile profile, LocomotionAnimationProfile animProfile)
```
- 遍历 `animProfile.modeProfiles` 匹配 posture+gait → 取 `AnimNativeSpeed`
- 返回 `gaitSpeed / animNativeSpeed`，匹配不到或异常时返回 1f
- **仅在 gait 或 posture 变化时调用**（Evaluate 内部缓存控制）
