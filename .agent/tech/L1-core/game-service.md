# GameService · 服务根节点

> **Last Verified**: 2026-06-22 | **Verification**: All referenced files exist, signatures match code

> `L1_Core/GameService.cs` — ModuleHub, `DefaultExecutionOrder=-500`，Module 树根节点 + 会话生命周期

## 调用链

```
被谁调:
  Unity Engine                   → Awake() (order=-500, 最早执行)
  EventDispatcherService         → HandleSessionStateChange() (订阅 SGameState)

调谁:
  GameContext                    → new GameObject + AddComponent + Initialize()
  GameContext                    → TryResolveService<EventDispatcherService>()
  ModuleHub                      → base.Awake() → 扫描子节点 + OnAssembleAll
                                 → base.Start() → OnWireAll
  IGameplaySessionHandler        → GetComponentsInChildren<>() → OnGameplaySessionEnd()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 持有 | GameContext | Awake 中主动创建，子 Service OnAssemble 前就位 |
| 依赖 | EventDispatcherService | Start 中 TryResolveService 解析（子 Service 在 OnAssemble 中已自注册） |
| 管理 | 所有 L2 Service | 子 ModuleChildMono，ModuleHub.Awake 自动扫描发现 |
| 被依赖 | — | 根节点，不被任何模块依赖 |

## 公开属性

```csharp
public static GameService Instance { get; }           // 全局单例
```

## 方法

### Awake()
```csharp
protected override void Awake()
```
- **流程**:
  1. 单例守卫 + DontDestroyOnLoad + DOTween 配置
  2. 主动创建 GameContext（确保子模块 OnAssemble 前就位）
  3. `base.Awake()` → 扫描 ModuleChildMono 子节点 → Register → OnAssembleAll
- **备注**: base.Awake() 返回后，所有子 Service 已在 GameContext 中完成自注册

### Start()
```csharp
protected override void Start()
```
- **流程**:
  1. `TryResolveService<EventDispatcherService>` + `Subscribe<SGameState>`（pre-wire）
  2. `base.Start()` → `Registry.OnWireAll()` → 所有子 Service.OnWire
  3. Editor 自动加载非 Core 场景（post-wire）
- **备注**: TryResolveService 在 base.Start() 之前——所有 Service 在 OnAssemble 中已注册，解析安全

### HandleSessionStateChange()
- 监听 SGameState，Playing→MainMenu 时触发 TeardownSession

### TeardownSession()
```csharp
private void TeardownSession()
```
- **用途**: `GetComponentsInChildren<IGameplaySessionHandler>(includeInactive: true)` 通知所有会话处理器

## 内部机制

- `_sessionWasActive` 守卫 — 只有经历过 Playing 状态后才触发 Teardown
- 所有 Service 通过 OnAssemble 自注册到 GameContext，不再需要 wired-count 验证

## 使用规则

- **GameService 是唯一根节点** — 不要在其他地方创建 Service 实例
- **GameContext 由 GameService 主动创建** — 不依赖场景预置
- **子 Service 通过 ModuleHub.Awake 自动扫描发现** — 无需手动注册
- **Service 在 OnAssemble 中 RegisterService，在 OnWire 中 Subscribe** — 禁止在 OnWire 中 Publish
