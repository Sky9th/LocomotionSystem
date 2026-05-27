# CharacterProfile · 角色参数配置

> `Character/Config/CharacterProfile.cs` — ScriptableObject，角色物理参数、检测参数、头部朝向参数

## 调用链

```
被谁调:
  CharacterActor.Awake()          → 序列化引用
  CharacterKinematic.Evaluate()   → 读取所有参数
  CharacterActor.Debug            → 读取探针/障碍参数用于 Gizmo 绘制
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterKinematic | 地面探针/障碍/HeadLook 参数读取 |
| 被依赖 | CharacterActor.Debug | Gizmo 绘制参数读取 |
| 被依赖 | CharacterHeadLook | maxHeadYaw/Pitch/Degrees、headLookRotationSpeed |
| 被依赖 | CharacterGroundDetection | groundProbeHeight/Radius、groundLayerMask、maxGroundSlopeAngle |
| 被依赖 | CharacterObstacleDetection | obstacleProbeOffset/Distance/LayerMask、min/maxClimbHeight |

## 公开属性

```csharp
[Header("Ground")]
[Range(0, 89)] public float maxGroundSlopeAngle = 55f;      // 最大可走斜坡角度
public LayerMask groundLayerMask = ~0;                        // 地面检测层
[Min(0)] public float groundReacquireDebounceDuration;        // 重新着地防抖时间
public bool enableGroundLocking = true;                       // 地面锁定开关
public float groundLockMaxDistance = 0.15f;                   // 地面锁定最大距离
public float groundLockVerticalOffset;                        // 地面锁定垂直偏移

[Header("Ground Probe")]
[Min(0.1f)] public float groundProbeHeight = 0.5f;           // 探针起点高度
[Min(0.1f)] public float groundProbeRadius = 0.25f;          // 探针球半径

[Header("Obstacle")]
public LayerMask obstacleLayerMask = ~0;                      // 障碍检测层
[Min(0)] public float obstacleProbeVerticalOffset = 0.15f;    // 障碍探测垂直偏移
[Min(0)] public float obstacleProbeDistance = 0.75f;          // 障碍探测距离
[Min(0.1f)] public float obstacleMinClimbHeight = 0.3f;       // 最小可攀爬高度
[Min(0)] public float obstacleMaxClimbHeight = 2f;            // 最大可攀爬高度

[Header("Head Look")]
[Range(0, 90)] public float maxHeadYawDegrees = 75f;          // 头部最大偏航角
[Range(0, 90)] public float maxHeadPitchDegrees = 75f;        // 头部最大俯仰角
[Min(0)] public float headLookRotationSpeed = 1f;             // 头部旋转速度
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 添加胶囊体高度/半径参数（用于姿势切换） | 待做 | 代码预留 |
| 添加受击/死亡动画参数 | 远期 | 旧 animation-design.md |
