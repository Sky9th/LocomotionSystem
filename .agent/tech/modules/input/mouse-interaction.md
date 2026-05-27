# 鼠标交互事件

> 日期: 2026-05-26 | 状态: 已实现

## 交互分层

| 层级 | Action | 绑定 | 用途 |
|------|--------|------|------|
| 主交互 | PrimaryInteract | 鼠标左键 | 攻击敌人 / 拾取物品 / 点击目标 |
| 副交互 | SecondaryInteract | 鼠标右键 | 移动指令 / 右键菜单 |
| 第三交互 | ThridInteract | E（按住） | 开门 / 搜索 / 拆解 |

## 数据流

```
InputSystem → InputActionHandler → EventDispatcher.Publish(SIAction*)
  → CharacterEventReceiver (订阅) → 帧内缓存
  → ReadPrimaryInteract / ReadSecondaryInteract (消费端调用)
```

## 新增文件

| 文件 | 说明 |
|------|------|
| `SIActionPrimaryInteract.cs` | 主交互结构体 |
| `SIActionSecondaryInteract.cs` | 副交互结构体 |
| `IAPlayerPrimaryInteract.cs` | 主交互 Handler ScriptableObject |
| `IAPlayerSecondaryInteract.cs` | 副交互 Handler ScriptableObject |

## CharacterEventReceiver

```csharp
Register<SIActionPrimaryInteract>();
Register<SIActionSecondaryInteract>();

ReadPrimaryInteract(out SIActionPrimaryInteract action)  → bool
ReadSecondaryInteract(out SIActionSecondaryInteract action) → bool
```
