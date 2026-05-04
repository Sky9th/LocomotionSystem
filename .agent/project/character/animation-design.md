# Animation 模块设计

> 日期: 2025-04-28
> 状态: 全部定稿, 待实现

---

## 1. 定位

动画模块负责**播放动画 clip 到 Animancer 层**。不计算数据（数据来自 Character 层），不仲裁业务规则（游戏逻辑通过回调注入），只提供 Layer 管理和 Driver 调度通道。

物理实体操作统一走 `CharacterRig`（`Character/Components/CharacterRig.cs`）

---

## 2. 目录结构

```
Character/Animation/
│
├── Components/
│   └── CharacterAnimationController.cs    ← MB [ExecutionOrder(10)]
│
├── DriverArbiter.cs                       ← 仲裁器 (纯 C#)
│
├── Drivers/
│   ├── ICharacterAnimationDriver.cs       ← Driver 接口
│   │
│   ├── LocomotionDriver.cs                ← 实现 Driver 接口, 持有 BaseLayer
│   │
│   └── Locomotion/                        ← LocomotionDriver 私有实现
│       ├── BaseLayer.cs                   ← 7状态FSM + ApplyTurnStepRotation()
│       ├── BaseStateKey.cs                ← enum: Idle/TurnInPlace/Moving/...
│       └── States/
│           ├── BaseIdleState.cs
│           ├── BaseTurnInPlaceState.cs
│           ├── BaseIdleToMovingState.cs
│           ├── BaseMovingState.cs
│           ├── BaseTurnInMovingState.cs
│           ├── BaseAirLoopState.cs
│           └── BaseAirLandState.cs
│
├── Requests/
│   ├── AnimationRequest.cs                ← 请求数据包 (class)
│   └── OnInterruptedBehavior.cs           ← Resume/Cancel enum
│
└── Config/
    ├── LocomotionAliasProfile.cs           ← SO (动画别名)
    ├── LocomotionAnimationProfile.cs       ← SO (动画参数)
    └── LocomotionModeProfile.cs            ← SO (模式转向速度)
```

**和旧代码差异**:
- 删 `Appliers/` → `ApplyTurnStepRotation()` 内置于 BaseLayer
- 删 `Conditions/` (8个) → 条件内联到 `State.CanEnterState`
- 删 `ICheck<T>` 泛型组合器 → Animancer 的 `CanEnterState` 已够用
- 删 `LocomotionAnimationController.cs` → LocomotionDriver 直接调 BaseLayer
- 删 `LocomotionAnimationContext.cs` → 直接传 `(snapshot, dt)`
- 删 `ILocomotionAnimationLayer` 多余字段 → 只保留 `Layer { get; }` + `Update(snapshot, dt)`

---

## 3. ILocomotionAnimationLayer

```csharp
internal interface ILocomotionAnimationLayer
{
    AnimancerLayer Layer { get; }
    void Update(in SCharacterSnapshot snapshot, float dt);
}
```

仅保留最小接口。调试信息直接读 `AnimancerLayer.CurrentState` 获取，不自己包装。未来新增层类型只需实现此接口并通过 Controller 注册。

---

## 4. ICharacterAnimationDriver

```csharp
internal interface ICharacterAnimationDriver
{
    int ChannelMask { get; }

    // 每帧由 Arbiter 调用 (仅 Active 时)
    void Drive(in SCharacterSnapshot snapshot, float dt);

    // 仲裁回调
    void OnInterrupted(AnimationRequest by);
    void OnResumed();
}
```

3 个成员。`BuildRequest` 删除 — Driver 通过 `Arbiter.SubmitRequest()` 主动提交请求。

---

## 5. AnimationRequest

```csharp
class AnimationRequest
{
    // 播放什么
    public AnimationClip Clip / StringAsset Alias;   // 二选一
    public float FadeIn;
    public float FadeOut;

    // 协商 (后来者读我, 自己判断)
    public int Tags;              // 位标记 (Movement|Combat|Reaction|Cinematic|...)
    public int Resistance;        // 0~N, 被打断难度

    // 打断行为
    public OnInterruptedBehavior Behavior;  // Resume / Cancel

    // 占哪层
    public int ChannelMask;       // FullBody | UpperBody | ...
}

enum OnInterruptedBehavior { Resume, Cancel }
```

无 Priority。无固定枚举层级表。后来者读 ActiveRequest 的 Tags+Resistance，自己做中断决策。

---

## 6. DriverArbiter

### API

