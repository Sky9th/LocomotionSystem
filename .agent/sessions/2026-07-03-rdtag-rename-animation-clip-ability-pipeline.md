# 2026-07-03 — RdTag 重命名 + AnimationClip 直引 + Ability 数据管线就位

## Background

承接上午的 EquipSocket Soft Wave 和 CharacterConst 全局收敛。下午继续推进 Ability 系统——补全 Tag 资产层级，将 StringAsset 动画引用改为 AnimationClip 直引，并修正 rTag→RdTag 命名统一。

## Changes

### rTag → RdTag 全量重命名
- 17 文件，40+ 处：类名、文件名、FindAssets 过滤、CreateAssetMenu、注释、UI 标签
- 删除 `L1_Core/rTag/` 整棵树（9 文件），重建为 `L1_Core/RdTag/`

### StringAsset → AnimationClip
- `AbilityActivationSO.animationAsset` → `animationClip`（AnimationClip）
- `AnimationRequest` 移除 Alias / HasAlias，`AbilityDriver` 移除 TryPlay 分支
- `ActivationImportExport` 新增 `ClipToJson`/`ClipFromJson`（`{GUID}|{name}` 格式）

### Ability Tag 层级补全（+18 新标签）
- Cost 域 7 个、Impact 4 个、Damage.Explosive、Blade 技能 5 个、Pistol2H.SuppressiveFire

### Ability 数据管线
- 新增 6 个 Activation（AttackB/D/2HitCombo/3HitComboA/B/DrawWeapon）
- 新增 5 个 Blade Ability（SlashB/Thrust/Combo2A/Combo3A/Combo3B）
- Sword tree 从 2→7 nodes
- Effect JSON: Impact/Cost effectTag 修正为 `Ability.*` 前缀

### Code Review 修复（6 项）
- ActivationEditorWindow: ObjectField 同步改名
- ClipFromJson: `|` → 固定 32 位 GUID + 3 个 Debug.LogWarning
- BuildTagLookup 去重 → `RdTagLookup.Build()`
- `_fbxClipCache` 缓存
- float 精度四舍五入
- Debug.Log `#if UNITY_EDITOR` 守卫

## Decisions

| 决策 | 替代方案 | 理由 |
|------|---------|------|
| AnimationClip 直引不用 StringAsset | 保留 TransitionLibrary | 当前不需要动画别名系统，直接引用更简单 |
| `{FBX_GUID}|{ClipName}` 格式 | 存完整路径、JSON 嵌套对象 | 简洁、32 位 GUID 固定长度避免分隔符歧义 |
| RdTagLookup 放 `RedDust.Core` | 放在 Ability Editor 目录 | 两边（Ability + Character Animation）都需要，Core 命名空间已有 import |
| 全量 rTag→RdTag | 分批 | 全局替换，一次彻底，避免新旧混用 |

## Known Issues

- **animationClip 零运行时消费者** — 管线只用 timer，还没接 AnimationRequest。延后处理
- **已有 .asset 数据丢失** — StringAsset→AnimationClip 类型和字段名都变了，旧序列化数据无法迁移，需 JSON 重新导入
- **Activation_Instant_Firearm / SuppressiveFire** — animationClip 为 null，等后续补枪械动画

## Cross-References

- Plan: [effect-tag-fix.md](../plans/effect-tag-fix.md)
- Tech: [gameplay-tag-ability.md](../tech/L1-core/gameplay-tag-ability.md)
- Tech: [ability-activation-assets.md](../tech/L2-services/L2-modules/L3-ability/ability-activation-assets.md)
- Version: [v0.34.0](../versions/v0.34.0.md)
- Session (上午): [2026-07-03-weapon-attach-point-character-const.md](2026-07-03-weapon-attach-point-character-const.md)

### Flag for Design Doc Creation
- [x] No design doc needed — infrastructure migrations, no player-visible behavior changes.
