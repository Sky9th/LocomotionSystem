---
name: inputsystem-action-workflow
description: "Use when adding, extending, reviewing, debugging, or fixing a Unity Input System Action in this repository; also use when 为 InputSystem 新增输入动作、审查现有 Input、排查输入 bug、修复 InputActionHandler、调整 IAction payload、检查 EventDispatcher 接线、更新 LocomotionInputModule 聚合，或检查 Unity Inspector 配置。"
---

# InputSystem Action Workflow

## 目的

当你需要在这个项目里新增、扩展、审查、调试或修复 Unity Input System Action 时，使用这个 Skill。

这个 Skill 采用“轻入口 + 按需读取子文档”的结构：

- 入口 SKILL.md 只保留触发条件、决策顺序和读取指引。
- 具体提问规则、Unity Editor 流程、运行时接入细节、代码模板拆到独立文档。
- 只有在当前任务确实需要时，再读取对应子文档，以降低上下文体积和重复 token 成本。

## 核心原则

- 输入层只负责翻译和分发，不负责玩法逻辑、物理计算、状态切换。
- InputActionHandler 不能直接修改 locomotion 状态、移动角色或操作物理。
- 标识符只用英文，命名必须明确表达意图。
- 遵守非空原则：初始化和配置在启动阶段完成，不要在业务路径里用空值检查补漏洞。
- 每次改动保持聚焦、易审查。

## 默认执行顺序

1. 先判断当前任务是新增、审查、调试还是修复。
2. 再判断当前任务是否存在关键歧义。
3. 如果有关键歧义，读取 docs/clarification.md，并只做最小必要确认。
4. 如果任务涉及 Unity Input System 资产、Binding、InputActionReference 或 Inspector 配置，读取 docs/unity-editor-workflow.md。
5. 如果任务涉及 payload、handler、EventDispatcher、LocomotionInputModule 或 SLocomotionInputActions，读取 docs/runtime-integration.md。
6. 如果只是需要快速落代码骨架，读取 docs/templates.md。
7. 只读取当前任务所需的最少文件，不要默认把所有子文档都读一遍。

## 任务分流

### 新增或扩展 Action

适用于创建新的 Action、Binding、payload、handler 或运行时接线。

### 审查或修复现有 Action

适用于排查输入不生效、相位错误、接线缺失、状态门控异常、聚合遗漏、Inspector 配置错误等问题。

## 当前项目的关键参考实现

只有在需要核对真实风格或特殊语义时，才读取这些文件：

- Assets/Scripts/Inputs/Actions/InputActionHandler.cs
- Assets/Scripts/Inputs/InputManager.cs
- Assets/Scripts/Inputs/Actions/Player/IAPlayerMove.cs
- Assets/Scripts/Inputs/Actions/Player/IAPlayerSprint.cs
- Assets/Scripts/Inputs/Actions/Player/IAPlayerJump.cs
- Assets/Scripts/Inputs/Actions/UI/IAUIEscape.cs
- Assets/Scripts/Inputs/Actions/System/IASystemTimeSlow.cs
- Assets/Scripts/Locomotion/Input/LocomotionInputModule.cs
- Assets/Scripts/Locomotion/Input/SLocomotionInputActions.cs

## 子文档说明

- docs/clarification.md
  - 何时必须先确认
  - 推荐的最小确认问题
  - 提问策略与默认假设

- docs/unity-editor-workflow.md
  - 从 InputSystem_Actions.inputactions 开始新增或检查 Action
  - Action Type、Binding、Interaction、Inspector 配置
  - handler 资产与 InputManager 接线

- docs/runtime-integration.md
  - payload 设计
  - handler 接入
  - 状态门控
  - 直接事件型与 locomotion 聚合型接线
  - 审查与修复重点

- docs/templates.md
  - Button Action 模板
  - Value Action 模板
  - Direct Event Action 模板
  - payload 与 locomotion 聚合模板

## 交付要求

当代理使用这个 Skill 处理 Input System 任务时，应当做到：

1. 先判断是否存在必须确认的关键信息缺口。
2. 只读取当前任务真正需要的子文档。
3. 如有必要，再读取最接近的真实参考实现。
4. 明确说明当前任务是新增、审查、调试还是修复。
5. 只做该任务所需的最小聚焦改动。
6. 明确指出哪些 Unity 编辑器步骤必须手动完成。
7. 如果任务涉及 locomotion 输入，要端到端检查整个聚合链路。