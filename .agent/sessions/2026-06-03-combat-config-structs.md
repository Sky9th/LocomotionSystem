# 2026-06-03 — Combat 配置层 + 数据结构 + 审查修复

## 目标

完成 Phase 4.1 Item 2-4，并通过设计文档对抗性审查修复 6 个问题。

## Item 2: 枚举

三个枚举：`ECombatSearchType`（Cone/RayLine/Circle）、`ESkillPhase`（None/Windup/Active/Fire/Recovery/Cancelled）、`SkillAnimationLayer`（FullBody/UpperBody）。

后续审查修复：`SkillPhase` → `ESkillPhase`（统一 E 前缀）、`EAnimationLayer` → `SkillAnimationLayer`（匹配 README）、`ESkillEventType` 提取到独立文件。

## Item 3: ScriptableObject 配置层

创建三个 SO 类型：
- `GameplayEffectSO` — 统一持续效果（冷却=Buff=Debuff 同管道），对标 UE GAS UGameplayEffect
- `SkillDefSO` — 单技能完整定义，cooldownEffect 引用 GameplayEffectSO（非 inline duration+tag）
- `WeaponSkillSetSO` — 4 槽技能组映射

关键设计决策：贯彻架构文档"冷却就是对自身施加的 Duration Effect"原则，SkillDefSO 不内联 cooldownDuration/cooldownTag，而是引用 GameplayEffectSO。Phase 5 引入 Buff 时用同一类型。

## Item 4: 数据结构

四个 readonly struct：
- `DamageInfo` — CombatPipeline 产出，CombatComponent.ApplyDamage() 消费
- `SHitEvent` — 命中事件（Audio/VFX 订阅）
- `SSkillEvent` — 技能事件（UI 反馈）
- `SNoiseEvent` — 噪音事件（AI 感知）

## 审查修复（6 项）

| # | 问题 | 修复 |
|---|------|------|
| 1 | noiseType 是 string 而非 GameplayTagDefinitionSO | 配置层改 SO 引用，运行时改 GameplayTag struct |
| 2 | HasTagExact O(n) foreach | 改为 `_tags.Contains()` O(1) |
| 3 | EAnimationLayer.cs vs README SkillAnimationLayer | 重命名对齐 README |
| 4 | README 目录未更新 L1 GameplayTag | 移除旧条目 +加 L1 提升说明 +加 GameplayEffectSO |
| 5 | SkillPhase 缺 E 前缀 | 重命名为 ESkillPhase |
| 6 | ESkillEventType 内联在 SSkillEvent.cs | 提取到独立文件 |

## 关联

- Tech: `L4-combat/README.md`（目录结构更新）
- Plans: `short-term.md` Phase 4.1 Item 1-4 完成
- Versions: `v0.6.8`
- Previous session: `2026-06-02-gameplaytag-l1-promotion.md`
