# 2026-07-05 — Ability Instance 统一模型

## 产出

引入 AbilityInstance + InstanceManager 统一主动/被动/装备/天赋技能生命周期。副作用 Pull 清理。Reactor 收敛为目标侧效果唯一入口。GetDamageEffects API 修正。

## 关键决策

- **AbilityInstance 合并 AbilityHandle** — 用户指出两个 class 可以合并，Instance 自身就是 owner 和字典 key
- **Pull > Push** — 副作用由目标侧 Tick 检查 Owner.IsActive 清理，InstanceManager 不追踪"影响了谁"
- **Reactor 4 阶段** — Damage → Effects → Reaction → Broadcast，不分支不重复
- **Self 走标准路径** — caster 加入 targets，BuildDamageInfo 产生 self-hit（Amount=0），Reactor 统一处理
- **效果落地都在 Reactor** — ExecutionState 不再直接改目标的 PT/Tag
- **CrossEntityEffects 删除** — Pull 模型不需要追踪
- **RdTagContainer 重构** — _tagsByOwner（owner→tags）替代 _tags+_tagOwners 双数据源
- **RemoveTagsWhere** — 委托模式替代 L1→L3 层级反转
- **GetDamageEffect→GetDamageEffects** — 返回 EffectSO[]，支持多伤害类型
- **AbilityPipeline** — 主动被动共用，IsIdle 含 RejectedState

## 双 Agent 审核（4 轮）

第一轮：9 个架构问题 + 8 个实现问题 → 全部修正
第二轮：Pull 模型方向确认
第三轮：Reactor 收敛方案
第四轮：最终 diff 审核，2 个修复 + 0 阻塞

## 已知问题

- Reactor→Caster OnHit 通知通路未建
- ComputeDamage 交叉乘积待按 element tag 匹配
- OLD #region 未删
- RangedWeaponSO 临时 SO 泄漏
