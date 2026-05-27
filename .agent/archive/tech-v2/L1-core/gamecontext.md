# GameContext · 全局上下文

> `Core/GameContext.cs` — MonoBehaviour，全局数据总线 + Service 注册中心

## 调用链

```
被谁调:
  GameService.Bootstrap()        → Initialize(), 之后所有 Service 通过它注册/查找
  BaseService.PublishState()     → UpdateSnapshot()
  BaseService.Register()         → (间接) RegisterService()
  BaseService.TryResolveService() → TryResolveService()
  GameService.TeardownSession()  → ClearSnapshots()
  所有 Service/外部系统          → TryGetSnapshot() 读取状态

调谁:
  LogManager.GetChannel()        → 自身日志
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | 所有 Service | 注册自身到 Registry |
| 被依赖 | 所有 Service、外部系统 | 通过 TryGetSnapshot 读取全局状态 |
| 被依赖 | BaseService.PublishState | UpdateSnapshot 原子写入 |
| 依赖 | LogManager | 日志输出 |

## 公开属性

```csharp
public static GameContext Instance { get; }   // 全局单例，Awake 中防重复
public bool IsInitialized { get; }            // Initialize() 调用后为 true
public int RegisteredServiceCount { get; }    // 已注册 Service 数量（Inspector 调试）
public int SnapshotCount { get; }             // 当前 Snapshot 数量（Inspector 调试）
public IEnumerable<Type> RegisteredServiceTypes { get; }  // 已注册 Service 类型列表
public IEnumerable<Type> SnapshotStructTypes { get; }     // 当前 Snapshot 类型列表
```

## 方法

### Initialize()
```csharp
public void Initialize()
```
- **用途**: 激活全局上下文，设置 Instance 和 isInitialized
- **调用者**: `GameService.Bootstrap()` Step 1
- **备注**: 幂等 — 重复调用只打 Debug 日志，不重置状态

### RegisterService<TService>()
```csharp
public void RegisterService<TService>(TService service) where TService : class
```
- **用途**: 将 Service 实例注册到 `serviceRegistry`，Key = `typeof(TService)`
- **参数**: `service` — Service 实例（null 会被忽略）
- **调用者**: 每个 Service 的 `OnRegister()` — 自己把自己注册进去
- **备注**: 泛型约束 `class`，确保只能注册引用类型

### TryResolveService<TService>()
```csharp
public bool TryResolveService<TService>(out TService service) where TService : class
```
- **用途**: 从 Registry 中按类型查找已注册的 Service
- **参数**: `out service` — 找到则返回实例，否则 null
- **返回**: 是否找到
- **调用者**: `BaseService.TryResolveService()`（带缓存层），或任何持有 GameContext 引用的代码
- **备注**: 推荐通过 `BaseService.TryResolveService()` 间接调用（带缓存），不要直接调此方法

### UpdateSnapshot<TSnapshot>()
```csharp
public void UpdateSnapshot<TSnapshot>(TSnapshot snapshot) where TSnapshot : struct
```
- **用途**: 写入/更新一个全局 Snapshot，Key = `typeof(TSnapshot)`
- **参数**: `snapshot` — struct 值（值拷贝存入 Dictionary）
- **调用者**: `BaseService.PublishState()` — 与 Dispatcher.Publish 原子执行
- **备注**: 不要直接调用 — 用 `PublishState()` 保证 Snapshot 和 Event 同步

### TryGetSnapshot<TSnapshot>()
```csharp
public bool TryGetSnapshot<TSnapshot>(out TSnapshot snapshot) where TSnapshot : struct
```
- **用途**: 读取一个全局 Snapshot
- **参数**: `out snapshot` — 找到则返回最新值，否则 default
- **返回**: 是否找到
- **调用者**: 任何需要读取全局状态的代码（CameraService 读 SPlayer、外部系统读 SCameraSnapshot 等）
- **备注**: 泛型约束 `struct`，只能读取值类型 Snapshot

### ClearSnapshots()
```csharp
public void ClearSnapshots()
```
- **用途**: 清空所有 Snapshot
- **调用者**: `GameService.TeardownSession()` — 会话回主菜单时清空
- **备注**: 不清理 Service Registry — Service 是常驻的，Snapshot 是会话级的

## 内部机制

### Awake()
- 防重复：如果已有 Instance 且不是自己 → `Destroy(gameObject)`
- 不在这里 Initialize — 等 `GameService.Bootstrap()` 调用

### OnDestroy()
- 如果自己是当前 Instance → 置空
- 清空 Registry

## 使用规则

- **Service 注册**: 只在 `OnRegister()` 中调 `RegisterService(this)`，其他地方不允许
- **Snapshot 写入**: 必须通过 `BaseService.PublishState(snapshot)`，不直接调 `UpdateSnapshot`
- **Snapshot 读取**: Component 可以读（通过 `GameContext.Instance.TryGetSnapshot`），但不能写
- **不存放业务逻辑**: GameContext 是纯数据通道，不包含任何判断/计算/状态转换

## 数据分层

GameContext 的值分为两层：

| 层 | 存储 | 生命周期 | 写入者 |
|----|------|---------|--------|
| **Service Registry** | `Dictionary<Type, object>` | 常驻（与 Core 同生命周期） | 各 Service 的 OnRegister() |
| **Snapshot Store** | `Dictionary<Type, object>` | 会话级（Teardown 时清空） | BaseService.PublishState() |

个体实体数据（如某个 Component 的状态）留在 Component 的 public 属性中，通过 `GetComponent<T>()` 读取。GameContext 只承载**全局单例状态**（Camera、Player、GameState）。

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Inspector 分组展示 Snapshots/Services 列表 | 待做 | 旧 gamecontext.md — 调试时不用翻代码 |
| 可配置的 `logDebugInfo` 开关 | 待做 | 旧 gamecontext.md — Struct 更新/Service 注册时输出日志 |
| ActorRegistry — 维护所有角色/NPC 的引用字典 | 待做 | 旧 gamecontext.md — 多人/NPC 系统需要 |
| WeatherContextStruct — 天气系统快照 | 远期 | 旧 gamecontext.md |
| 多人支持 — `Dictionary<PlayerId, SPlayerContext>` | 远期 | 旧 gamecontext.md — 网络多人时周期性推送快照 |
