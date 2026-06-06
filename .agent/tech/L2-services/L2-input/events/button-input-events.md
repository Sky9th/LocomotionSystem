# 按钮输入事件 (×6)

`Assets/Scripts/Services/L2_Input/Events/`

## 文件列表

| 文件 | 事件类型 | 负载 | CreateAssetMenu |
|------|---------|------|-----------------|
| SprintInputEvent.cs | `InputEvent<bool>` | 冲刺按下/释放 | Events/Input/Sprint Event |
| SecondaryInteractEvent.cs | `InputEvent<bool>` | 右键交互 | Events/Input/Secondary Interact Event |
| PrimaryInteractEvent.cs | `InputEvent<bool>` | 左键交互 | Events/Input/Primary Interact Event |
| CrouchInputEvent.cs | `InputEvent<bool>` | 蹲下 | Events/Input/Crouch Event |
| ProneInputEvent.cs | `InputEvent<bool>` | 趴下 | Events/Input/Prone Event |
| StandInputEvent.cs | `InputEvent<bool>` | 站立 | Events/Input/Stand Event |

## 统一模式

所有按钮事件继承 `InputEvent<bool>`，覆写逻辑完全相同：

```csharp
protected override void OnPerformed(InputAction.CallbackContext ctx)
{
    Raise(ctx.ReadValueAsButton());
}

protected override void OnCanceled(InputAction.CallbackContext ctx)
{
    Raise(ctx.ReadValueAsButton());
}
```

- 不做逻辑判断，只做翻译：`ctx.ReadValueAsButton()` → `Raise(bool)`
- 由订阅方（PlayerInput handler）决定如何响应

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| → 继承 | InputEvent<bool> | 父类 |
| ← 调度 | InputService | 管理生命周期（初始化/启停） |
| ← 订阅 | PlayerInput | 通过 EventHub.Get<T>() 取得后 Register |
