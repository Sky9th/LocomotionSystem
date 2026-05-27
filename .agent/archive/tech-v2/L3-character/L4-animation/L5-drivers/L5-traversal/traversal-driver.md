# TraversalDriver · 攀爬驱动

> `Character/Animation/Drivers/Traversal/TraversalDriver.cs` — BaseCharacterAnimationDriver，一次性攀爬动画驱动

## 调用链

```
被谁调:
  Unity 生命周期 → OnEnable/OnDisable (继承自 BaseCharacterAnimationDriver)
  DriverArbiter  → Evaluate/Drive/OnStarted/OnCompleted/OnInterrupted

调谁:
  brain.SubmitRequest(this, request)  → 提交攀爬动画请求
  brain.CharacterRig.*               → 物理控制（抑制锁定/忽略碰撞/Kinematic）
  ResolveClimbAlias(height)          → 根据高度选择攀爬动画
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 继承 | BaseCharacterAnimationDriver | 基类 |
| 依赖 | AnimationAliasProfile | ClimbUp 动画别名 |
| 依赖 | AnimationBrain | 通过基类获取 CharacterRig |

## 公开属性

```csharp
public override int ChannelMask => 1 << 0;   // FullBody 通道
```

## 方法

### Evaluate()
```csharp
public override void Evaluate(in CharacterFrameContext ctx, float dt)
```
- **用途**: 检测攀爬条件 → 提交 AnimationRequest
- **条件**: Jump 按钮按下 + GroundedIdle/Moving + 障碍 CanClimb
- **调用者**: DriverArbiter

### Drive()
```csharp
public override void Drive(in CharacterFrameContext ctx, float dt)
```
- **用途**: 驱动 — OneShot Driver 不需要，空实现
- **调用者**: DriverArbiter

### OnStarted()
```csharp
public override void OnStarted()
```
- **用途**: 请求被接受时 — 抑制地面锁定 + 忽略障碍碰撞 + 启用 Kinematic
- **调用者**: DriverArbiter

### OnCompleted()
```csharp
public override void OnCompleted()
```
- **用途**: 攀爬完成 — 设置地面 Y + 恢复 Kinematic + 恢复碰撞 + 恢复地面锁定
- **调用者**: DriverArbiter

### OnInterrupted()
```csharp
public override void OnInterrupted(AnimationRequest by)
```
- **用途**: 被打断时 — 恢复 Kinematic/锁定/碰撞
- **调用者**: DriverArbiter

### ResolveClimbAlias()
```csharp
private StringAsset ResolveClimbAlias(float obstacleHeight)
```
- **用途**: 根据障碍物高度选择攀爬动画
- **逻辑**: <=0.6m → ClimbUpHalfMeter；<=1.1m → ClimbUp1meter；>1.1m → ClimbUp2meter
- **调用者**: Evaluate

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Vault/StepOver 穿越类型实现 | 待做 | 代码预留 |
| 攀爬逻辑需要实际场景测试验证中断/恢复链路 | 待做 | 旧 animation-architecture-plan.md |
