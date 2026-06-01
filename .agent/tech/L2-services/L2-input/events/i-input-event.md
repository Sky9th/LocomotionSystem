# IInputEvent

`Assets/Scripts/Services/L2_Input/Events/IInputEvent.cs`

## 调用链

```
InputService
  │  Initialize / Enable / Disable / Dispose / SupportsState
  ▼
IInputEvent  ← InputEvent<T> 实现
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| ← 实现 | InputEvent<T> | 泛型基类实现 |
| ← 调用 | InputService | 通过 `is IInputEvent` 模式匹配统一管理 |

## 方法

### InitializeEvent()
- **用途**: 解析 InputActionReference，绑定 performed/canceled 回调

### EnableEvent()
- **用途**: 启用 Unity InputAction

### DisableEvent()
- **用途**: 禁用 Unity InputAction

### DisposeEvent()
- **用途**: 取消回调绑定，释放 InputAction

### SupportsState(state)
- **用途**: 检查事件在指定游戏状态下是否应激活
- **返回**: `bool`
