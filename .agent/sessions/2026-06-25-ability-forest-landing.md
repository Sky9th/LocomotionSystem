# 2026-06-25 AbilityForest 落地 + Actor 联动

## 背景
AbilityTreeSO 已有静态数据（7 技能 + 3 天生树），但运行时"角色持有哪些树？哪些节点解锁？当前武器下可用哪些技能？"是空白。

## 核心决策

### 命名与归属
- 名称从 `AbilityTreeManager` → `ActiveAbilityTrees` → `AbilityForest`（树→森林，与项目命名风格一致）
- 归属两轮辩论后定：`RedDust.Character.Ability`，文件 `L3_Character/Ability/`
- 关键理由：UnlockedNodeIds 是角色状态（和 HP 同类），非通用管道。CharacterCombat 先例——操作 Ability 类型但归属 Character。

### 多来源活跃树集合
- 单一 `List<ActiveTree>` 而非三层固定槽位
- 来源：innate / talent / routine / learned
- `source: object` 标识精确移除（卸武器 → RemoveBySource(itemInstance)）

### 自动 Resolve
- 所有状态变化方法内部自动调用 Resolve：AddTree/RemoveBySource/UnlockNode/SetWeaponTags
- CharacterActor 不手动调 Resolve—ctx.AbilityForest 引用即最新结果

### 树级武器过滤
- 武器兼容是树级开关，不是技能级
- compatibleWeaponTags 为空 = 不过滤（徒手/纯被动树），非空 = 必须与 weaponTags 有交集

## 产出
- `AbilityForest.cs` — 纯 C# 运行时类
- `ability-forest.md` — 设计文档
- CharacterActor / BuildContext / PlayerDirector 联动改造
