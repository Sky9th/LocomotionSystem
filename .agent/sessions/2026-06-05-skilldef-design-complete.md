# 2026-06-05 SkillDefSO 重构完成

> v0.7.0 · 从平铺属性到资产组合

## 设计流程

逐类分析 16 类技能属性 → 确认归属 → 拆分为 SO 组合：
1. 激活方式 + 动画 + 阶段 → SkillActivationSO
2. 目标搜索 → SkillSearchSO (基类+3子类)
3. 伤害+消耗+冲击+斩杀 → GameplayEffectSO 体系 (abstract base+5子类)
4. 冷却 → CooldownEffectSO (独立)
5. 噪音 → NoiseEventSO (独立)
6. 标签门控 → selfTag + TagMutualExclusionSO
7. 连招 → ComboLink struct
8. 被动技能 → PassiveSkillSO

## 关键决策

- damageType 删除，effectTag 统一路由防御公式
- canKill 删除，改为防御方 State.Unkillable 标签
- NoiseEffectSO 不继承 GE（噪音不是效果）
- CooldownEffectSO 独立类型（冷却永远单数）
- 标签互斥集中管理（父标签下子标签自动互斥）
- comboNextSlots → ComboLink{nextSkill, windowStart, windowDuration, bypassCooldown}
- 被动技能=事件→条件→效果 (枚举+可选 EventChannel)
- 双 Agent 审查推敲，确认无遗漏

## 最终覆盖率

15/16 类已覆盖。投射物/位移/召唤/视音/AI 延后。
