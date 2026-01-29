# Locomotion 子系统说明

## 目标
- 构建基于 RuntimeServiceBase 生命周期的确定性、对设计友好的角色移动管线。
- 保持输入、物理、表现分离：Locomotion 负责消费意图与推进物理，其它系统只需观测快照/事件。

## 架构
1. **LocomotionManager : RuntimeServiceBase（注册器）**
   - 充当全局注册中心，持有默认调参配置、全局调试开关，并在 `Register(GameContext)` 中完成自身注入。
   - 提供 `RegisterComponent/UnregisterComponent` API，维护所有激活的 `LocomotionAgent`，并按需暴露玩家组件引用（供摄像机、UI 获取）。
   - 汇总组件快照或将玩家快照写回 GameContext，广播跨角色事件（如全局重力调整、群体状态统计）。

2. **LocomotionAgent（每角色挂载）**
   - MonoBehaviour 形式挂在玩家或 AI 身上，持有 CharacterController/Rigidbody、局部配置 Profile、动画钩子等。
   - `OnEnable` 时尝试向 LocomotionManager 注册，若 Manager 尚未初始化则在 Awake 中缓存引用并在下一帧重试。
   - 内部维护状态机、地面检测与物理推进逻辑，输出 `PlayerLocomotionStruct`（或通用 `LocomotionSnapshotStruct`）。
   - 区分输入来源：玩家组件订阅 EventDispatcher（`PlayerMoveIntentStruct` 等），AI 组件通过脚本接口直接注入 MoveIntent/DesiredVelocity，最终都调用相同的运动计算。

3. **输入链路**
   - 玩家：`PlayerMoveAction` 发布意图 → 玩家 `LocomotionAgent` 订阅并缓存。
   - AI：脚本/行为树直接调用组件方法（如 `SetDesiredDirection(Vector3)`）或设置虚拟意图 Struct。
   - 所有意图结构都是只读 DTO，确保与核心运动逻辑解耦。

4. **状态机**
   - 状态：`Idle`、`Walk`、`Sprint`、`Airborne`，可扩展 `Slide`、`Climb`。
   - 转换条件依赖地面检测、意图强度、体力等标志。
   - 每个状态定义加速度、最大速度、阻尼及进入/退出事件。

5. **地面与表面**
   - 每帧执行一次 Raycast/Spherecast，求得地面法线、坡度、材质标签。
   - 以 `GroundContact` Struct 缓存结果并写入 `PlayerLocomotionStruct`。
   - 通过 ScriptableObject 配置坡度阈值、台阶高度等容差。

6. **物理集成**
   - 同时支持 CharacterController（默认）与 Rigidbody，可用策略模式或序列化枚举切换。
   - CharacterController 在 `Update` 中驱动，Rigidbody 在 `FixedUpdate` 中施力。
   - 暴露 `OnMoveComputed(Vector3 desiredVelocity)` 等调试回调。

7. **快照与事件**
   - `PlayerLocomotionStruct` 字段：位置、速度、前向、上向、当前状态、是否贴地、地面法线、坡度角等。
   - 每次物理计算完成后调用 `GameContext.UpdateSnapshot(locomotionStruct)`。
   - 通过 EventDispatcher 广播状态事件（开始移动、进入冲刺、落地、离地）。

8. **配置资产**
   - `LocomotionTuningProfile` ScriptableObject 保存各状态速度、加速度、重力覆写等。
   - 可选曲线：加速度-坡度、平滑 Blend 时间等。
   - 通过序列化字段注入 LocomotionManager，保持代码无状态。

9. **调试与工具**
   - Inspector 开关：Gizmo（目标方向、地面法线）、运行时指标（当前速度、坡度）。
   - 提供 Editor `LocomotionDebugger` 面板用于快速查看状态/意图。

## 实现顺序
1. 定义基础数据结构（`PlayerLocomotionStruct`、`GroundContactStruct`），仅包含 Idle/Walk 所需字段。
2. 实现 `LocomotionManager`（注册中心），并建立 `LocomotionAgent` 框架：包含意图缓存、CharacterController 流程、Idle↔Walk 状态判定。
3. 增加最小化 ScriptableObject 配置（步行速度、加速度、地面检测参数），并允许不同组件指派不同 Profile。
4. 玩家组件推送快照到 GameContext，Manager 同步注册，Inspector 中验证 Idle/Walk 状态及速度更新。
5. 在 Idle/Walk 验证稳定后，再按需拓展 Sprint、Airborne、Jump 等高级动作，并为 AI 组件铺设专属输入接口。
