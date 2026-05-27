# 03-input · 输入系统

> 基于 Unity Input System 的三层架构：InputService(管理) → InputActionHandler(生命周期) → IAction 接口(具体动作)。输入数据单向流动，输入层只生成 DTO 不操作游戏对象。

## 调用链

```
Unity Input System (Input Action Asset)
  │
  ├── performed/canceled 回调
  │   │
  │   ▼
  │   InputActionHandler.Execute()          ← SO 资产，每个动作一个
  │   │
  │   ├── 读取 raw input (Vector2/bool)
  │   ├── 滤波/归一化处理
  │   ├── 组装 SIActionXxx struct
  │   └── eventDispatcher.Publish(struct)
  │
  ├── InputService                          ← BaseService，生命周期持有
  │   ├── actionHandlers[] 序列化数组
  │   ├── EnableActions / DisableActions
  │   └── EnforceHandlerStatePermissions (按游戏状态启用/禁用)
  │
  └── ── EventDispatcher ──→ 订阅方
        ├── CharacterEventReceiver          ← 角色帧内缓存
        │   ├── ReadActions(out SCharacterInputActions)
        │   ├── ReadPrimaryInteract / ReadSecondaryInteract
        │   └── ReadCameraControl / ReadMouseGroundPosition
        ├── GameStateService                ← IAUIEscape → ESC 暂停
        ├── TimeService                     ← IASystemTimeSlow/Resume → 时间控制
        └── CameraService                   ← 通过 SCameraSnapshot 间接（非本模块）
```

## 分层

| 层 | 说明 |
|----|------|
| **管理 (InputService)** | Service 生命周期、全局 Enable/Disable、按游戏状态切换权限 |
| **处理 (InputActionHandler)** | SO 资产，每个 Input Action 一个，统一 Initialize/Enable/Disable/Dispose |
| **数据 (SIActionXxx)** | 只读 struct，通过 EventDispatcher 发布，不引用场景对象 |

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| InputService | 01-core (EventDispatcherService, GameStateService, TimeService) | 订阅 SGameState，按状态控制输入权限 |
| InputActionHandler | InputService | InputService 持有数组，统一调度生命周期 |
| SIActionMove/Look | 02-character (CharacterActor, CameraService) | 角色每帧消费移动/朝向数据 |
| SIActionButtonXxx | 02-character (CharacterEventReceiver) | 桥接输入事件到角色帧循环 |
| IAUIEscape | 01-core (GameStateService) | 订阅 SIActionUIEscape，触发暂停 |
| IASystemTimeSlow/Resume | 01-core (TimeService) | 订阅 SIActionWorldSpeed，修改 Time.timeScale |
| CharacterEventReceiver | 02-character (CharacterActor) | Actor 每帧 ReadActions，填入 CharacterFrameContext |

## 设计决策

| 决策 | 原因 |
|------|------|
| InputActionHandler 为 ScriptableObject | 可复用、可配置，不同角色/场景用不同 SO 组合 |
| 统一 EventDispatcher 发布 | 输入只生成 DTO，不关心谁消费，保持单向解耦 |
| SButtonInputState 分离 isRequested/isReleased | 帧信号 vs 持续状态区分，一次请求不会被多帧重复消费 |
| 游戏状态权限控制 | Playing 时只启用玩家输入，菜单时只启用 UI 输入 |
| 按钮 struct 统一模板模式 | 9 个按钮动作结构体和 handler 全部同构，批量生成 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| CharacterEventReceiver WASD 解禁（Phase 4 A* Pathfinding 驱动） | 待做 | 09-pathfinding | 代码 TODO (CharacterEventReceiver.cs:50) |
| InputActionHandler 增加上下文注入（摄像机 Transform、姿态引用） | 待做 | — | 旧 input-manager.md |
| 第三交互 (ThirdInteract, E 键按住) 支持 | 待做 | — | 旧 mouse-interaction.md |
| 输入重映射 / 按键绑定 UI | 远期 | 04-ui | 旧 input-manager.md |
| 手柄/控制器输入支持 | 远期 | — | 旧 input-manager.md |
| 输入录制/回放调试工具 | 远期 | — | 旧 input-manager.md |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [input-service.md](input-service.md) | InputService — 生命周期、权限控制、Handler 编排 |
| [input-action-handler.md](input-action-handler.md) | InputActionHandler — SO 基类、Initialize/Enable/Disable/Dispose |
| [ia-player-move.md](ia-player-move.md) | IAPlayerMove — WASD/摇杆 → Vector2 世界方向 |
| [ia-player-look.md](ia-player-look.md) | IAPlayerLook — 鼠标 Delta → 朝向 |
| [ia-player-button-actions.md](ia-player-button-actions.md) | IAPlayerCrouch/Jump/PrimaryInteract/Prone/Run/SecondaryInteract/Sprint/Stand/Walk |
| [ia-system-actions.md](ia-system-actions.md) | IASystemTimeSlow/Resume — 时间缩放控制 |
| [ia-ui-escape.md](ia-ui-escape.md) | IAUIEscape — ESC 键暂停/菜单 |
| [s-action-move.md](s-action-move.md) | SIActionMove — 移动动作 struct |
| [s-action-look.md](s-action-look.md) | SIActionLook — 朝向动作 struct |
| [s-button-input-state.md](s-button-input-state.md) | SButtonInputState — 按钮状态 model |
| [s-action-button-structs.md](s-action-button-structs.md) | 9 个按钮动作 struct (Crouch/Jump/Prone/Run/Sprint/Stand/Walk/PrimaryInteract/SecondaryInteract) |
| [character-event-receiver.md](character-event-receiver.md) | CharacterEventReceiver — 角色输入桥接 + SCharacterInputActions |
