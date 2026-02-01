# InputManager Guidance

## Overview
- `Assets/Scripts/Inputs/InputManager.cs` 是 Unity 新输入系统与游戏逻辑之间的唯一桥梁，负责把控制器/键鼠信号转换为可复用的「输入 IAction」数据（简称 IAction），并确保这些 IAction 在每帧都能被稳定采样。
- `GameManager` 在启动阶段实例化并注册 `InputManager`，同时把它挂接到 `EventDispatcher`，以保证所有输入事件都通过消息系统传播且遵循统一的激活/停用顺序。
- `InputManager` 严禁直接操作 Transform、Physics 或 Animator；它只负责整理数据并发送事件/快照。

## Folder Structure（来自 InputManager design）
1. `Assets/Scripts/Inputs/Actions/`
	- 存放 `.inputactions` 生成的 C# 包装类或针对特定 Action Map 的手写辅助脚本。
	- 采用 `Actions/<ActionMap>/<ActionName>Action.cs` 层级命名，例如 `Assets/Scripts/Inputs/Actions/Player/MoveAction.cs`、`Assets/Scripts/Inputs/Actions/Player/LookAction.cs`。如需继续细分（例：Ground/Air），应延伸为 `Actions/Player/Ground/MoveAction.cs` 以保持层次清晰。
2. Action 抽象层
	- 所有 Action 均继承自同一抽象基类，至少实现 `void Execute(InputAction.CallbackContext context)`（命名可根据具体基类约定调整）。
	- 抽象基类负责：缓存 InputManager 传入的上下文（相机、姿态依赖等）、提供 Enable/Disable 生命周期方法，并暴露 `Dispose()` 清理钩子。事件派发权留在 InputManager。
	- 具体 Action 只做输入解析 → 数据结构封装 → 通过委托或接口把数据回传给 InputManager，禁止直接触碰场景对象或自行访问 `EventDispatcher`。
3. `Assets/Scripts/Structs/`
	- 存放可跨子系统复用的只读数据结构（IAction、上下文快照等）。根目录下的 `Core/MetaStruct.cs` 定义 `StructMeta`（统一的时间戳/帧信息），业务 DTO 按子文件夹分类，其中「IAction」目录替代旧的 `Intents/`。所有结构体统一移除 `Struct` 后缀，改用 `S` 前缀（例如 `IActions/SPlayerMoveIAction.cs`）以提升可读性。
	- Input 层仅负责生成这些 DTO，写入逻辑保持单向；其余系统只读，避免出现“读取后反写输入模块”的双向依赖。
4. `Assets/Scripts/Inputs/InputManager.cs`
	- 集中管理所有 Action Map，负责启用/禁用 Action、订阅回调并将处理好的数据转发给 `EventDispatcher`。
	- `InputManager` 是 `EventDispatcher` 的唯一出入口：它监听各 Action 的输出并统一广播事件，确保消息顺序和订阅点可追踪。
	- 场景中始终仅有一个 `InputManager` 组件挂载在 GameObject 上；它由 `GameManager` 驱动，随场景切换保持引用一致。
	- `InputManager` 不直接引用具体某个 Action（如 `PlayerMoveAction` 字段），而是维护一个抽象的动作集合（字典或序列）。这样可以通过配置注册所有动作，并对它们执行统一的 `Initialize/Enable/Disable/Dispose` 调度。
	- 推荐封装：
		1. 序列化一个 `InputActionHandler[]` 或 ScriptableObject 列表，便于在 Inspector 中挂载所有需要的动作。
		2. 在 `InputManager.Initialize` 内遍历集合，把通用上下文（相机、事件回调）传给每个动作。
		3. `EnableActions/DisableActions` 里循环整套集合，避免漏启或漏停。

