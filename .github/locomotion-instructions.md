# Locomotion 子系统说明

## 目标
- 构建基于 RuntimeServiceBase 生命周期的确定性、对设计友好的角色移动管线。
- 保持输入、物理、表现分离：Locomotion 负责消费 IAction 与推进物理，其它系统只需观测快照/事件。

## 架构
### 玩家 Rig 约定
- Player 物体下至少包含 `Model`（渲染/动画）与 `Follow`（摄像机/朝向枢轴）两个子节点。
- 摄像机默认锁定 `Follow` 作为 `Follow`/`LookAt` 目标，旋转摄像机只会驱动 `Follow`，不会直接改变 Player 或 `Model` 的朝向。
- Locomotion 始终以 `Follow` 的水平前向作为角色的“期望前进方向”；即便 `Model` 还未对齐，也根据 `Follow` 的朝向来采样输入与推进速度。
- 当 `Model` 朝向与 `Follow` 不一致时，Locomotion 需要触发转身（补间或立即对齐），确保最终 `Model` 与 `Follow` 同步，避免摄像机与运动矢量脱节。

-### 数据流（Handler → Agent → Manager → GameContext → Adapter）
- 所有原始输入或 AI 指令都以 `S` 前缀的 IAction 结构进入 `LocomotionAgent`。
- Agent 在本地状态机中消化这些 IAction，生成唯一可信的 `SPlayerLocomotion`，并通过 `LocomotionManager` 推送到 `GameContext`。
- 各类 `LocomotionAdapter` 只读 Agent/Manager 提供的快照，驱动 Animator、音频、特效等表现层。

1. **LocomotionManager : RuntimeServiceBase（注册器）**
   - 充当全局注册中心，持有默认调参配置、全局调试开关，并在 `Register(GameContext)` 中完成自身注入。
   - 提供 `RegisterComponent/UnregisterComponent` API，维护所有激活的 `LocomotionAgent`，并按需暴露玩家组件引用（供摄像机、UI 获取）。
   - 汇总组件快照或将玩家快照写回 GameContext，广播跨角色事件（如全局重力调整、群体状态统计）。

2. **LocomotionAgent（每角色挂载）**
   - MonoBehaviour 形式挂在玩家或 AI 身上，持有 CharacterController/Rigidbody、局部配置 Profile 等核心依赖。
   - `OnEnable` 时尝试向 LocomotionManager 注册，若 Manager 尚未初始化则在 Awake 中缓存引用并在下一帧重试。
   - 通过 IAction Handler（例如后续会替换 `IntentHandler<T>` 的实现）订阅玩家输入（Move、Jump 等）或 AI 指令，统一缓存在 Agent 内部；同一 Agent 可以同时拥有多种 Handler。
   - 在 `Update`/`FixedUpdate` 中读取最新 IAction、驱动状态机与地面检测，生成单一可信的 `SPlayerLocomotion`（或通用快照）。
   - 调用 `PushSnapshot()` 把快照同步给 LocomotionManager；需要时 Manager 再写入 GameContext，供其他系统查询。

3. **LocomotionAdapter（参数与表现桥接器）**
   - 独立 MonoBehaviour，作为 LocomotionAgent 的伴随组件，**只读** Agent 推送的快照/IAction 缓存，把速度、加速度、状态标签等映射到 Animancer 动画状态、VFX、音频或 UI；不再负责生成/修改快照。
   - 暴露序列化字段以配置 Animancer 状态或 Transition 资源引用、混合权重/平滑曲线、阈值触发（如落地、加速），并在 `Update`/`LateUpdate` 中执行同步，确保表现调参与核心物理解耦。
   - 可以针对不同表现需求挂载多个 Adapter（动画、脚步音、屏幕震动等）；基类 `LocomotionAdapter` 可直接承担“通用表现桥接”职责，必要时再派生出专用实现（如动画专用、脚步音专用）。
   - 每个 Adapter 仅消费数据，必要时做局部平滑或延迟处理，并可通过 `Logger` / 事件输出调试信息，方便设计师验证表现层逻辑而无需触碰 Agent 状态机。

4. **输入链路**
   - 玩家：Input System 的 `PlayerMoveAction` 等 Handler 发布 IAction → 对应 `LocomotionAgent` 订阅并 `BufferPlayerMoveIAction`（或其它缓存函数）。
   - 视角：`PlayerLookAction` 仍负责驱动摄像机/Follow，但 Locomotion 不再订阅 `SPlayerLookIAction`，只需从 `Follow` Transform 读取结果即可。
   - AI：脚本/行为树直接调用 Agent 提供的 API（如 `SetDesiredDirection(Vector3)`、`InjectIAction`）写入同一套缓存。
   - IAction 结构始终不可变，Agent 是唯一的写入口；Handler、AI 或 Adapter 均不得直接修改 Agent 的状态机数据。

5. **状态机**
   - 状态：`Idle`、`Walk`、`Sprint`、`Airborne`，可扩展 `Slide`、`Climb`。
   - 转换条件依赖地面检测、IAction 强度、体力等标志；额外记录 `Follow` 与 `Model` 的夹角，在需要快速转身（如>120°）时可进入专用 `Turn`/`SnapTurn` 状态。
   - 每个状态定义加速度、最大速度、阻尼及进入/退出事件，并指定如何将 `Follow` 前向映射为 `Model` 的目标前向（直接对齐或缓动）。

