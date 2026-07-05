# 2026-07-06 — SkillCard UI Component

## Background

技能栏 (AbilityBarOverlay) 目前只显示 UIIconSlot 图标槽——图标 + 冷却覆层 + 快捷键标签。用户 hover 或选中技能时无法查看该技能的详细信息（阶段时序、伤害修正、消耗、硬直、Buff、连招、噪音等）。需要一个 hover 弹出的技能详情卡片，从 ActiveAbilitySO 自动提取并展示完整信息。

这是 S5 技能 UI 管线的第一个组件，后续还会扩展被动技能展示和升级预览。

## Changes

### L2_UI — 新增组件
- `SkillCardData.cs` — 展示数据结构，`FromActiveAbility()` 工厂方法从 ActiveAbilitySO 提取所有字段，预格式化效果文本为字符串数组
- `SkillCard.cs` — `[ExecuteAlways]` MonoBehaviour 组件，`SetData(SkillCardData)` 填充，`SetVisible(bool)` 控制 fade 显隐，ApplyTheme() 从 UIThemeSO 读取颜色/字体
- `CreateSkillCardPrefab.cs` — Editor 菜单脚本 (`RedDust > UI > Create SkillCard Prefab`)，一键创建完整 Prefab 层级 + 组件连线
- `SkillCard.prefab` — 根节点 VerticalLayoutGroup + ContentSizeFitter，子节点：Icon/Name/Description/Cooldown/ActivationInfo/TimingSection/EffectsSection/ComboSection/Noise

### L2_UI — UIIconSlot hover 事件
- 实现 `IPointerEnterHandler`, `IPointerExitHandler`
- 暴露 `onHoverChanged(UIIconSlot, bool)` 回调

### L2_UI — AbilityBarOverlay hover 集成
- 新增 `skillCardPrefab` 序列化字段
- `Start()` 中实例化 SkillCard 并隐藏
- `OnSlotHover(index, hovered)` — 填充数据 + 定位 + SetVisible

### Fonts
- `NotoSansSC[wght] SDF.asset` — 通过 Font Asset Creator 重新生成，Static + 2048×2048 + 7000汉字字符集
- `Assets/Fonts/7000汉字+符号+英文字符集.txt` — 字符集文件

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| SkillCardData struct 解耦 UI 与 Ability 层 | A: 直接传 ActiveAbilitySO 给 SkillCard → UI 层需要知道 EffectSO 子类，耦合过深。B: 用 UnityEvent/ScriptableObject 事件通道 → 过度设计。 | SkillCardData 纯数据 struct，效果文本在工厂中预格式化——UI 只做显示 |
| Prefab 用编辑器脚本创建 | A: 手工建 YAML prefab → 易出错，fileID 引用复杂。B: 手拖创建 → 每次重建手动操作 | 菜单脚本一步到位，可重复执行 |
| hover 事件放在 UIIconSlot | A: 放在 AbilityBarOverlay 做 raycast 检测 → 重复造轮子。B: 新建 HoverableSlot wrapper → 增加一层间接 | UIIconSlot 本身就有交互语义，加 IPointerEnter/Exit 最直接，武器槽也能复用 |
| 生成 7000 常用汉字 SDF atlas | A: Dynamic 模式按需生成 → 运行时首次渲染字符会闪 □。B: 只生成项目用到的 ~50 个中文字 → 新技能名可能缺字 | Static atlas 一次生成，消除所有缺失警告 |

## Known Issues

- [ ] SkillCard 定位在 slot 正上方，未做屏幕边界 clamp——窗口边缘时卡片可能超出屏幕 (P2)
- [ ] 字体 atlas 当前 2048×2048，新增大量非 CJK 字符时需要确认 atlas 空间充足 (P2)
- [ ] 没有做 hover 延迟——快速划过多个 slot 时卡片频繁切换 (P3)

## Cross-References

### Related Sessions
- None — 首个 SkillCard 实现

### Related Plans
- [../plans/short-term-plan.md](../plans/short-term-plan.md) — S5 技能 UI 管线

### Related Tech Docs
- [../tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md](../tech/L2-services/L2-modules/L3-ability/ability-pipeline-design.md) — 技能管道设计，数据来源
- [../tech/L2-services/L2-modules/L3-ability/ability-component.md](../tech/L2-services/L2-modules/L3-ability/ability-component.md) — AbilityComponent 提供 ActiveAbilities 查询

### Related Design Docs
- None

### Flag for Design Doc Creation
- [ ] No design doc needed — SkillCard 是纯展示组件，无设计面变更（展示内容由已有技能数据决定）
