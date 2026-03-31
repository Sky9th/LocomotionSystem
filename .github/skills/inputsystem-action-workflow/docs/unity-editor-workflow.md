# Unity Editor 工作流

## 输入资产起点

这个项目当前使用的主输入资产是：

- Assets/Settings/Inputs/InputSystem_Actions.inputactions

在写任何 handler 代码之前，先从 Unity Editor 里把输入定义好。

## 从 Input System 开始新增或检查 Action

1. 在 Project 窗口中打开 Assets/Settings/Inputs/InputSystem_Actions.inputactions。
2. 在 Input Actions 编辑器里确认目标输入应该属于哪个 Action Map。
   - 玩家常规操作放在 Player。
   - UI 或系统级输入根据语义放到对应 map；如果当前资产里没有合适 map，再谨慎新增。
3. 对于新增任务，在目标 Action Map 下新增 Action。
4. 对于审查或修复任务，先检查现有 Action 的 Name、Action Type、Expected Control Type 是否正确。
5. 检查或补齐 Binding。
6. 如果需要多设备支持，给 Keyboard&Mouse、Gamepad 等分别补齐 Binding。
7. 如果是复合输入，例如移动方向，使用 2D Vector 等 composite，不要在代码里手工拼装离散按键。
8. 保存 Input Actions 资产，让 Unity 重新导入。

## Action Type 与 Control Type 建议

- 移动、视角这类连续值输入：
  - Action Type 通常为 Value
  - Expected Control Type 通常为 Vector2
- 跳跃、冲刺、交互这类按钮输入：
  - Action Type 通常为 Button
  - Expected Control Type 选择 Button 或保持项目当前约定
- 只关心触发瞬间的一次性命令：
  - 通常仍然使用 Button
  - 再通过 context.performed 控制只在触发时发布

## Interaction 与 Processor 原则

- 只有设计确实需要时再加 Interaction，例如 Hold、Tap、MultiTap。
- 不要把业务逻辑塞进 Interaction 配置里，Input System 配置只负责输入语义。
- 如果项目里已经通过 handler 做轻量整理，就不要在 Processor 和 handler 里重复做同类处理。

## Binding 设计建议

- 优先先把单设备链路打通，再补多设备绑定。
- 同一个 Action 的多设备 Binding 应该表达同一语义，不要让键鼠和手柄的行为定义不一致。
- 对按钮类 Action，先确认你要的是按住状态，还是一次性触发，再决定是否加 Hold 等 Interaction。

## 资产与 Inspector 接线

1. 确认 InputSystem_Actions.inputactions 中的 Action 已保存并可被引用。
2. 通过 CreateAssetMenu 创建或定位对应的 handler ScriptableObject 资产。
3. 按照项目现有约定，把资产放到合适目录：
   - 玩家控制类 handler 资产目前放在 Assets/Data/InputAction/Control/
   - 其他系统或 UI handler 资产放在 Assets/Data/InputAction/ 下的相应位置
4. 选中该 handler 资产，在 Inspector 中绑定正确的 InputActionReference。
5. 确认 InputActionReference 指向的是正确的 Action Map 和 Action，而不是同名的错误条目。
6. 找到实际运行时使用的 InputManager 组件。
7. 确认这个 handler 资产已经加入 InputManager 的 actionHandlers 数组；如果没有，就补上。
8. 进入 Play Mode，验证这个 Action 至少能触发一次，并确认 performed 或 canceled 语义符合预期。

## 编辑器侧最小检查清单

- Action 已加入 InputSystem_Actions.inputactions。
- Action 有正确的 Binding。
- Action Type 和 Expected Control Type 正确。
- handler 资产已创建或已找到。
- handler 资产已绑定 InputActionReference。
- handler 资产已加入 InputManager.actionHandlers。