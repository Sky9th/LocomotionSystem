# LocomotionSystem - 子系统评价

---

## 1. 核心基础设施

### GameManager（引导器）
**评分：★★★★☆**

**优点：**
- 4 阶段确定性引导（Register → Attach → Activate → Ready）设计清晰，避免了 Unity Awake/Start 顺序的不确定性
- 自动发现机制（`GetComponentsInChildren<BaseService>`）支持热插拔服务，添加新服务无需修改引导代码
- `[DefaultExecutionOrder(-500)]` 确保最先执行
- 详细的 Logger 日志覆盖每个阶段

**问题：**
- `registeredServices` 列表与 `GameContext.serviceRegistry` 存在数据冗余，两者独立维护但预期一致
- `RegisterService()` 返回 bool 但失败后继续执行，可能导致部分服务注册成功而部分失败的中间状态
- `AttachDispatcherToServices()` 中如果 `eventDispatcher.IsRegistered` 为 false 则全部跳过，但没有回滚已完成注册的服务

**建议：**
- 引入 `BootstrapResult` 记录成功/失败状态
- 失败时提供 `Rollback()` 清理机制

---

### GameContext（服务注册表 + 快照缓存）
**评分：★★★★☆**

**优点：**
- 双重职责设计巧妙：服务定位 + 结构体快照缓存，统一对外接口
- `TryGetSnapshot<T>()` 配合 `where T : struct` 约束限制，确保用法正确
- `TryResolveService<T>()` 返回 bool + out 参数模式，类型安全

**问题：**
- `UpdateSnapshot<T>()` 直接覆盖无版本控制，消费者无法感知快照是否已被消费
- `serviceRegistry` 和 `contextSnapshots` 均无线程安全保护（虽当前非必需，但若未来引入 Jobs 需要重新审视）
- `IsInitialized` 的 setter 在 `OnDestroy` 中设置为 false 但 `Initialize()` 有幂等保护，而 `Awake()` 中无相应处理

**建议：**
- 为快照增加帧计数器或版本号
- 明确文档标注单线程使用约束

---

### EventDispatcher（事件总线）
**评分：★★★★☆**

**优点：**
- 泛型 `Subscribe<TPayload>/Publish<TPayload>` 提供完整类型安全
- 自动附加 `MetaStruct`（时间戳 + 帧索引）到每个事件
- `inspectorListeners` 调试显示，运行时可见订阅状态
- 继承 `BaseService`，融入有序引导流程

**问题：**
- `Publish()` 中 `handlers.ToArray()` 每帧每事件类型分配一次数组，在大量事件发布时产生 GC 压力
- 无事件优先级或顺序保证（按注册顺序），对于有顺序依赖的场景不够灵活
- `Clear()` 方法为 public 但无使用场景，存在误用风险

**建议：**
- 考虑使用 `List<Delegate>` + `for` 循环 + 安全修改列表方案替代 `.ToArray()`
- 将 `Clear()` 改为 `internal` 或移除

---

### BaseService（服务生命周期）
**评分：★★★★★**

**优点：**
- 精心设计的生命周期 Hook 体系：`OnRegister` → `OnDispatcherAttached` → `OnSubscriptionsActivated` → `OnServicesReady`
- `serviceCache` 跨服务依赖缓存避免重复解析
- `RequireService<T>()` vs `TryResolveService<T>()` 区分强制和可选依赖
- 与 `GameManager` 的四阶段引导严格对应

**问题：**
- `serviceCache` 无失效/刷新机制，如果某个服务被替换（当前不会发生），缓存会变脏
- `RequireService<T>()` 返回 null 时调用方需自行判空，不如抛异常直观

**建议：**
- 考虑返回 `bool` 或使用 C# nullable ref types 标注

---

## 2. 输入系统

### InputManager + InputActionHandler
**评分：★★★★☆**

**优点：**
- ScriptableObject 驱动的输入处理程序，设计师可在 Inspector 中配置
- `EnforceHandlerStatePermissions()` 统一管理 GameState 对应的输入启用/禁用
- 延迟初始化模式（`InitializeHandler` → `Enable` → `Dispose`），生命周期清晰

