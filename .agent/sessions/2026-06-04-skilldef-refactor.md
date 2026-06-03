# 2026-06-04 SkillDefSO 配置层重构

> v0.6.10

## 做了什么

从「所有属性平铺在一个 SO」拆为资产组合：

- **SkillActivationSO** — 封装「怎么放」（12 字段）：激活方式、动画、阶段标记、取消规则
- **SkillSearchSO** — 封装「往哪打」：抽象基类 + ConeSearchSO / RaySearchSO / CircleSearchSO
- **CombatComponent** — 战斗组件占位，注入 CharacterActor
- **WeaponSkillSetSO 删除** — 武器→技能映射是角色层职责

## 设计决策

1. **动画 = 阶段机时间轴本体**。Phase markers 描述动画的自然阶段，animationSpeed 是唯一调参旋钮
2. **单技能 vs 连续技**：1 SkillDefSO = 1 SkillActivationSO = 1 动画。多段通过 comboNextSkills 串联
3. **删除 canMoveWhileCasting**：ESkillAnimationLayer (FullBody/UpperBody) 已区分
4. **删除 transitionTime**：总时长由动画决定
5. **删除 recoveryCancelWindow**：不需要格斗游戏精度
6. **Combat 不关心槽位**：TryActivate 接收 SkillDefSO 直接执行

## 进行中

SkillDefSO 重构未完成。16 类属性中 Identity/SkillActivationSO/SkillSearchSO 已确认，其余 11 类待逐套分析。

## 已修改文件

- `Config/SkillDefSO.cs`
- `Actor/CharacterActor.cs`
- `Combat/CombatComponent.cs`
