# IEventListener

`Assets/Scripts/L1_Core/Events/IEventListener.cs`

## 调用链

```
EventHub.OnEnable/OnDisable
  │
  ▼
IEventListener  ← 模块纯类实现
  ├── BindEvents()    → Register 事件 handler
  └── UnbindEvents()  → Unregister 事件 handler
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| ← 实现 | PlayerInput | 输入事件订阅 |
| ← 驱动 | EventHub | OnEnable/OnDisable 调用 |

## 方法

### BindEvents()
```csharp
void BindEvents()
```
- **用途**: 注册所有事件监听
- **调用者**: EventHub.OnEnable

### UnbindEvents()
```csharp
void UnbindEvents()
```
- **用途**: 取消所有事件监听
- **调用者**: EventHub.OnDisable

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 无 | — | — |
