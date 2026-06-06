# 2026-06-07 主动技能闭合回路

> v0.9.0

## 动机

被动管线（陷阱→伤害→结算→广播）已验证通过，需要实现主动技能入口，打通 Q 按键到伤害结算的完整链路。

## 关键决策

- **TryActivate 瞬发模式**：当前为 fire-and-forget，不涉及 AbilityDriver 阶段机（Slice 3）
- **输入不进 Intent**：技能激活是离散事件，SCharacterIntent 只描述连续的运动意图。Director 直接调用 TryActivate
- **槽位临时挂 CharacterActor**：技能树/装备系统缺失，`[SerializeField] private AbilityDefSO skillSlot1/2` 为临时方案
- **Cost 预检两阶段**：Phase 1 全部资产检查、Phase 2 逐项扣除，避免半扣状态
- **PeekStatCallback + ModifyStatCallback 拆分**：预检只读，扣除只写
- **Rules→Physiology 重命名**：Rule 是角色固有生理规律（帧驱动永久），Buff 是外部施加临时效果（事件驱动有时限）
- **DamageRule/BatchDamageRule/PassiveGainRule 删除**：Ability 管道已接管伤害和回复
- **EffectSO.grantedTag 移除**：duration>0 时统一用 effectTag 标识持续效果
- **CostEffectSO.statTag→statDef**：直接引用 StatDefinitionSO 资产，避免 Tag 间接查找

## 新增文件

- `L2_Input/Events/FirstSkillInputEvent.cs` — 技能槽1输入事件
- `L2_Input/Events/SecondSkillInputEvent.cs` — 技能槽2输入事件
- `L3_Ability/Execution/AbilitySearchUtility.cs` — 搜索执行静态工具（Cone/Ray/Circle）
- `L3_Character/Stats/Physiology/` — 生理规则子系统（5个文件）

## 修改文件

- `AbilityExecutor.cs` — TryActivate + 回调 + 日志
- `AbilityReactor.cs` — 结算日志
- `PlayerDirector.cs` — 按键→TryActivate 路由
- `PlayerInput.cs` — 技能事件订阅
- `CharacterCombat.cs` — PeekStat/ModifyStat 桥接
- `CostEffectSO.cs` — statTag→statDef
- `EffectSO.cs` — 移除 grantedTag
- `SDamageInfo.cs` — 移除 GrantedTag
- `CharacterStats.cs` — Get(StatDefinitionSO) 重载

## 已知待做

- AbilityDriver 阶段机（Slice 3）
- 技能槽位管理器（替换 CharacterActor 临时字段）
- Tag 过期系统（duration 到期自动移除）
- BuffEffectSO / DOT 持续伤害