**问题：**
- `EnforceHandlerStatePermissions()` 内部嵌套多个 if 检查，逻辑分支多且难以测试
- 每个 InputActionHandler 独立订阅 `InputAction.performed`/`canceled`，大量处理程序时可能影响性能（Unity InputAction 回调在 native 层）
- `IAPlayerJump` 中的 `Logger.Log` 属调试残余，生产环境应移除

**建议：**
- 将 `SupportsState` 的判断提前到 `InputManager` 层面统一处理，减少每个 Handler 的重复逻辑
- 统一移除 Handler 子类中的调试日志

---

### LocomotionInputModule
**评分：★★★☆☆**

**优点：**
- 内聚良好，封装了 Agent 需要的所有输入订阅
- `ReadActions()` 后自动清空单帧信号（`IsRequested/IsReleased`），避免状态残留
- `lastMoveAction` 机制解决了 MoveAction 在当前帧为空时回退到上一帧

**问题：**
- `PutAction<T>()` 使用 9 个 `typeof()` 分支 + `(object)` 装箱，每帧多类型事件会有分配开销
- 动态委托注册模式（`RegisterAction<TPayload>` 内定义局部函数）在 IL2CPP 下可能有兼容性问题
- `hasCameraControl` 仅在 `owner.IsPlayer` 为 true 时更新，非玩家 Agent 始终得不到相机数据——这是有意设计但未在文档中说明

**建议：**
- 如果是 IL2CPP 目标平台，验证局部函数在泛型方法中的行为
- 考虑使用 `SwitchExpression`（C# 8.0+）或类型映射 Dictionary 替代。

---

### 输入 DTO 设计
**评分：★★★★★**

**优点：**
- `SButtonInputState` 的四态模型（`IsPressed`/`IsRequested`/`IsReleased`/Phase）精确表达了输入语义
- 所有 DTO 为不可变结构体（`readonly struct`），线程安全且零堆分配
- 每个 DTO 提供静态 `None` 和 `CreateEvent()` 工厂方法，确保一致构造
- `ClearFrameSignals()` 方法支持两阶段消耗（先读单帧信号，然后清零）

**问题：**
- 无明显问题

---

## 3. 运动引擎

### LocomotionMotor
**评分：★★★★☆**

**优点：**
- 职责清晰：运动学计算 + 地面/障碍物探针 + Transform 修正
- 速度平滑机制简单有效：`MoveTowards` 基于加速度限制
- 地面稳定化管线设计精致：raw → accumulate → debounce → stabilized
- 支持根运动位置和旋转的独立应用

**问题：**
- `EvaluateGroundContactAndApplyConstraints()` 在计算 `correctedPosition` 时直接修改了 `actorTransform.position`，此修改在 `Evaluate()` 方法中途发生，使得 `position` out 参数与后续使用的 `position` 变量不一致
- `UpdateFreezePositionY()` 依赖 `actorTransform.GetComponent<Rigidbody>()`（构造函数中获取），如果运行时 Rigidbody 被替换，引用会过时
- `StabilizeGroundContact()` 使用 `reacquireDebounceDuration` 的防抖逻辑在当前默认值为 0 时始终可重新着地，实际效果等同于无防抖

**建议：**
- 将 Transform 修改集中到 `Evaluate()` 末尾执行
- 将 `actorRigidbody` 的获取从构造函数移到动态查找或注入

---

### LocomotionGroundDetection
**评分：★★★★☆**

**优点：**
- 两步检测法设计合理：BoxCast（稳定性） + Raycast（距离测距）
- 斜面判定通过法线与向上的夹角实现，准确简洁
- `IsWalkableSlope()` 提取为独立方法，可在障碍物检测中复用

**问题：**
- `EvaluateGroundContact()` 中射线未命中时直接返回 `SGroundContact.None`，即使 BoxCast 已命中——丢失了着地状态信息
- 两个检测使用不同的 LayerMask（都使用 `layerMask` 参数），但逻辑上 Ray 和 Box 可能应对不同层级（如忽略角色自身的 collider）
- 缺少对角色自身碰撞体的显式排除

