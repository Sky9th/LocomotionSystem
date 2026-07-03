# GameEvent / GameEvent\<T\>

`Assets/Scripts/L1_Core/Events/GameEvent.cs`

同一文件内定义两层抽象：非泛型 `GameEvent` 为 EventHub 提供统一的类型约束基类，泛型 `GameEvent<T>` 实现具体的事件发布/订阅管道。一个 `.asset` 实例 = 一条事件管道。

## 类型层级

```
ScriptableObject
  └── GameEvent (abstract, non-generic)
        ├── ListenerCount (abstract int)
        └── ClearAllListeners() (abstract)
        └── GameEvent<T> (abstract generic)
              ├── Register(Action<T>)
              ├── Unregister(Action<T>)
              ├── Raise(T)
              └── Editor_NotifyRaised() [#if UNITY_EDITOR]
                    └── InputMoveEvent, HitEvent, GameStateChangedEvent, ...
```

## 调用链

```
发布方 (L2-L3)
  │  EventHub.Get<ConcreteEvent>() → 获取资产引用
  │  .Raise(payload)  →  通知所有已注册 listener
  ▼
GameEvent<T>.Raise(T)
  │  倒序遍历 List<Action<T>>
  │  listener?.Invoke(payload)
  ▼
订阅方 (L2-L3)
  │  EventHub.Get<ConcreteEvent>()→ .Register(handler)
  │  EventHub.Get<ConcreteEvent>()→ .Unregister(handler)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| → 继承 | ScriptableObject | 最终基类 |
| ← 使用 | 所有发布方 | 通过 EventHub.Get<T>() 获取资产，调用 Raise() |
| ← 使用 | 所有订阅方 | 通过 EventHub.Get<T>() 获取资产，调用 Register()/Unregister() |
| ← 引用 | EventHub | 5 个 `GameEvent[]` 数组在 Inspector 中引用具体子类资产 |
| ← 引用 | PassiveAbilitySO / Editor | 比 ScriptableObject 更窄的类型约束 |

## 非泛型 GameEvent（抽象根类）

```csharp
public abstract class GameEvent : ScriptableObject
{
    public abstract int ListenerCount { get; }
    public abstract void ClearAllListeners();
}
```
- **用途**: 为 EventHub 的 `Get<T>() where T : GameEvent` 提供类型约束，同时为 Editor 工具提供统一基类
- **不含** Register/Unregister/Raise —— 这些在泛型子类中定义

## 泛型 GameEvent\<T\>（抽象管道）

### 公开属性

| 属性 | 类型 | 用途 |
|------|------|------|
| `ListenerCount` | `override int` | 当前注册 listener 数（运行时） |

### 方法

#### Register(handler)
```csharp
public void Register(Action<T> handler)
```
- **用途**: 订阅事件（去重：同一 handler 不重复添加）
- **参数**: `handler` — 回调
- **调用者**: 订阅方（通常在 `OnWire()` / `Start()` 中）

#### Unregister(handler)
```csharp
public void Unregister(Action<T> handler)
```
- **用途**: 取消订阅
- **调用者**: 订阅方（通常在 `OnDestroy()` 中）

#### Raise(payload)
```csharp
public void Raise(T payload)
```
- **用途**: 发布事件，通知所有注册的 listener
- **参数**: `payload` — 事件负载（强类型 struct）
- **调用者**: 发布方

#### ClearAllListeners()
- **用途**: 清空 listener 列表（调用 `List.Clear()`）

## 内部机制

- listener 列表通过 `List<Action<T>>` 管理，不序列化
- `Raise()` 使用倒序遍历（`for i = Count-1 downto 0`），防止回调中修改列表导致异常
- `Raise()` 内 `#if UNITY_EDITOR` 调用 `Editor_NotifyRaised()`，触发 `OnAnyRaised` 静态事件供 Editor 工具订阅（运行时拓扑高亮）
- `[ContextMenu("Raise (Test)")]` 提供 Editor 右键测试入口，以 `default(T)` 为 payload 触发

## 使用规则

- `GameEvent<T>` 是 **abstract** 类，必须创建具体 sealed 子类才能作为 `.asset` 使用
- 所有事件类型统一使用 `GameEvent<T>` —— 无 `InputEvent<T>` 分化
- 已有具体子类示例：`InputMoveEvent : GameEvent<SVector2InputPayload>`、`HitEvent : GameEvent<SDamageInfo>`、`GameStateChangedEvent : GameEvent<SGameState>` 等 30+ 个

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 无 | — | — |
