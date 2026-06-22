# 按钮输入事件

> **Last Verified**: 2026-06-22 | **Verification**: All referenced files exist, signatures match code

`Assets/Scripts/Services/L2_Input/Events/`

## 事件列表

### Player（14 个）
| 文件 | menuName | fileName |
|------|----------|----------|
| CrouchInputEventSO.cs | `.../Player/Crouch` | Crouch |
| SprintInputEventSO.cs | `.../Player/Sprint` | Sprint |
| ProneInputEventSO.cs | `.../Player/Prone` | Prone |
| StandInputEventSO.cs | `.../Player/Stand` | Stand |
| WalkInputEventSO.cs | `.../Player/Walk` | Walk |
| JumpInputEventSO.cs | `.../Player/Jump` | Jump |
| AttackInputEventSO.cs | `.../Player/Attack` | Attack |
| MoveInputEventSO.cs | `.../Player/Move` | Move |
| LookInputEventSO.cs | `.../Player/Look` | Look |
| NextInputEventSO.cs | `.../Player/Next` | Next |
| PreviousInputEventSO.cs | `.../Player/Previous` | Previous |
| PrimaryInteractInputEventSO.cs | `.../Player/PrimaryInteract` | PrimaryInteract |
| SecondaryInteractInputEventSO.cs | `.../Player/SecondaryInteract` | SecondaryInteract |
| ThridInteractInputEventSO.cs | `.../Player/ThridInteract` | ThridInteract |

### Combat（6 个）
| 文件 | menuName | fileName |
|------|----------|----------|
| Equip1InputEventSO.cs | `.../Combat/Equip1` | Equip1 |
| Equip2InputEventSO.cs | `.../Combat/Equip2` | Equip2 |
| Equip3InputEventSO.cs | `.../Combat/Equip3` | Equip3 |
| Skill1InputEventSO.cs | `.../Combat/Skill1` | Skill1 |
| Skill2InputEventSO.cs | `.../Combat/Skill2` | Skill2 |
| Skill3InputEventSO.cs | `.../Combat/Skill3` | Skill3 |

### System（2 个）
| 文件 | menuName | fileName |
|------|----------|----------|
| TimeSlowInputEventSO.cs | `.../System/TimeSlow` | TimeSlow |
| TimeResumeInputEventSO.cs | `.../System/TimeResume` | TimeResume |

### UI（1 个）
| 文件 | menuName | fileName |
|------|----------|----------|
| EscapeInputEventSO.cs | `.../UI/Escape` | Escape |

## 基类

| 文件 | CreateAssetMenu | 说明 |
|------|----------------|------|
| ButtonInputEventSO | ❌ 无 | 按钮事件根，暴露 IsPressed / IsRequested / IsReleased |
| Vector2InputEventSO | ❌ 无 | 双轴连续输入，暴露 CurrentValue / HasInput |
| FloatInputEventSO | ❌ 无 | 单轴连续输入，暴露 CurrentValue / HasInput |

> 基类不设 CreateAssetMenu — 它们是原型，不应从菜单创建实例。

## 命名规范

- **menuName**: PascalCase，无空格。格式 `RedDust/Events/Input/{Category}/{Name}`
- **fileName**: PascalCase 简单名，无 Input 无 EventSO 后缀。如 `Equip1`（非 `Equip1EventSO` 或 `Equip1InputEventSO`）
- **类名**: `{Name}InputEventSO`，继承 `ButtonInputEventSO`
- **实际资产**: 统一为 `{Category}/{Name}.asset`，如 `Combat/Equip1.asset`

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| ← 继承 | ButtonInputEventSO → InputEventBase → EventChannelBase | 三层继承链 |
| ← 调度 | InputService | InitializeEvent + EnableEvent |
| ← 订阅 | PlayerInput（IEventListener） | 通过 EventHub.Get\<T\>() 取得后订阅 OnRaised |
