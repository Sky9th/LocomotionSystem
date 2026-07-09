# 按键设置设计文档

> 日期: 2026-05-26
> 状态: 键鼠优先，手柄预留

---

## 设计原则

- **优先适配键鼠**：所有操作以键盘+鼠标为第一输入设备
- **手柄可规划但不实装**：保留 InputSystem 手柄 binding，代码只处理键鼠路径
- **交互分层**：左键主交互、右键副交互、E 键第三交互

---

## Player 动作映射

### 移动与视角

| 动作 | 键鼠 | 手柄（预留） |
|------|------|-------------|
| Move | WASD / 方向键 | Left Stick |
| Look | 鼠标移动 | Right Stick |
| Sprint | Left Shift（按住） | Left Stick Press |
| Walk | Left Ctrl（按住） | — |
| Crouch | C | Button East |
| Prone | Z | — |
| Stand | X | — |
| Jump | Space | Button South |

### 交互

| 动作 | 键鼠 | 说明 |
|------|------|------|
| PrimaryInteract | 鼠标左键 | 主交互：攻击敌人 / 拾取物品 / 点击 UI |
| SecendaryInteract | 鼠标右键 | 副交互：移动指令 / 右键菜单 |
| ThridInteract | E（按住） | 持续交互：开门 / 搜索 / 拆解 |

### 战斗

| 动作 | 键鼠 | 说明 |
|------|------|------|
| Attack | 鼠标左键 / Enter | 与 PrimaryInteract 共用左键，战斗上下文触发 |

### 切换

| 动作 | 键鼠 | 手柄（预留） |
|------|------|-------------|
| Previous | 1 | D-Pad Left |
| Next | 2 | D-Pad Right |

---

## UI 动作映射

| 动作 | 键鼠 |
|------|------|
| Navigate | WASD / 方向键 |
| Submit | Enter |
| Cancel | Escape |
| Point | 鼠标位置 |
| Click | 鼠标左键 |
| RightClick | 鼠标右键 |
| MiddleClick | 鼠标中键 |
| ScrollWheel | 滚轮 |
| Esc | Escape |

---

## System 动作映射

| 动作 | 键鼠 | 说明 |
|------|------|------|
| TimeSlow | Q | 慢速时间 |
| TimeResume | E | 恢复时间 |

---

## 手柄规划（不实装）

- InputSystem 中已预留 Gamepad / Joystick / XR 的 binding
- 代码层不做手柄适配，仅保留 `InputActionHandler` 的扩展空间
- 未来实装时只需添加 Handler 的 asset 即可