## InputActionHandler 运行规范
- 当前实现（[Assets/Scripts/Inputs/Actions/InputActionHandler.cs](../Assets/Scripts/Inputs/Actions/InputActionHandler.cs)）以 ScriptableObject 形式持有 `InputActionReference`，`InitializeHandler` 会：
	1. 校验 `EventDispatcher`，并缓存到 `eventDispatcher` 字段。
	2. 解析引用得到的运行时 `InputAction`，为其注册 `performed/canceled` → `Execute` 回调。
	3. 将 `IsContextBound` 置为 true，便于 `InputManager` 判断是否可以启用。
- `Enable()`/`Disable()` 必须只由 `InputManager` 调用，它们负责 `InputAction.Enable/Disable` 并维护 `IsEnabled`，确保 ScriptableObject 不会在 Editor 中处于未知状态。
- `Dispose()` 既会取消订阅 Input System 回调，也会清空 `eventDispatcher` 引用；关闭场景或更换 Action 时必须调用以防止重复订阅。
- 新增 Action 时：
	1. 继承 `InputActionHandler` 并实现 `Execute()`。
	2. 在 `Execute()` 内用 `context.ReadValue<T>()` 采样输入，对采集值进行滤波/归一化，然后组装成 IAction 结构（遵循 `S` 前缀命名）。
	3. 通过 `eventDispatcher.Publish(iaction)` 将最终数据广播出去，保持 “输入 → IAction → 系统” 单向链路。

-## 现有案例：PlayerMoveAction
- 文件：`Assets/Scripts/Inputs/Actions/Player/PlayerMoveAction.cs`
- 功能：读取 `Vector2` WASD 值，按 `deadZone`/`normalizeWorldDirection` 配置过滤后，生成 `SPlayerMoveIAction`（`RawInput`+世界方向+时间戳）。
- `CalculateWorldDirection()` 目前将输入的 X/Y 映射到世界 X/Z 平面（未参考摄像机朝向）。如需朝向对齐，可在此方法里注入 `Camera` 或 `Transform` 上下文，并在 `InitializeHandler` 时由 `InputManager` 传递。
- `SPlayerMoveIAction` 位于 `Assets/Scripts/Structs/IActions/SPlayerMoveIAction.cs`，内部持有 `StructMeta` 来记录统一的时间戳/帧号；其它子系统（如 `PlayerController`）只需订阅 `EventDispatcher` 即可获取并消费。

## InputManager 生命周期梳理
1. `GameManager` 调用 `InputManager.Initialize(eventDispatcher, playerCamera)`：
	- 缓存 `eventDispatcher`，标记 `isInitialized = true`。
	- 执行 `ConfigureActions()`，为 Inspector 中序列化的 `InputActionHandler[]` 逐个注入 dispatcher 并注册 Input System 回调。
2. `OnEnable()` 自动触发 `EnableActions()`；只有在 `isInitialized` 为 true 时才会启用，避免 Awake→Enable 时机错乱。
3. `OnDisable()` 将所有 handler 失能；`OnDestroy()` 进一步 `Dispose()`，确保 Play Mode 退出或场景卸载后没有残留订阅。

## 新增动作或改造流程时的注意事项
- 始终由 `GameManager` 驱动 `InputManager.Initialize()`，不要在别处直接调用 `ConfigureActions()`，避免多次绑定事件。
- 所有新建的 `InputActionHandler` 资产需要在 `InputManager` Inspector 中注册，否则生命周期方法不会被执行。
- 若 Action 需要访问摄像机或姿态数据，可扩展 `InputActionHandler.InitializeHandler(...)` 的参数签名，并从 `InputManager.Initialize(...)` 内传入。保持在一个地方统一 wiring，便于调试。
- 输入 IAction 应保持只读结构体（统一以 `S` 前缀命名，参考 `SPlayerMoveIAction`），后续系统可以安全地缓存副本，避免引用型数据被无意修改。
- 调试阶段可在 Action 内部使用 `Debug.Log`（见 `PlayerMoveAction`），但请记得在进入性能测试前加条件或移除，以免刷屏。