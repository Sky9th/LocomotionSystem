# GameService · 服务根节点

> `Core/GameService.cs` — MonoBehaviour, `DefaultExecutionOrder=-500`，Bootstrap 启动序列 + 会话生命周期

## 调用链

```
被谁调:
  Unity Engine                   → Awake() (order=-500, 最早执行)
  EventDispatcherService         → HandleSessionStateChange() (订阅 SGameState)

调谁:
  GameContext                    → Initialize(), GetComponentInChildren<>()
  EventDispatcherService         → GetComponentInChildren<>(), Subscribe(), RegisterService()
  所有 BaseService               → GetComponentsInChildren<>(), Register(), AttachDispatcher(), ActivateSubscriptions(), NotifyInitialized()
  IGameplaySessionHandler        → OnGameplaySessionEnd()
  SceneService                   → SetCurrentContentScene()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 持有 | GameContext | 子 Component，Awake 中获取 |
| 持有 | EventDispatcherService | 子 Component，最先注册 |
| 持有 | 所有 BaseService | 子 Component，Bootstrap 中发现并注册 |
| 被依赖 | — | 根节点，不被任何模块依赖 |

## 公开属性

```csharp
public static GameService Instance { get; }   // 全局单例
```

## 方法

### Awake()
```csharp
private void Awake()
```
- **用途**: 防重复 Instance + DontDestroyOnLoad + 启动 Bootstrap
- **备注**: 设置 `DOTween.defaultTimeScaleIndependent = true`（UI 动画不随 Time.timeScale 冻结）

### Bootstrap()
```csharp
private void Bootstrap()
```
- **用途**: 5 步启动序列
- **流程**:
  1. 获取 `GameContext` 子 Component → `Initialize()`
  2. 获取 `EventDispatcherService` → 注册 + 自身订阅 `SGameState`
  3. 遍历所有 `BaseService` 子 Component → `Register(context)` → 加入 `registeredServices` 列表
  4. `AttachDispatcherToServices()` — 逐个注入 Dispatcher
  5. `ActivateServiceSubscriptions()` — 逐个激活事件订阅
  6. `InitializeServices()` — 逐个调用 `OnServicesReady()`
- **调用者**: `Awake()`
- **备注**: 幂等 — `isBootstrapped` 守卫。Editor 下非 Core 场景自动模拟 `SSceneLoadComplete`

### RegisterService()
```csharp
private bool RegisterService(BaseService service, string label)
```
- **用途**: 对单个 Service 调用 `Register(context)`
- **参数**: `service` — Service 实例；`label` — 类型名（用于日志）
- **返回**: 是否注册成功
- **调用者**: `Bootstrap()` Step 2/3

### HandleSessionStateChange()
```csharp
private void HandleSessionStateChange(SGameState state, MetaStruct meta)
```
- **用途**: 监听状态变化，Playing→MainMenu 时触发 TeardownSession
- **调用者**: EventDispatcher 回调
- **备注**: 用 `sessionWasActive` 守卫——只有经历过 Playing 状态后才触发 Teardown

### TeardownSession()
```csharp
private void TeardownSession()
```
- **用途**: 遍历所有 Service，对实现了 `IGameplaySessionHandler` 的调用 `OnGameplaySessionEnd()`；最后 `ClearSnapshots()`
- **调用者**: `HandleSessionStateChange()`

### OnDestroy()
```csharp
private void OnDestroy()
```
- **用途**: 取消 `SGameState` 订阅，置空 Instance

### AttachDispatcherToServices() / ActivateServiceSubscriptions() / InitializeServices()
- 三个内部方法，遍历 `registeredServices` 列表，分别调用对应阶段方法
- **调用者**: `Bootstrap()` Step 4/5/6

## 内部机制

- `isBootstrapped` 守卫 — 防止 Bootstrap 重复执行
- `sessionWasActive` 守卫 — 只有经历过 Playing 状态后才触发 Teardown，避免 MainMenu 初始化时误触发
- `registeredServices` 列表 — 在 Bootstrap 中填充，Teardown 和 Editor 路径都依赖此列表

## 使用规则

- **GameService 是唯一根节点** — 不要在其他地方创建 Service 实例
- **不要直接访问 `registeredServices`** — 通过 GameContext 查找 Service
- **Teardown 中 GameService 抢先订阅 SGameState** — 确保 Teardown 先于 UIService/InputService 的 SGameState 回调执行

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| Bootstrap 失败回滚 — 已注册的 Service 回退 | 待做 | 当前失败时只 LogError，不回滚已注册的 Service |
| Editor 路径与运行时路径统一 | 待做 | 当前 Editor 直开有多处条件编译分支 |
