# EventChannels

`Assets/Scripts/L1_Core/Events/EventChannels.cs`

## 调用链

```
CharacterActor.Awake()
  │  GetComponent<EventChannels>()
  ▼
EventChannels (MB)
  │  Get<T>()  →  订阅方/发布方查找事件通道
  │  RegisterListener()  →  模块注册自己的 IEventListener
  │
  ├── OnEnable  →  遍历 listeners → BindEvents()
  └── OnDisable →  遍历 listeners → UnbindEvents()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| → 持有 | EventChannelBase[] | 集中持有所有事件资产引用 |
| → 驱动 | IEventListener[] | OnEnable/OnDisable 遍历调用 |
| ← 使用 | CharacterActor | GetComponent 获取 |
| ← 使用 | PlayerDirector | 构造时 RegisterListener |
| ← 使用 | PlayerInput | Get<T>() 查找事件 |

## 公开方法

### Get<T>()
```csharp
public T Get<T>() where T : EventChannelBase
```
- **用途**: 按类型获取事件通道
- **返回**: 通道引用，未注册返回 null
- **调用者**: 发布方和订阅方

### RegisterListener(listener)
```csharp
public void RegisterListener(IEventListener listener)
```
- **用途**: 注册事件监听者。模块初始化时调用，只能新增不重复
- **参数**: `listener` — 实现 IEventListener 的对象
- **调用者**: PlayerDirector 构造时等

## 内部机制

- **Awake**: 构建 `Dictionary<Type, EventChannelBase>` lookup
- **OnEnable**: 遍历已注册的 IEventListener，调用 `BindEvents()`
- **OnDisable**: 遍历已注册的 IEventListener，调用 `UnbindEvents()`
- 事件通道资产通过 `[SerializeField]` 在 Inspector 中赋值

## 使用规则

- `Get<T>()` 需要 `T` 是 `channels` 数组中某个元素的具体类型——用资产的 `.GetType()` 做 Key
- `RegisterListener()` 需在 `Awake` 中调用——在 `OnEnable` 触发绑定之前

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 支持发布方自动注册 | 待讨论 | SO Event Channel 讨论 |
