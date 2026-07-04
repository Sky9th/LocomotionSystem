# 2026-07-04 — Damage Number UI Hint System

## Background

技能 Pipeline 伤害计算链路（ExecutionState → AbilityReactor → HitEvent）已在前几次提交中完整落地，但伤害结果没有玩家可见的视觉反馈。需要实现简易 UI 伤害飘字来提供命中反馈。同时需考虑后续尸潮场景——大量单位同时受伤时不能有性能问题。

Part of the ability pipeline feedback loop (step 8 — Broadcast → UI consumers).

## Changes

### L2_UI/HUD — DamageNumber System
- Added `DamageNumberOverlay` — UIOverlay subclass, subscribes to HitEvent via GameContext.EventHub, manages object pool of floating text widgets
- Added `DamageNumberWidget` — single floating damage number, DOTween DOAnchorPosY rise + DOFade out, auto-recycle to pool on animation complete
- Coordinate conversion: `Camera.WorldToScreenPoint` → `RectTransformUtility.ScreenPointToLocalPointInRectangle`

### L2_UI/Core
- Added `DamageNumberOverlay` to `UIOverlayId` enum
- UIService `HandleGameState(Playing)` now shows DamageNumberOverlay alongside Vitals/AbilityBar/WeaponBar

### Prefabs
- Created `DamageNumberOverlay.prefab` — full-screen stretch RectTransform, no CanvasGroup (interaction disabled via override)
- Created `DamageNumberWidget.prefab` — TMP_Text, Anchor=(0.5,0.5), Pivot=(0.5,0.5), default inactive

### UIPanelConfigSO
- Registered DamageNumberOverlay overlay entry in PanelConfig

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Screen Space Overlay Canvas | A: World Space Canvas per character — 尸潮上百 Canvas → Draw Call 爆炸. B: World Space root Canvas (code-created) — 无行业先例，脱离 UIService 生命周期. | Screen Space Overlay 单 Canvas 合批，天然由 UIService 管理，行业标准做法 |
| `RectTransformUtility.ScreenPointToLocalPointInRectangle` 坐标转换 | A: `anchoredPosition = screenPos / scaleFactor` — Canvas Scaler 不同 mode 下不准. | Unity 官方 API，自动处理 anchor/pivot/scaler，跨分辨率可靠 |
| Widget anchor/pivot = (0.5, 0.5) 居中 | Anchor=(0.5, 1.0) — 与 RectTransformUtility 返回的原点（overlay pivot 0.5,0.5 即中心）不一致，造成坐标错位 | 两个中心对齐，localPos(0,0) = 屏幕中央 = widget 中央 |
| `override PlayEnterSequence => null` | 保留基类动画 → blocksRaycasts=true 拦截全屏点击 | Overlay 不需要 fade 动画且不能拦截交互 |
| 显示预减免原始伤害 | 改 AbilityReactor 传 finalAmount → 改管线，影响面大 | 第一期最小改动，后续扩展事件结构体 |
| 过滤 Amount <= 0（完全回避） | 不改 AbilityReactor 的 Raise 逻辑 — 观察者模式语义不改 | Overlay 侧过滤，不改管线 |
| 对象池上限 30，耗尽静默丢弃 | A: 动态扩容 → GC 风险. B: 复用最早 → 动画打断不干净 | 尸潮场景上限 30 合理，超出丢弃比 GC spike 好 |

## Known Issues

- [ ] 显示的是预减免伤害而非终伤 — 护甲减免后玩家看到的是减免前的数字（P2 — 后续扩展事件结构体携带 finalAmount）
- [ ] `DOTween.Sequence()` 每次 Play 分配新对象 — 尸潮峰值可能产生 GC 压力（P2 — profiling 后评估是否需要预分配）
- [x] Pool double-reference bug（CreateWidget 同时 push+return）— 已修复
- [x] CanvasGroup blocksRaycasts 默认值泄露 — OnInitialize 显式设为 false
- [x] base.OnDestroy() 缺失 — 已添加

## Cross-References

### Related Sessions
- [2026-07-04-session-prompt-v0.26.0](../memory/../sessions/2026-07-04-session-prompt-v0.26.0.md) — same date, different topic (Properties refactor context)

### Related Plans
- [../plans/eventual-meandering-bird.md](../plans/eventual-meandering-bird.md) — Damage Number Hint System implementation plan (with industry evidence)

### Related Tech Docs
- tech/L2-services/L2-ui/HUD/damage-number-overlay.md — to be created
- tech/L2-services/L2-ui/HUD/damage-number-widget.md — to be created

### Flag for Design Doc Creation
- [x] No design doc needed — damage numbers are a UI implementation of existing damage system, no new gameplay mechanics or design-facing changes.
