# 代码命名约定

日期: 2026-05-23

## Struct 前缀

| 前缀 | 用途 | 示例 |
|------|------|------|
| `S` | 全局单例快照（GameContext 存储） | `SGameState`、`SCameraContext`、`SPlayer` |
| `SIAction` | 输入动作（EventDispatcher 发布） | `SIActionMove`、`SIActionLook`、`SIActionJump` |
| `SCharacter` | 角色系统内部 struct | `SCharacterMotor`、`SCharacterKinematic`、`SCharacterInputActions` |

规则源自 `9809b78` — 将 `STimeScaleIAction` 等不统一的命名改为 `SIActionWorldSpeed`。

## 不可变 Struct

- 只读字段或仅 get 属性
- 构造函数完整初始化
- 不内嵌 MetaStruct（由 EventDispatcher 统一生成）