| 方法 | 调用方 | 作用 |
|---|---|---|
| `RegisterDriver(driver)` | OnEnable | 注册, 返回 bool |
| `UnregisterDriver(driver)` | OnDisable | 移除, Fallback 下一个默认 |
| `SubmitRequest(driver, request)` | Driver (Update/Drive) | 主动提交动画请求 |
| `Release(driver)` | Driver (Update/Drive) | Driver 自己结束自己的 Active |
| `Resolve(snapshot, dt)` | Controller | 每帧调度入口 |

### 每帧处理顺序

```
Resolve(snapshot, dt):

1. 处理请求队列:
   foreach (queue 中的请求 → 按 Resistance 排序)
     与 ActiveRequest 比较 → 裁决 → Accept/Reject
     通知 OnInterrupted / OnResumed
   清空队列

2. 检查动画完成:
   NormalizedTime >= 0.99 → OnComplete 判定 → Resume/Stay

3. Drive: activeDriver?.Drive(snapshot, dt)
```

### 请求队列

- 无 `BuildRequest` 轮询 — Driver 通过 `SubmitRequest()` 主动提交
- 每帧开始处理, 清空
- 同 Driver 重复提交 → 最后一次覆盖 (去重)
- 不通过的当场丢弃, 无积压
- 拒绝后 Driver 自主决定: 下帧重试/丢弃/降级

### 默认 Driver

- 第一个注册的是默认
- Unregister 后顺位下一个
- 激活默认时回调 OnResumed

### Release vs OnInterrupted

- `Release(driver)` → Driver 自己结束自己 (Dance 按取消)
- `OnInterrupted(by)` → 被别人的请求打断时通知

### 对比旧版

| | 旧 (Polling) | 新 (Submit) |
|---|---|---|
| 请求提交 | `BuildRequest()` 每帧轮询 | `SubmitRequest()` Driver 主动 |
| 请求队列 | 无 (逐 Driver 取第一个) | 本帧队列, 排序裁决 |
| 自己结束 | 不支持 | `Release()` |
| Driver 生命周期 | 手动清理 | `UnregisterDriver` 自动 Fallback |

---

## 7. 方案决策: Component Driver

每个 Driver 是挂载在 Model 上的 Component，`OnEnable` 中自注册到父节点 Controller。配置由 Driver 自己持有 `[SerializeField]`，不经过 CharacterActor 传递。

### GameObject 层级

```
Model
  ├── CharacterAnimationController    ← 仲裁入口
  ├── LocomotionDriver ([待建])       ← Component 钩子
  │     ├── [Serialize] LocomotionAliasProfile, LocomotionAnimationProfile
  │     └── [Serialize] LocomotionModeProfile
  ├── NamedAnimancerComponent
  └── Animator
```

### 接口扩展: BaseCharacterAnimationDriver

```csharp
abstract class BaseCharacterAnimationDriver : MonoBehaviour, ICharacterAnimationDriver
{
    void OnEnable()  → GetComponent<CharacterAnimationController>().RegisterDriver(this);
    void OnDisable() → UnregisterDriver;
}
```

Driver 通过 `OnEnable` 自注册，Controller 不再主动创建。

### CharacterActor 简化

```csharp
// 移除: locomotionAlias, locomotionAnimationProfile 序列化字段
// Start 中只传 rig
characterAnimation?.SetRig(characterRig);
```

动画配置 (3 SO) 下放到 Driver Component 自己管理。

---

## 8. CharacterAnimationController

```csharp
[DefaultExecutionOrder(10)]   // 确保在 CharacterActor(0) 之后执行
public sealed class CharacterAnimationController : MonoBehaviour
{
    // Layer 0: FullBody   (无 Mask, 仲裁)
    // Layer 1: UpperBody  (upperBodyMask, 仲裁)
    // Layer 2: Additive   (additiveMask, 仲裁)
    // Layer 3: Facial     (facialMask, 仲裁)
    // Layer 4: HeadLook   (headMask, 常驻, 不仲裁)
    // Layer 5: Footstep   (footMask, 常驻, 不仲裁)

    private DriverArbiter fullBodyArbiter;
    private CharacterRig characterRig;

    public void RegisterDriver(ICharacterAnimationDriver driver) { ... }

    internal void SetRig(CharacterRig rig) { characterRig = rig; }
    // Driver 通过 BaseCharacterAnimationDriver.OnEnable 自注册, Config SO 由 Driver 自己 [SerializeField] 管理

    public void Apply(in SCharacterSnapshot snapshot)
    {
        fullBodyArbiter.Update(snapshot, Time.deltaTime);
        UpdateHeadLook(snapshot, fullBodyArbiter.ActiveRequest);
    }

    private void UpdateHeadLook(SCharacterKinematic kin, AnimationRequest active)
    {
        // active.Tags 含 Combat/Reaction/Cinematic → 关闭 HeadLook
        // 否则 → Vector2Mixer.Parameter = kin.LookDirection
    }

    private void OnAnimatorMove()
    {
        characterRig.ApplyPosition(animator.deltaPosition);
        characterRig.ApplyRotation(animator.deltaRotation);
    }
}
```

