# 2026-06-28 — Slots PropertyTree 改造 + Entity→CharacterActor 数据管线

## Background

`Common/Slots` 是一个 `SlotDef[]` Struct 数组，全部塞在 OverridesJson 里——9 个槽位打包成一个 JSON 黑盒。编辑不可见、继承全量覆写、每个槽位无法独立配置 AcceptTags 默认值。同时 CharacterActor 自己从 `propertyPreset` 创建 PropertyTable，与 EntityService 的 Entity.Properties 是两份独立实例，GO 状态修改不可见。

本 session 将 Slots 从数组改为 PropertyTree 原生节点，每个槽位独立 PropertyDefSO；将 CharacterActor 的 Properties 来源统一为 Entity（通过 Identity 中转），形成 L2→L3 单向数据流。

## Changes

### Slots PropertyTree
- `properties_all.json`: 删除 `Common/Slots` Struct 数组定义，改为 14 个独立 SlotDef PropertyDefSO（RightHand/LeftHand/Head/Chest/Leg×2/Foot×2/Back/Pouch/Scope/Magazine/Muzzle + fallback Slots）
- Entity 树: `Common/Slots` 叶节点 → `Slots/` 根级空文件夹
- Human 树: +9 身体槽位节点，各引专属 defId（Body 改为 Chest 以避 Human 树 Body 文件夹冲突）
- BodyArmor 树: +3 Pouch 槽位 (defId: Pouch)
- Firearm 树: +3 附件槽位 (Scope/Magazine/Muzzle，各引专属 defId)
- 新增 4 棵近战武器子树: Blade (+BleedChance)、Axe (+ArmorPierce)、Blunt、Spear ← MeleeWeapon
- SlotDef: `AcceptTags` 类型 `GameplayTagDefinitionSO[]` → `string[]`（兼容 JSON 序列化）

### Entity→CharacterActor 数据管线
- EntityService.Spawn: 确保 GO 上有 Identity（`GetComponent + AddComponent` 兜底），调用 `identity.SetProperties(entity.Properties)` push
- Identity: +`Properties` 属性 + `SetProperties()` 方法
- CharacterActor: 删 `propertyPreset` 字段，删 `Properties` 属性，Awake 通过 `ResolveComponents` 取 Identity 传入 buildCtx
- CharacterBuildContext: +`Identity` 属性，`Properties` 改为 `Identity?.Properties` 转发
- PropertyTable: +`GetChildren(parentPath)` 文件夹遍历
- CharacterContainer.OnWire: 用 `GetChildren("Slots")` + `GetStruct<SlotDef>` 替代 `GetStructArray`
- CharacterDefSO: 清理 `ISerializationCallbackReceiver`、`StandardBodySlots`、`BuildOverridesJson`
- UIService: `_playerActor.Properties` → `_playerActor.BuildContext.Properties`

### 动画兜底
- BaseAirLoopState / BaseAirLandState: 空动画时跳过播放，打 Warning，直接跳 Idle（避免 FSM 卡死）

### 工程
- CreateAssetMenu: CharacterDefSO / ItemDefSO 归拢至 `RedDust/Entity/`
- PlayerService: +测试 spawn（Zombie + Blade into RightHand）
- `[RequireComponent(typeof(Identity))]` on CharacterActor

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Slots 改为独立 PropertyDefSO | A: 保持 Struct 数组 → 编辑器不可见、继承全量覆写。B: 单独 SlotDef 文件夹 + 子属性 → 多了 27 个树节点。 | 独立 PropertyDefSO 给出每个槽位专属默认 AcceptTags，编辑可见，自然继承 |
| 槽位 Body→Chest NodeId | A: 保持 Body → 和 Human 树 Body 文件夹同名冲突，第二个被 MergeAllNodes 跳过 | Chest 语义更准确（躯干防具），且不冲突 |
| EntityService push Properties via Identity | A: push 到 CharacterActor 直接 → EntityService 需要 `using RedDust.Character`（L2→L3 反向依赖）。B: CharacterActor pull from GameContext → 违反"不反向查询"原则 | Identity 是 L3 内部组件，EntityService 已经认识，L2→L3 通过 L3 层内中转 |
| CharacterActor Properties 改为 buildCtx.Identity?.Properties | A: 保留 Properties 属性 → 冗余缓存。B: 全部走 buildCtx.Properties | Identity 统一入口，Properties 是转发属性，调用方不改 |
| 空 Air/Land 动画跳 Idle | A: fallback 到 idleL 播放 → 姿势正确但无着陆过渡。B: 直接 ForceSetState(Idle) → 跳过着陆 | 直接跳 Idle 最安全，不引入假动画。Warning 提醒补动画 |

## Known Issues

- [ ] Zombie LocomotionSet_TypeA_Relax 缺少 airLight/airHard/landLight/landHard 动画 — AirLoop Tick 落地后直接跳 Idle（P1 — 补动画）
- [ ] Zombie LocomotionSet_TypeA_Relax runMixer._Animations 为空 — HasFullLocomotion=false (P2 — 补 run 动画)
- [ ] ItemDefSO assets 尚未创建 — Blade/Pistol def 仍为空 (P1 — 创建资产)
- [ ] PlayerService 测试代码需在资产就位后清理 (P2)
- [ ] PropertyTreeSO .asset 文件需从 properties_all.json 重新导入

## Cross-References

### Related Sessions
- [2026-06-27-container-character-slots.md](2026-06-27-container-character-slots.md) — Container 系统落地 + CharacterContainer 初版
- [2026-06-26-entity-base-tree.md](2026-06-26-entity-base-tree.md) — Entity 基树 + Common/Slots 上游依赖

### Related Plans
- [../plans/foamy-stargazing-bentley.md](../plans/foamy-stargazing-bentley.md) — 本 session 的实施计划

### Related Tech Docs (待更新)
- tech/L2-services/L2-entity-service/ — EntityService 改动
- tech/L2-services/L2-modules/L3-container/ — SlotDef + Container + ContainerSlot
- tech/L2-services/L2-modules/L3-character/ — CharacterActor + CharacterContainer + CharacterBuildContext
- tech/L2-services/L2-modules/L3-identity/ — Identity 改动
- tech/L2-services/L2-modules/L3-properties/ — PropertyTable.GetChildren
- tech/L2-services/L2-modules/L3-item/ — ItemDefSO CreateAssetMenu

### Flag for Design Doc Creation
- [x] No design doc needed — internal architectural refactoring, no player-facing behavior change.
