# CharacterObstacleDetection · 障碍检测

> `Character/Kinematic/CharacterObstacleDetection.cs` — static 类，前方 Raycast + 高度探针

## 调用链

```
被谁调:
  CharacterKinematic.Evaluate()
    → CharacterObstacleDetection.TryDetectForwardObstacle(...)

调谁:
  Physics.Raycast()    ← Unity 射线检测
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterKinematic | 唯一调用者 |
| 输出 | SForwardObstacleDetection | struct 输出 (19 个字段) |

## 方法

### TryDetectForwardObstacle()
```csharp
internal static bool TryDetectForwardObstacle(
    Vector3 actorPosition, Vector3 forward, float probeVerticalOffset,
    float probeDistance, int layerMask, float minClimbHeight, float maxClimbHeight,
    float maxSlopeAngleDegrees, out SForwardObstacleDetection result)
```
- **用途**: 检测前方障碍物，判断类型（斜坡/障碍）和攀爬可行性
- **参数**: 
  - `actorPosition` — 角色世界位置
  - `forward` — 探测方向
  - `probeVerticalOffset` — 探测起点垂直偏移
  - `probeDistance` — 探测距离
  - `layerMask` — 检测层
  - `minClimbHeight` — 最小可攀爬高度
  - `maxClimbHeight` — 最大可攀爬高度
  - `maxSlopeAngleDegrees` — 斜坡角度阈值
  - `result` — 输出检测结果
- **返回**: 是否有命中
- **调用者**: `CharacterKinematic.Evaluate()`
- **备注**: 两步检测 — 前方射线 → 如果是障碍则顶部探针（从 maxClimbHeight*2 向下检测）

### 检测逻辑

```
1. 前方 Raycast:
   - 起点 = actorPosition + up * probeVerticalOffset
   - 方向 = forward
   - 距离 = probeDistance
   - 无命中 → 返回 false

2. 判断表面:
   - surfaceAngle = Angle(hit.normal, up)
   - isSlope = surfaceAngle <= maxSlopeAngle
   - isObstacle = !isSlope

3. 障碍高度探针:
   - 起点 = hitPoint + forward*0.05, y = actor.y + maxClimbHeight*2
   - 向下 Raycast maxClimbHeight*3
   - 有命中 → hasTopSurface, topPoint, topNormal

4. 结果计算:
   - obstacleHeight = hasTopSurface ? topPoint.y - actor.y : infinity
   - canClimb = isObstacle && hasTopSurface && minClimb <= height <= maxClimb
   - canVault/canStepOver 当前恒为 false（预留）
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| canVault/canStepOver 判定实现 | 待做 | 代码预留 |
