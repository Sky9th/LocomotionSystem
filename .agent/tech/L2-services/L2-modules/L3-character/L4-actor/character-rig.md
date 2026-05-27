# CharacterRig · 物理实体入口

> `Character/Components/CharacterRig.cs` — 纯 C# 类，封装 Rigidbody/Transform/CapsuleCollider 写入

## 调用链

```
被谁调:
  CharacterKinematic:
    → FreezePositionY / SetGroundedY / ZeroVelocity / SetSuppressGroundLock
  AnimationBrain.OnAnimatorMove():
    → ApplyPosition / ApplyPositionPlanar / ApplyRotation
  BaseLayer (FSM 状态):
    → ApplyTurnStepRotation → ApplyRotation
  TraversalDriver:
    → SetSuppressGroundLock / IgnoreCollisionWith / SetKinematic / SetGroundedY

调谁:
  Rigidbody   → constraints/velocity/isKinematic
  Transform   → position/rotation
  CapsuleCollider → height/center
  Physics.IgnoreCollision → 碰撞忽略
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterKinematic | 地面锁定/物理约束 |
| 被依赖 | AnimationBrain | 根运动位移/旋转转发 |
| 被依赖 | BaseLayer/FSM | 程序化转身旋转 |
| 被依赖 | TraversalDriver | 攀爬时物理控制 |
| 被依赖 | LocomotionDriver | 在 OnResumed 时 InvalidateAnimationCache |
| 依赖 | Rigidbody | Unity 物理组件 |
| 依赖 | CapsuleCollider | Unity 碰撞组件 |

## 公开属性

```csharp
internal bool SuppressGroundLock => suppressGroundLock;  // 是否抑制地面锁定（空中/攀爬时）
```

## 方法

### CharacterRig()
```csharp
internal CharacterRig(Transform root, Transform model)
```
- **用途**: 构造，解析 root 上的 Rigidbody 和 CapsuleCollider
- **参数**: `root` — 物理根 Transform；`model` — 视觉模型根 Transform
- **调用者**: `CharacterActor.Awake()`
- **备注**: model 通常为 AnimationBrain 所在 Transform

### ApplyModelPosition / ApplyModelPositionPlanar
```csharp
internal void ApplyModelPosition(Vector3 delta)
internal void ApplyModelPositionPlanar(Vector3 delta)
```
- **用途**: 移动 visual model 位置（全部 / 仅 XZ 平面）
- **调用者**: `AnimationBrain.OnAnimatorMove()`

### ApplyModelRotation
```csharp
internal void ApplyModelRotation(Quaternion delta)
```
- **用途**: 旋转 visual model
- **调用者**: `AnimationBrain.OnAnimatorMove()`

### ApplyPosition / ApplyPositionPlanar
```csharp
internal void ApplyPosition(Vector3 delta)
internal void ApplyPositionPlanar(Vector3 delta)
```
- **用途**: 移动 root transform（全部 / 仅 XZ 平面）
- **调用者**: `AnimationBrain.OnAnimatorMove()`

### ApplyRotation
```csharp
internal void ApplyRotation(Quaternion delta)
```
- **用途**: 旋转 root transform
- **调用者**: BaseLayer.ApplyTurnStepRotation、AnimationBrain.OnAnimatorMove

### SetGroundedY
```csharp
internal void SetGroundedY(float y)
```
- **用途**: 将 root 位置锁定到地面高度
- **调用者**: `CharacterKinematic.EvaluateGroundContactAndApplyConstraints()`

### FreezePositionY
```csharp
internal void FreezePositionY(bool freeze)
```
- **用途**: 设置 Rigidbody Y 轴约束（着地时冻结防下落，空中解冻）
- **调用者**: `CharacterKinematic.EvaluateGroundContactAndApplyConstraints()`

### SetCapsuleHeight
```csharp
internal void SetCapsuleHeight(float height, Vector3 center)
```
- **用途**: 修改碰撞体尺寸（用于蹲伏/趴下）
- **调用者**: 预留，当前未使用

### SetSuppressGroundLock
```csharp
internal void SetSuppressGroundLock(bool suppress)
```
- **用途**: 抑制/恢复地面锁定（空中/攀爬时用）
- **调用者**: BaseAirLoopState、TraversalDriver
- **备注**: suppress=true 时自动调用 FreezePositionY(false)

### IgnoreCollisionWith
```csharp
internal void IgnoreCollisionWith(Collider other, bool ignore)
```
- **用途**: 临时忽略/恢复与某个 Collider 的碰撞（攀爬时）
- **调用者**: TraversalDriver

### ZeroVelocity
```csharp
internal void ZeroVelocity()
```
- **用途**: 将 Rigidbody velocity 置零
- **调用者**: `CharacterKinematic.EvaluateGroundContactAndApplyConstraints()`

### SetKinematic
```csharp
internal void SetKinematic(bool kinematic)
```
- **用途**: 设置 Rigidbody isKinematic（攀爬时临时开启）
- **调用者**: TraversalDriver
- **备注**: 恢复时回到之前的状态（wasKinematic）

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| SetCapsuleHeight 用于蹲伏/趴下姿势切换 | 待做 | 代码预留 |
| SetVelocity/ApplyForce 用于受击/击退 | 待做 | 旧 animation-design.md |
