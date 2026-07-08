# 2026-07-08 — Entity 资产目录重组

## Background

Entity 编辑器架构拆分为 5 大模块（Character/Weapon/Prop/SceneItem/Building）已在前几个 session 完成，但 `Assets/Data/Entities/` 资产目录仍沿用旧结构。`Equipment/` 目录混合了 Weapon（L3_Weapon）和 Prop（L3_Prop）两个模块的资产，`SceneItems/` 目录缺失，`Backpack.asset` 仍使用已废弃的 `ItemDefSO` 类型。目录与代码架构错位，阻碍后续大量资产创建和迭代。

## Changes

### 资产目录重组
- 新增 `Weapons/Melee/`、`Weapons/Ranged/` — 承接 Blade、Pistol
- 新增 `Props/Armor/`、`Props/Consumable/`、`Props/Ammo/`、`Props/Tool/`、`Props/Container/`、`Props/Material/` — 6 子目录一一对应 6 种 PropDefSO 子类
- 新增 `SceneItems/` — 就绪，暂无资产
- 删除 `Equipment/` 目录（含 .meta）

### 资产迁移
- `Blade.asset`（MeleeWeaponSO）: Equipment/ → Weapons/Melee/
- `Pistol.asset`（RangedWeaponSO）: Equipment/ → Weapons/Ranged/
- `Backpack.asset`: Equipment/ → Props/Container/，同时 Script GUID 从废弃 `ItemDefSO` 切换为 `ContainerSO`

### 代码
- `EntityEditorWindow` 新增 `GetAssetDirForType(Type)` 虚方法（默认回退到 `GetDefaultAssetDir()`），`CreateAsset` 改用此方法按类型路由到子目录
- `WeaponEditorWindow` 覆写路由：MeleeWeaponSO → Weapons/Melee，RangedWeaponSO → Weapons/Ranged
- `PropEditorWindow` 覆写路由：6 种子类各映射到 Props 对应子目录

### 清理
- 删除 `Scripts/Shared/Editor/Entity/` 空目录

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 目录组织以具体 SO 类型为叶子节点（如 Weapons/Melee, Weapons/Ranged） | A: 保持 Equipment/ 并在其下建 Weapon/Prop 子目录 → Equipment 不是 L3 模块概念，导致层级混乱。B: 所有资产平铺在 Entities/ 根目录 → 无法区分类型，量大时不可维护。 | 每个叶子目录对一个 C# SO 类型，目录树 = 类型树的投影，清晰无歧义 |
| Equipment 概念保留在 PropertyTree 中但不映射为文件目录 | A: 同步建 `Entities/Equipment/` → PropertyTree 的继承链是属性结构维度，Entity 资产目录是类型维度，不应耦合 | 内部重构，行为不变 |
| `GetAssetDirForType` 作为 `EntityEditorWindow` 基类虚方法 | A: 直接在子类覆写 `CreateAsset` → 需要复制目录验证逻辑，代码重复。B: 改 `GetDefaultAssetDir` 签名为 `GetDefaultAssetDir(Type)` → 破坏接口，现有 CharacterEditorWindow 等需要无效参数 | 最小改动，子类仅覆盖路由表，目录创建逻辑仍由基类统一处理 |
| Backpack 从 ItemDefSO 原地改为 ContainerSO | A: 删掉旧文件重建新 SO → 丢失 OverridesJson 数据 | ContainerSO 是空壳类，所有数据在 PropertyPresetSO 字段中，改 GUID 即可无缝迁移 |

### 设计原则
- **目录 = L3 模块投影**：`Entities/` 下每个目录对应一个 `PropertyPresetSO` 的具体子类
- **PropertyTree 继承链 vs 资产目录是正交维度**：Trees/ 组织属性结构，Entities/ 组织类型实例

## Known Issues

- [ ] `Props/Armor/`、`Props/Consumable/`、`Props/Ammo/`、`Props/Tool/`、`Props/Material/`、`SceneItems/` 均为空目录 — 暂无对应资产（P2 — 后续按需创建）
- [ ] `items_all.json` 仍引用旧路径 — 需要重新导出（P1 — 等待 Editor 在 Unity 中验证后再导出）
- [ ] 新增目录的 `.meta` 文件由 Bash `mkdir` 生成，非 Unity 创建的标准 `.meta` — Unity 会在 Refresh 时自动修正（P3 — 无影响）

## Cross-References

### Related Sessions
- [2026-07-08-entity-editor-architecture-refactor.md](2026-07-08-entity-editor-architecture-refactor.md) — L3_Item 拆分为 5 大 Entity 模块 + EntityEditor 基类架构，本次是其资产目录侧的延续
- [2026-07-08-template-preset-dropdown.md](2026-07-08-template-preset-dropdown.md) — Template 字段改为预设下拉列表，通过 PropertyTree assetName 查找

### Related Tech Docs
- [tech/L2-services/L2-entity-service/entity-service.md](../tech/L2-services/L2-entity-service/entity-service.md) — EntityService 模块概述，需更新 Editor 部分
- [tech/L2-services/L2-modules/L3-weapon/README.md](../tech/L2-services/L2-modules/L3-weapon/README.md) — Weapon 模块，Editor 节需更新
- [tech/L2-services/L2-modules/L3-prop/README.md](../tech/L2-services/L2-modules/L3-prop/README.md) — Prop 模块，Editor 节需更新

### Related Design Docs
- None — 内部重构，无设计面变更。

### Flag for Design Doc Creation
- [x] No design doc needed — internal directory restructure, no player-facing behavior changes.