**建议：**
- 射线未命中但 Box 命中时，距离设置为 0 而非丢弃整个结果

---

### LocomotionObstacleDetection
**评分：★★★★☆**

**优点：**
- 简洁的前方射线 + 顶部下探双重检测
- 正确区分斜坡和障碍物（基于法线角度）
- 高度探针使用 `maxClimbHeight + padding` 确保覆盖

**问题：**
- `canVault` 和 `canStepOver` 始终为 `false`，未实现
- 障碍物判定仅基于单次射线，对于复杂几何（如栏杆、半高墙）可能产生误判
- `HeightProbeForwardInset = 0.05f` 硬编码，可能不适用于所有障碍物类型

**建议：**
- 考虑增加 ShapeCast 或多重射线提高鲁棒性
- 将 `canVault`/`canStepOver` 的判定参数化到 `LocomotionProfile`

---

### LocomotionKinematics
**评分：★★★★★**

**优点：**
- 简洁清晰：4 个核心函数各司其职
- 局部/世界空间速度转换基于正交基构造，数学正确
- 有符号转向角度计算使用 `Vector3.SignedAngle`，简洁高效
- `SmoothVelocity` 提供 2D 和 3D 重载

**问题：**
- `ComputeDesiredPlanarVelocity()` 在 `moveAction.HasInput` 为 false 时返回 zero，未区分"无输入"和"输入为零"的情况

---

### LocomotionHeadLook
**评分：★★★★☆**

**优点：**
- 基于旋转矩阵的局部空间转换方案数学正确
- 偏航/俯仰的 clamp 受 `LocomotionProfile` 参数控制

**问题：**
- `Evaluate()` 中 `modelRoot.forward` 用于构建 bodyRotation，但 modelRoot 可能与 root 的旋转不同步（根运动延迟），导致一帧偏差
- 返回的 Vector2(yaw, pitch) 中 pitch 做了取反（`pitch = -euler.x`），但未在注释中说明原因

---

## 4. 离散运动状态系统

### LocomotionGraph + Aspects
**评分：★★★★☆**

**优点：**
- Aspect 模式将正交维度完全解耦，添加新状态维度无需修改现有 Aspect
- 每个 Aspect 接口单一：`ILocomotionAspect<TState>` 提供 `Update` + `Reset`
- `LocomotionGraph.Evaluate()` 按固定顺序更新 Aspect，行为可预测

**问题：**
- **GaitAspect 存在 bug**：`MoveAction.Phase == Canceled` 时设置 Gait 为 Idle，但 `Performed` 阶段如果 sprint/run 切换仅做 toggle，意味着从 sprint 切换到 run 需要明确按键。同时 Walk/Run 按钮未集成到 GaitAspect — `WalkAction` 和 `RunAction` 完全被忽略
- `SLocomotionInputActions` 包含 `WalkAction` 和 `RunAction` 但 GaitAspect 从未使用它们
- `PhaseAspect` 未实现 `Landing` 阶段，始终只有 GroundedIdle/GroundedMoving/Airborne
- Condition 始终为 `ELocomotionCondition.Normal`，无任何逻辑改变它

**建议：**
- 修复 GaitAspect 以消费 Walk/Run 按钮
- 将每个 Aspect 的特定输入绑定逻辑与 Aspect 本身分离（或至少确保一致性）

---

### LocomotionTurningGraph
**评分：★★★★★**

**优点：**
- 清晰的时间稳定化逻辑：朝向必须稳定 `lookStabilityDuration` 秒才能触发转向
- 进入/退出双阈值（`turnEnterAngle` 65° / `turnCompletionAngle` 5°）防止抖动
- 仅在 GroundedIdle 或 GroundedMoving 时允许 Turning 标志

**问题：**
- `turnDebounceDuration` 参数已在 `LocomotionProfile` 中定义但未被使用
- `lastDesiredYaw` 在 Reset 后首次帧计算 `yawDelta` 使用 0f，可能导致虚高的 delta

