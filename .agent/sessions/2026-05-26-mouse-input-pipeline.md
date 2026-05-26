# 2026-05-26 鼠标交互事件管线

## 改动

- 禁用 WASD 移动订阅（GridAgent 将在 Phase 4 接管移动）
- 新增 PrimaryInteract（鼠标左键）和 SecondaryInteract（鼠标右键）事件管线
- InputSystem Player Map 重构：Interact → ThridInteract，新增 PrimaryInteract/SecondaryInteract
- 添加按键设置设计文档
- 添加子 Agent 模型选择约定

## 技术细节

- 新建 `SIActionPrimaryInteract` / `SIActionSecondaryInteract` 结构体（复用 SButtonInputState）
- 新建 `IAPlayerPrimaryInteract` / `IAPlayerSecondaryInteract` Handler（继承 InputActionHandler）
- CharacterEventReceiver 注册两个事件，暴露 `ReadPrimaryInteract()` / `ReadSecondaryInteract()` 方法
- 加入临时调试日志 `LogInteract()`，仅打印 Pressed/Released 帧信号

## 已知问题

- SecendaryInteract 存在拼写错误，待 Editor 中修复
- 两个 .asset 的 InputActionReference 需要在 Editor 中拖入对应 Action
- 调试日志在性能测试前需移除或用条件编译包裹
