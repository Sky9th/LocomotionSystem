# L2_Input · 输入服务

## 层级定位

| 层级 | 说明 |
|------|------|
| **L2 (Services)** | InputService 属于服务层，由 GameService.Bootstrap() 驱动生命周期，依赖 EventDispatcher 进行模块间通信 |
| **Actions** | 自身代码 — InputActionHandler 系列，每个 SO 资产对应一个 Input Action |
| **Structs** | 自身代码 — 输入数据结构体 (DTO) |

输入系统的设计遵循"数据单向流动"原则：**输入层只生成 DTO，不操作游戏对象**。所有输入数据通过 EventDispatcher 广播，消费方自行订阅。

## 调用链

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Unity Input System                          │
│                    (Input Action Asset)                            │
└──────────────────────┬──────────────────────────────────────────────┘
                       │ performed / canceled 回调
                       ▼
┌─────────────────────────────────────────────────────────────────────┐
│  InputService                    [L2 服务层 — 生命周期管理]          │
│  ├── actionHandlers[] 序列化数组                                    │
│  ├── EnableActions / DisableActions                                 │
│  └── EnforceHandlerStatePermissions (按 EGameState 过滤)            │
└──────────────────────┬──────────────────────────────────────────────┘
                       │ 驱动生命周期
                       ▼
┌─────────────────────────────────────────────────────────────────────┐
│  InputActionHandler                [L4 处理器层 — SO 基类]          │
│  ├── InitializeHandler(dispatcher)                                  │
│  ├── Execute(CallbackContext) → 组装 struct → Publish               │
│  └── Enable / Disable / Dispose                                     │
└───────┬───────────────────┬───────────────────┬─────────────────────┘
        │                   │                   │
        ▼                   ▼                   ▼
┌──────────────┐  ┌─────────────────┐  ┌──────────────────┐
│ Player       │  │ System          │  │ (IAUIEscape)     │
│ IAPlayerMove │  │ IASystemTimeSlow│  │ IAUIEscape       │
│ IAPlayerLook │  │ IASystemTimeRes │  └────────┬─────────┘
│ IAPlayerC×9  │  └────────┬────────┘           │
└───────┬──────┘           │                    │
        │                  │                    │
        ▼                  ▼                    ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    EventDispatcher.Publish(struct)                   │
│  SIActionMove/Look  │  SIActionWorldSpeed  │  SIActionUIEscape      │
└──────────┬──────────────────────┬──────────────────────┬─────────────┘
           │                      │                      │
           ▼                      ▼                      ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────────┐
│ 02-character     │  │ 01-core         │  │ 01-core              │
│ CharacterEvent   │  │ TimeService     │  │ GameStateService     │
│ Receiver         │  │ Time.timeScale  │  │ Playing ↔ Paused     │
│ (帧缓存 → Actor) │  │                 │  │                      │
└──────────────────┘  └──────────────────┘  └──────────────────────┘
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| InputService | 01-core (EventDispatcherService, GameStateService) | 订阅 SGameState，按状态控制输入权限 |
| InputActionHandler | InputService | InputService 持有数组，统一调度生命周期 |
| IAPlayerMove / IAPlayerLook / IAPlayerButton* | 02-character (CharacterEventReceiver) | 角色每帧消费移动/朝向/按钮数据 |
| IASystemTimeSlow / IASystemTimeResume | 01-core (TimeService) | 订阅 SIActionWorldSpeed，修改 Time.timeScale |
| IAUIEscape | 01-core (GameStateService) | 订阅 SIActionUIEscape，触发暂停 |

## 设计决策

| 决策 | 原因 |
|------|------|
| InputActionHandler 为 ScriptableObject | 可复用、可配置，不同角色/场景用不同 SO 组合 |
| 统一 EventDispatcher 发布 | 输入只生成 DTO，不关心谁消费，保持单向解耦 |
| SButtonInputState 分离 isRequested/isReleased | 帧信号 vs 持续状态区分，一次请求不会被多帧重复消费 |
| 游戏状态权限控制 | Playing 时只启用玩家输入，菜单时只启用 UI 输入 |
| 按钮 struct 统一模板模式 | 9 个按钮动作结构体和 handler 全部同构，批量生成 |
| WASD 移动暂由 A* Pathfinding 替代 | CharacterEventReceiver 中 SIActionMove 注册被注释，Phase 4 解禁 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| CharacterEventReceiver WASD 解禁 (Phase 4 A* Pathfinding 驱动) | 待做 | 09-pathfinding | 代码 TODO (CharacterEventReceiver.cs:49-50) |
| InputActionHandler 增加上下文注入（摄像机 Transform、姿态引用） | 待做 | — | 旧 input-manager.md |
| 第三交互 (ThirdInteract, E 键按住) 支持 | 待做 | — | 旧 mouse-interaction.md |
| Handler 按优先级排序 | 待做 | — | 旧 input-manager.md |
| 输入重映射 / 按键绑定 UI | 远期 | 04-ui | 旧 input-manager.md |
| 手柄/控制器输入支持 | 远期 | — | 旧 input-manager.md |
| 输入录制/回放调试工具 | 远期 | — | 旧 input-manager.md |
| 动态注册/注销 Handler | 远期 | — | 旧 input-manager.md |

