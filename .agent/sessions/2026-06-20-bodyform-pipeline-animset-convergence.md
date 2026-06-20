# 2026-06-20 — BodyForm 管道 + AnimSet 收敛 + 枚举重组

## Background

角色姿态数据分散在三处：`EPosture`/`EMovementGait` 在 Discrete 管道，`Equip.Grip.*` 在 OwnedTags 走旁路，BodyForm（战备/放松）不存在。AnimSet 解析在 `CharacterActor` 和 `LocomotionDriver` 两处重复调用 `GripTable.Resolve()`。

如果要加 CombatStance，直接塞 Tag 或塞 Discrete 都是在延续混乱。本次 session 的核心目标：先设计 Body 姿态的完整 Tag 树 + 对应枚举体系，再收敛 AnimSet 解析到单一数据源，最后接入 BodyForm 管道前段（Director → Intent → BuildContext）。后半段（Tag 派生、GripTable combatSet 联动消费）延后。

## Changes

### Body Tag 体系设计
- `gameplay-tag.md` 新增 Body 章节：`Body.Form.*`（Relax/Combat）、`Body.Posture.*`（Standing/Crouching/Prone）、`Body.Locomotion.*`（Idle/Walk/Run/Sprint），13 标签
- `tags_all.json` 写入 Body 标签，待 Unity 导入
- 设计原则确定：Tag 是枚举的单向派生，由 `CharacterActor.RefreshBodyTags()` 全量刷新保证互斥

### 枚举拆分
- `LocomotionEnums.cs` 删除，拆为 `Enums/` 目录下四个独立文件：
  - `ELocomotionPhase.cs`、`EPosture.cs`、`EMovementGait.cs`、`EBodyForm.cs`
- `EBodyForm`：新枚举 `Relax = 0, Combat = 1`

### 角色姿态管道
- `SCharacterIntent`：新增 `DesiredBodyForm` 字段 + 构造参数
- `PlayerDirector`：新增 `ProcessDebugCombatToggle()`（按键 4→Combat，5→Relax）、`ResolveBodyForm()`
- `CharacterActor.Update()`：缓存 `buildCtx.BodyForm = intent.DesiredBodyForm`
- `CharacterBuildContext`：新增 `ResolvedLocoAnimSet` 和 `BodyForm` 属性

### AnimSet 收敛
- `GripTable.Resolve()` 签名改为 `Resolve(GameplayTagContainer, EBodyForm)`，内部根据 `bodyForm == Combat` 选 combatSet
- `GripAnimationEntry`：新增可选 `combatSet` 字段
- `CharacterActor` 成为唯一解析点，存入 `buildCtx.ResolvedLocoAnimSet`
- `LocomotionDriver.Evaluate()`：从 `buildCtx.ResolvedLocoAnimSet` 读，不再调用 `GripTable.Resolve()`
- `ILocomotionSimulator.Simulate()` / `GroundLocomotion.Simulate()`：删 `animSet` 参数，从 `buildCtx` 读

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| BodyForm 用枚举不进 Discrete | A: 放进 Discrete 跟 Posture 同路径 → 但 BodyForm 不是 FSM 热路径消费方，白占字段。B: 纯 Tag → 内部链路不一致 | BuildContext 是正确位置：Director 写，下游只读，Tag 派生时一并处理 |
| GripTable.Resolve 收 enum 而非查 Tag | A: 内部查 `HasTag("Body.Form.Combat")` → 链路不统一 | Character 内部统一走枚举，Tag 留给外部系统查询 |
| Enum 拆分为独立文件 | A: 保持一个大文件 → 当前已 3 枚举，再加会混。B: 放 Config/ 目录 → Config 是 SO 资产目录 | 放模块根级 `Enums/`，全模块引用 |
| AnimSet 收敛到 BuildContext | A: 放 CharacterFrameContext → AnimSet 不是帧级瞬态，放 BuildContext 语义正确 | BuildContext 已有 GripTable/DefaultSet，Resolved 是它们的计算结果 |
| 生产环境 BodyForm 应由 Director 响应操作 | A: CharacterActor 每帧从 intent 拿 → 当前是过渡方式 | TODO 标注清楚：事件驱动后 Director/装备系统直接写 BuildContext，Actor 不参与 |

## Known Issues

- [ ] `GripTable_Human.asset` 中 combatSet 字段全为 null——需 Unity Inspector 拖入 Combat 资产（P1）
- [ ] `RefreshBodyTags()` 未实现——Tag 统一 session 再做（P1）
- [ ] NpcDirector 未处理 `DesiredBodyForm`，使用默认值 Relax（P2）
- [ ] BodyForm 无外部消费方：AI/UI/Audio 暂未接入（P2）
- [ ] 13 个 Body Tag .asset 文件未创建——需 Unity 导入工具执行（P1）
- [x] AnimSet 重复解析已消除——仅 `CharacterActor` 一处

## Cross-References

### Related Sessions
- [2026-06-20-grip-switching-animation-reorg.md](2026-06-20-grip-switching-animation-reorg.md) — Grip 切换链路和动画资产重组，本次延续动画管线整理

### Related Plans
- [../plans/relax-combat-composed-ullman.md](../plans/relax-combat-composed-ullman.md) — Body 姿态管线实施计划

### Related Tech Docs
- [../tech/L1-core/gameplay-tag.md](../tech/L1-core/gameplay-tag.md) — 新增 Body 章节，设计原则新增"Body Tag 是枚举派生"
- `L3_Character/Enums/` — 新目录，四个枚举文件

### Flag for Design Doc Creation
- [x] No design doc needed — BodyForm 管线是内部架构整理，无玩家可见行为变化。Body Tag 树设计已记录在 gameplay-tag.md 中。
