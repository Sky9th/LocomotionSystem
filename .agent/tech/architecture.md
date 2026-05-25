# LocomotionSystem – Copilot 指南

该 Instruction 应保持简洁和高度概括，具体的设计细节和约定请放在项目内的其他文档中。

## 项目速览
- Unity 2022.3 URP 项目，目标平台 Windows Standalone，启用 Unity Input System。
- 运行时脚本位于 `Assets/Scripts/`；美术资源位于 `Assets/Art/` 和 `Assets/External/`，通常不在代码任务中修改。
- `.sln`、`.csproj` 等解决方案文件由 Unity 自动生成，禁止手动编辑。

## 动画资源
- **Synty Animation Base Locomotion**：基础移动动画（Idle/Walk/Run/Sprint/Jump/Land）
- **PROTOFACTOR Ultimate Animation Collection**：攀爬动画集（ClimbUp/ClimbDown/Landing/Exit/WallJump）
- **Movement MocapAnimPack 2.0**：动作捕捉动画（备选）

## 系统架构

### 核心原则
- 数据由上至下参数传递，不反向查询 GameContext
- Service 通过 `PublishState<T>()` 统一写入 GameContext + Dispatcher
- Component 禁止直接调用 `GameContext.Instance.UpdateSnapshot()`
- 个体实体数据留在 Component public 属性，通过 `GetComponent<T>()` 读取
- Service 间通过 Dispatcher（push）或 GameContext（pull）通信，不持有彼此引用
- CharacterRig 统一物理实体写入入口（Transform/Rigidbody/Collider）
- 父模块调用子模块，不跨级调用
- 动画驱动通过 Driver/Arbiter 模式：LocoDriver（连续）+ TraversalDriver（一次性）

### 角色系统 (`Assets/Scripts/Character/`)

```
Character/
├── Components/
│   ├── CharacterActor.cs              [MB] 组合根，Update 调用链
│   ├── CharacterActor.Debug.cs        [partial] Gizmo 可视化
│   ├── CharacterFrameContext.cs       [struct] 内部数据总线
│   └── CharacterRig.cs               [纯C#] 物理写入入口
├── Config/
│   └── CharacterProfile.cs            [SO] 地面/障碍物参数
├── Animation/
│   ├── Components/AnimationBrain.cs   [MB,EO(-10)] 6层+仲裁入口
│   ├── DriverArbiter.cs              仲裁器(请求队列/生命周期)
│   ├── Drivers/                        LocoDriver(连续) + TraversalDriver(一次性)
│   ├── Requests/AnimationRequest.cs   请求数据(Tags/Resistance/回调)
│   └── Config/                        Alias/Animation/Mode Profiles
├── Input/
│   ├── CharacterInputModule.cs        事件订阅+输入聚合
│   └── SCharacterInputActions.cs      10种输入动作
├── Kinematic/
│   ├── CharacterKinematic.cs          地面(SphereCast)/障碍物/朝向
│   ├── CharacterGroundDetection.cs    SphereCast 单探头
│   ├── CharacterObstacleDetection.cs  前方射线+高度探测
│   ├── CharacterHeadLook.cs           头部偏航/俯仰
│   └── SGroundContact.cs / SForwardObstacleDetection.cs
├── Locomotion/
│   ├── GroundLocomotion.cs            编排器(Motor+Stance)
│   ├── Motor.cs                       速度计算
│   ├── Stance.cs                      Phase/Gait/Posture/Turning
│   └── Config/LocomotionProfile.cs    [SO] 移动参数
├── Enums/
│   └── LocomotionEnums.cs             Phase/Gait/Posture
```

### 稳态调用链
```
CharacterActor.Update()
  → InputModule.ReadActions → ctx.Input
  → CharacterKinematic.Evaluate → ctx.Kinematic (SphereCast地面检测)
  → GroundLocomotion.Simulate → ctx.Motor + ctx.Discrete
  → AnimationBrain.Apply(ctx)
    → DriverArbiter.Resolve
      → EvaluateDrivers ← TraversalDriver 读ctx提交请求
      → ProcessQueue → AcceptRequest(OnStarted) → 中断 → 播放
      → CheckCompletion → OnCompleted → 恢复 LocoDriver
  → GameContext.UpdateSnapshot(snapshot)

AnimationBrain.OnAnimatorMove()
  → SuppressGroundLock? ApplyPosition : ApplyPositionPlanar
```

### 其他子系统
- **EventDispatcher**：解耦消息中心，`Action<TPayload, MetaStruct>` 订阅模式，详见 [eventdispatcher-instructions.md](eventdispatcher-instructions.md)
- **InputManager**：将设备信号转换为 IAction，通过 EventDispatcher 广播，详见 [inputmanager-instructions.md](inputmanager-instructions.md)
- **GameContext**：运行期上下文，存放快照与服务注册，详见 [gamecontext-instructions.md](gamecontext-instructions.md)
- **UI System**：统一管理 Screen/Overlay，只读 GameContext，详见 [ui-instructions.md](ui-instructions.md)

### 关键设计决策
- **Component Driver 模式**：Inspector 可视化，OnEnable 自注册
- **从ctx读输入**：单一订阅点(InputModule)，驱动层只消费 CharacterFrameContext
- **Evaluate 接口**：Driver 非活跃时也能提交请求
- **SphereCast 单探头**：膝盖高度往下探，替代不稳的 BoxCast+Raycast
- **Y轴单一路径**：常态不碰Y，攀爬/落地时 SuppressGroundLock 解锁
- **CharacterRig 统一入口**：SetKinematic + IgnoreCollision + ZeroVelocity 集中管理
- **无全局优先级**：Request 自带 Tags+Resistance，后来者自读

### Struct 设计约定
- 以 `S` 前缀开头，不可变快照（只读字段或仅 get 属性）
- 构造函数完整初始化所有数据
- 不内嵌 MetaStruct，元数据由 EventDispatcher 在发布时统一生成
