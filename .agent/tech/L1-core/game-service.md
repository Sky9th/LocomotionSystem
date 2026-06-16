# GameService · 服务根节点

> `Core/GameService.cs` — ModuleBehaviour, `DefaultExecutionOrder=-500`，Module 树根节点 + 会话生命周期

## 调用链

```
被谁调:
  Unity Engine                   → Awake() (order=-500, 最早执行)
  EventDispatcherService         → HandleSessionStateChange() (订阅 SGameState)
  子 Service                     → NotifyServiceWired() (OnWire 末尾)

调谁:
  GameContext                    → new GameObject + AddComponent + Initialize()
  EventDispatcherService         → GetComponentInChildren<>(), RegisterService(), Subscribe()
  ModuleBehaviour                → base.Awake() + base.OnWire() → Registry.OnAssembleAll/OnWireAll
  IGameplaySessionHandler        → GetComponentsInChildren<>() → OnGameplaySessionEnd()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 持有 | GameContext | 主动 `new GameObject + AddComponent` 创建 |
| 持有 | EventDispatcherService | 子 Component，OnWire 中注册到 GameContext |
| 持有 | 所有 L2 Service | 子 Component，ModuleBehaviour 自动发现 |
| 被依赖 | — | 根节点，不被任何模块依赖 |

## 公开属性

```csharp
public static GameService Instance { get; }           // 全局单例
public void NotifyServiceWired()                       // 子 Service OnWire 末尾调用
```

## 方法

### Awake()
```csharp
private new void Awake()
```
- **用途**: 单例守卫 + DontDestroyOnLoad + DOTween 配置 → `base.Awake()` 启动 Module 树
- **备注**: ModuleBehaviour.Awake 自动发现所有 IInitializable 子组件，调用 OnAssemble → Registry.OnAssembleAll

### OnAssemble()
```csharp
public override void OnAssemble()
```
- **用途**: 主动实例化 `GameContext`（`new GameObject + AddComponent`），确保在子模块之前就位
- **调用者**: `ModuleBehaviour.Awake()` (链：Awake → OnAssemble → Registry.OnAssembleAll)

### OnWire()
```csharp
public override void OnWire()
```
- **流程**:
  1. 手动找到 EventDispatcherService → 注册到 GameContext + 订阅 SGameState
  2. `base.OnWire()` → `Registry.OnWireAll()` → 所有子 Service.OnWire
  3. 验证 `_wiredCount == Registry.Count`
  4. Editor 自动加载非 Core 场景
- **调用者**: `ModuleBehaviour.Start()` (链：Start → OnWire)

### HandleSessionStateChange()
- 同旧版 — 监听 SGameState，Playing→MainMenu 时触发 TeardownSession

### TeardownSession()
```csharp
private void TeardownSession()
```
- **用途**: `GetComponentsInChildren<IGameplaySessionHandler>(includeInactive: true)` 替代旧 `registeredServices` 列表

## 内部机制

- `_wiredCount` — 每个子 Service 在 OnWire 末尾调 `NotifyServiceWired()`，与 `Registry.Count` 对比验证完整性
- `_sessionWasActive` 守卫 — 只有经历过 Playing 状态后才触发 Teardown

## 使用规则

- **GameService 是唯一根节点** — 不要在其他地方创建 Service 实例
- **GameContext 由 GameService 主动创建** — 不依赖场景预置
- **子 Service 通过 Module 树自动发现** — 无需手动注册