### SerializeField 配置

| 字段 | 类型 | 说明 |
|---|---|---|
| `animancer` | `NamedAnimancerComponent` | Auto-resolve: `GetComponentInChildren` |
| `animator` | `Animator` | Auto-resolve: `GetComponentInChildren` |
| `forwardRootMotion` | `bool` (default true) | OnAnimatorMove 开关 |
| `applyRootMotionPlanarPositionOnly` | `bool` (default true) | 仅应用 XZ 平面位移 |
| `upperBodyMask` | `AvatarMask` | Layer 1 |
| `additiveMask` | `AvatarMask` | Layer 2 |
| `facialMask` | `AvatarMask` | Layer 3 |
| `headMask` | `AvatarMask` | Layer 4 (HeadLook 常驻) |
| `footMask` | `AvatarMask` | Layer 5 (Footstep 常驻) |

运行时注入: `characterRig`, `fullBodyArbiter`, LocomotionDriver, AliasProfile, AnimationProfile。

## 8. BaseLayer FSM (旧→新映射)

FSM 逻辑已在旧代码充分测试, 不需要重新设计。仅做架构适配。以下是完整映射。

### BaseLayer 自身

| 旧 | 新 |
|---|---|
| `ILocomotionAnimationLayer` (4成员) | 精简为 `Layer { get }` + `Update(snapshot, dt)` |
| `LocomotionAnimationContext` (7字段包装) | 直接传 `(SCharacterSnapshot, float dt)` |
| `context.Snapshot.Motor.TurnAngle` | `snapshot.Locomotion.Motor.TurnAngle` |
| `context.Snapshot.Kinematic.GroundContact` | `snapshot.Kinematic.GroundContact` |
| `context.Alias / Profile / Transformer` | 构造注入: `aliasProfile`, `animProfile`, `characterRig` |
| `context.DeltaTime` | `dt` 参数 |
| `SLocomotionAnimationLayerSnapshot` | 删除 — 调试信息直读 `AnimancerLayer.CurrentState` |
| `LocomotionAnimationController` (编排) | 删除 — `LocomotionDriver.Update()` 直接调 `BaseLayer.Update()` |

### 状态 (7个)

| 旧 | 新 |
|---|---|
| `ICheck<T>` 条件文件 (CanEnter*.cs 7个) | 删除 — `State.CanEnterState` 内联 |
| `context.Check<CanEnterXxx>()` | `snapshot.Locomotion.Discrete.Phase == GroundedMoving && !IsTurning` |
| `TurnAngleStepRotationApplier.TryApply(profile, motor, snapshot, dt)` | `Owner.ApplyTurnStepRotation()` (BaseLayer 内部方法) |
| `motor.ApplyDeltaRotation(...)` | `characterRig.ApplyRotation(...)` |
| `Owner.context.LocomotionProfile` (TurnInMoving 前向意图) | `snapshot.Locomotion.Motor.DesiredLocalVelocity` |

### 每个状态的 CanEnter 条件

| 状态 | 条件 (内联到 CanEnterState) |
|---|---|
| **Idle** | `Phase == GroundedIdle && !IsTurning` |
| **TurnInPlace** | `Phase == GroundedIdle && IsTurning` |
| **IdleToMoving** | `Phase == GroundedMoving && IsTurning` 且 FSM 上次状态为 Idle |
| **Moving** | `Phase == GroundedMoving && !IsTurning` |
| **TurnInMoving** | `Phase == GroundedMoving && IsTurning && DesiredLocal.y ≥ 0.9×speed && abs(DesiredLocal.x) ≤ 0.1×speed` |
| **AirLoop** | `Phase == Airborne` |
| **AirLand** | `DistanceToGround < landDistanceThreshold` (0.5m) |

### 每个状态的 Clip

