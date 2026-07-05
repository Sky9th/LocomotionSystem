# CharacterHeadLook · 头部注视计算 ⛔ STALE

> **Last Verified**: 2026-07-06 | **Verification**: STALE — `CharacterHeadLook.cs` deleted in v0.38.6. Head Look IK 延后。
>
> `Character/Kinematic/CharacterHeadLook.cs` — static 类（已删除），计算头部 yaw/pitch。保留本文档供将来 IK 实现参考。

## 调用链

```
被谁调:
  CharacterKinematic.Evaluate()
    → CharacterHeadLook.Evaluate(viewForward, modelRoot, rootTransform, profile)

调谁:
  Quaternion.LookRotation / Inverse / eulerAngles  ← Unity API
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterKinematic | 唯一调用者 |
| 依赖 | CharacterProfile | 读取 maxHeadYaw/Pitch/Degrees 限制 |

## 方法

### EvaluatePlanarHeading()
```csharp
internal static Vector3 EvaluatePlanarHeading(Vector3 viewForward, Transform rootTransform)
```
- **用途**: 将相机朝向投影到水平面，得到运动朝向
- **参数**: `viewForward` — 视线方向；`rootTransform` — 角色 Transform
- **返回**: 归一化平面朝向
- **调用者**: 当前仅保留，未在流水线中使用（heading 由 CharacterActor 直接计算）

### Evaluate()
```csharp
internal static Vector2 Evaluate(Vector3 viewForward, Transform modelRoot, Transform rootTransform, CharacterProfile profile)
```
- **用途**: 计算头部在身体坐标系下的 yaw/pitch，归一化到 [-1, 1]
- **参数**: `viewForward` — 视线方向；`modelRoot` — 模型根（取 bodyRotation）；`rootTransform` — 角色根（fallback）；`profile` — 配置（角度限制）
- **返回**: `Vector2(yaw, pitch)` 归一化值
- **调用者**: `CharacterKinematic.Evaluate()`
- **备注**: yaw 正 = 右转，pitch 正 = 抬头；profile 为 null 时使用最小值 1e-3 防止除零

### NormalizeAngle180()
```csharp
private static float NormalizeAngle180(float angle)
```
- **用途**: 将角度归一化到 [-180°, 180°]
- **调用者**: `Evaluate()`

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| headLookRotationSpeed 当前未被 AnimationBrain 使用 | 待做 | 代码字段存在但未接入 |
