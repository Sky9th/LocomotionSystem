# IAUIEscape
> **源文件**: `Assets/Scripts/Inputs/Actions/UI/IAUIEscape.cs`

继承 InputActionHandler。ESC 键触发暂停/菜单。

## 调用链

```
Unity Input System (Escape Action)
  └── performed/canceled
      └── IAUIEscape.Execute(context)
          ├── context.ReadValueAsButton() → isPressed
          ├── 组装 SIActionUIEscape(isPressed)
          └── eventDispatcher.Publish(struct)
              └── GameStateService (订阅) → 切换 Playing/Paused
              └── UIService (通过 SGameState 间接) → 显示暂停菜单
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | InputActionHandler | 基类生命周期 |
| 发布 | SIActionUIEscape | EventDispatcher 广播 |
| 消费 | 01-core (GameStateService) | 订阅 SIActionUIEscape |
| 消费 | 01-core (SGameState → UIService) | 间接，通过状态转换驱动 UI |

## 公开属性

无。（继承自 InputActionHandler 的 IsContextBound、IsEnabled 属性，参见 input-action-handler.md）

## 配置参数

| 参数 | 类型 | 默认值 | 用途 |
|------|------|--------|------|
| `publishOnCanceled` | bool | false | 是否在 canceled 时也发布 |

## 方法

### Execute()
```csharp
protected override void Execute(InputAction.CallbackContext context)
```
- **用途**: 读取 ESC 按钮按下状态 → 发布 SIActionUIEscape
- **参数**: `context` — Unity Input System 回调上下文
- **调用者**: Unity Input System performed/canceled
- **备注**: publishOnCanceled=false 时只在 performed+isPressed 时发布

### OnSupportsState()
未覆写 OnSupportsState，默认返回 true。由 InputService.EnforceHandlerStatePermissions 统一控制。

## 未来规划

无。
