# UIIconSlot — 通用槽位显示

> **Source**: `Assets/Scripts/Services/L2_UI/Components/UIIconSlot.cs`
> **Last Verified**: 2026-07-06 | **Verification**: All referenced files exist, signatures match code

技能槽、武器槽共用的图标槽组件。`[ExecuteAlways]` + Theme SO + DOTween 动画。2026-07-06 新增 hover 事件。

## Call Chain

```
AbilityBarOverlay.Update()
  → UIIconSlot.SetIcon() / SetCooldown() / SetSlotLabel()
  → UIIconSlot.OnPointerEnter() → onHoverChanged(slot, true)
  → UIIconSlot.OnPointerExit()  → onHoverChanged(slot, false)
```

| Called By | Calls |
|-----------|-------|
| `AbilityBarOverlay`, `WeaponBarOverlay` | `UIThemeSO` — font/size for labels |
| `UnityEngine.EventSystems` | `DOTween` — cooldown fill |

## Coupled Modules

| Direction | Module | Relationship |
|-----------|--------|-------------|
| Consumer → | `UIThemeSO` | 读取 bodyFont + smallFontSize |
| Consumed by | `AbilityBarOverlay` | 技能槽实例化 + 刷新 + hover |
| Consumed by | `WeaponBarOverlay` | 武器槽实例化 + 刷新 |

## Public Properties

| Property | Type | Purpose |
|----------|------|---------|
| `onHoverChanged` | `Action<UIIconSlot, bool>` | hover 状态变化回调，参数 (slot, isHovered) |

## Methods

### SetIcon()
```csharp
public void SetIcon(Sprite sprite)
```
- **Purpose**: 设置槽位图标，null=清空并隐藏 iconImage
- **Callers**: AbilityBarOverlay.RefreshSlots()

### SetCooldown()
```csharp
public void SetCooldown(float remaining, float total)
```
- **Purpose**: 设置冷却覆层。remaining≤0 隐藏，>0 显示 Radial360 sweep
- **Callers**: AbilityBarOverlay.RefreshSlots()
- **Notes**: 首次进入冷却 snap fillAmount 避免 DOTween 追赶

### SetSelected()
```csharp
public void SetSelected(bool selected)
```
- **Purpose**: 选中状态边框
- **Callers**: AbilityBarOverlay.RefreshSlots()

### SetKeybind()
```csharp
public void SetKeybind(string key)
```
- **Purpose**: 快捷键标签（"Q", "E", "1"）
- **Callers**: AbilityBarOverlay.EnsureSlots()

### SetSlotLabel()
```csharp
public void SetSlotLabel(string label)
```
- **Purpose**: 槽位底部文字（技能名/武器名）
- **Callers**: AbilityBarOverlay.RefreshSlots()

### SetEmpty()
```csharp
public void SetEmpty()
```
- **Purpose**: 清空所有显示

### OnPointerEnter() / OnPointerExit()
```csharp
public void OnPointerEnter(PointerEventData eventData)
public void OnPointerExit(PointerEventData eventData)
```
- **Purpose**: IPointerEnterHandler / IPointerExitHandler 实现，触发 `onHoverChanged`
- **Callers**: Unity EventSystem (自动)
- **Added**: 2026-07-06

## Internal Mechanics

`Awake()` 初始化：selectionBorder=off, cooldownFill=Filled/Radial360/Top, 字体从 theme 读取。`Update()` Edit mode 下 snap cooldown fill。`OnDestroy()` 清理 DOTween。

## Future Plans

| Plan | Status | Source |
|------|--------|--------|
| 快捷键配置化（非硬编码 Q~U） | 待做 | AbilityBarOverlay TODO |
