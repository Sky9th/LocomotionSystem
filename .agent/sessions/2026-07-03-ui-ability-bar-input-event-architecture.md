# 2026-07-03 第二场 — UI 技能/武器槽 + Input 事件架构决策

## 背景

技能管道已落地，需要 HUD 层展示冷却和状态。开始时创建了合并式 ActionBarOverlay（武器+技能），后根据用户反馈拆分为独立 Overlay。

## 改动

### 新增 UI 组件
- **UIIconSlot**：通用槽位显示，纯 setter 组件
- **AbilityBarOverlay**：动态技能槽，轮询冷却/选中，输入代码已注释（待迁至 PlayerSkillHandler）
- **WeaponBarOverlay**：动态武器槽，轮询背包容器
- **DebugOverlay**：管道状态 + 冷却计时

### 新增 API
- `AbilityExecutor.Pipeline` getter + `GetCooldownRemaining()` + `GetAbilityCooldownRemaining()`
- `ActiveAbilityPipeline.CurrentState` 属性

### 删除
- `ActionBarOverlay.cs`（合并方案）
- `PlayerDirector.ProcessSkillInput` + `TryActivateSkill`（技能移至 UI）

## 架构决策

本次最核心的产出不是代码，是 [ui-communication-architecture.md](../tech/L2-services/L2-ui/ui-communication-architecture.md)。经过正反辩论 + 参考 Unreal GAS/Lyra + QFramework 四层架构，确定：

1. **Query 走快照**：GameContext + PlayerID，不持有 L3 引用
2. **Event 走广播**：System → UI 通知
3. **Input 事件消费者在 System 层**：不在 UI，不在 Director
4. **Command 直调**：通过 GameContext + EntityService + PlayerID 拿引用
5. **不引入 Command 层/ICommand 接口**

### 未完成

- InputSkill/InputEquip 事件消费者尚未归位（UI 已注释，PlayerSkillHandler/PlayerEquipHandler 待创建）
- 装备逻辑仍在 PlayerDirector（后续迁至 PlayerEquipHandler）

## 交叉引用

- Decision: [ui-communication-architecture.md](../tech/L2-services/L2-ui/ui-communication-architecture.md)
- Version: [v0.34.1](../versions/v0.34.1.md)
- Session (上午): [2026-07-03-rdtag-rename-animation-clip-ability-pipeline.md](2026-07-03-rdtag-rename-animation-clip-ability-pipeline.md)
