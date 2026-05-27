# CharacterGroundDetection · 地面检测

> `Character/Kinematic/CharacterGroundDetection.cs` — static 类，SphereCast 向下探测地面

## 调用链

```
被谁调:
  CharacterKinematic.EvaluateStableGroundContact()
    → CharacterGroundDetection.EvaluateGroundContact()

调谁:
  Physics.SphereCast()    ← Unity Physics API
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterKinematic | 唯一调用者 |
| 输出 | SGroundContact | struct 输出 (IsGrounded/Distance/Normal) |

## 方法

### EvaluateGroundContact()
```csharp
internal static SGroundContact EvaluateGroundContact(
    Vector3 position, float probeHeight, float probeRadius,
    int layerMask, float maxSlopeAngleDegrees)
```
- **用途**: SphereCast 向下探测，返回地面接触信息
- **参数**: `position` — 角色位置；`probeHeight` — 探针起点高度；`probeRadius` — 球体半径；`layerMask` — 检测层；`maxSlopeAngleDegrees` — 最大可走斜坡角
- **返回**: SGroundContact（无命中时返回 None）
- **调用者**: `CharacterKinematic.EvaluateStableGroundContact()`
- **备注**: 使用 `QueryTriggerInteraction.Ignore` 忽略 Trigger

### IsWalkableSlope()
```csharp
internal static bool IsWalkableSlope(Vector3 surfaceNormal, float maxSlopeAngleDegrees)
```
- **用途**: 判断表面法线是否在可走斜坡角度以内
- **参数**: `surfaceNormal` — 表面法线；`maxSlopeAngleDegrees` — 最大角度
- **返回**: true = 可走斜坡
- **调用者**: `EvaluateGroundContact()`

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 添加 CapsuleCast 支持（更精确的碰撞体检测） | 待做 | 旧 module-analysis.md |
