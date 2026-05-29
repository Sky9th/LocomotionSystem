# 代码命名约定

## Struct 前缀

| 前缀 | 用途 | 示例 |
|------|------|------|
| `S` | 全局上下文快照（GameContext 存储） | `SGameState`、`SPlayer`、`SCameraSnapshot` |
| `SIAction` | 输入动作（EventDispatcher 发布） | `SIActionMove`、`SIActionLook`、`SIActionJump` |
| `SCharacter` | 角色系统内部 struct | `SCharacterMotor`、`SCharacterKinematic`、`SCharacterInputActions` |

## 不可变 Struct

- `readonly struct`，仅 get 属性
- 构造函数完整初始化
- 不内嵌 MetaStruct（由 EventDispatcher 统一生成）

## 层级豁免

以下类型不受 L1-L5 单向依赖约束，可被任意层引用：

- **契约枚举**（如 `EGameState`）—— 纯数据定义
- **事件驱动 Struct**（`SSceneLoadComplete`、`SIActionMove` 等）—— 通过 EventDispatcher 分发

详见 `namespace-rules.md`。
