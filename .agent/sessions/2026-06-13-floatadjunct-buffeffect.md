# 2026-06-13 — FloatAdjunct + BuffEffectSO 落地

## 背景

Ability Pipeline 的 Search / Activation / Noise 三个维度已有完整资产树文档，但 Effect 系统缺少 Buff/Debuff 支持。DamageEffectSO 只能表达伤害（瞬时或 DoT），ImpactEffectSO 只能表达瞬时硬直，无法表达"减速 2s""护甲 +20 5s"等持续状态修改。

## 做了什么

### FloatAdjunct — Properties 只读修正层
- 新建 `FloatAdjunct` 类：Owner/TargetPath/ValueAdd/ValueMultiply/ExpiryTime
- 与 `FloatModifier`（持续修改 Current）正交：Adjunct 不改 Current，只在读 Effective 时叠加
- `Effective = clamp((Current + ΣValueAdd) × ΠValueMultiply, Min, Max)`
- `GetFloat` 保持返回 raw Current，新增 `GetEffectiveFloat` 返回修正后值
- FloatState.Tick 自动清理过期 Adjunct（ExpiryTime <= Time.time）

### BuffEffectSO — Ability 侧 Buff/Debuff 统一底座
- EffectSO 新子类，`grantedTags[]` + `adjuncts[]`（SBuffAdjunct 模板）
- 统一表达：临时 Buff（技能 selfEffects）、天赋（Passive OnEquip, duration≤0）、条件 Buff（被动触发）、装备 Buff
- 运行时翻译：读 BuffEffectSO → 拼 FloatAdjunct → agent.AddAdjunct → FloatState 桶
- Tag 过期由 AbilityExecutor._buffTags 管理，与冷却共用 0.5s 清理周期
- EffectImportExport 全链路支持 "Buff" 类型

### 4 个 Demo 技能 JSON
- 刀·轻击 / 刀·重劈 / 手枪·普通射击 / 手枪·压制射击
- 6 个 `*_all.json` 全量文件按子系统目录摆放

## 设计决策
- 不建 BuffInstance 类 — FloatAdjunct 归属 EntityProperties，Tags 归属 OwnedTags，各自在 Actor 内部
- valueAdd 按层数线性叠加（×stackCount），valueMultiply 不随层数变化
- 堆叠的非线性曲线留到 Phase 2
- 写入用 GetFloat（raw Current），读取修正值用 GetEffectiveFloat

## 涉及模块
- `L3_Properties/` — FloatAdjunct + FloatState + EntityProperties + PropertyAgent
- `L3_Ability/` — BuffEffectSO + AbilityExecutor + EffectImportExport
- `Tags/` — 新增 Tag_SuppressiveFire
