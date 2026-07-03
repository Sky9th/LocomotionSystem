# UI ↔ L3 通信架构

> **Status**: Decision Record | **Date**: 2026-07-03  
> **Source**: 辩论 + 参考 Unreal GAS/Lyra、QFramework 四层架构

## 三层语义

```
Query（读）     GameContext 快照 + PlayerID
                UI 不持有任何 L3 引用
                方向：UI → GameContext（拉）

Event（通知）   "X 发生了" — 广播，无返回值
                方向：System → UI（推）
                示例：PlayerSpawned, Hit, GameStateChanged

Input（触发）   "玩家想做什么" — 介于通知和命令之间
                方向：InputService → System（推）
                示例：InputSkill1Event, InputEquip1Event
                ★ 消费者必须是 System 层，不是 UI

Command（执行）  "做 X" — 点对点，可带返回值
                方向：System 内部 / UI（拖拽场景） → L3
                实现：GameContext + EntityService + PlayerID → 拿引用 → 直调
                没有 Command 类、CommandBus、ICommand 接口
```

## Input 事件的消费者归属

参考 Lyra：`InputTag → ASC::ProcessAbilityInput() → TryActivateAbility(Handle)`

| Input 事件 | 当前消费者（错） | 应该的消费者 |
|-----------|-----------------|------------|
| `InputSkill1Event` | `AbilityBarOverlay` → `BuildContext` 后门 → `Enqueue()` | `AbilityExecutor` 自己订阅 → `Enqueue()` |
| `InputEquip1Event` | `PlayerDirector` → `BuildContext` 后门 → `Place/Remove` | 系统层 handler |


## 物品操作路径

按 UI 是否参与交互分：

| 操作 | 触发 | UI 是交互媒介？ | 路径 |
|------|------|:---:|------|
| 技能激活 | 按键/按钮 | 否 | InputEvent → Executor 订阅 → Enqueue |
| 捡取 | 靠近按 F | 否 | InputEvent → 拾取 handler → Container.Place |
| 丢弃 | 右键 | 否 | InputEvent → 丢弃 handler → Container.Remove + EntityService.Spawn |
| **拖拽** | UI 拖放 | **是** | UI → GameContext → EntityService → 拿 Container 引用 → 直调 Swap/Place |

拖拽是唯一例外——需要逐帧反馈（槽位高亮、失败弹回）。UI 通过合法 GameContext 路径拿引用直调，跟 PlayerService 同规则。

## 引用获取路径

```
后门（废）:
  uiService.PlayerActor?.BuildContext
    → ctx.Ability / ctx.CharacterContainer / ctx.AbilityForest

正门（统一）:
  GameContext.Instance.TryResolveService<EntityService>(out svc)
  svc.GetView("player_local")
    → GetComponent<AbilityExecutor>()
    → GetComponent<CharacterActor>() → CharacterContainer
```

## 不做

- 不引入 Command 层、CommandBus、ICommand 接口
- 不引入 IAbilityExecutor/IAbilityCommander 包装接口
- 不把 `Enqueue` 套进第二个事件
- Query 不需要快照之外的新概念

## 验证

1. UI Overlay 不直接引用 L3 内部类型（`CharacterBuildContext` 等）
2. `TryGetPlayerActor` / `PlayerActor` 属性被移除
3. Input 事件消费者位置正确（System 层，而非 UI/Director）
4. 拖拽场景通过 GameContext 合法获取容器引用