**建议：**
- 移除未使用的参数或实现其逻辑

---

### LocomotionTraversalGraph
**评分：★★★☆☆**

**优点：**
- 清晰的 4 阶段生命周期模型
- 与 Jump 按钮联动触发
- 可以同时输入跳跃和移动（不要求静止）

**问题：**
- `DefaultCommittedDuration = 0.45f` 硬编码，未参数化
- `TryBuildTraversalRequest()` 每次都创建新的 `SLocomotionTraversal`，即使条件不满足
- Completed/Canceled 状态仅持续一帧（`clearTerminalStageNextFrame`），消费者必须在过渡帧内读取
- 穿越后 `CreateActionControlled()` 锁定 Phase 为 GroundedIdle 但未考虑空中穿越场景

**建议：**
- 参数化 Committed 持续时间
- 使用对象池或延迟构造避免分配
- 考虑让 Completed/Canceled 持续至少 2 帧以便 UI 展示

---

### LocomotionCoordinatorBase
**评分：★★★★☆**

**优点：**
- 清晰的模板方法模式：`Evaluate()` 依次委托给 Graph/TraversalGraph/TurningGraph
- 穿越 Committed 时覆盖离散状态为 `CreateActionControlled()`，正确处理优先级

**问题：**
- `Evaluate()` 中先 update `currentState`（Graph），再被 traversal override 覆盖，再被 turning 修改——三次状态写入，增加了理解成本
- `LocomotionDiscrete.Structs` 命名空间与 `SLocomotionDiscrete` 放在 `Discrete.Structs` 下，但 `SLocomotionDiscrete.CreateActionControlled()` 返回新实例是纯函数——结构合理但可考虑移到工厂类

---

## 5. 动画系统

### LocomotionAnimancerPresenter
**评分：★★★★☆**

**优点：**
- 清晰的 MonoBehavior 桥接层，将 Agent 的快照传给纯 C# 动画控制器
- 根运动转发通过 `OnAnimatorMove()` 精确集成到 Motor
- `BuildAnimationSnapshot()` 从层收集快照的方式独立且可测

**问题：**
- `Start()` 中创建 `LocomotionAnimationController` 的依赖注入完全硬编码（顺序：base=0, head=1, foot=2），如果 Animancer 层被外部修改会出错
- 如果 Agent 在 Start 后重新创建（如 Respawn），`controller` 不会重新初始化

**建议：**
- 提取初始化逻辑到独立方法，支持重新初始化
- 层索引从常量或配置文件读取

---

### BaseLayer FSM
**评分：★★★★☆**

**优点：**
- 7 个状态覆盖了运动动画的主要场景
- 状态转换采用 `TrySetState()` + 条件检查模式，避免状态间的隐式转换
- `TurnAngleStepRotationApplier` 在 Turning/Moving 状态中一致驱动模型旋转

**问题：**
- **状态转换逻辑分散**：`TrySetState()` 调用在 Tick 中按顺序排列，隐式定义了优先级，但此优先级未在任何地方明确说明
- `BaseMovingState.UpdateMovementMixerParameterIfNeeded()` 直接访问 `Owner.Layer.CurrentState` 检查是否为 `Vector2MixerState`，这种外部检测方式脆弱——如果别名引用的是非 Mixer 剪辑则静默失败
- `BaseTurnInMovingState.IsForwardOnlyIntent()` 重复了 `CanEnterTurnInMovingStateCondition` 的前向意图检查逻辑
- `CanEnterAirLoopState` 仅基于 `!IsGrounded` 但 `CanEnterState` 始终返回该值，意味着从任何状态都可能跳入空中（包括已经在地面上的 Idle 状态）
- 许多 Logger.Log 调用为调试残余

**建议：**
- 将状态转换逻辑抽取到独立的条件匹配表
- 合并重复的前向意图检查逻辑

---

### 条件系统
**评分：★★★★★**