### 动画状态概览（基于 Animancer）
- 参考资产：直接使用角色相关的 Loop/Turn/Look 等动画剪辑或 Animancer Transition 资产，默认进入 Idle 动画状态，当角色平面速度大于 0 时切入 Walk/Run 状态；当 `IsTurning` 与 `TurnAngle` 满足阈值时，切换到原地转身相关的动画状态，转身完成或速度恢复为 0 再退回 Idle。
- 基础状态应至少包含：Idle、Walk/Run、Turn In Place、Airborne 等，必要时可拆分更多细分状态（如 Sprint、Slide）。
- 头部/上半身可通过额外的 Animancer 层或局部 Mask（如只影响 Head 骨骼）叠加 Look additive clip，使头部跟随输入而不影响身体，保持与核心 Locomotion 状态解耦。
- LocomotionAdapter 必须与这些状态保持一致：速度来自 `SPlayerLocomotion.Velocity.magnitude`，`IsTurning/TurnAngle` 由 Agent 的朝向差计算；触发时机务必与 Animancer 状态切换条件同步，避免逻辑/表现错位。

### Walk 2D 混合设计（Animancer）
- 现有 `MoveX/MoveY` 只反映玩家输入方向，无法描述 Root Motion 实际速度，因此新增 `PlanarSpeedX`、`PlanarSpeedY` 两个平面速度分量字段，分别对应世界空间（或角色本地）水平/垂直速度分量。
- 速度计算：以 `FollowAnchor` 的水平前向和右向为基，使用规范化输入向量乘以配置化 `MoveSpeed` 得到期望速度，再投影到 `PlanarSpeedX`（右向分量）、`PlanarSpeedY`（前向分量）。角色仍由 Root Motion 推进，只是通过该速度推断混合占比，后续可引入全局 `SpeedMultiplier` 调节不同角色差异。
- 动画侧（Animancer）：Walk 相关动画使用 2D 混合状态（如 Directional Mixer）混合 Forward/Strafe/Back clip；混合参数使用 `PlanarSpeedX/PlanarSpeedY`，从而保证直走、斜走、转弯时的动画比例与实际朝向一致。
- Adapter 任务：除 `MoveX/MoveY` 继续写入输入向量外，新增写入 `PlanarSpeedX/Y`（来源于 `SPlayerLocomotion` 中计算结果），确保表现层与逻辑数据完全对齐。

6. **地面与表面**
   - 每帧执行一次 Raycast/Spherecast，求得地面法线、坡度、材质标签。
   - 以 `SGroundContact` 缓存结果并写入 `SPlayerLocomotion`。
   - 通过 ScriptableObject 配置坡度阈值、台阶高度等容差。

7. **物理集成**
   - 同时支持 CharacterController（默认）与 Rigidbody，可用策略模式或序列化枚举切换。
   - CharacterController 在 `Update` 中驱动，Rigidbody 在 `FixedUpdate` 中施力。
   - 暴露 `OnMoveComputed(Vector3 desiredVelocity)` 等调试回调。

8. **快照与事件**
   - `SPlayerLocomotion` 字段：位置、速度、前向、上向、当前状态、是否贴地、地面法线、坡度角等，由 Agent 在运动计算后组装；前向统一来自 `Follow` 的水平投影，并记录 `Model` 与 `Follow` 的对齐误差，方便 Adapter 做“转身动画”。
   - Agent 调用 `PushSnapshot()` → LocomotionManager `PublishSnapshot()` 缓存 → Manager 视情况把玩家快照写入 `GameContext`。
   - Agent 或 Manager 可通过 EventDispatcher 广播状态事件（开始移动、进入冲刺、落地、离地），Adapter 只做监听而不生成事件。

9. **配置资产**
   - `LocomotionTuningProfile` ScriptableObject 保存各状态速度、加速度、重力覆写等。
   - 可选曲线：加速度-坡度、平滑 Blend 时间等。
   - 通过序列化字段注入 LocomotionManager，保持代码无状态。

10. **调试与工具**
   - Inspector 开关：Gizmo（目标方向、地面法线）、运行时指标（当前速度、坡度）。
   - 提供 Editor `LocomotionDebugger` 面板用于快速查看状态/IAction。

## 实现顺序
1. 定义基础数据结构（`SPlayerLocomotion`、`SGroundContact`），仅包含 Idle/Walk 所需字段。
2. 实现 `LocomotionManager`（注册中心），并建立 `LocomotionAgent` 框架：包含 IAction 缓存、CharacterController 流程、Idle↔Walk 状态判定。
3. 增加最小化 ScriptableObject 配置（步行速度、加速度、地面检测参数），并允许不同组件指派不同 Profile。
4. 玩家组件推送快照到 GameContext，Manager 同步注册，Inspector 中验证 Idle/Walk 状态及速度更新。
5. 在 Idle/Walk 验证稳定后，再按需拓展 Sprint、Airborne、Jump 等高级动作，并为 AI 组件铺设专属输入接口。
