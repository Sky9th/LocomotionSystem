# 2026-07-10 — Entity contentId P0 基础设施落地

## Background

Phase 5 物品经济正在建设中。Mod 架构要求每个资产有稳定的、人类可读的 `contentId`，不随 Unity asset 重命名而断裂。此前 `AssetCatalog` 以 `ScriptableObject.name` 为 key，无 Mod 可寻址 ID 体系。

这是短期计划中唯一的 P0——Mod 架构补课。和 Phase 5 并行推进，不阻塞物品经济开发。

## Changes

### PropertyTree 扩展
- `properties_all.json` — 新增 `Id` (String) + `Category` (RdTag) PropertyDef
- Entity 树 Common 节点下追加 `Id` + `Category` 子节点，继承传播到全部 25+ 子 Tree
- `PropertyTable.GetRdTag()` — RdTag 语义读取薄封装（底层走 `_strings` 存储）

### contentId 系统
- `ContentIdUtility.cs` (新) — `BuildContentId(prefix, category, id)` / `GetContentId(props)`
- `OfficialPrefix = "rd."` 为默认值，Mod 可传自定义前缀

### EntityEditorWindow
- `DrawBasicSection()` — Content Id 只读预览（`EditorGUI.BeginDisabledGroup(true)`）
- `Save()` — 首次保存时 Id 未设 → 从 asset name 自动推导 snake_case（跳过 "New*" 默认名）
- `AssetNameToSnakeCase()` — 辅助方法

### 性能优化
- `GetTemplateCache()` — `ResolvePresetSOs` 的 5s 静态缓存，消除每帧 `AssetDatabase.FindAssets`
- `_propertyGroups` / `_propertyGroupOrder` — SelectPreset 时预计算，DrawPropertyOverrides 直接读缓存
- `TagTreeModel.GetCached()` (新) — TagPicker 打开时不再重复扫描 AssetDatabase

### 数据迁移（49 件 P5.0 物品）
- 3 个 entity JSON 文件全量追加 `Common/Category` + `Common/Id`
- 3 个 Python gen 脚本同步更新
- Unity Editor reimport → 49 个 .asset 获得 Category + Id

### 弹药 Tag 迁移
- `tags_all.json` — 新增 `Entity.Ammo.Caliber` + 4 口径子标签 + `Blunt`
- `ammo_all.json` / `equipment_all.json` — Tags `Ammo.Caliber.*` → `Entity.Ammo.Caliber.*`
- TagPicker `Entity.*` 域约束现在覆盖全部实体类型

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| contentId 走 PropertyTree，不加 C# 字段 | A: PropertyPresetSO 加 `public string contentId;` → 破坏数据驱动模式，15 个子类需要逐个适配 | PropertyTree 是已有数据通路，OverridesJson 透传，Mod 天然可覆写 |
| Category 独立于 Tags | A: Category 从 Common/Tags 自动推导 → Tags 是杂项标签桶（含 Grip、Material 等非分类标签），不适合做 contentId 来源 | Category 是纯粹分类字段，Tags 是行为标签桶。各管各的 |
| contentId 编辑器永远只读 | A: 允许手动编辑 → 物品引用彻底断链风险 | 开发期 reimport 更新，发布后冻结。不给人改的机会 |
| `rd.` 前缀不存资产 | A: 硬编码进 editor 预览 `rd.` 前缀 → 前缀是运行时引擎按来源动态追加的，不应写入资产 | 资产只存 Category + Id，前缀由引擎在 contentId 构建时追加 |
| 5s TTL 缓存（模板 + TagTree） | A: AssetPostprocessor 监听变更即时失效 → 过度设计 | Editor 工具中 5s 足够实用，代码简单 |

## Known Issues

- [ ] `Common/Id` 分类内唯一性未强制校验 — P1 AssetCatalog 索引时做 (P2)
- [ ] `AssetNameToSnakeCase` 对缩写词（如 MP5、AK47）转换不完美，但手动 Id 覆盖即可
- [ ] ContentIdUtility 尚未接入任何运行时管线 — P1 接入 AssetCatalog 查找

## Cross-References

### Related Sessions
- [2026-07-09-mod-community-strategy.md](2026-07-09-mod-community-strategy.md) — Mod 架构框架制定，contentId 需求的来源

### Related Plans
- [../plans/short-term.md](../plans/short-term.md) — P0: PropertyPresetSO 加 contentId 字段（最终方案偏离原计划，走 PropertyTree 而非 C# 字段）
- [../plans/p0-lucky-salamander.md](../../.claude/plans/p0-lucky-salamander.md) — P0 实施计划（已批准并完成）

### Related Tech Docs
- ContentIdUtility + PropertyTable.GetRdTag — 待 rd-tech-doc 归档

### Related Design Docs
- 无 — 这是 Mod 架构基础设施，不改变玩家可见行为。

### Flag for Design Doc Creation
- [x] No design doc needed — internal infrastructure, no design-facing changes.