## 子文档索引

### L2 Service

| 文件 | 内容 |
|------|------|
| [input-service.md](input-service.md) | InputService — 生命周期、权限控制、Handler 编排 |

### Actions (L2 自身代码)

| 文件 | 内容 |
|------|------|
| [actions/input-action-handler.md](actions/input-action-handler.md) | InputActionHandler — SO 基类、生命周期 |
| [actions/L5-player/ia-player-move.md](actions/L5-player/ia-player-move.md) | IAPlayerMove — WASD → 世界方向 |
| [actions/L5-player/ia-player-look.md](actions/L5-player/ia-player-look.md) | IAPlayerLook — 鼠标 Delta → 朝向 |
| [actions/L5-player/L5-button/ia-player-crouch.md](actions/L5-player/L5-button/ia-player-crouch.md) | IAPlayerCrouch — 蹲下 |
| [actions/L5-player/L5-button/ia-player-jump.md](actions/L5-player/L5-button/ia-player-jump.md) | IAPlayerJump — 跳跃 |
| [actions/L5-player/L5-button/ia-player-primary-interact.md](actions/L5-player/L5-button/ia-player-primary-interact.md) | IAPlayerPrimaryInteract — 主交互 |
| [actions/L5-player/L5-button/ia-player-prone.md](actions/L5-player/L5-button/ia-player-prone.md) | IAPlayerProne — 趴下 |
| [actions/L5-player/L5-button/ia-player-run.md](actions/L5-player/L5-button/ia-player-run.md) | IAPlayerRun — 跑步切换 |
| [actions/L5-player/L5-button/ia-player-secondary-interact.md](actions/L5-player/L5-button/ia-player-secondary-interact.md) | IAPlayerSecondaryInteract — 副交互 |
| [actions/L5-player/L5-button/ia-player-sprint.md](actions/L5-player/L5-button/ia-player-sprint.md) | IAPlayerSprint — 冲刺 |
| [actions/L5-player/L5-button/ia-player-stand.md](actions/L5-player/L5-button/ia-player-stand.md) | IAPlayerStand — 站立 |
| [actions/L5-player/L5-button/ia-player-walk.md](actions/L5-player/L5-button/ia-player-walk.md) | IAPlayerWalk — 行走切换 |
| [actions/L5-system/ia-system-time-slow.md](actions/L5-system/ia-system-time-slow.md) | IASystemTimeSlow — 时间减速 |
| [actions/L5-system/ia-system-time-resume.md](actions/L5-system/ia-system-time-resume.md) | IASystemTimeResume — 时间恢复 |
| [actions/L5-ui/ia-ui-escape.md](actions/L5-ui/ia-ui-escape.md) | IAUIEscape — ESC 键暂停 |

### Structs (L2 自身代码)

| 文件 | 内容 |
|------|------|
| [L4-structs/s-action-ui-escape.md](L4-structs/s-action-ui-escape.md) | SIActionUIEscape — ESC 动作 DTO |
| [L4-structs/L5-control/s-action-move.md](L4-structs/L5-control/s-action-move.md) | SIActionMove — 移动 DTO |
| [L4-structs/L5-control/s-action-look.md](L4-structs/L5-control/s-action-look.md) | SIActionLook — 朝向 DTO |
| [L4-structs/L5-control/L5-button/s-button-input-state.md](L4-structs/L5-control/L5-button/s-button-input-state.md) | SButtonInputState — 按钮状态 model |
| [L4-structs/L5-control/L5-button/s-action-crouch.md](L4-structs/L5-control/L5-button/s-action-crouch.md) | SIActionCrouch — 蹲下 DTO |
| [L4-structs/L5-control/L5-button/s-action-jump.md](L4-structs/L5-control/L5-button/s-action-jump.md) | SIActionJump — 跳跃 DTO |
| [L4-structs/L5-control/L5-button/s-action-primary-interact.md](L4-structs/L5-control/L5-button/s-action-primary-interact.md) | SIActionPrimaryInteract — 主交互 DTO |
| [L4-structs/L5-control/L5-button/s-action-prone.md](L4-structs/L5-control/L5-button/s-action-prone.md) | SIActionProne — 趴下 DTO |
| [L4-structs/L5-control/L5-button/s-action-run.md](L4-structs/L5-control/L5-button/s-action-run.md) | SIActionRun — 跑步切换 DTO |
| [L4-structs/L5-control/L5-button/s-action-secondary-interact.md](L4-structs/L5-control/L5-button/s-action-secondary-interact.md) | SIActionSecondaryInteract — 副交互 DTO |
| [L4-structs/L5-control/L5-button/s-action-sprint.md](L4-structs/L5-control/L5-button/s-action-sprint.md) | SIActionSprint — 冲刺 DTO |
| [L4-structs/L5-control/L5-button/s-action-stand.md](L4-structs/L5-control/L5-button/s-action-stand.md) | SIActionStand — 站立 DTO |
| [L4-structs/L5-control/L5-button/s-action-walk.md](L4-structs/L5-control/L5-button/s-action-walk.md) | SIActionWalk — 行走切换 DTO |
