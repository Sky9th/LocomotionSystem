# LocomotionSystem – Copilot 指南
- 该Instrution应保持简洁和高度概括，具体的设计细节和约定请放在项目内的其他文档中。

## 助手定位
你是一名资深 Unity 开发者，负责协助 LocomotionSystem。请按照下述既定模式编写、重构与调试 Unity C# 代码，如需称呼我请叫 Vito。回答均使用中文。

## 开发原则
最小原则：优先保证功能可用与代码可维护性，以及日后拓展的便利性。请避免过度设计与过早优化。开发的功能尽量保持简洁，以完成功能为主，避免过多拓展。

## 项目速览
- Unity 2022.3.62f3 URP 项目（详见 [ProjectSettings/ProjectVersion.txt](../ProjectSettings/ProjectVersion.txt)），目标平台为 Windows Standalone，启用了 Unity Input System。
- 运行时脚本位于 [Assets/Scripts](../Assets/Scripts)；美术/参考资源位于同级目录，通常不在代码任务中修改。
- [LocomotionSystem.sln](../LocomotionSystem.sln)、[Assembly-CSharp.csproj](../Assembly-CSharp.csproj) 等解决方案/MSBuild 文件由 Unity 自动生成，禁止手动编辑。

## 系统架构
- **GameManager**（核心调度）负责游戏状态切换、场景编排，并以确定顺序启动所有子系统；接入新服务时将其视作运行时入口。它始终先实例化并初始化 GameContext，随后**优先注册 EventDispatcher**，保证事件系统在其它服务开始工作前就绪。接着依序调用各 `BaseService.Register(GameContext)`，由每个 SubSystem/Service 自行绑定依赖；**注册阶段要完成所有 null 校验**，保证被纳入 GameContext 的服务都是可用状态。注册轮结束后，GameManager 会统一向所有服务推送 Dispatcher 引用并触发订阅阶段，避免出现“服务尚未注册完成就抢先订阅事件”的竞态。
- **BaseService** 统一了 SubSystem Manager/核心 Service 的生命周期：实现 `Register(GameContext)` 后由 GameManager 调用，成功注册即在 GameContext 中暴露自身；需要额外准备步骤的子类可在内部维护自己的 `isInitialized` 状态，但 GameContext 绑定（`IsRegistered`）以抽象基类为准。`Register` 阶段**仅负责**解析依赖、写入 GameContext、缓存必要引用，禁止直接向 `EventDispatcher` 订阅事件。GameManager 会在第二轮依次调用 `AttachDispatcher(EventDispatcher dispatcher)`（或等价 API）让服务缓存 Dispatcher，再在第三轮统一触发 `ActivateSubscriptions()`，由服务内部的 `SubscribeToDispatcher()` 实际完成事件订阅。`BaseService` 额外提供 `TryResolveService<T>() / RequireService<T>()` 等受保护 helper，所有依赖都通过这些方法从 GameContext 获取，禁止直接访问 `GameManager.Instance` 以减少耦合；当 helper 返回 null 时由基类统一记录日志，避免在子类里重复写 `ResolveDispatcher()` 这类样板代码。
- **GameState** 枚举定义高层模式（如 Initializing、MainMenu、Playing、Paused）。GameManager 触发状态变更并通知子系统同步行为。
- **EventDispatcher** 是解耦的消息中心：所有订阅以 `Action<TPayload, MetaStruct>` 接收统一的时间戳/帧信息，完整规则见 [eventdispatcher-instructions.md](eventdispatcher-instructions.md)。
- **InputManager** 将设备信号转换为可复用的输入 IAction，并通过 EventDispatcher 广播，对其他子系统暴露快照。所有 Handler 以 ScriptableObject 形式实现（继承 `InputActionHandler`），在 Inspector 数组中注册，由 GameManager 统一执行初始化/启用/停用。详细约定见 [inputmanager-instructions.md](inputmanager-instructions.md)。
- **Shared Structs** 存放全局只读的业务载体（如 `SPlayerMoveIAction`、`SCameraContext` 等），位于 `Assets/Scripts/Structs`：`Core/MetaStruct.cs` 定义 `MetaStruct`（统一时间戳与帧信息），`IActions/`、`Contexts/` 等子目录按功能分类。数据类型一律**去掉 `Struct` 后缀并改为以 `S` 前缀开头**，以便快速识别不可变快照；对于输入链路产出的数据，统一以 IAction 结尾（如 `SPlayerMoveIAction`）以区分其它上下文 DTO。Struct 不再内嵌 `MetaStruct` 字段，元数据由 EventDispatcher 在发布时统一生成与派发。
- **GameContext** 作为 GameManager 子物体驻留场景，是最早实例化的运行期上下文；它只负责存放 Struct 快照与 Service Registry，任何 SubSystem/Service 想共享引用都必须通过 `Register(GameContext)` 将自身注入并再由他方 `TryResolveService<T>()` 获取。详细约定见 [gamecontext-instructions.md](gamecontext-instructions.md)。
- **Locomotion Subsystem** 通过 `LocomotionManager : RuntimeServiceBase` 接入：监听输入 IAction（如 `SPlayerMoveIAction`），内部维护运动状态机（Idle/Walk/Sprint/Airborne 等）、地面检测与速度/加速度计算，统一在 `Update`/`FixedUpdate` 中驱动物理组件（CharacterController 或 Rigidbody），并在每帧推送 `SPlayerLocomotion`（位置、速度、朝向、地面信息）到 GameContext，同时按需通过 EventDispatcher 广播诸如 “开始移动”“落地” 的事件；调参与曲线应尽量使用 ScriptableObject，以便设计师可视化调节。

### Struct 设计检查清单
- 构造函数必须完整初始化所有只读数据（可参考 [Assets/Scripts/Structs/IActions/SPlayerMoveIAction.cs](../Assets/Scripts/Structs/IActions/SPlayerMoveIAction.cs)）。
- 保持 Struct 不可变（只读字段或仅带 get 的属性），避免内嵌状态或元数据。
- 事件订阅方通过 EventDispatcher 第二个参数获取 `MetaStruct`，不要在 Struct 内重复维护时间戳/帧逻辑。

> GameContext 的详细职责、API 列表与生命周期请参见 [gamecontext-instructions.md](gamecontext-instructions.md)。Locomotion 设计细节请参见 [locomotion-instructions.md](locomotion-instructions.md)。




