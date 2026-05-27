# EventDispatcherService · 事件总线

> `Core/EventDispatcherService.cs` — 类型安全的轻量事件总线，继承 BaseService

## 调用链

```
被谁调:
  GameService.Bootstrap()        → 第一个注册的 Service
  所有 Service                   → Subscribe / Unsubscribe / Publish
  BaseService.PublishState()     → (间接) Publish()

调谁:
  GameContext                    → RegisterService(this)
  Subscriber 回调                → Action<TPayload, MetaStruct>
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GameContext | 注册自身 |
| 被依赖 | 所有 Service | 通过它发布/订阅事件 |

## 公开属性

无公开属性。Inspector 中 `inspectorListeners` 列表显示当前订阅状态（调试用）。

## 方法

### Subscribe<TPayload>()
```csharp
public void Subscribe<TPayload>(Action<TPayload, MetaStruct> handler)
```
- **用途**: 订阅指定类型的事件
- **参数**: `handler` — 回调 `(payload, meta) => {}`，null 会被忽略
- **调用者**: Service 的 `OnSubscriptionsActivated()` 或 `OnRegister()` 中
- **备注**: 同一个 handler 不会重复添加（Contains 检查）；用 `typeof(TPayload)` 作为字典 Key

### Unsubscribe<TPayload>()
```csharp
public void Unsubscribe<TPayload>(Action<TPayload, MetaStruct> handler)
```
- **用途**: 取消订阅
- **参数**: `handler` — 之前 Subscribe 的同一个委托实例
- **调用者**: Service 的 `OnDestroy()` 中（防止回调到已销毁对象）
- **备注**: 该类型无订阅者时自动从字典移除 Key

### Publish<TPayload>()
```csharp
public void Publish<TPayload>(TPayload payload)
```
- **用途**: 发布事件，通知所有订阅者
- **参数**: `payload` — 事件数据 struct
- **调用者**: Service 在状态变更时调用；或通过 `BaseService.PublishState()` 间接调用
- **备注**: 自动附带 `MetaStruct`（Timestamp + FrameIndex）；发布前 `ToArray()` 快照 handler 列表，防止回调中修改集合

### Clear()
```csharp
public void Clear()
```
- **用途**: 清空所有订阅
- **备注**: 当前没有被调用（预留）

### OnRegister()
```csharp
protected override bool OnRegister(GameContext context)
```
- **用途**: 调用 `context.RegisterService(this)` 注册自身
- **备注**: `EventDispatcherService` 是 Bootstrap 中第一个注册的 Service

### OnServicesReady()
```csharp
protected override void OnServicesReady()
```
- **用途**: 空实现 — 无需跨 Service 初始化

## 内部机制

```csharp
private readonly Dictionary<Type, List<Delegate>> listeners;  // Type→handler 列表映射
```

- `RefreshInspectorListeners()`: 每次 Subscribe/Unsubscribe 后更新 `inspectorListeners` 列表，在 Inspector 中实时显示当前订阅状态

## 使用规则

- **Payload 类型即 Key** — 不需要额外的 EventId 枚举或字符串
- **订阅在 OnSubscriptionsActivated，取消在 OnDestroy** — 成对操作
- **优先用 PublishState** — 需要同时更新 GameContext 时，用 `BaseService.PublishState()` 而非直接调 `Publish()`
- **不存放业务逻辑** — 纯通道，不判断/过滤/转换事件
- **无状态 Payload** — 发布不可变 Struct，事件内不要持有场景引用（GameObject/Transform）；需要上下文时在 payload 中传 ID 或由订阅方查询 GameContext
- **主线程专用** — 所有回调在 `Publish()` 的同一帧同步执行，不做跨帧缓存
- **禁止自行构造 MetaStruct** — 消费方必须信任 Dispatcher 附带的 MetaStruct，所有时序分析统一依赖它
- **不支持优先级** — Subscribe/Unsubscribe 仅维护委托列表，若需顺序保证由调用方自行包装

## 调试

- 需要时可在 `Publish` 前加条件编译的 `Debug.Log` 输出 payload（正式版本关闭）
- 若遇到订阅未移除导致多次回调，在 `Unsubscribe` 前后加 `Debug.Assert` 验证

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 一次性订阅 (OneShotListener) — 收到一次后自动取消 | 待做 | 旧 event-dispatcher.md — 某些一次性事件（PlayerSpawned）只需响应一次 |
| 多线程支持 — 外围队列将 Publish 推入主线程 | 远期 | 旧 event-dispatcher.md — 网络/多人场景 |
| 优先级支持 — Subscribe 时指定回调顺序 | 远期 | 旧 event-dispatcher.md — 当前用订阅先后控制顺序，不精确 |