| 状态 | Clip | 额外 |
|---|---|---|
| Idle | `alias.idleL`/`idleR` | |
| TurnInPlace | `alias.turnInPlace90L`/`R` (按 TurnAngle 符号) | `ApplyTurnStepRotation()` |
| IdleToMoving | `alias.idleToRun180L`/`R` | 播完自动切 Moving |
| Moving | `alias.walkMixer`/`runMixer`/`sprint` (按 Gait) | `Vector2Mixer.Parameter = ActualLocalVelocity / moveSpeed`; `ApplyTurnStepRotation()` |
| TurnInMoving | `alias.turnInWalk/Run/Sprint180L`/`R` | `ApplyTurnStepRotation()` |
| AirLoop | `alias.AirLoop` | |
| AirLand | `alias.AirLand` | 播完 → Idle/Moving/TurnInPlace |

### TurnAngleStepRotation (BaseLayer 方法)

```csharp
internal bool ApplyTurnStepRotation()
{
    var absAngle = Mathf.Abs(Snapshot.Locomotion.Motor.TurnAngle);
    if (absAngle <= Mathf.Epsilon) return false;

    var turnSpeed = AnimationProfile.GetTurnSpeed(
        Snapshot.Locomotion.Discrete.Posture,
        Snapshot.Locomotion.Discrete.Gait,
        Snapshot.Locomotion.Discrete.Gait != EMovementGait.Idle);
    if (turnSpeed <= 0f) return false;

    var step = Mathf.Min(turnSpeed * DeltaTime, absAngle);
    var delta = Mathf.Sign(Snapshot.Locomotion.Motor.TurnAngle) * step;
    characterRig.ApplyRotation(Quaternion.AngleAxis(delta, Vector3.up));
    return true;
}
```

### 不变的部分

| 项目 | 原因 |
|---|---|
| 7 State 的 Tick 转换逻辑 | 旧代码已验证 |
| `TrySetState/ForceSetState/Play/PlayIfChanged` | 不变 |
| `Animancer.StateMachine<BaseStateKey, FsmState>` | 不变 |
| Vector2Mixer 参数驱动 | 不变 |
| Tick 内转换优先级 (高→低) | 不变 |
| CanEnter 条件语义 | 不变 (只改写法) |
| `HasCompleted() → NormalizedTime >= 0.99` | 不变 |

## 9. 调用链

```
CharacterActor.Update()
  ├── Steps 1-4 (Input/Kinematic/Locomotion)
  ├── snapshot = new SCharacterSnapshot(...)
  ├── characterAnimation.Apply(in snapshot)        ← 直传
  └── GameContext.UpdateSnapshot(snapshot)

CharacterAnimationController.Apply(snapshot):
  └── fullBodyArbiter.Update(snapshot, dt)
       ├── BuildRequest() 轮询
       ├── 协商 → 打断/恢复
       ├── ActiveDriver.Update(snapshot, dt)
       │    └── BaseLayer.FSM.Tick(snapshot)
       └── UpdateHeadLook(snapshot, ActiveRequest)

CharacterAnimationController.OnAnimatorMove():
  └── characterRig.ApplyPosition/Rotation
```

---

## 10. CharacterRig

不为 Animation 专属，是 Character 层共享服务：

```csharp
// Character/Components/CharacterRig.cs (纯 C#)
internal sealed class CharacterRig
{
    void ApplyPosition(Vector3 delta);
    void SetGroundedY(float y);
    void ApplyRotation(Quaternion delta);
    void FreezePositionY(bool freeze);
    void ApplyForce(Vector3 force, ForceMode mode);
    void SetVelocity(Vector3 velocity);
    void SetKinematic(bool kinematic);
    void SetCapsuleHeight(float height, Vector3 center);
    void EnableCollider(bool enable);
}
```

### 持有方

| 持有方 | 注入方式 |
|---|---|
| `CharacterKinematic` | 构造注入 |
| `CharacterAnimationController` | `Initialize(rig, ...)` |
| `LocomotionDriver` | via Controller |
| `BaseLayer` | via LocomotionDriver |

---

## 11. 状态

| 话题 | 状态 |
|---|---|
| 组件 + Apply/OnAnimatorMove | ✅ |
| Driver 接口 + 仲裁协商 | ✅ |
| AnimationRequest + Tags/Resistance | ✅ |
| Layer 接口精简 | ✅ |
| 场景 1-5 处理 | ✅ |
| BaseLayer FSM 映射 | ✅ |
| SerializeField 配置 | ✅ |
| 默认 Driver | ✅ |
| CharacterRig | ✅ |
| 动画资源 (Animancer 原生) | ✅ |
