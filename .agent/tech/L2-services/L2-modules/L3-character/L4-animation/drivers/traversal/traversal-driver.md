# TraversalDriver · 攀爬驱动

> `Character/Animation/Drivers/Traversal/TraversalDriver.cs` — BaseAnimationDriver，一次性攀爬动画驱动
>
> **Last Verified**: 2026-07-07 | **Verification**: All referenced files exist, signatures match code. Dot Product trigger + LocomotionAnimationSetSO migration complete.

## 调用链

```
被谁调:
  Unity 生命周期 → OnEnable/OnDisable (继承自 BaseAnimationDriver)
  DriverArbiter  → Evaluate/Drive/OnStarted/OnCompleted/OnInterrupted

调谁:
  brain.SubmitRequest(request)      → 提交攀爬动画请求（DriverType=Traversal 自动路由）
  brain.FullBodyLayer.Play(clip)    → 播放攀爬动画
  brain.CharacterRig.*             → 物理控制（抑制锁定/忽略碰撞/Kinematic）
  ResolveClimbClip(set, height)    → 根据高度从 LocomotionAnimationSetSO 选择攀爬动画
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 继承 | BaseAnimationDriver | 基类 |
| 依赖 | LocomotionAnimationSetSO | traversal 动画引用（climbUpHalfMeter/1meter/2meter 等） |
| 依赖 | CharacterBuildContext | 通过 `brain.BuildContext.TraversalSet` 获取当前动画集 |
| 依赖 | AnimationBrain | 通过基类 brain 获取 FullBodyLayer / CharacterRig / BuildContext |
| 依赖 | ForwardObstacleDetection | 运行时障碍数据（CanClimb / Distance / Normal / Height / Collider / TopPoint） |

## 字段

```csharp
private Collider obstacleCollider;   // Evaluate 捕获的障碍碰撞体
private Vector3 topPoint;            // Evaluate 捕获的障碍顶部世界坐标
private bool _isActive;              // 攀爬进行中守卫，防重复触发
```

## 常量

```csharp
private const float ClimbProximityThreshold = 0.3f;   // 贴近距离阈值
private const float ClimbFaceAngleThreshold = 0.8f;    // Dot Product 正面顶墙阈值（cos36°）
```

## 公开属性

```csharp
public override int ChannelMask => 1 << 0;   // FullBody 通道
```

## 方法

### Evaluate()
```csharp
public override void Evaluate(in SCharacterFrameContext ctx, float dt)
```
- **用途**: 检测攀爬条件 → 提交 AnimationRequest
- **条件链**: _isActive 守卫 → DesiredLocalVelocity.y > 0.1f（有前进意图）→ 非空中 → CanClimb && Distance < 0.3m（贴近可爬障碍）→ dot(moveDir, -obs.Normal) > 0.8f（正面顶墙，非擦墙）→ 提交
- **Notes**: Dot Product 是跨引擎标准——Unity CC 用 `-0.85`，Godot 用 `0.8`，Unreal 用 `0.7-0.9`。`0.8` 对应 ~36° 锥角，有效区分"正面顶墙想翻"和"擦墙路过/走向墙角"
- **调用者**: DriverArbiter

### Drive()
```csharp
public override void Drive(in SCharacterFrameContext ctx, float dt)
```
- **用途**: 驱动 — OneShot Driver 不需要，空实现
- **调用者**: DriverArbiter

### OnStarted()
```csharp
public override void OnStarted(AnimationRequest request)
```
- **用途**: 请求被接受时 — 设置 _isActive → 播放攀爬 clip 到 FullBodyLayer → 抑制地面锁定 + 忽略障碍碰撞 + 启用 Kinematic
- **调用者**: DriverArbiter

### OnCompleted()
```csharp
public override void OnCompleted()
```
- **用途**: 攀爬完成 — 清除 _isActive → 设置地面 Y + 恢复 Kinematic + 恢复碰撞 + 恢复地面锁定
- **调用者**: DriverArbiter

### OnInterrupted()
```csharp
public override void OnInterrupted(AnimationRequest by)
```
- **用途**: 被打断时 — 清除 _isActive → 恢复 Kinematic/锁定/碰撞
- **调用者**: DriverArbiter

### ResolveClimbClip()
```csharp
private static ClipTransition ResolveClimbClip(LocomotionAnimationSetSO set, float obstacleHeight)
```
- **用途**: 根据障碍物高度从 LocomotionAnimationSetSO 选择攀爬动画（替代旧 `AnimationAliasProfile` + `StringAsset` 路径）
- **逻辑**: ≤0.6m → climbUpHalfMeter；≤1.1m → climbUp1meter；>1.1m → climbUp2meter
- **调用者**: Evaluate

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Vault/StepOver 穿越类型实现 | 待做 | 代码预留 |
| 攀爬逻辑需要实际场景测试验证中断/恢复链路 | P1 | 当前无可攀爬墙体配置 |
| 切换到 OffMeshLink 标记方案（需 FollowerEntity） | P2 | Dot Product 当前方案足够，等路径层整体升级 |
