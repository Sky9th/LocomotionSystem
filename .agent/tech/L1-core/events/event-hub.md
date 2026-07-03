# ✅ VERIFIED — 2026-07-04 与代码对齐

> **重写**: 本文档已全面重写，与 `EventHub.cs` 实际代码对齐。
> **验证方式**: 逐行对比源码，删除所有不存在的 API（RegisterListener / OnEnable 驱动 / PlayerDirector）。
> **以 `L1_Core/Events/EventHub.cs` 代码为准。**

---

# EventHub

`Assets/Scripts/L1_Core/Events/EventHub.cs`

事件通道汇入点，继承 `ModuleChildMono`，挂载在 ModuleHub 所在 GameObject。

提供按类型查找 GameEvent 资产的统一入口，发布方和订阅方通过 `Get<T>()` 获取通道引用。

## 调用链

```
GameService.Start()
  │  GetComponentInChildren<EventHub>()  →  获取 EventHub 引用
  │  gameContext.RegisterService(_eventHub)  →  注册为全局服务
  ▼
L2 服务 (TimeService / SceneService / UIService …)
  │  GameContext.Instance.TryResolveService(out _eventHub)
  │  _eventHub.Get<ConcreteEvent>().Register(handler)
  ▼
CharacterActor (RequireComponent<EventHub>)
  │  GetComponent<EventHub>()  →  角色局部的 EventHub
  ▼
EventHub.Awake()
  │  Collect()  →  遍历 5 个 GameEvent[] 数组，构建 Dictionary<Type, GameEvent>
  └── Get<T>()  →  订阅方/发布方按类型查找通道
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| → 持有 | `GameEvent[]` x5 | 5 个分类数组，Inspector 赋资产，Awake 时 Collect 到 lookup |
| ← 使用 | GameService | `Start()` 中 GetComponentInChildren 获取，注册进 GameContext |
| ← 使用 | CharacterActor | `[RequireComponent]` + GetComponent 获取角色级 EventHub |
| ← 使用 | TimeService / SceneService / 各 L2 Service | `GameContext.TryResolveService` 获取 |
| ← 使用 | CharacterCombat / AbilityReactor | 通过 BuildContext.EventHub 或本地 GetComponent 获取，调用 `Get<T>().Register()` |

## 数据

### 5 个分类 GameEvent[] 数组

| 数组名 | 用途 |
|--------|------|
| `gameStateEvents` | GameStateChangedEvent, GameStateChangeRequestEvent |
| `sceneEvents` | SceneLoadRequestEvent, SceneLoadCompleteEvent 等 |
| `playerEvents` | PlayerSpawnedEvent |
| `inputEvents` | InputMoveEvent, InputAttackEvent, InputJumpEvent 等 |
| `abilityEvents` | HitEvent |

## 公开方法

### Get<T>()
```csharp
public T Get<T>() where T : GameEvent
```
- **用途**: 按类型获取事件通道
- **泛型约束**: `T : GameEvent`（非泛型抽象根类）
- **返回**: 通道引用，未注册打印 `Debug.LogError` 并返回 null
- **调用者**: 所有发布方和订阅方

## 内部机制

- **Awake()**: 依次调用 `Collect()` 处理 5 个 `GameEvent[]` 数组
- **Collect(GameEvent[])**: 遍历数组元素，以 `.GetType()` 为 Key 存入 `Dictionary<Type, GameEvent> lookup`，遇重复 Key 打印 `Debug.LogError`
- **Get<T>()**: 从 lookup 中以 `typeof(T)` 查找，命中的强转为 `T` 返回
- **OnWire()**: 空实现，仅为满足 ModuleChildMono 抽象契约
- 没有 OnEnable / OnDisable 逻辑，没有 IEventListener 管理

## 使用规则

- `Get<T>()` 需要 `T` 是 lookup 中某个资产的运行时类型——使用 `.GetType()` 精确匹配
- 分发模式：GameService 持有"服务级" EventHub 注册进 GameContext，CharacterActor 持有"角色级" EventHub（`[RequireComponent]` 确保挂载）
- 服务获取：L2 服务通过 `GameContext.TryResolveService(out EventHub)` 获取服务级 EventHub

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 支持发布方自动注册 | 待讨论 | SO Event Channel 讨论 |
