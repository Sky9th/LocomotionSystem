# SForwardObstacleDetection · 障碍检测结果结构体

> `Character/Kinematic/SForwardObstacleDetection.cs` — [Serializable] readonly struct，前方障碍物检测结果

## 调用链

```
创建者:
  CharacterObstacleDetection.TryDetectForwardObstacle()
    → new SForwardObstacleDetection(19字段)

消费者:
  CharacterKinematic → 聚合到 SCharacterKinematic
  CharacterActor.Debug → Gizmo 障碍绘制
  TraversalDriver → 读取 CanClimb/HasTopSurface/ObstacleHeight
```

## 公开属性

```csharp
public bool HasHit { get; }                          // 是否有命中
public bool HasTopSurface { get; }                   // 是否探测到顶部表面
public bool IsSlope { get; }                         // 是否为斜坡
public bool IsObstacle { get; }                      // 是否为障碍
public bool CanClimb { get; }                        // 是否可以攀爬
public bool CanVault { get; }                        // 是否可以翻越 (当前恒 false)
public bool CanStepOver { get; }                     // 是否可以迈过 (当前恒 false)
public float Distance { get; }                       // 障碍物距离
public float ObstacleHeight { get; }                 // 障碍物高度
public Vector3 Point { get; }                        // 命中点
public Vector3 Normal { get; }                       // 命中法线
public Vector3 TopPoint { get; }                     // 顶部探测点
public Vector3 TopNormal { get; }                    // 顶部法线
public float SurfaceAngle { get; }                   // 表面角度
public Vector3 Direction { get; }                    // 探测方向
public Collider Collider { get; }                    // 命中的碰撞体
public int HitLayer => Collider?.gameObject.layer ?? -1;  // 命中层
public static SForwardObstacleDetection None { get; }      // 空值
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterObstacleDetection | 创建者 |
| 被依赖 | CharacterKinematic | 聚合到 SCharacterKinematic |
| 被依赖 | TraversalDriver | 读取 CanClimb/HasTopSurface |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| CanVault/CanStepOver 实际判定 | 待做 | 代码预留 |
