# 2026-07-11 — P1 AssetCatalog contentId 地址系统 + 命名空间隔离

## Background

P0 建立了 `Common/Category` + `Common/Id` PropertyTree 节点和 `ContentIdUtility` 拼接逻辑，但 `AssetCatalog` 仍以 `ScriptableObject.name` 为 key 查找资产。P5.1 测试代码被迫硬编码 `"Machete"` / `"M1911"` 等 asset name。Mod 架构要求每个资产有稳定的、人类可读的 `contentId`，P1 完成 AssetCatalog 层的索引切换。

命名空间隔离是 Mod 系统的前置条件——`rd.x` 官方物品，`MyMod.x` Mod 自有物品。调用方传裸 contentId 时程序自动补命名空间，带前缀时精确匹配用于跨命名空间覆写。

## Changes

### AssetCatalog — 名称索引 → contentId 索引
- `_characters` + `_items` 删除，`_byContentId` 统一字典（`Dictionary<string, PropertyPresetSO>`），字段初始化 `= new()` 保证非空
- `InitCharacters` + `InitItems` → `InitPresets` 单入口，`AssetService` 侧 Characters 并入 items 列表
- `FindItem` / `FindCharacter` 两步查找：精确匹配（跨命名空间）→ 自动补 `rd.` 前缀 → Error
- `FindCharacter` 类型不匹配时 `Debug.LogWarning` 而非静默返回 null
- `InitPresets` 中 ContentId 缺失时 `Debug.LogWarning`，不静默跳过

### PropertyPresetSO — _contentId 序列化字段
- 新增 `[SerializeField] private string _contentId` + `public string ContentId` getter + `SetContentId(string id)` setter
- Editor Save（`EntityEditorWindow.Save`）+ JSON Import（`EntityImporter.SyncContentId`）时自动写入
- `SetContentId` 无 `#if UNITY_EDITOR`，Editor + Runtime 统一访问
- 存储完整路径（`Entity.Equipment.Weapon.Melee.Blade.machete`），不含 `rd.` 前缀

### ContentIdUtility — DELETE
- `Common/Id` 已存完整路径，`BuildContentId(category, id)` 拼接逻辑冗余。零引用，全类删除。

### CommonConstants — OfficialNamespace 全局常量
- 新增 `public const string OfficialNamespace = "rd."`，`AssetCatalog` 3 处引用集中到单一定义

### EntityImporter + EntityEditorWindow — 自动化 contentId 同步
- `EntityImporter.ApplyFields` → `SyncContentId` → 从 `OverridesJson` 解析 `Common/Id` → `SetContentId`
- `EntityEditorWindow.Save()` 尾部同步 `_contentId`
- Import + Save 两条路径全覆盖，运行时零 JSON Parse

### PlayerService — contentId 字符串迁移
- `characterDefKey`: `[SerializeField] string` → `const string CharacterDefKey`
- 删除 `zombieDefKey` 序列化字段
- 两处 `FindItem` 硬编码 asset name → 裸 contentId（无 `rd.` 前缀）

### 数据 — 59 实体 JSON 全量补齐
- `Common/Id` 短名 → 完整路径（`machete` → `Entity.Equipment.Weapon.Melee.Blade.machete`）
- 6 个 JSON 全覆盖：Equipment 26 + Consumable 11 + Ammo 12 + Building 4 + Character 4 + SceneItem 2

### Plans 清理
- 删除 6 个过期/已完成计划，2 个移入 design/tech 目录

## Decisions

| Decision | Alternatives | Reason |
|----------|-------------|--------|
| `_byContentId` 字段初始化 `= new()` | A: 在构造函数中初始化 B: 在 InitPresets 中初始化 → Find 方法需 null guard | 结构保证非空，Find 方法不判空——符合 NPE 原则。InitPresets 重新 assign 覆盖旧字典 |
| `SetContentId` 不加 `#if UNITY_EDITOR` | A: Editor-only → Runtime 无法调用，但 Import 等 Editor 代码需要跨越程序集边界 | 更简单、更可见。Editor 调用方不因条件编译而断裂 |
| contentId 存储不含 `rd.` 前缀 | A: 资产直接存 `rd.Entity.xxx` → 资产绑定命名空间，无法跨命名空间复用 | 命名空间是运行时索引层的概念，不是资产属性。Mod 加载时可自由选择命名空间 |
| InitCharacters + InitItems → InitPresets | A: 保留两个独立方法 → 两个字典分别管理 Items 和 Characters | `CharacterDefSO` 继承 `PropertyPresetSO`，同一字典统一管理。类型区分在 FindCharacter 内 `as` 转型 |
| ContentIdUtility 直接删除 | A: 保留并改为薄封装 B: 保留 OfficialPrefix 常量 | 拼接逻辑已冗余，全类无引用。`rd.` 常量移至 `CommonConstants` |
| `Common/Id` 更新为完整路径 | A: 保留短名 + Category 拼接 → 需要 `ContentIdUtility.BuildContentId` | 完整路径消除运行时拼接，一行 `ContentId` getter 替代整个工具类 |

## Known Issues

- [ ] `_contentId` 与 `OverridesJson["Common/Id"]` 无 `OnValidate` 自动同步——Inspector 直接改 OverridesJson 文本区时可能不一致 (P2)
- [ ] Mod 加载未实现，`rd.` 前缀硬编码——Mod SDK 阶段需支持从 Mod Manifest 读取命名空间 (P1)
- [ ] `FindCharacter` 报错日志不再列出可用 keys——调试 contentId 拼写错误时线索减少 (P2)
- [ ] `SetContentId(null)` 静默接受——调用方不应传 null，但 setter 无验证 (P2)
- [ ] PlayerService 注释内的 Zombie/Backpack 测试代码 Key 格式已过期——取消注释时需更新为 contentId 格式
- [ ] Build 未验证——仅在 Editor Play Mode 测试

## Cross-References

### Related Sessions
- [2026-07-11-p5.1-item-spawn.md](2026-07-11-p5.1-item-spawn.md) — P5.1 完成，P1 是其直接后续
- [2026-07-10-entity-contentid-p0.md](2026-07-10-entity-contentid-p0.md) — P0 contentId 基础设施落地，P1 将其接入 AssetCatalog

### Related Plans
- [../plans/short-term.md](../plans/short-term.md) — P1 已完成，P5.2 为下一步

### Related Tech Docs
- AssetCatalog / PropertyPresetSO / EntityImporter — 待 rd-tech-doc 更新
- [../tech/L2-services/L2-entity-service/entity-service.md](../tech/L2-services/L2-entity-service/entity-service.md) — EntityImporter 章节待更新

### Related Design Docs
- [../design/mod-community-decision-record.md](../design/mod-community-decision-record.md) — 命名空间隔离和 contentId 体系的设计来源

### Flag for Design Doc Creation
- [x] No design doc needed — internal infrastructure (contentId addressing + namespace isolation), no player-facing design changes.
