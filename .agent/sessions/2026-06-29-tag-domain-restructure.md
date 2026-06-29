# 2026-06-29 — GameplayTag 按模块产出域重构

## Background

原有 Tag 系统是一棵 10 根的平铺树（State/Skill/Damage/Effect/Noise/Impact/Stat/Body/Equip/Actor），所有 Tag 混在一起，编辑器 TagPicker 无约束，策划无法区分每个字段该选哪个根的 Tag。盘上实际状态已与文档严重不一致——State 和 Effect 根完全缺失，同时又出现了文档未记录的 Ability/AbilityTree/Cost/Execute/Weapon 等根。

本次按"每个模块只定义自己产出的 Tag 域"原则，逐模块梳理 L1→L3 各层级的 Tag 消费与产出关系，重构整个 Tag 域架构，并落地技术文档。

## Changes

### Tag 域文档体系
- `gameplay-tag.md` — 标记 ⛔ OUTDATED，保留作历史参考
- `gameplay-tag-ability.md` — Ability 模块产出：Definition/Tree/Execute/Damage/Effect/Impact 6 个二级节点。流派 = 动画包，`Definition.Active.{Melee|Ranged}.{流派}.{武器}.{技能}`
- `gameplay-tag-identity.md` — Identity 模块产出：Species/Kind/Faction/Role。根从 Actor 改名为 Identity，子域 Identity→Kind
- `gameplay-tag-body.md` — Character 模块产出：Form/Posture/Locomotion，枚举派生
- `gameplay-tag-entity.md` — Entity 系统产出：Weapon(Melee/Ranged) + Item(Weapon/Armor/Ammo/...)
- `gameplay-tag-grip.md` — 独立根：纯握法 6 标签 (Unarmed/OneHanded/TwoHanded/DualWield/Fencing/Shield)
- `README.md` — 索引更新

### 全量 Tag 引用盘点
- 扫描全部 40 个引用 GameplayTag 的 .cs 文件，识别出 18 个 Tag 消费字段
- L1_Core（非 GameplayTag 目录）：无
- L2 Service 层：无
- L3_Ability：14 个字段（最大消费者）
- L3_Identity：2 个字段
- L3_Character：1 个字段（GripAnimationTableSO.gripTag）
- L3_Container：1 个字段（SlotDef.AcceptTags，string[] 形式）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Tag 根按模块产出域划分 | A: 保持原 10 根平铺结构 — 策划无法区分选 Tag 域；B: 按运行时行为分类 | A 就是当前混乱的根源。模块产出边界清晰、责任明确 |
| Equipment 拆为 Entity.Weapon + Entity.Item + Grip 三个独立域 | A: 单 Equipment 根包含 Type/Grip/Slot — 三者消费方完全不同且命名冲突 | Grip 是动画域独立根，Weapon+Item 归 Entity 系统产出 |
| Ability.Definition 流派 = 动画包 | A: 流派按战斗类型分 Combat/Utility — 太泛；B: 流派按武学套路分 — 应该是技能树分类不是技能定义 | 动画包是客观约束，美术有几套动画就几个流派 |
| Grip 纯握法 6 标签，不编码武器类型 | A: Grip.OneHandedBlade + Grip.OneHandedSidearm — 武器信息泄漏进 Grip | 武器类型走 Weapon 域，GripAnimationTableSO 远期用 Grip×Weapon 双维度匹配 |
| Cooldown 并入 Definition 作为 sharedCooldownTag 引用节点 | A: 独立 Cooldown 二级节点 — 镜像 Definition 结构，冗余 | 直接用 Definition 的分类节点做冷却组 |

## Known Issues

- [ ] Tag 资产尚未实际创建——所有文档仅为设计定案 (P1)
- [ ] TagPicker rootFilter 机制已实现但从未使用——需要按字段添加约束 (P2)
- [ ] SlotDef.AcceptTags 使用 string[] 而非 GameplayTagDefinitionSO[] (P2)
- [ ] 旧盘 Tag 资产需要迁移/重命名以匹配新结构 (P1)
- [ ] AbilityDefSO 已删除（替换为 ActiveAbilitySO/PassiveAbilitySO）
- [ ] Noise 标签域待后续处理（独立根，AI 系统消费）
- [ ] 单手持手枪（Pistol1H）缺动画包

## Cross-References

### Related Plans
- [../plans/ability-tag-ability-tag-purring-grove.md](../../plans/ability-tag-ability-tag-purring-grove.md) — Ability Tag 设计计划

### Related Tech Docs
- [tech/L1-core/gameplay-tag.md](../tech/L1-core/gameplay-tag.md) — ⛔ OUTDATED
- [tech/L1-core/gameplay-tag-ability.md](../tech/L1-core/gameplay-tag-ability.md) — 新建
- [tech/L1-core/gameplay-tag-identity.md](../tech/L1-core/gameplay-tag-identity.md) — 新建
- [tech/L1-core/gameplay-tag-body.md](../tech/L1-core/gameplay-tag-body.md) — 新建
- [tech/L1-core/gameplay-tag-entity.md](../tech/L1-core/gameplay-tag-entity.md) — 新建
- [tech/L1-core/gameplay-tag-grip.md](../tech/L1-core/gameplay-tag-grip.md) — 新建

### Flag for Design Doc Creation
- [ ] No new design doc needed — this was architectural documentation restructuring.
