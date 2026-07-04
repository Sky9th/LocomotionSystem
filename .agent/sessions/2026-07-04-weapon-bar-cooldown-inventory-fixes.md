# 2026-07-04 — Weapon Bar + Cooldown UI + Inventory Deep Cache Fixes

## Background

武器栏之前动态读取背包物品，切换装备后 label 消失。冷却覆层首帧有 0→1 动画闪烁。技能消耗系统报错 `Property 'Stamina' not in PropertyTable`，因为 `CostState` 用 `cost.def.Id`（短名 "Stamina"）直接查 `PropertyTable._structure`（存完整路径 "Vitals/Stamina"）。这些零散问题阻碍了武器/技能 UI 的基础验证流程。

## Changes

### UI — WeaponBarOverlay
- 固定三槽 [空手, 剑, 手枪]，不再从背包 Inventory 动态读取
- 新增 `ResolveWeapon(bpInv, equip, entityId)` 在背包 + RightHand 两处查找武器实体
- 选中态 `selectedIndex` 同步 `equip?.RightHand.Id`
- `SetSelected` 必须在 `SetEmpty` 之后调用（`SetEmpty` 内部会 `SetSelected(false)`）
- `FallbackLabels` 数组硬编码 "空手"/"剑"/"手枪" 用于实体未找到时的展示
- `equip?.RightHand` null 保护（`equip` 可为 null）

### UI — UIIconSlot.SetCooldown
- 冷却首帧 (`justStarted`) 直接 snap `fillAmount` + `DOTween.Kill(cooldownFill)` 杀旧 tween
- 防止残留 DOTween 覆盖 snap 值，消除 0→1→递减 的视觉闪烁

### Inventory — InventoryQuery
- 新增 `AllItemsDeep`（IReadOnlyDictionary<string, Entity>），key 格式 `{entityId}/{slotPath}`
- 新增 `CollectRecursive` 递归遍历嵌套容器，maxDepth=10
- 新增 `FindItemDeep(entityId)` 在所有层级搜索
- 新增 `RefreshAllItemsDeep()` 手动重建缓存
- `slot.Items == null` 空保护

### Ability — CostState + PropertyTable
- `PropertyTable` 新增 `TryGetPath(PropertyDefSO def, out string path)` 反查方法（O(n)，已标 TODO）
- `CostState.PeekViaTable` / `ModifyViaTable` 改用 `TryGetPath` 替代 `cost.def.Id` 直传

### Character — CharacterActor + CharacterConst
- `CharacterConst.Vitals` 新增 `Stamina = "Vitals/Stamina"`
- `CharacterActor.Start()` 新增体力回复 `FloatModifier`（PerSecond Delta=25f）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| `TryGetPath` 用 O(n) 遍历 | A: 构造时建反向索引 `Dictionary<PropertyDefSO, string>` → O(1) | 当前属性表 <50 项，O(n) 够用。已标 TODO，达到数百时再建索引 |
| WeaponBarOverlay 固定三槽硬编码 | A: 数据驱动从装备槽读 | 临时版快速验证武器切换流程，后续迁移至数据驱动 |
| 冷却 snap + `DOTween.Kill` | A: 改 `SetCooldown` 参数增加 snap 模式；B: 动画层增加 `from` 参数 | 最小改动，不改 `SetCooldown` 公共语义 |
| `SetSelected` 放在 `SetEmpty` 之后 | A: 让 `SetEmpty` 增加 `keepSelection` 参数 | `SetEmpty` 是通用清空，增加参数影响所有调用方；调序更简单 |

## Known Issues

- [ ] `AllItemsDeep` 缓存永不过期 — 调用方需手动 `RefreshAllItemsDeep()`，物品增删后未接入自动失效（P1 — 后续加 invalidation hook）
- [ ] `PropertyTable.AddModifier` 内部 `KeyNotFoundException` — `EnsureFloatState` 静默失败后裸调 `_floatStates[key]`，Hunger + Stamina 两个 modifier 都有此风险（P2 — 预存问题，需改 `AddModifier` 本身）
- [ ] `WeaponBarOverlay.FallbackLabels` 数组长度与 weapons 列表硬耦合 3（P3 — 临时三槽，重构时解除）

## Cross-References

### Related Sessions
- [2026-07-04-ui-weapon-ability-bar-prefabs.md](2026-07-04-ui-weapon-ability-bar-prefabs.md) — 武器栏技能栏 Prefab 落地，本次基于此做 UI 修复
- [2026-07-04-ability-icon-generation.md](2026-07-04-ability-icon-generation.md) — 技能图标生成，与冷却 UI 同一轮

### Related Tech Docs
- [tech/L2-services/L2-modules/L3-container/container.md](../tech/L2-services/L2-modules/L3-container/container.md) — `RdContainer.AllItems()` / `SlotsOrdered` 结构
- [tech/L2-services/L2-modules/L3-properties/property-table.md](../tech/L2-services/L2-modules/L3-properties/property-table.md) — `PropertyTable._structure` 路径格式

### Flag for Design Doc Creation
- [x] No design doc needed — all changes are internal fixes / temporary test code, no design-facing changes.
