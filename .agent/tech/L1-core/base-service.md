# BaseService · Service 基类（已删除）

> **v0.16.0 已删除。** Service 现在直接继承 `ModuleComponent`，通过 `OnAssemble` / `OnWire` 自管理。

## 迁移说明

旧 BaseService 提供的功能已拆分到每个 Service 自身：

| 旧 BaseService 成员 | 迁移方式 |
|---------------------|---------|
| `Register(context)` / `OnRegister` | `OnWire()` 中 `GameContext.Instance.RegisterService(this)` |
| `AttachDispatcher` / `OnDispatcherAttached` | `OnWire()` 中 `GameContext.Instance.TryResolveService(out _dispatcher)` |
| `ActivateSubscriptions` / `OnSubscriptionsActivated` | `OnWire()` 中 `_dispatcher.Subscribe<...>(...)` |
| `NotifyInitialized` / `OnServicesReady` | `OnWire()` 末尾 |
| `PublishState<T>(snapshot)` | 私有 `PublishSnapshot<T>()` helper 或内联 |
| `TryResolveService<T>()` / `RequireService<T>()` | `GameContext.Instance.TryResolveService<T>()` |
| `Log` / `GameContext` / `Dispatcher` | 私有字段自管理 |

## 原始文档（v0.15.x 及之前）

> `Core/BaseService.cs` — 所有 Service 的抽象基类，提供 4 阶段初始化 + 内置工具方法

## 调用链

```
被谁调:
  GameService.Bootstrap()        → Register(), AttachDispatcher(), ActivateSubscriptions(), NotifyInitialized()
  子类自身                       → TryResolveService(), RequireService(), PublishState()

调谁:
  GameContext                    → RegisterService(), TryResolveService(), UpdateSnapshot()
  EventDispatcherService         → Publish()
  LogManager                     → GetChannel()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GameContext | 通过它注册自身、查找其他 Service、写入 Snapshot |
| 依赖 | EventDispatcherService | 通过它发布事件 |
| 依赖 | LogManager | 日志输出 |
| 被依赖 | 所有 Service 子类 | 继承此基类 |

## 公开属性

```csharp
public bool IsRegistered { get; }              // OnRegister 成功后为 true
protected GameContext GameContext { get; }     // 注入的全局上下文
protected EventDispatcherService Dispatcher { get; }  // 注入的事件分发器
protected LogChannel Log { get; }              // 日志频道（频道名=类名）
```

## 4 阶段生命周期

### 阶段 1: Register()
```csharp
public void Register(GameContext context)
```
- **用途**: 注入 GameContext，调用 `OnRegister()`
- **调用者**: `GameService.Bootstrap()` Step 3
- **备注**: 内部守卫 — 重复调用跳过、null context 报错

### OnRegister()
```csharp
protected abstract bool OnRegister(GameContext context)
```
- **用途**: 子类覆写 — 在这里调用 `context.RegisterService(this)` 注册自身
- **返回**: 是否注册成功（false 会清空 GameContext 引用）
- **调用者**: `Register()`

### 阶段 2: OnDispatcherAttached()
```csharp
protected virtual void OnDispatcherAttached()
```
- **用途**: Dispatcher 已注入但订阅尚未激活，子类可选覆写
- **调用者**: `GameService.AttachDispatcherToServices()`

### 阶段 3: OnSubscriptionsActivated()
```csharp
protected virtual void OnSubscriptionsActivated()
```
- **用途**: 子类覆写 — 注册事件监听 (`Dispatcher.Subscribe<T>()`)
- **调用者**: `GameService.ActivateServiceSubscriptions()`

### 阶段 4: OnServicesReady()
```csharp
protected abstract void OnServicesReady()
```
- **用途**: 子类覆写 — 所有 Service 就绪后的跨 Service 初始化
- **调用者**: `GameService.InitializeServices()`

## 内置工具方法

### TryResolveService<TService>()
```csharp
protected bool TryResolveService<TService>(out TService service, bool logWarning = true)
```
- **用途**: 从 GameContext 查找 Service（带缓存）
- **参数**: `logWarning` — 找不到时是否打 Warning（默认 true）
- **返回**: 是否找到
- **备注**: 首次查找后缓存到 `serviceCache`，后续直接返回缓存

### RequireService<TService>()
```csharp
protected TService RequireService<TService>()
```
- **用途**: 强制获取 Service，失败时 LogError
- **返回**: Service 实例或 null
- **备注**: 用于必需依赖 — 没有这个 Service 就不该继续运行

### PublishState<TSnapshot>()
```csharp
protected void PublishState<TSnapshot>(TSnapshot snapshot) where TSnapshot : struct
```
- **用途**: 原子操作 — 同时写入 GameContext Snapshot + 发布事件
- **参数**: `snapshot` — struct 值
- **备注**: 核心设计 — 保证两个通道数据一致。不要分开调用 UpdateSnapshot 和 Publish

## Service 间通信规则

- **写**: Service 通过 `PublishState<T>(snapshot)` 同时写入 GameContext + Dispatcher
- **读**: 通过 Dispatcher 订阅（push）或 GameContext 轮询（pull）
- **禁止**: Service 持有其他 Service 的直接引用（如 `private PlayerService _playerService`）
- **允许**: Service 持有自己创建的 GameObject/内部对象引用
- **Component 只读**: Component 只能读取 Snapshot，写入由所属 Service 完成

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Service 初始化失败回滚 | 待做 | 当前 OnRegister 返回 false 时只清空自身引用，不考虑已注册 Service 的回滚 |
