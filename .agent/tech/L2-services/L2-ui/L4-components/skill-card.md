# SkillCard — 技能详情卡片

> **Source**: `Assets/Scripts/Services/L2_UI/Components/SkillCard.cs`
> **Last Verified**: 2026-07-06 | **Verification**: All referenced files exist, signatures match code

技能详情弹出卡片。hover 技能槽时显示完整信息：图标、名称、描述、冷却、激活方式、阶段时序、效果（伤害修正/硬直/消耗/Buff）、连招、噪音。

## Call Chain

```
AbilityBarOverlay.OnSlotHover()
  → SkillCardData.FromActiveAbility(ActiveAbilitySO)  // 提取数据
  → SkillCard.SetData(data)                           // 填充卡片
  → SkillCard.SetVisible(true)                        // fade in
```

| Called By | Calls |
|-----------|-------|
| `AbilityBarOverlay` — hover 时 SetData + SetVisible | `UIThemeSO.GetColorSet()` — 主题色 |
| 任何持有 ActiveAbilitySO 的调用方 | `DOTween` — CanvasGroup fade |

## Coupled Modules

| Direction | Module | Relationship |
|-----------|--------|-------------|
| Consumer → | `UIThemeSO` | 读取颜色/字体配置 |
| Consumed by | `AbilityBarOverlay` | hover 时填充数据并显示 |
| Data → | `SkillCardData` | 数据结构，解耦 Ability 层 |

## Public Properties

| Property | Type | Purpose |
|----------|------|---------|
| `_visible` | `bool` | 当前显隐状态 |

## Methods

### SetData()
```csharp
public void SetData(SkillCardData data)
```
- **Purpose**: 填充卡片所有字段并刷新显示
- **Params**: `data` — 从 `ActiveAbilitySO` 提取的展示数据
- **Callers**: `AbilityBarOverlay.OnSlotHover()`

### SetVisible()
```csharp
public void SetVisible(bool visible)
```
- **Purpose**: 控制卡片显隐，带 DOTween fade 动画 (0.15s)
- **Params**: `visible` — true=fade in, false=fade out 后 SetActive(false)
- **Callers**: `AbilityBarOverlay.OnSlotHover()`
- **Notes**: Edit 模式下直接 snap alpha，不走动画

### RefreshDisplay()
```csharp
private void RefreshDisplay()
```
- **Purpose**: 遍历所有序列化字段，按数据填充文本/显隐，无数据段自动隐藏
- **Notes**: Effects/Timing/Combo 段通过 Section GameObject 显隐控制

### ApplyTheme()
```csharp
private void ApplyTheme()
```
- **Purpose**: Awake 时从 UIThemeSO 读取 surface 颜色 + bodyFont 应用到所有 TMP_Text

## Internal Mechanics

`[ExecuteAlways]` + `Awake()` ApplyTheme + `OnValidate()` delayCall 刷新。DOTween 调用在 `if (!Application.isPlaying) return` 守卫下。`OnDestroy()` 中 `DOTween.Kill(canvasGroup)` 清理。

## Future Plans

| Plan | Status | Source |
|------|--------|--------|
| 屏幕边界 clamp | 待做 | 2026-07-06 session |
| hover 延迟 (防快速划过) | 待做 | 2026-07-06 session |
| 被动技能展示模式 | 待做 | — |