**优点：**
- 泛型 `ICheck<TContext>` + `And/Or/Not` 组合器设计优雅
- 所有条件为 `readonly struct`，零堆分配
- `CheckExtensions.Check<TCheck>()` 提供流畅的调用语法
- 每个条件都是单文件、单结构体，职责极其单一

**问题：**
- `AndCondition`/`OrCondition` 支持两个操作数，若需要三个条件需嵌套使用，表达式会变得冗长
- `default(TLeft).Evaluate()` 模式依赖 struct 的无参构造，但如果 struct 添加了参数化构造，行为可能改变

---

### HeadLookLayer
**评分：★★★★☆**

**优点：**
- Vector2Mixer 驱动方案正确，X=yaw, Y=pitch
- 平滑处理避免头部抖动
- 使用 LocomotionProfile 的头部限制映射归一化参数

**问题：**
- `mixerInitialized` 初始化将子动画 Speed 设为 0、Weight 设为 1，这假定 Mixer 子动画按顺序排列（Up/Down/Left/Right），但未验证子动画数量或顺序
- `smoothedYaw` 和 `smoothedPitch` 在每帧结束时保留状态，但如果 `context` 切换到不同角色，这些值可能不匹配

---

### FootLayer
**评分：★☆☆☆☆**

**问题：**
- 完全为存根实现：`Update()` 不播放任何动画，仅输出空快照
- 注释中有残留的测试代码：`Layer.TryPlay(alias.runUp)` 被注释掉
- 即使未来实现，当前设计缺乏脚部 IK 集成和地面适应性

---

## 6. 相机系统

### CameraManager
**评分：★★★★☆**

**优点：**
- Cinemachine 集成干净，使用 VirtualCamera 的 Follow/LookAt 绑定
- Anchor 位置跟随 Agent 位置 + verticalOffset，Anchor 旋转由鼠标 Look 驱动
- Pitch 限制（maxPitchDegrees）使用归一化角度（NormalizeAngle180），处理正确

**问题：**
- `LateUpdate` 和 `Update` 中都发布 `SCameraContext`——`TickLocalPlayerAnchor` 在 Update 中发布一次，`PushCameraSnapshotToContext` 在 LateUpdate 中写入 GameContext。两者可能有轻微时间偏差
- Anchor Y 坐标锁定（`anchorLockedY`）逻辑仅在 `HandleLocomotionSnapshot` 首次触发时锁定，之后即使角色位置改变也不再更新——可能导致相机在角色攀爬时位置错误
- 相机旋转直接修改 Anchor 的 `eulerAngles`，跨 180° 边界时可能出现万向节锁问题

**建议：**
- 统一相机快照的发布时间点为 LateUpdate
- 考虑使用 Quaternion.Slerp 而非直接 Euler 角度操作

---

## 7. UI 系统

### UIManager
**评分：★★★★☆**

**优点：**
- 清晰的 Screen（独占）vs Overlay（可叠加）模式
- ScriptableObject 驱动的配置（UIScreenConfig / UIOverlayConfig）
- OnEnter/OnExit/OnShow/OnHide 生命周期回调完整
- GameContext 注入到每个 UI 元素

**问题：**
- `BuildScreenLookup()` 和 `BuildOverlayLookup()` 大量重复代码（~90% 相似）
- UI 元素通过 Instantiate 创建，但未在 `OnDestroy` 中清理，可能导致残留
- 缺少焦点管理（谁在接收导航输入）

---

### LocomotionDebugOverlay
**评分：★★★★★**

**优点：**
- 完整的实时运动状态可视化，覆盖所有 Motion/Ground/Obstacle/Animation 字段
- 支持单 TextMeshPro 和三分割布局（summary + left + right）
- 刷新间隔可配置，降低性能影响
- `FormatBool`/`FormatDistance` 等静态本地函数，格式化清晰

**问题：**
- 格式化代码冗长（~300 行），与数据访问混合
- `RenderDockedLayout` 将格式化函数作为委托传递，在每帧 Refresh 中产生分配

---

## 8. 角色动画请求系统

