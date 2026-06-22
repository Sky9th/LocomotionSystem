# 2026-06-22 — TraversalAnimationSetSO 并入 + Zombie 动画配置落地

## Background

`TraversalAnimationSetSO` 是一个独立的 SO 类型，仅包含 6 个攀爬/落地 ClipTransition 字段。`TraversalDriver`
目前还是 TODO stub，无运行时消费者。将其合并到 `LocomotionAnimationSetSO` 可减少 SO 类型，策划少拖一个资产。

同时，Zombie 角色动画需要配置落地。从 PROTOFACTOR 源整理 33 个 Zombie FBX 动画文件，按 Human 模式
推导 3 个不同风格（TypeA/TypeB/TypeE）的 JSON 动画配置。

## Changes

### TraversalAnimationSetSO 合并
- `LocomotionAnimationSetSO` — 新增 6 个 traversal 字段（climbUpHalfMeter/1m/2m、climbDown1m/2m、landFromWall）
- `TraversalAnimationSetSO.cs` — **删除**，字段迁入 LocomotionAnimationSetSO
- `CharacterAnimationProfileSO` — 删除 `traversalSet` 字段
- `CharacterBuildContext` — `TraversalSet` 改返回 `ResolvedLocoAnimSet`
- `TraversalDriver` — TODO 注释更新为新类名

### AnimationImportExport 修复
- `LocomotionSetEntry` — 新增 6 个 traversal `ClipTransitionEntry` 字段
- `ExportLocomotionSet` / `ApplyLocomotionSet` — traversal 字段导出/导入
- `EnsureTransitionsInstantiated` — traversal 字段 null-coalesce（新老 SO 兼容）
- `GripEntryItem` — 新增 `combatSet` 字段（修复预存缺陷：Human Combat 动画集导入导出时丢失）
- `ExportGripTable` / `ApplyGripTable` — combatSet 导出/导入
- 删除 `TraversalSetEntry` 类、`ExportTraversalSet`、`ImportTraversalSet`、`ApplyTraversalSet` 方法及所有 traversal 引用

### 动画数据目录重构
- Human 数据迁移至 `Human/Male/` 子目录（为多 Style 铺路：`Human/Male/`、`Human/Female/`）
- Zombie 按 TypeA/TypeB/TypeE 分目录存储，各自独立 Config/GripTable/LocomotionSets
- `Human.json` — 所有路径从 `Human/` 改为 `Human/Male/`
- 新增 `Zombie_TypeA.json`、`Zombie_TypeB.json`、`Zombie_TypeE.json` — 各含 Relax+Combat 两套 locomotionSet

### PROTOFACTOR FBX 重组
- Human FBX 从根层级 `1H_Blade/`、`1H_Sidearm/` 移入 `Human/` 子目录
- Zombie FBX 导入 `PROTOFACTOR/Zombie/`，按 Unarmed/Locomotion/Combat+Relax/Attack/Reaction/Special 组织
- External/Assets/Zombie/ 同步建立相同结构（33 FBX + 33 meta）

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Traversal 合并到 LocomotionSet 而非保留独立 SO | A: 保留 TraversalAnimationSetSO 不动 → 多一个 SO 类型，策划多配一个资产。B: 合并到 CharacterAnimationProfileSO → Profile 字段过长。 | traversal 本质是 locomotion 的特殊状态（攀爬），放在同一 SO 内语义合理，且 TraversalDriver 是 TODO 无消费者 |
| `combatSet` 在 GripEntryItem 中以可选字段出现 | A: 改造 GripTable 的 JSON 结构（加数组、改 key）→ 破坏 Human.json 兼容性 | 可选 string 字段向后兼容，Human.json 不加此字段时行为不变 |
| Zombie 用 3 个独立 JSON 而非 1 个多 profile JSON | A: 单 JSON 含 3 个 profile → `AnimationExportFile.gripTable` 是单数，不支持多表 | 当前导入器设计就是 1 文件 = 1 角色类型，3 个文件是最小改动 |
| AnimSet 继承（Style 维度）方案讨论后搁置 | A: StyleTable 独立覆盖 → 引入新 SO 类型。B: AnimSet 继承链 → 需要 parent 穿透。C: Tag 平表 → 等分 fallback 歧义 | 当前阶段"暴力导入"（独立 JSON + 独立目录）足够，后续通过导入导出批量处理 |

## Known Issues

- [ ] `crawlAnimNativeSpeed` 在 SO 中有字段，但 LocomotionSetEntry 和 JSON 中未序列化（预存缺陷，暂不修复）
- [ ] `walkMixer` 1 个前向 clip 的配置（Zombie 非方向性移动）未经运行时验证
- [ ] Zombie 缺少 Attack/Reaction/Special 动画的 SO 和 Driver——当前仅 Locomotion 域配置完成
- [ ] Human Combat 的 GripTable `combatSet` 历史数据可能已丢失——需重新导入 Human.json 验证

## Cross-References

### Related Sessions
- [2026-06-20-animation-import-refactor.md](2026-06-20-animation-import-refactor.md) — AnimationImportExport 重构基础

### Related Tech Docs
- [tech/.../animation-style-table.md](../tech/L2-services/L2-modules/L3-character/L4-animation/config/animation-style-table.md) — 本次创建的 Style 维度分析文档（已回滚删除）
- [tech/.../locomotion-animation-profile.md](../tech/L2-services/L2-modules/L3-character/L4-animation/config/locomotion-animation-profile.md) — LocomotionAnimationSetSO 关联文档

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactor + data reorganization, no design-facing changes.
