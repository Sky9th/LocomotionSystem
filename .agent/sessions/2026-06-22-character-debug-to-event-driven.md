# 2026-06-22 — Character Debug → Event-Driven + Naming Unified

## Background

Relax/Combat 形态开发期间，PlayerDirector 留下了两处临时 debug 按键：Alpha1~3 切换 Grip、Alpha4~5 切换 BodyForm。Combat 输入事件已补上（Equip1/2/3InputEventSO），需要将 debug 手段改为事件驱动。同时 Combat/System 事件的 menuName 使用空格分隔、实际 .asset 文件名也带空格、fileName 带多余 EventSO 后缀——与 Player 事件的 PascalCase 规范不一致。

## Changes

### PlayerDirector — Debug → Event-Driven
- 删除 `ProcessDebugGripSwitch()`、`ProcessDebugCombatToggle()`、`debugGripIndex`、`currentBodyForm`
- 新增 `ProcessEquipInput()`: Equip1/2/3 事件 → toggle GripTable entries[0/1/2] grip tag，武器互斥
- 新增 `equippedSlots[]` 字段追踪装备状态
- `ResolveBodyForm()` 改为派生：任意 slot 装备 → Combat，否则 Relax
- 新增 `ProcessSkillInput()`: 聚合 Skill1/Skill2 激活逻辑
- 新增 `TryActivateSkill()`: 统一空值检查和日志

### PlayerInput — Equip 事件绑定
- 新增 Equip1/2/3 字段、属性、BindEvents/UnbindEvents、Handler、ClearFrameSignals
- 修复 typo: `SencondSkillRequested` → `SecondSkillRequested`（5 处）

### L2_Input — 命名统一
- ButtonInputEventSO / Vector2InputEventSO / FloatInputEventSO: 移除 CreateAssetMenu
- Combat/System 8 个事件 SO: menuName 去空格（`"Skill 1"` → `"Skill1"`）+ fileName 去 EventSO 后缀（`"Skill1EventSO"` → `"Skill1"`）
- 8 对 .asset + .meta: 重命名去空格（`Equip 1.asset` → `Equip1.asset`）
- 新增 Equip3InputEventSO.cs / Skill3InputEventSO.cs

### CharacterActor
- 更新 TODO 注释：BodyForm 由 Director 意图驱动，非临时方案

### InputSystem
- InputSystem_Actions.inputactions 新增 Equip1/2/3 按键绑定

### Prefabs
- Player.prefab / GameManager.prefab: 连线 Equip SO 到 EventHub / InputService

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| BodyForm 由装备状态派生而非独立 toggle | A: 保留独立 BodyForm toggle 键 → 状态不同步。B: 新增专门的 CombatStance 事件 → 过度设计。 | 派生逻辑简单可靠，单一事实源（装备状态） |
| Equip1→entries[0] 映射约定 | A: 按 gripTag 名查找 → 必须在运行时匹配字符串。 | GripTable entries 按序排列，隐式映射在 debug 代码中已验证 |
| Director 直接写 OwnedTags | A: 立即实现 GripSwitchEvent 事件通道 → 装备系统未就绪，过早抽象。 | 保留 TODO，装备系统完成后改为 GripSwitchEvent |
| 不添加空值守卫（fail-fast） | A: 加 null check 使缺失事件降级 → 隐藏配置错误。 | 与现有 crouchEvent 等行为一致，NRE 提前暴露问题 |

## Known Issues

- [ ] Equip3 不生效 — Equip3InputEventSO.asset 尚未拖入 EventHub channels + InputService inputEvents（P1 — 用户手动连线后解决）
- [ ] Director 直接写 OwnedTags 是过渡方案 — 装备系统完成后由 GripSwitchEvent 替代（P2 — 依赖 L3_Equipment）

## Cross-References

### Related Sessions
- None — 本轮首次 Character 方向 session

### Related Plans
- [../plans/foamy-wandering-boot.md](../plans/foamy-wandering-boot.md) — 本次改动的实现计划

### Related Tech Docs
- [tech/L2-services/L2-modules/L3-character/L4-actor/character-actor.md](../tech/L2-services/L2-modules/L3-character/L4-actor/character-actor.md) — TODO 注释更新
- [tech/L2-services/L2-modules/L3-character/L4-director/player/player-director.md](../tech/L2-services/L2-modules/L3-character/L4-director/player/player-director.md) — ProcessEquipInput / ProcessSkillInput

### Related Design Docs
- None — internal refactor, no design-facing changes.

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactor. Debug keys replaced with Equip events, player-visible behavior unchanged.