### CharacterAnimationController + Drivers
**评分：★★★☆☆**

**概述：** 此系统与 Locomotion 动画系统（LocomotionAnimancerPresenter）**完全独立**，代表着两套并行的动画方案。当前实现看起来是早期架构实验的产物。

**优点：**
- 4 通道模型（FullBody/UpperBody/Additive/Facial）设计合理
- Driver 模式将业务逻辑与播放解耦
- `CharacterAnimationRequest` 支持 Clip 和 Alias 两种播放方式

**问题：**
- **双重动画架构**：`CharacterAnimationController` 使用 `ICharacterAnimationDriver`，`LocomotionAnimancerPresenter` 使用 `ILocomotionAnimationLayer`——两者都管理 Animancer 层，存在**潜在的层索引冲突风险**
- `CharacterAnimationController.ConfigureRuntimeLayers()` 直接访问 `animancer.Layers[(int)channel]`，使用的索引与 `LocomotionAnimancerPresenter.Start()` 中的索引完全可能重叠
- `FullBodyCharacterAnimationDriver` 仅处理 full body，其他通道无实现
- `CharacterAnimationController` 未被任何现有代码使用（仅在 `Character.cs` 组件中被引用，但该组件未在任何地方被实例化）

**建议：**
- 考虑统一两套动画架构，或将 Locomotion 动画作为 CharacterAnimationController 的一个 Driver 实现
- 如果决定废弃此方案，应清理相关代码

---

## 9. 工具类

### Logger
**评分：★★★★☆**

**优点：**
- 自动识别数据类型并格式化（简单类型、Unity 对象、字典、枚举、结构体）
- `MaxDepth` 和 `Cyclic` 检测防止无限序列化
- `ReferenceEqualityComparer` 用于 visited set，正确处理引用相等
- 支持 pretty-print 和压缩格式

**问题：**
- 反射序列化（`SerializeWithReflection`）在运行时开销大，仅应用于调试
- 格式化为字符串并传递给 `Debug.Log` 意味着即使日志级别被过滤，格式化已发生

---

### GizmoDebugUtility
**评分：★★★★★**

**优点：**
- 清除了 `LocomotionAgent.Debug.cs` 中的 Gizmo 绘制逻辑
- 箭头线（`DrawArrowLine`）、球体（`DrawSphere`）、线框盒（`DrawWireBox`）API 一致
- `#if UNITY_EDITOR` 保护确保不在构建中包含

---

## 总结评分

| 子系统 | 评分 | 核心评价 |
|---|---|---|
| 核心基础设施 | ★★★★☆ | 架构设计精良，缺少错误回滚 |
| 输入系统 | ★★★★☆ | SO 驱动可配置，输入聚合可有优化 |
| 运动引擎 | ★★★★☆ | 地面检测完善，GroundContact 检测修复后更佳 |
| 离散状态系统 | ★★★★☆ | Aspect 模式优秀，GaitAspect 有逻辑缺陷 |
| 动画系统 (BaseLayer FSM) | ★★★★☆ | 状态覆盖全面，转换逻辑可更系统化 |
| 动画系统 (FootLayer) | ★☆☆☆☆ | 存根实现，尚未开发 |
| 动画条件系统 | ★★★★★ | 泛型组合器设计杰作 |
| 相机系统 | ★★★★☆ | Cinemachine 集成良好，双时间点发布需统一 |
| UI 系统 | ★★★★☆ | Screen/Overlay 模式清晰，代码重复较多 |
| 调试 UI | ★★★★★ | 实时可视化完善 |
| 角色动画请求系统 | ★★★☆☆ | 与运动动画架构冲突，需抉择 |
| Logger | ★★★★☆ | 功能完善，反射路径性能需关注 |

**整体评价：** 这是一套架构设计出色的运动系统，在服务生命周期管理、事件驱动通信、Aspect 模式、不可变 DTO 等方面展现了成熟的工程思维。主要改进空间在于：GaitAspect 的输入消费 bug、地面检测边缘情况、双重动画架构的整合、以及 FootLayer 的补全。
