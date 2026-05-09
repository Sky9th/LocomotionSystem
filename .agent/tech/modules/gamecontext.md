# GameContext 设计说明

## 概述
GameContext 常驻于 GameManager 的子层级，是场景内“运行期上下文”的唯一可信入口。所有需要共享访问的运行时数据（摄像机状态、玩家姿态、天气/AI 反馈、服务入口等）都应以 `S` 前缀的不可变结构体快照形式注册到 GameContext，避免脚本间随意互相引用造成耦合。

## 核心职责
- 统一缓存关键场景引用，并在状态/场景变更时及时刷新。
- 聚合常用快照结构（如 CameraContextStruct、PlayerPoseStruct），向其他子系统提供稳定的只读数据。
- 作为服务定位器的受控外观层，安全地暴露 EventDispatcher、InputManager 等运行期服务。
- 为输入/摄像机/角色等子系统提供获取上下文的 API，避免它们直接依赖具体 GameObjects。

## 层级与依赖注入
1. 在 `GameManager` 下创建子 GameObject（建议命名 `GameContext`），挂载 `GameContext` 组件并保持 `DontDestroyOnLoad`。
2. `GameManager.Awake()` 阶段寻找该组件并调用 `Initialize(EventDispatcher dispatcher, InputManager input)`，必要时再注入其他服务。
3. `Initialize` 内部应校验必需引用，序列化字段缺失时日志警告，必要时尝试自动查找（例如 `GetComponentInChildren`）。

## 数据分层
- **Struct Snapshot Layer**：仅缓存结构体快照，例如 `SCameraContext`（位置、前向、近远裁剪等）、`SPlayerPose`、`SWeatherContext`。每个子系统负责在自身更新点调用 `GameContext.UpdateSnapshot(struct)` 写入最新状态，消费方通过 `TryGetSnapshot<TStruct>` 读取上一帧稳定值。
- **Service Access Layer**：对外暴露 `EventDispatcher Dispatcher { get; }`、`InputManager Input { get; }` 等核心服务。除 GameManager 外，不允许其他脚本直接持有服务引用，如需扩展可通过 `RegisterService/ TryResolveService` 实现。

## 公共 API 建议
- `void UpdateSnapshot<TStruct>(TStruct data) where TStruct : struct`
- `bool TryGetSnapshot<TStruct>(out TStruct data) where TStruct : struct`
- `void RegisterService<TService>(TService service)` / `bool TryResolveService<TService>(out TService service)`

通过统一接口传递 Struct，有助于保持上下文不可变，避免外部脚本直接持有 GameObject 引用并修改状态。

## 生命周期
1. **Awake**：缓存自身引用，准备 Inspector 配置。
2. **Initialize**：由 GameManager 调用，完成服务注入、初始快照注册、事件订阅。
3. **OnEnable / OnDisable**：在需要时订阅/退订全局事件（如 SceneManager.sceneLoaded、玩家重生事件）以协调快照更新。
4. **LateUpdate**（可选）：若某些快照由 GameContext 统一采样，可在此帧末刷新；否则由子系统自行推送。
5. **OnDestroy**：解除所有事件订阅并清空注册表，确保重新加载场景时可以干净初始化。

## 协作要点
- **输入/摄像机/AI 子系统**：各自维护专属 Struct（例如 `SPlayerMoveIAction`、`SCameraContext`、`SEnemyAwareness`），并在更新后调用 `UpdateSnapshot` 推送；需要数据的系统通过 `TryGetSnapshot` 读取。
- **EventDispatcher**：GameContext 可订阅玩家重生、场景切换等事件以触发快照重建或清理；也可在必要时广播“ContextUpdated”类型事件，提醒订阅者刷新本地缓存。
- **其他服务**：例如 AudioService、UIService、天气系统，可在初始化时通过 `RegisterService` 暴露给需要的子系统，保持依赖集中。

## Inspector 与调试
- 在 Inspector 中分组展示：Snapshots（只读预览最近一次 Struct）、Services 列表。
- 提供 `bool logDebugInfo` 选项，在 Struct 被更新或服务被注册时输出日志，便于调试。
- 可在自定义 Editor 中列出当前缓存的 Struct 类型名称及更新时间，辅助排查数据链路。

## 扩展建议
- 定义 `SCameraContext`、`SPlayerPose` 等专用结构，与 Shared Structs 约定保持一致。
- 增设 `ActorRegistry`、`WeatherContextStruct` 等结构化数据，让共享状态都以 Struct 形式存在。
- 若接入多人或网络，可在 GameContext 内维护 `Dictionary<PlayerId, SPlayerContext>` 并周期性推送快照。
- 将可选配置（如摄像机偏移、输入灵敏度默认值）转成 ScriptableObject，由各子系统消费后再把结果写成 Struct 快照，保持上下文只读。
