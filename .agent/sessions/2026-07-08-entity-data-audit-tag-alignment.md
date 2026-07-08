# 2026-07-08 — Entity Data Audit + Tag Alignment + Test Data

## Background

v0.41.0 完成了 Entity 分类四层架构对齐，Entities 数据目录按 L3 模块拆分为 6 个子目录（Ammo/Building/Character/Consumable/Equipment/SceneItem）。拆分后 `*_all.json` 文件未经过逐模块审核，存在 Tag 路径不一致（Building 缺 `Entity.` 前缀、Material/Attachment 未建 Tag）和空模块缺测试数据两个问题。本次逐文件审核并修复。

## Changes

### Tag 树补全 (tags_all.json)
- +7 新 Tag，均以 `Entity.` 为根前缀：
  - `Entity.Building.Defense` / `Entity.Building.Defense.Wall` — 防御建筑 → 墙壁
  - `Entity.Building.Crafting` / `Entity.Building.Crafting.Station` — 制作建筑 → 工作台
  - `Entity.Consumable.Material.Wood` — 木材材料
  - `Entity.Equipment.Attachment` / `Entity.Equipment.Attachment.Scope` — 武器配件 → 瞄准镜

### Building Tag 路径修正 (building_all.json)
- WoodenWall Tags: `Building.Defense.Wall` → `Entity.Building.Defense.Wall`, `Material.Wood` → `Entity.Consumable.Material.Wood`
- Workbench Tags: `Building.Crafting.Station` → `Entity.Building.Crafting.Station`, `Material.Wood` → `Entity.Consumable.Material.Wood`

### Equipment 修正 (equipment_all.json)
- description: "Weapon entities … (category updated to Equipment)" → "Equipment entities …"
- Pistol Slots/Scope AcceptTags: `Attachment.Scope` → `Entity.Equipment.Attachment.Scope`

### 测试数据补全（3 个空模块 → 9 条实体）
- Ammo (+3): PistolAmmo, RifleAmmo, ShotgunShell — 全量弹药模板覆盖
- Consumable (+4): CannedFood(Food), Bandage(Medical), WoodLog(Material), WheatSeed(Seed) — 双 entityType 各 2 条
- SceneItem (+2): Barrel(Environment), WoodenCrate(Entity) — 基础场景物品

### Unity 自动同步 (.asset)
- WoodenWall.asset / Workbench.asset: OverridesJson 字段随 JSON 同步更新
- NewBuildingDef.asset + .meta: 因 templateName=null 被 Unity 移除
- Human.asset: templateId 废弃字段清理
- Boot.asset: Addressables 组自动更新

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Tag 路径统一 `Entity.` 前缀 | 保持 Building 用独立根域 `Building.Defense.Wall` | Equipment 已用 `Entity.Equipment.Weapon.Melee.Blade`，Building 应一致 |
| Material.Wood 放 `Entity.Consumable.Material.Wood` | 创建独立 `Material` 根级 Tag 域 | Material 是 Consumable 的子类型，Tag 域应与 SO 层级一致 |
| Attachment.Scope 放 `Entity.Equipment.Attachment.Scope` | 创建独立 `Attachment` 根级 Tag 域 | Scope 是武器配件，隶属 Equipment 域 |
| 空模块每条用基础默认数据（无 overrides） | 添加富 overrides（Tags/属性覆写） | 测试阶段先验证模板解析链路，具体覆写后续按需添加 |

## Known Issues

- [ ] NewBuildingDef（Building #1）templateName 为 null，.asset 已自动删除 — P2 — 后续需指定模板或从 JSON 移除
- [ ] Attachment Tag 域仅有 Scope 一个子 Tag — P2 — 未来配件（Silencer/Magazine/Bipod）需补充
- [ ] 新增 9 条实体的 .asset 尚未通过 Import 生成（仅 JSON 存在） — P2 — 需在 Editor 中执行 Import

## Cross-References

### Related Sessions
- [2026-07-08-entity-classification-four-layer-alignment.md](2026-07-08-entity-classification-four-layer-alignment.md) — v0.41.0 Entity 分类四层架构对齐，本次审核的基线

### Related Tech Docs
- [../tech/L1-core/gameplay-tag-entity.md](../tech/L1-core/gameplay-tag-entity.md) — Entity 系统 Tag 域文档，本次新增 7 个 Tag
- [../tech/L2-services/L2-modules/L3-equipment/README.md](../tech/L2-services/L2-modules/L3-equipment/README.md)
- [../tech/L2-services/L2-modules/L3-ammo/README.md](../tech/L2-services/L2-modules/L3-ammo/README.md)
- [../tech/L2-services/L2-modules/L3-consumable/README.md](../tech/L2-services/L2-modules/L3-consumable/README.md)
- [../tech/L2-services/L2-modules/L3-building/README.md](../tech/L2-services/L2-modules/L3-building/README.md)
- [../tech/L2-services/L2-modules/L3-sceneitem/README.md](../tech/L2-services/L2-modules/L3-sceneitem/README.md)

### Flag for Design Doc Creation
- [x] No design doc needed — data audit + tag alignment + test data, no design-facing changes.
