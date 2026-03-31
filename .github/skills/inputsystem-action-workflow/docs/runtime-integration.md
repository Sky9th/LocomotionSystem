# 运行时接入与审查

## 决策树

先判断目标 Action 属于哪一类。

### 1. 直接事件型 Action

适用于 payload 只需要被某个系统直接消费，不需要进入按帧聚合的 locomotion 输入快照。

典型特征：

- 这是一个系统命令、UI 命令或一次性请求。
- Locomotion 模块不需要持续记住它的最新值。

### 2. Locomotion 聚合型 Action

适用于 locomotion 系统需要在每个仿真步骤都读取该输入的最新状态。

典型特征：

- 角色代理会持续从 SLocomotionInputActions 读取这个值。
- 它表达的是 stance、gait、movement、camera look、jump intent 或其他 locomotion 相关输入。

## 新增或扩展时的实施顺序

### 第 1 步：定义 payload struct

- 玩家与 locomotion 控制目前放在 Assets/Scripts/Inputs/Structs/Control/。
- 命名遵循现有约定：S{Name}IAction。
- 只表达输入意图，不要把玩法结论塞进去。
- 如果需要明确默认值，提供 None。
- 只有在下游逻辑确实需要区分按下和松开时，才加入 InputActionPhase。

payload 形状选择规则：

- 按钮按下/松开语义：使用 bool，必要时再带 InputActionPhase。
- 只关心触发瞬间的一次性语义：通常不需要持续 pressed 状态。
- 轴或摇杆输入：保留原始数值类型，例如 Vector2。

### 第 2 步：实现 handler ScriptableObject

- 把新 handler 类加到对应目录：
  - Assets/Scripts/Inputs/Actions/Player/
  - Assets/Scripts/Inputs/Actions/UI/
  - Assets/Scripts/Inputs/Actions/System/
- 继承 InputActionHandler。
- 添加 CreateAssetMenu，并沿用现有菜单命名风格。
- 重写 Execute(InputAction.CallbackContext context)。
- 先处理 !IsEnabled 的早返回。
- 根据控件类型选择正确的读取 API。
- 把原始输入翻译成强类型 payload。
- 通过 eventDispatcher.Publish(payload) 发布。

读取 API 约定：

- Button Action 优先使用 context.ReadValueAsButton()。
- Vector Action 使用 context.ReadValue<Vector2>()。
- 如果 Action 只应在按下时触发，要显式判断 context.performed。
- 如果需要处理松开事件，要记住 InputActionHandler 基类已经订阅了 performed 和 canceled。

### 第 3 步：判断是否需要状态门控

- gameplay 输入通常限制为 state == EGameState.Playing。
- system action 是否可用则取决于它的具体意图。
- 如果 handler 自己就能清晰表达可用状态，不要把这个判断塞到无关消费者里。

### 第 4 步：接入运行时链路

#### A. 直接事件型 Action

通常不需要改聚合模块。

1. 确保目标系统订阅新的 payload 类型。
2. 对称地处理 subscribe 和 unsubscribe。
3. 让消费者专注于领域行为，而不是输入解码。

#### B. Locomotion 聚合型 Action

必须同时修改下面这些位置：

1. LocomotionInputModule
   - 增加 backing field
   - 在 Reset() 中重置
   - 在构造函数中调用 RegisterAction<TPayload>()
   - 在 PutAction<TPayload>() 中完成赋值

2. SLocomotionInputActions
   - 增加构造参数
   - 增加公开属性
   - 把它接入 None

3. 读取该聚合快照的 locomotion 消费者
   - 显式读取新属性
   - 如果它是离散输入，要明确 phase 语义

## 审查与修复重点

- handler 已存在，但没有被 InputManager 引用。
- payload 已添加，但 LocomotionInputModule 没有更新。
- LocomotionInputModule 已更新，但 SLocomotionInputActions 没有更新。
- Button 输入使用了 context.ReadValue<bool>()，其实更适合用 context.ReadValueAsButton()。
- handler 里混入了本应属于消费者的玩法逻辑。
- 由于漏写 OnSupportsState，导致 action 在错误状态下触发。
- 一个一次性 Action 错误地同时在 performed 和 canceled 时发布。
- handler 的 InputActionReference 指向了错误的 Action。
- InputSystem_Actions.inputactions 里的 Binding、Action Type、Expected Control Type 与代码读取方式不匹配。

## 命名约定

- Handler 类：IAPlayerJump、IAUIEscape、IASystemTimeSlow
- Payload struct：SJumpIAction、SUIEscapeIAction、STimeScaleIAction
- 聚合属性：JumpAction、SprintAction、MoveAction

避免使用含义模糊的名字，例如 InputData、ActionData、HandleInput、DoAction。