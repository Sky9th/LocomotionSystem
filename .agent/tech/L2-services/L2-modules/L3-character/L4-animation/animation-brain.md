# AnimationBrain · 动画总控

> `Character/Animation/Components/AnimationBrain.cs` — MonoBehaviour [DefaultExecutionOrder(-10)]，6层 Animancer 管理 + HeadLook 混合 + RootMotion

## 调用链

```
被谁调:
  CharacterActor.Awake()          → GetComponentInChildren 获取引用
  CharacterActor.SetRig(rig)      → 注入 CharacterRig
  CharacterActor.Update()         → Apply(in ctx)
  BaseCharacterAnimationDriver    → OnEnable/OnDisable 注册驱动
  Unity Animation Phase           → OnAnimatorMove()

调谁:
  DriverArbiter.Resolve()         → 仲裁 + 驱动动画
  baseLayer.TryPlay()             → Animancer 播放
  headLookMixer.Parameter         → HeadLook 混合参数
  characterRig.Apply*()           → 根运动转发
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | CharacterActor | 持有引用，每帧 Apply(ctx) |
| 被依赖 | BaseCharacterAnimationDriver | OnEnable/OnDisable 注册/注销 |
| 依赖 | DriverArbiter | FullBody 仲裁器 |
| 依赖 | CharacterRig | 根运动转发、物理写入 |
| 依赖 | NamedAnimancerComponent | Animancer 播放器 |
| 依赖 | AnimationAliasProfile | 动画别名 SO |
| 依赖 | LocomotionAnimationProfile | 动画参数 SO |
| 依赖 | AvatarMask ×5 | 各层 AvatarMask |

## 常量

```csharp
public const int TotalLayerCount = 6;
public const int FullBody  = 0;    // 全身 — 仲裁
public const int UpperBody = 1;    // 上身 — 预留
public const int Additive  = 2;    // 叠加 — 预留
public const int Facial    = 3;    // 面部 — 预留
public const int HeadLook  = 4;    // 头部 — 常驻不仲裁
public const int Footstep  = 5;    // 脚步 — 常驻不仲裁
```

## 公开属性

```csharp
internal CharacterRig CharacterRig => characterRig;                // 物理实体入口
public NamedAnimancerComponent Animancer => animancer;             // Animancer 组件
public AnimancerLayer FullBodyLayer => fullBodyLayer;              // FullBody 层
public AnimancerLayer HeadLookLayer => headLookLayer;              // HeadLook 层
```

## 方法

### Awake()
```csharp
private void Awake()
```
- **用途**: 初始化 6 层 Animancer 层 + DriverArbiter + 绑定 Mask + 初始化 HeadLook Mixer
- **调用者**: Unity 生命周期
- **备注**: execution order -10，早于 CharacterActor (0)

### OnAnimatorMove()
```csharp
private void OnAnimatorMove()
```
- **用途**: 根运动转发 — 将 animator.deltaPosition/Rotation 通过 CharacterRig 写入
- **调用者**: Unity Animation Phase
- **备注**: SuppressGroundLock 时允许 Y 轴位移，否则仅 XZ 位移

### SetRig()
```csharp
internal void SetRig(CharacterRig rig)
```
- **用途**: 注入 CharacterRig 引用
- **调用者**: `CharacterActor.Awake()`

### Apply()
```csharp
internal void Apply(in CharacterFrameContext ctx)
```
- **用途**: 每帧动画驱动入口
- **调用者**: `CharacterActor.Update()`
- **备注**: 先 resolve fullBodyArbiter，再 UpdateHeadLook，最后 ApplySpeedMultiplier

### SpeedMultiplier
```csharp
public float SpeedMultiplier { get; private set; } = 1f;
```
- **用途**: 步态动画速度乘数 = 角色期望移速 / 动画原生速度，供所有动画层读取
- **备注**: gait 变化或 AnimationState 切换时更新

### ApplySpeedMultiplier()
```csharp
private void ApplySpeedMultiplier(in CharacterFrameContext ctx)
```
- **用途**: gait 或 AnimationState 变化时计算并应用 SpeedMultiplier 到 FullBody 层
- **调用者**: Apply()
- **备注**: 遍历 animationProfile.modeProfiles 找到匹配的 animNativeSpeed，未配置则 Speed=1

### UpdateHeadLook()
```csharp
private void UpdateHeadLook(in CharacterFrameContext ctx)
```
- **用途**: 更新 HeadLook Mixer 参数（smoothed yaw/pitch）
- **调用者**: Apply()
- **备注**: 首帧 freeze 所有子动画（Speed=0, Weight=1, NormalizedTime=1）

### FreezeHeadLookChildren()
```csharp
private void FreezeHeadLookChildren()
```
- **用途**: 冻结 HeadLook Mixer 的所有子动画（预烘焙姿势）
- **调用者**: UpdateHeadLook（首帧）

### RegisterDriver / UnregisterDriver
```csharp
internal void RegisterDriver(ICharacterAnimationDriver driver)
internal void UnregisterDriver(ICharacterAnimationDriver driver)
```
- **用途**: Driver 注册/注销 — 委托给 DriverArbiter
- **调用者**: BaseCharacterAnimationDriver.OnEnable/OnDisable

### SubmitRequest(driver, request) / SubmitRequest(request)
```csharp
internal void SubmitRequest(ICharacterAnimationDriver driver, AnimationRequest request)
internal void SubmitRequest(AnimationRequest request)
```
- **用途**: 动画请求提交。带 driver 参数的重载由 Driver 内部调用；无 driver 参数的重载按 `request.DriverType` 解析对应 Driver，外部调用方无需持有 Driver 引用
- **调用者**: Driver 内部 / 外部通过 Brain 门面

### Release()
```csharp
internal void Release()
```
- **用途**: 释放当前活跃 Driver — 委托给 DriverArbiter.ReleaseActive()
- **调用者**: 外部（不打断不需要指定 Driver，同一时间只有一个活跃）

### BindLayer()
```csharp
private AnimancerLayer BindLayer(int index, AvatarMask mask)
```
- **用途**: 绑定 AvatarMask 到指定索引的 AnimancerLayer
- **调用者**: Awake()

## 内部机制

### 根运动转发
```
OnAnimatorMove():
  if !forwardRootMotion → return
  if suppressGroundLock → ApplyPosition (包含 Y)
  else → ApplyPositionPlanar (仅 XZ)
  if applyRootMotionRotation → ApplyRotation (默认 false，由代码 ApplyTurnStepRotation 控制)
```

### HeadLook 混合
```
UpdateHeadLook():
  首帧 → FreezeHeadLookChildren (Speed=0, Weight=1, NormalizedTime=1)
  每帧:
    target = ctx.Kinematic.LookDirection
    speed = animationProfile.headLookSmoothingSpeed
    smoothedYaw = MoveTowards(smoothedYaw, target.x, step)
    smoothedPitch = MoveTowards(smoothedPitch, target.y, step)
    headLookMixer.Parameter = Vector2(smoothedYaw, smoothedPitch)
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| UpperBody/Additive/Facial 层 DriverArbiter 接入 | 待做 | 旧 animation-design.md |
| Footstep 层动画实现（当前仅作为事件回调注入） | 待做 | 代码 TODO |
| HeadLook 受 activeRequest Tags 控制关闭（战斗/反应时） | 待做 | 旧 animation-design.md |
| 使用 headLookRotationSpeed 配置（当前未使用） | 待做 | 代码字段存在未接入 |
