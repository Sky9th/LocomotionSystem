# CharacterPhysique

> L4 Kinematic — 角色物理属性运行时缓存 struct。

## 职责

Init 时从 `PropertyAgent` 一次性读取 9 个 Float 属性填充强类型字段，替代旧 `LocomotionProfileSO` + `KinematicProfileSO` 两套 SO。hot path 每帧直接字段访问，零字符串开销。

## 字段

| 分组 | 字段 | Property 路径 | 默认值 | 旧来源 |
|------|------|-------------|--------|--------|
| Movement | `Acceleration` | `Movement/Acceleration` | 5 | LocoProfileSO.acceleration |
| | `MaxSlopeAngle` | `Movement/MaxSlopeAngle` | 55 | KProfileSO.maxGroundSlopeAngle |
| Body | `Height` | `Body/Height` | 1.8 | 新增 |
| | `ObstacleProbeVertical` | `Body/ObstacleProbeVertical` | 0.15 | KProfileSO.obstacleProbeVerticalOffset |
| | `ObstacleProbeDistance` | `Body/ObstacleProbeDistance` | 0.75 | KProfileSO.obstacleProbeDistance |
| | `ObstacleMinClimb` | `Body/ObstacleMinClimb` | 0.3 | KProfileSO.obstacleMinClimbHeight |
| | `ObstacleMaxClimb` | `Body/ObstacleMaxClimb` | 1.8 | KProfileSO.obstacleMaxClimbHeight |
| Head | `MaxHeadYaw` | `Body/MaxHeadYaw` | 75 | KProfileSO.maxHeadYawDegrees |
| | `MaxHeadPitch` | `Body/MaxHeadPitch` | 75 | KProfileSO.maxHeadPitchDegrees |

## 使用

```csharp
// CharacterActor.OnAssemble() — 初始化一次
ctx.Physique = CharacterPhysique.FromAgent(agent);

// 每帧 hot path — 零开销
motor.Evaluate(kin, intent, desiredSpeed, ctx.Physique.Acceleration, dt);
headLook.Evaluate(view, modelRoot, root, ctx.Physique.MaxHeadYaw, ctx.Physique.MaxHeadPitch);
```

## TODO

后续 Properties 扩展（负重、移速修正等）在此 struct 追加字段。
