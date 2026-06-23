# L3_SkillTree · 技能树系统

> ⚠ **DRAFT** — 未定稿。核心方向确定，但与 L3_Ability 的关系尚未厘清。

> **Last Verified**: 2026-06-23 | **Verification**: DESIGN PHASE — 代码尚未创建

## 层级定位

独立 L3 模块，位于 `Services/Modules/L3_SkillTree/`。技能树是技能的真实来源——武器只决定能不能用，技能树决定会什么。

角色"装备"一个技能树到技能树槽（容器逻辑），和装备武器到身体槽是同一种模式。

## 核心

**武器不决定技能。技能树 × 武器 = 可用技能。**

```
Q/E/R/F 栏 = 当前武器 Tag ∩ 当前技能树.abilities
  太刀(tag=Weapon.Blade) + 八极拳(compatibleTags=[Weapon.Blade])
    → [顶肘, 崩拳, 贴山靠, ...]
  太刀(tag=Weapon.Blade) + 泰拳(compatibleTags=[Weapon.Blade])
    → [膝撞, 肘击, 鞭腿, ...]
  空手 + 八极拳(compatibleTags=[])       ← 空数组 = 空手可用
    → [顶肘, 崩拳, 贴山靠, ...]
```

## 架构

```
SkillTreeSO : EntityDefSO
  ├── [继承] PropertyTreeSO Template     ← 技能树属性
  ├── [继承] string OverridesJson
  │
  ├── displayName: "八极拳"
  ├── compatibleWeaponTags: GameplayTag[]    ← 声明兼容哪些武器
  │     [Weapon.Blade, Weapon.Staff, Weapon.Axe]
  │     空数组 = 空手可用
  │
  └── abilities: AbilityDefSO[]             ← 技能树提供的技能
        [顶肘, 崩拳, 贴山靠, ...]
```

**兼容性方向**：技能树声明自己兼容哪些武器标签——不是武器声明兼容哪些技能树。同一种武器（太刀）能用的技能树是可扩展的（新 DLC 加新流派），不需要改太刀的 ItemDefSO。

## 与容器的关系

角色有一个"当前技能树"槽——`Container<SkillTreeSO>`。容量=1。和身体装备槽同一种容器逻辑。

## 调用链

```
技能栏求交:
  EquipmentComponent.GetActiveWeapon() → ItemInstance.Def（读 PropertyTree 中的 ItemTags）
  SkillTreeSlot.Current → SkillTreeSO
  求交: ItemTags ∩ SkillTreeSO.compatibleWeaponTags
    → 命中 → SkillTreeSO.abilities 填入 Q/E/R/F

技能激活:
  同 Ability Pipeline——SkillTreeSO 提供 AbilityDefSO 引用，
  AbilityExecutor.TryActivate() 执行。
```

## ❓ 与 L3_Ability 的关系（待厘清）

当前问题：
- AbilityDefSO 已存在，属于 L3_Ability 模块
- SkillTreeSO 持有 `AbilityDefSO[]`——跨模块引用
- 技能树提供的技能和角色空手通用技能（翻滚/脚踢）是什么关系？
- 技能树的"连击链"（L→L→H→终结技）是否属于技能树系统还是 Ability 系统？
- 装备技能树时是否需要"卸下"旧技能？技能树槽的 Place/Remove 逻辑？

## 设计决策

| 决策 | 原因 |
|------|------|
| 兼容性方向：技能树→武器 | 新流派不要求修改已有武器定义。DLC/Mod 加技能树即可 |
| 技能树是独立 SO（非 AbilityDefSO 的集合） | 技能树有自身的身份（名称、稀有度、属性），不只是技能列表 |
| 角色只装备一个技能树 | 设计文档的"选择一个套路+一把武器=确定技能栏" |
| 和装备同理——放入容器=激活 | 同一套容器逻辑。学到的技能树在"已掌握"容器，装备的在"当前"槽 |

## 未定问题

- [ ] SkillTreeSO 继承 EntityDefSO 还是独立？
- [ ] 连击链（SComboLink[]）是否放在技能树里？
- [ ] 技能树稀有度（残卷/完整/精妙/绝学）是升级系统还是不同 SO？
- [ ] 和 L3_Ability 的模块边界在哪？

## 参考

| 来源 | 内容 |
|------|------|
| ability-inventory.md §1.1 | 武学套路——11 个流派、4 级稀有度、连击链规则 |
| ability-inventory.md §1.0 | 武器基础技——所有角色捡到武器即拥有的默认技能 |
| ability-inventory.md §1.3 | 套路与武器对照——哪些套路适用哪些武器 |
