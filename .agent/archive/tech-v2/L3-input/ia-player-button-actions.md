# IAPlayer Button Actions · 按钮动作处理器

> 9 个按钮式输入动作，全部继承 InputActionHandler，同构模式。读取 `ReadValueAsButton()` → 组装 `SIActionXxx.CreateEvent()` → `eventDispatcher.Publish()`。

## 文件清单

| 文件 | Action | 对应 struct |
|------|--------|-------------|
| `Actions/Player/Button/IAPlayerCrouch.cs` | 蹲下 | SIActionCrouch |
| `Actions/Player/Button/IAPlayerJump.cs` | 跳跃 | SIActionJump |
| `Actions/Player/Button/IAPlayerPrimaryInteract.cs` | 主交互 (鼠标左键) | SIActionPrimaryInteract |
| `Actions/Player/Button/IAPlayerProne.cs` | 趴下 | SIActionProne |
| `Actions/Player/Button/IAPlayerRun.cs` | 跑步切换 | SIActionRun |
| `Actions/Player/Button/IAPlayerSecondaryInteract.cs` | 副交互 (鼠标右键) | SIActionSecondaryInteract |
| `Actions/Player/Button/IAPlayerSprint.cs` | 冲刺切换 | SIActionSprint |
| `Actions/Player/Button/IAPlayerStand.cs` | 站立 | SIActionStand |
| `Actions/Player/Button/IAPlayerWalk.cs` | 行走切换 | SIActionWalk |

## 调用链

```
Unity Input System (Performed/Canceled)
  └── handler.Execute(context)
      ├── context.ReadValueAsButton() → bool
      ├── SIActionXxx.CreateEvent(isPressed, context.phase)
      └── eventDispatcher.Publish(struct)
          └── CharacterEventReceiver (订阅) → 帧缓存
```

## 耦合模块 (所有 9 个共用)

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | InputActionHandler | 基类生命周期 |
| 发布 | SIActionCrouch / Jump / PrimaryInteract 等 | EventDispatcher 广播 |
| 消费 | 02-character (CharacterEventReceiver) | 订阅所有 SIActionXxx |

## 公共方法模式

所有 9 个 Handler 的 Execute() 完全同构：

### Execute()
```csharp
protected override void Execute(InputAction.CallbackContext context)
```
- **用途**: 读取按钮状态 → 组装 struct → 发布事件
- **参数**: `context` — Unity Input System 回调上下文
- **调用者**: Unity Input System performed/canceled
- **备注**: 仅在 IsEnabled 时处理

各 Handler 的差异仅在于发布的 struct 类型：

| Handler | CreateEvent 调用 |
|---------|-----------------|
| IAPlayerCrouch | `SIActionCrouch.CreateEvent(rawInput, context.phase)` |
| IAPlayerJump | `SIActionJump.CreateEvent(rawInput, context.phase)` |
| IAPlayerPrimaryInteract | `SIActionPrimaryInteract.CreateEvent(rawInput, context.phase)` |
| IAPlayerProne | `SIActionProne.CreateEvent(rawInput, context.phase)` |
| IAPlayerRun | `SIActionRun.CreateEvent(rawInput, context.phase)` |
| IAPlayerSecondaryInteract | `SIActionSecondaryInteract.CreateEvent(rawInput, context.phase)` |
| IAPlayerSprint | `SIActionSprint.CreateEvent(rawInput, context.phase)` |
| IAPlayerStand | `SIActionStand.CreateEvent(rawInput, context.phase)` |
| IAPlayerWalk | `SIActionWalk.CreateEvent(rawInput, context.phase)` |

### OnSupportsState()
```csharp
// 所有 9 个均未覆写 OnSupportsState，默认返回 true
// 由 InputService.EnforceHandlerStatePermissions 统一控制
```

## IAPlayerJump 特殊点

```csharp
// 增加了 LogChannel 调试日志
var log = LogManager.GetChannel(nameof(IAPlayerJump));
log.Debug("Jump input received.");
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 第三交互 (ThirdInteract, E 键按住) 按钮 | 待做 | 旧 mouse-interaction.md |
