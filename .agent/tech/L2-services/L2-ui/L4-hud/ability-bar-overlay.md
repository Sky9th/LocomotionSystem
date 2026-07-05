# AbilityBarOverlay — 主动技能栏

> **Source**: `Assets/Scripts/Services/L2_UI/HUD/AbilityBarOverlay.cs`
> **Last Verified**: 2026-07-06 | **Verification**: All referenced files exist, signatures match code

技能栏 Overlay。动态实例化 UIIconSlot，事件驱动刷新，hover 弹出 SkillCard 详情卡片。

## Call Chain

```
UIOverlay (base)
  → AbilityBarOverlay
    ├── Start()
    │     └── Instantiate(SkillCard, transform)  // 创建卡片
    ├── Update()                                  // 0.15s 刷新
    │     ├── Entity.Query.Ability.ActiveAbilities
    │     ├── EnsureSlots(count)
    │     │     ├── Instantiate(UIIconSlot, slotContainer)
    │     │     ├── SetKeybind(Q~U)
    │     │     └── slot.onHoverChanged += OnSlotHover
    │     └── RefreshSlots(ability)
    │           ├── SetIcon / SetSlotLabel / SetCooldown / SetSelected
    │           └── ability.GetCooldownRemaining()
    └── OnSlotHover(index, hovered)
          ├── SkillCardData.FromActiveAbility(_actives[index])
          ├── SkillCard.SetData(data)
          ├── SkillCard.SetVisible(true/false)
          └── RectTransform.position = slot position + offset
```

## Coupled Modules

| Direction | Module | Relationship |
|-----------|--------|-------------|
| Consumer → | `UIIconSlot` | 槽位 prefab 实例化 + 刷新 + hover 订阅 |
| Consumer → | `SkillCard` | hover 时实例化 + 填充 + 显隐 |
| Consumer → | `SkillCardData` | 从 ActiveAbilitySO 提取数据 |
| Reads from | `Entity.Query.Ability` | ActiveAbilities + GetCooldownRemaining + IsActive |

## Public Properties

(None — all logic private/internal)

## Methods

### Start()
```csharp
private void Start()
```
- **Purpose**: 实例化 SkillCard（从 skillCardPrefab），初始隐藏
- **Callers**: Unity lifecycle

### Update()
```csharp
private void Update()
```
- **Purpose**: 0.15s 间隔刷新槽位：读 ActiveAbilities → EnsureSlots → RefreshSlots
- **Callers**: Unity lifecycle

### EnsureSlots()
```csharp
private void EnsureSlots(int count)
```
- **Purpose**: 动态创建/激活槽位，订阅 hover 事件，SetKeybind
- **Callers**: Update()

### RefreshSlots()
```csharp
private void RefreshSlots(AbilityQuery ability)
```
- **Purpose**: 遍历 actives，逐槽位刷新图标/标签/冷却/选中状态
- **Callers**: Update()

### OnSlotHover()
```csharp
private void OnSlotHover(int index, bool hovered)
```
- **Purpose**: hover 进入→填充 SkillCard 数据+定位+显示；离开→隐藏
- **Callers**: UIIconSlot.onHoverChanged (event)
- **Added**: 2026-07-06

## Internal Mechanics

继承 `UIOverlay`。硬编码 7 键 Q~U。`_hoveredIndex` 防止 exit 时误关其他 slot 的卡片。Card 定位：pivot=(0.5, 1)，position=slot.position + cardOffset。

## Future Plans

| Plan | Status | Source |
|------|--------|--------|
| 快捷键配置化 | 待做 | 代码 TODO |
| 被动技能并行展示 (PassiveBarOverlay 合并) | 待做 | PassiveBarOverlay 已独立 |
