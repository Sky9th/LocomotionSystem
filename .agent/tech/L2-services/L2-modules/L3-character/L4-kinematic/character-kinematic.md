# CharacterKinematic · 运动学入口

> `Character/Kinematic/CharacterKinematic.cs` — 纯 C# 类，每帧评估位置/朝向/地面/障碍/HeadLook

## 调用链

```
被谁调:
  CharacterActor.Update()
    → characterKinematic.Evaluate(profile, heading, dt)

调谁:
  CharacterHeadLook.Evaluate()              → 头部朝向计算
  CharacterGroundDetection.EvaluateGroundContact() → 地面接触检测
  CharacterObstacleDetection.TryDetectForwardObstacle() → 障碍检测
  characterRig.FreezePositionY()            → 物理 Y 轴冻结
  characterRig.SetGroundedY()               → 地面锁定
  characterRig.ZeroVelocity()               → 速度置零
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterActor | 每帧 Evaluate() 调用者 |
| 依赖 | CharacterHeadLook | 计算头部注视方向 |
| 依赖 | CharacterGroundDetection | 地面接触检测 |
| 依赖 | CharacterObstacleDetection | 障碍检测 |
| 依赖 | CharacterRig | 地面锁定/物理约束 |
| 依赖 | CharacterProfile | 配置参数 |
| 输出 | SCharacterKinematic | struct 输出聚合 |
| 输出 | SGroundContact | 地面接触中间结果 |

## 方法

### CharacterKinematic()
```csharp
internal CharacterKinematic(Transform actorTransform, Transform modelRoot, CharacterRig characterRig)
```
- **用途**: 构造
- **参数**: `actorTransform` — 角色 Transform；`modelRoot` — 视觉模型 Transform；`characterRig` — 物理写入入口
- **调用者**: `CharacterActor.Awake()`

### Reset()
```csharp
internal void Reset()
```
- **用途**: 重置地面接触历史缓存（OnDisable 时调用）
- **调用者**: `CharacterActor.OnDisable()`

### Evaluate()
```csharp
internal SCharacterKinematic Evaluate(CharacterProfile profile, Vector3 heading, float deltaTime)
```
- **用途**: 完整运动学评估 — HeadLook + 地面检测 + 障碍检测 → 聚合输出
- **参数**: `profile` — 角色 SO 配置；`heading` — 运动朝向；`deltaTime` — 帧时间
- **返回**: 包含位置/朝向/地面/障碍的完整 SCharacterKinematic
- **调用者**: `CharacterActor.Update()`
- **备注**: profile 为 null 时 throw

### EvaluateGroundContactAndApplyConstraints()
```csharp
private SGroundContact EvaluateGroundContactAndApplyConstraints(CharacterProfile profile, float deltaTime, ref Vector3 position)
```
- **用途**: 评估地面接触 + 执行地面锁定约束（冻结 Y、修正 Y、速度置零）
- **参数**: `profile` — 配置；`deltaTime` — 帧时间；`position` — ref 传出修正后的位置
- **返回**: 稳定化的地面接触
- **调用者**: `Evaluate()`

### EvaluateStableGroundContact()
```csharp
private SGroundContact EvaluateStableGroundContact(CharacterProfile profile, Vector3 position, float deltaTime)
```
- **用途**: 两步地面接触评估 — 原始检测 → 累积 → 稳定化
- **调用者**: `EvaluateGroundContactAndApplyConstraints()`

### Accumulate()
```csharp
private static SGroundContact Accumulate(in SGroundContact cur, in SGroundContact prev, float dt)
```
- **用途**: 累积地面状态持续时间 — 同态续增，换态归零
- **调用者**: `EvaluateStableGroundContact()`

### Stabilize()
```csharp
private SGroundContact Stabilize(in SGroundContact raw, in SGroundContact prevStable, float debounce, float dt)
```
- **用途**: 地面防抖 — 仅当 canReacquire（防抖时间已过或前帧已着地）时才允许着地
- **调用者**: `EvaluateStableGroundContact()`

## 内部机制

### 地面稳定化逻辑

```
原始检测 → Accumulate(持续计时) → Stabilize(防抖)
  canReacquire = debounce<=0 || prevStable.IsGrounded || prevStable.StateDuration >= debounce
  candidate = raw.IsGrounded && canReacquire ? raw : raw.WithIsGrounded(false)
  → 再 Accumulate → 输出
```

### 地面锁定

```
SuppressGroundLock 时 → 跳过锁定，直接返回 contact.WithIsGrounded(true)
非 Suppress 时:
  FreezePositionY(enableGroundLocking && contact.IsGrounded)
  if (接触地面 && 距离 < groundLockMaxDistance):
    SetGroundedY(contactPoint.y + offset)  ← 修正 Y
    ZeroVelocity()                          ← 速度置零
    position.y = newY
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 胶囊体投射替代 SphereCast 地面检测 | 待做 | 旧 module-analysis.md |
